using ZLab.Services;

namespace ZLab.Tests;

public sealed class StatusTransitionDetectorTests
{
    [Fact]
    public void FirstPoll_Baseline_ProducesNoBalloon()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: null,
            next: ContainerStatusKind.Up,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.None, kind);
    }

    [Fact]
    public void UpToDown_ManagedEnabled_YieldsCaiu()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Up,
            next: ContainerStatusKind.Down,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.Caiu, kind);
    }

    [Fact]
    public void UpToAbsent_ManagedEnabled_YieldsCaiu()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Up,
            next: ContainerStatusKind.Absent,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.Caiu, kind);
    }

    [Fact]
    public void DownToUp_ManagedEnabled_YieldsSubiu()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Down,
            next: ContainerStatusKind.Up,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.Subiu, kind);
    }

    [Fact]
    public void AbsentToUp_ManagedEnabled_YieldsSubiu()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Absent,
            next: ContainerStatusKind.Up,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.Subiu, kind);
    }

    [Fact]
    public void UnchangedDown_YieldsNone()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Down,
            next: ContainerStatusKind.Down,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.None, kind);
    }

    [Fact]
    public void UnchangedUp_YieldsNone()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Up,
            next: ContainerStatusKind.Up,
            managed: true,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.None, kind);
    }

    [Fact]
    public void DisabledItem_NeverNotifies()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Up,
            next: ContainerStatusKind.Down,
            managed: true,
            enabled: false);

        Assert.Equal(TransitionNotifyKind.None, kind);
    }

    [Fact]
    public void UnmanagedItem_NeverNotifies()
    {
        var kind = StatusTransitionDetector.ShouldNotify(
            previous: ContainerStatusKind.Down,
            next: ContainerStatusKind.Up,
            managed: false,
            enabled: true);

        Assert.Equal(TransitionNotifyKind.None, kind);
    }

    [Fact]
    public void SubsequentDownPolls_DoNotRepeatCaiu()
    {
        var first = StatusTransitionDetector.ShouldNotify(
            ContainerStatusKind.Up, ContainerStatusKind.Down, managed: true, enabled: true);
        var second = StatusTransitionDetector.ShouldNotify(
            ContainerStatusKind.Down, ContainerStatusKind.Down, managed: true, enabled: true);

        Assert.Equal(TransitionNotifyKind.Caiu, first);
        Assert.Equal(TransitionNotifyKind.None, second);
    }
}
