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
    private readonly PerScreen<CameraShakeRenderer> renderers = new(() => new CameraShakeRenderer());
    private readonly PerScreen<HashSet<Tree>> localTreeFalls = new(() => new HashSet<Tree>());
    private string[] explosionModIds = null!;
    private Harmony harmony = null!;
    internal ModConfig Config { get; private set; } = new();
    internal bool CanShake => this.Config.EnableScreenShake && this.Config.ShakeStrength > 0 && Context.IsWorldReady;

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        this.explosionModIds = new[] { this.ModManifest.UniqueID };
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
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
        helper.Events.Display.RenderingWorld += this.OnRenderingWorld;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.ConsoleCommands.Add("impactful_test", "Trigger an Impactful camera impulse. Usage: impactful_test [strength]", this.ImpactfulTest);
        helper.ConsoleCommands.Add("impactful_status", "Show Impactful's runtime settings and registered gameplay hooks.", this.ImpactfulStatus);

        this.harmony = new Harmony(this.ModManifest.UniqueID);
        // The parameterless overload's assembly detection breaks when JIT-inlined.
        this.harmony.PatchAll(typeof(ModEntry).Assembly);
        this.LogPatchStatus(LogLevel.Trace);
    }

    internal void Emit(float strength, Vector2 direction)
    {
        if (!this.CanShake)
            return;

        this.controllers.Value.AddImpulse(strength, direction);
        this.Monitor.Log($"Queued camera impulse: {strength} pixels, direction={direction}.", LogLevel.Trace);
    }

    internal void TrackTreeFall(Tree tree, bool isLocalTool)
    {
        var inCurrentLocation = tree.Location == Game1.currentLocation;
        this.Monitor.Log($"Tree fall started at {tree.Tile}: localTool={isLocalTool}, currentLocation={inCurrentLocation}, canShake={this.CanShake}, trees={this.Config.Trees}.", LogLevel.Trace);
        if (isLocalTool && this.CanShake && this.Config.Trees && inCurrentLocation)
            this.localTreeFalls.Value.Add(tree);
    }

    internal void ForgetTreeFall(Tree tree)
    {
        if (this.localTreeFalls.Value.Remove(tree))
            this.Monitor.Log($"Discarded tracked tree at {tree.Tile} before a landing callback.", LogLevel.Trace);
    }

    internal void TriggerTreeLanding(Tree tree)
    {
        var tracked = this.localTreeFalls.Value.Remove(tree);
        var inCurrentLocation = tree.Location == Game1.currentLocation;
        this.Monitor.Log($"Tree landed at {tree.Tile}: tracked={tracked}, currentLocation={inCurrentLocation}, canShake={this.CanShake}, trees={this.Config.Trees}.", LogLevel.Trace);
        if (!tracked || !inCurrentLocation || !this.CanShake || !this.Config.Trees)
            return;

        var horizontal = tree.shakeLeft.Value ? -0.2f : 0.2f;
        this.Emit(ImpactTuning.TreeFall, new Vector2(horizontal, 1f));
        this.Monitor.Log($"Queued tree landing impulse: {ImpactTuning.TreeFall} pixels.", LogLevel.Trace);
    }

    internal void NotifyExplosion(StardewValley.GameLocation location, Microsoft.Xna.Framework.Vector2 tileLocation, int radius)
    {
        this.Monitor.Log($"Explosion detected in {location.NameOrUniqueName} at {tileLocation}, radius={radius}.", LogLevel.Trace);
        this.TriggerExplosion(location.NameOrUniqueName, tileLocation.X, tileLocation.Y, radius);
        if (Context.IsMultiplayer && Context.IsMainPlayer)
            this.Helper.Multiplayer.SendMessage(new ExplosionMessage(location.NameOrUniqueName, tileLocation.X, tileLocation.Y, radius), "Explosion", modIDs: this.explosionModIds);
    }

    internal void TriggerExplosion(string locationName, float tileX, float tileY, int radius)
    {
        if (!this.CanShake || !this.Config.Explosions)
        {
            this.Monitor.Log($"Explosion feedback disabled: canShake={this.CanShake}, explosions={this.Config.Explosions}.", LogLevel.Trace);
            return;
        }

        var player = Game1.player;
        if (player.currentLocation?.NameOrUniqueName != locationName)
        {
            this.Monitor.Log($"Explosion outside current location: {locationName}.", LogLevel.Trace);
            return;
        }

        var center = new Microsoft.Xna.Framework.Vector2(tileX * 64f + 32f, tileY * 64f + 32f);
        var playerCenter = player.GetBoundingBox().Center;
        var away = new Vector2(playerCenter.X - center.X, playerCenter.Y - center.Y);
        var distanceTiles = away.Length() / 64f;
        var strength = ImpactTuning.GetExplosionStrength(radius, distanceTiles);
        this.Monitor.Log($"Explosion feedback: distance={distanceTiles:F1} tiles, strength={strength:F1} pixels.", LogLevel.Trace);
        if (strength <= 0)
            return;

        var direction = away.LengthSquared() > 0.001f ? away : DirectionFromFacing(player.FacingDirection);
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
        var enabled = this.CanShake && Game1.activeClickableMenu is null && !Game1.dialogueUp && Game1.currentMinigame is null;
        if (!enabled && this.controllers.Value.IsActive)
            this.Monitor.Log($"Cleared shake: canShake={this.CanShake}, menu={Game1.activeClickableMenu is not null}, dialogue={Game1.dialogueUp}, minigame={Game1.currentMinigame is not null}.", LogLevel.Trace);
        var visible = !enabled || !Game1.options.screenFlash || Game1.flashAlpha <= 0.5f;
        this.controllers.Value.Advance((float)Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds, enabled, this.Config.ShakeStrength, visible);
        if (!this.CanShake || !this.Config.Trees)
            this.localTreeFalls.Value.Clear();
    }

    private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        if (!this.CanShake || Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.currentMinigame is not null)
        {
            this.renderers.Value.Remove();
            this.controllers.Value.Clear();
            return;
        }

        this.renderers.Value.Apply(this.controllers.Value.CurrentOffset);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.renderers.Value.Remove();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;

        this.controllers.Value.Clear();
        this.localTreeFalls.Value.Clear();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.localTreeFalls.Value.Clear();
    }

    private void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e)
    {
        var falls = this.localTreeFalls.Value;
        if (falls.Count == 0)
            return;

        foreach (var removed in e.Removed)
        {
            if (removed.Value is Tree tree)
                this.ForgetTreeFall(tree);
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.renderers.Value.Remove();
        this.renderers.ResetAllScreens();
        this.controllers.ResetAllScreens();
        this.localTreeFalls.ResetAllScreens();
    }

    private void ImpactfulStatus(string command, string[] args)
    {
        this.Monitor.Log($"Runtime: worldReady={Context.IsWorldReady}, screen={Context.ScreenId}, canShake={this.CanShake}, menu={Game1.activeClickableMenu is not null}, dialogue={Game1.dialogueUp}, minigame={Game1.currentMinigame is not null}.", LogLevel.Info);
        this.Monitor.Log($"Settings: strength={this.Config.ShakeStrength}%, mining={this.Config.Mining}, trees={this.Config.Trees}, combat={this.Config.Combat}, playerDamage={this.Config.PlayerDamage}, explosions={this.Config.Explosions}.", LogLevel.Info);
        this.LogPatchStatus(LogLevel.Info);
    }

    private void LogPatchStatus(LogLevel level)
    {
        var methods = this.harmony.GetPatchedMethods()
            .Select(method => $"{method.DeclaringType?.Name}.{method.Name}")
            .OrderBy(name => name)
            .ToArray();
        this.Monitor.Log($"Registered {methods.Length} gameplay hooks: {string.Join(", ", methods)}.", methods.Length == 0 ? LogLevel.Error : level);
    }

    private void ImpactfulTest(string command, string[] args)
    {
        if (args.Length > 1 || (args.Length == 1 && (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0 || parsed > ShakeController.HardMaximumPixels)))
        {
            this.Monitor.Log($"Usage: {command} [strength], where strength is greater than zero and no more than {ShakeController.HardMaximumPixels}.", LogLevel.Warn);
            return;
        }

        if (!this.CanShake)
        {
            this.Monitor.Log("Load a save and enable screen shake with strength above zero before testing.", LogLevel.Warn);
            return;
        }
        if (Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.currentMinigame is not null)
        {
            this.Monitor.Log("Close menus, dialogue, or minigames before testing.", LogLevel.Warn);
            return;
        }

        var strength = args.Length == 1 ? float.Parse(args[0], CultureInfo.InvariantCulture) : ImpactTuning.TestImpulse;
        this.Emit(strength, DirectionFromFacing(Game1.player.FacingDirection));
        this.Monitor.Log($"Queued a {strength}-pixel world-render impulse at {this.Config.ShakeStrength}% strength.", LogLevel.Info);
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
            this.controllers.ResetAllScreens();
        if (!config.EnableScreenShake || config.ShakeStrength <= 0 || !config.Trees)
            this.localTreeFalls.ResetAllScreens();
    }
}

internal readonly record struct ExplosionMessage(string LocationName, float TileX, float TileY, int Radius);

internal static class ImpactTuning
{
    // Strengths are world-render offsets in viewport pixels, before zoom.
    // Keep routine impacts small and reserve the maximum for the strongest
    // explosion.
    public const float TestImpulse = 5f;
    public const float LargeRockBreak = 8f;
    public const float PlayerDamage = 10f;
    public const float TreeFall = 6f;
    public const float Parry = 14f;
    public const float CherryBomb = 12f;
    public const float Bomb = 20f;
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
