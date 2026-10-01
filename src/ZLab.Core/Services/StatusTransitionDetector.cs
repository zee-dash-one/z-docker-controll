namespace ZLab.Services;

public enum TransitionNotifyKind
{
    None = 0,
    Caiu,
    Subiu
}

/// <summary>
/// Pure helper: previous vs next status → whether a tray balloon should fire.
/// </summary>
public static class StatusTransitionDetector
{
    /// <summary>
    /// <paramref name="previous"/> null means baseline not established (first poll) → never notify.
    /// Notify only when managed &amp;&amp; enabled and state actually transitions Up↔Down/Absent.
    /// </summary>
    public static TransitionNotifyKind ShouldNotify(
        ContainerStatusKind? previous,
        ContainerStatusKind next,
        bool managed,
        bool enabled)
    {
        if (previous is null)
            return TransitionNotifyKind.None;
        if (!managed || !enabled)
            return TransitionNotifyKind.None;
        if (previous.Value == next)
            return TransitionNotifyKind.None;

        var wasUp = IsUpLike(previous.Value);
        var isUp = IsUpLike(next);
        var wasDown = IsDownLike(previous.Value);
        var isDown = IsDownLike(next);

        if (wasUp && isDown)
            return TransitionNotifyKind.Caiu;
        if (wasDown && isUp)
            return TransitionNotifyKind.Subiu;

        return TransitionNotifyKind.None;
    }

    public static bool IsUpLike(ContainerStatusKind status) =>
        status == ContainerStatusKind.Up;

    public static bool IsDownLike(ContainerStatusKind status) =>
        status is ContainerStatusKind.Down or ContainerStatusKind.Absent;
}
