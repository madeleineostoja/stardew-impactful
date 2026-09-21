namespace Impactful.Framework;

internal sealed class HitStopController
{
    private int remainingFrames;

    internal bool IsActive => this.remainingFrames > 0;

    internal void Request(int frames)
    {
        this.remainingFrames = Math.Max(this.remainingFrames, frames);
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
