using System.Globalization;
using System.Numerics;
using HarmonyLib;
using Impactful.Framework;
using Impactful.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Impactful;

public sealed class ModEntry : Mod
{
    internal static ModEntry Instance { get; private set; } = null!;

    private readonly PerScreen<ShakeController> controllers = new(() => new ShakeController());
    private readonly PerScreen<HitStopController> hitStops = new(() => new HitStopController());
    private readonly PerScreen<HashSet<Tree>> localTreeFalls = new(() => new HashSet<Tree>());
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
        helper.ConsoleCommands.Add("impactful_test", "Trigger an Impactful camera impulse. Usage: impactful_test [strength]", this.ImpactfulTest);

        new Harmony(this.ModManifest.UniqueID).PatchAll();
    }

    internal void Emit(float strength, Vector2 direction)
    {
        if (!Context.IsWorldReady || !this.Config.EnableScreenShake || this.Config.ShakeStrength <= 0)
            return;

        this.controllers.Value.AddImpulse(strength, direction);
    }

    internal void RequestHitStop(int frames)
    {
        if (!Context.IsWorldReady || Context.IsMultiplayer || !this.Config.HitStop)
            return;

        this.hitStops.Value.Request(frames);
    }

    internal bool TryConsumeHitStopFrame()
    {
        if (!Context.IsWorldReady || Context.IsMultiplayer || !this.Config.HitStop)
        {
            this.hitStops.Value.Clear();
            return false;
        }

        return this.hitStops.Value.TryConsumeFrame();
    }

    internal void MarkLocalTreeFall(Tree tree)
    {
        if (this.Config.Trees)
            this.localTreeFalls.Value.Add(tree);
    }

    internal void TriggerTreeLanding(Tree tree)
    {
        if (!this.localTreeFalls.Value.Remove(tree) || !this.Config.Trees || tree.Location != Game1.currentLocation)
            return;

        var horizontal = tree.shakeLeft.Value ? -0.2f : 0.2f;
        this.Emit(ImpactTuning.TreeFall, Vector2.Normalize(new Vector2(horizontal, 1f)));
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
        var strength = ImpactTuning.GetExplosionStrength(radius, distanceTiles);
        if (strength <= 0)
            return;

        var direction = away.LengthSquared() > 0.001f ? Vector2.Normalize(away) : DirectionFromFacing(player.FacingDirection);
        this.Emit(strength, direction);
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
        if (!this.Config.EnableScreenShake || this.Config.ShakeStrength <= 0 || Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.currentMinigame is not null)
            this.controllers.Value.Clear();
    }

    internal void ApplyPendingCameraImpulse()
    {
        var offset = this.controllers.Value.ConsumeOffset(this.Config.EnableScreenShake, this.Config.ShakeStrength);
        if (offset == Vector2.Zero || Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        Game1.viewport.X += (int)MathF.Round(offset.X, MidpointRounding.AwayFromZero);
        Game1.viewport.Y += (int)MathF.Round(offset.Y, MidpointRounding.AwayFromZero);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.controllers.Value.Clear();
        this.hitStops.Value.Clear();
        this.localTreeFalls.Value.Clear();
    }

    private void ImpactfulTest(string command, string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0 || parsed > ShakeController.HardMaximumPixels)))
        {
            this.Monitor.Log($"Usage: {command} [strength], where strength is greater than zero and no more than {ShakeController.HardMaximumPixels}.", LogLevel.Warn);
            return;
        }

        var strength = args.Length == 1 ? float.Parse(args[0], CultureInfo.InvariantCulture) : ImpactTuning.TestImpulse;
        var direction = Context.IsWorldReady ? DirectionFromFacing(Game1.player.FacingDirection) : new Vector2(0, 1);
        this.Emit(strength, direction);
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
        if (!config.EnableScreenShake || config.ShakeStrength <= 0)
            this.controllers.Value.Clear();
    }
}

internal readonly record struct ExplosionMessage(string LocationName, float TileX, float TileY, int Radius);

internal static class ImpactTuning
{
    // Vanilla's club special moves the viewport by roughly 28 pixels RMS at
    // 100% zoom. Keep routine impacts well below it and reserve that peak for
    // the strongest explosion.
    public const float ArtifactSpot = 2f;
    public const float OrdinaryRockBreak = 3f;
    public const float TestImpulse = 5f;
    public const float LargeRockBreak = 5f;
    public const float PlayerDamage = 12f;
    public const float TreeFall = 10f;
    public const float Parry = 16f;
    public const float CherryBomb = 14f;
    public const float Bomb = 23f;
    public const float MegaBomb = 28f;

    public static float GetExplosionStrength(int radius, float distanceTiles)
    {
        var falloffStart = radius + 1f;
        var maximumDistance = radius + 6f;
        if (distanceTiles >= maximumDistance)
            return 0;

        var peakStrength = radius <= 3 ? CherryBomb : radius <= 5 ? Bomb : MegaBomb;
        if (distanceTiles <= falloffStart)
            return peakStrength;

        return peakStrength * (1f - (distanceTiles - falloffStart) / (maximumDistance - falloffStart));
    }
}
