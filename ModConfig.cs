namespace Impactful;

public sealed class ModConfig
{
    public bool EnableScreenShake { get; set; } = true;
    public int ShakeStrength { get; set; } = 100;
    public bool Mining { get; set; } = true;
    public bool Combat { get; set; } = true;
    public bool PlayerDamage { get; set; } = true;
    public bool Explosions { get; set; } = true;

    public void Normalize()
    {
        this.ShakeStrength = Math.Clamp(this.ShakeStrength, 0, 200);
    }
}
