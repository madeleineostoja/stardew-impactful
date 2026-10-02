namespace Impactful.Framework;

internal sealed class HitStopController
{
    private int remainingFrames;

    internal bool IsActive => this.remainingFrames > 0;

    internal void Request(int frames)
    {
        this.remainingFrames = Math.Max(this.remainingFrames, frames);
    }

    internal void RequestDamage(int damage, bool isClubAttack)
    {
        if (damage > 0)
            this.Request(isClubAttack ? 2 : 1);
    }

    internal void RequestKill(bool isClubAttack)
    {
        this.Request(isClubAttack ? 4 : 3);
    }

    internal bool TryConsumeFrame()
    {
        if (this.remainingFrames <= 0)
            return false;

        this.remainingFrames--;
        return true;
    }

    internal void Clear()
    {
        this.remainingFrames = 0;
    }
}
