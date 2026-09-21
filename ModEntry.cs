using System.Globalization;
using System.Numerics;
using HarmonyLib;
using Impactful.Framework;
using Impactful.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Impactful;

public sealed class ModEntry : Mod
{
    internal static ModEntry Instance { get; private set; } = null!;

    private readonly PerScreen<ShakeController> controllers = new(() => new ShakeController());
    private readonly PerScreen<CameraShakeRenderer> renderers = new(() => new CameraShakeRenderer());
    internal ModConfig Config { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        try
        {
            this.Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Couldn't read config.json; using defaults. {ex.Message}", LogLevel.Warn);
            this.Config = new ModConfig();
        }
        this.Config.Normalize();

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Display.RenderingWorld += this.OnRenderingWorld;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.ConsoleCommands.Add("impact_test", "Trigger an Impactful camera impulse. Usage: impact_test [strength]", this.ImpactTest);

        new Harmony(this.ModManifest.UniqueID).PatchAll();
    }

    internal void Emit(float strength, int durationMilliseconds, Vector2 direction)
    {
        if (!Context.IsWorldReady || !this.Config.EnableScreenShake || this.Config.ShakeStrength <= 0)
            return;

        this.controllers.Value.AddImpulse(strength, durationMilliseconds, direction);
    }

    internal void NotifyExplosion(StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tileLocation, int radius)
    {
        this.TriggerExplosion(location.NameOrUniqueName, tileLocation.X, tileLocation.Y, radius);
        if (Context.IsMainPlayer)
            this.Helper.Multiplayer.SendMessage(new ExplosionMessage(location.NameOrUniqueName, tileLocation.X, tileLocation.Y, radius), "Explosion");
    }

    internal void TriggerExplosion(string locationName, float tileX, float tileY, int radius)
    {
        var player = Game1.player;
        if (!this.Config.Explosions || player.currentLocation?.NameOrUniqueName != locationName)
            return;

        var center = new Microsoft.Xna.Framework.Vector2(tileX * 64f + 32f, tileY * 64f + 32f);
        var playerCenter = new Microsoft.Xna.Framework.Vector2(player.GetBoundingBox().Center.X, player.GetBoundingBox().Center.Y);
        var away = new Vector2(playerCenter.X - center.X, playerCenter.Y - center.Y);
        var distanceTiles = away.Length() / 64f;
        var maximumDistance = radius + 6f;
        if (distanceTiles >= maximumDistance)
            return;

        var direction = away.LengthSquared() > 0.001f ? Vector2.Normalize(away) : DirectionFromFacing(player.FacingDirection);
        var (strength, duration) = radius <= 3
            ? (ImpactTuning.CherryBomb, 130)
            : radius <= 5 ? (ImpactTuning.Bomb, 175) : (ImpactTuning.MegaBomb, 220);
        var falloff = 1f - distanceTiles / maximumDistance;
        this.Emit(strength * falloff * falloff, duration, direction);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        new GenericModConfigMenuIntegration(this.Helper, this.ModManifest, () => this.Config, this.SetConfig, key => this.Helper.Translation.Get(key)).Register();
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == this.ModManifest.UniqueID && e.Type == "Explosion")
        {
            var message = e.ReadAs<ExplosionMessage>();
            this.TriggerExplosion(message.LocationName, message.TileX, message.TileY, message.Radius);
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        this.controllers.Value.Advance((float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds, this.Config.EnableScreenShake, this.Config.ShakeStrength);
    }

    private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        if (!this.Config.EnableScreenShake || this.Config.ShakeStrength <= 0)
        {
            this.renderers.Value.Remove();
            this.controllers.Value.Clear();
            return;
        }

        this.renderers.Value.Apply(this.controllers.Value);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.renderers.Value.Remove();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.renderers.Value.Remove();
        this.controllers.Value.Clear();
    }

    private void ImpactTest(string command, string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0 || parsed > ShakeController.HardMaximumPixels)))
        {
            this.Monitor.Log($"Usage: {command} [strength], where strength is greater than zero and no more than {ShakeController.HardMaximumPixels}.", LogLevel.Warn);
            return;
        }

        var strength = args.Length == 1 ? float.Parse(args[0], CultureInfo.InvariantCulture) : 1.2f;
        var direction = Context.IsWorldReady ? DirectionFromFacing(Game1.player.FacingDirection) : new Vector2(0, 1);
        this.Emit(strength, 95, direction);
    }

    internal static Vector2 DirectionFromFacing(int facingDirection)
    {
        return facingDirection switch
        {
            0 => new Vector2(0, -1),
            1 => new Vector2(1, 0),
            2 => new Vector2(0, 1),
            3 => new Vector2(-1, 0),
            _ => new Vector2(0, 1)
        };
    }

    private void SetConfig(ModConfig config)
    {
        config.Normalize();
        this.Config = config;
    }
}

internal readonly record struct ExplosionMessage(string LocationName, float TileX, float TileY, int Radius);

internal static class ImpactTuning
{
    public const float MiningHit = 0.65f;
    public const float StoneBreak = 1.2f;
    public const float MeleeHit = 0.7f;
    public const float ClubHit = 1.2f;
    public const float PlayerDamage = 1.5f;
    public const float CherryBomb = 1.8f;
    public const float Bomb = 2.6f;
    public const float MegaBomb = 3.4f;
}
