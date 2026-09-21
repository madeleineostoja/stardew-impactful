using StardewModdingAPI;

namespace Impactful.Integrations;

public sealed class GenericModConfigMenuIntegration
{
    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly Func<ModConfig> getConfig;
    private readonly Action<ModConfig> setConfig;
    private readonly Func<string, string> translate;

    public GenericModConfigMenuIntegration(IModHelper helper, IManifest manifest, Func<ModConfig> getConfig, Action<ModConfig> setConfig, Func<string, string> translate)
    {
        this.helper = helper;
        this.manifest = manifest;
        this.getConfig = getConfig;
        this.setConfig = setConfig;
        this.translate = translate;
    }

    public void Register()
    {
        var api = this.helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(this.manifest, this.Reset, this.Save);
        api.AddSectionTitle(this.manifest, () => this.translate("config.general.title"));
        api.AddBoolOption(this.manifest, () => this.getConfig().EnableScreenShake, value => this.getConfig().EnableScreenShake = value, () => this.translate("config.enable.name"), () => this.translate("config.enable.description"));
        api.AddNumberOption(this.manifest, () => this.getConfig().ShakeStrength, value => this.getConfig().ShakeStrength = Math.Clamp(value, 0, 200), () => this.translate("config.strength.name"), () => this.translate("config.strength.description"), 0, 200);
        api.AddBoolOption(this.manifest, () => this.getConfig().HitStop, value => this.getConfig().HitStop = value, () => this.translate("config.hit-stop.name"), () => this.translate("config.hit-stop.description"));
        api.AddSectionTitle(this.manifest, () => this.translate("config.categories.title"));
        this.AddCategory(api, "mining", config => config.Mining, (config, value) => config.Mining = value);
        this.AddCategory(api, "combat", config => config.Combat, (config, value) => config.Combat = value);
        this.AddCategory(api, "damage", config => config.PlayerDamage, (config, value) => config.PlayerDamage = value);
        this.AddCategory(api, "explosions", config => config.Explosions, (config, value) => config.Explosions = value);
        this.AddCategory(api, "trees", config => config.Trees, (config, value) => config.Trees = value);
    }

    private void AddCategory(IGenericModConfigMenuApi api, string key, Func<ModConfig, bool> get, Action<ModConfig, bool> set)
    {
        api.AddBoolOption(this.manifest, () => get(this.getConfig()), value => set(this.getConfig(), value), () => this.translate($"config.{key}.name"), () => this.translate($"config.{key}.description"));
    }

    private void Reset() => this.setConfig(new ModConfig());

    private void Save()
    {
        var config = this.getConfig();
        config.Normalize();
        this.helper.WriteConfig(config);
    }
}
