using ZLab.Services;

namespace ZLab.Tests;

public class SessionUiRulesTests
{
    [Fact]
    public void Disconnected_enter_primary_end_disabled_empty()
    {
        var f = SessionUiRules.For(SessionState.Disconnected, serverResponds: false);
        Assert.True(f.EnterEnabled);
        Assert.True(f.EnterPrimary);
        Assert.Equal("Entrar", f.EnterText);
        Assert.False(f.EndSessionEnabled);
        Assert.True(f.ShowEmpty);
        Assert.False(f.ShowDashboard);
        Assert.False(f.EnergyVisible);
        Assert.True(f.WakeEnabled);
    }

    [Fact]
    public void Disconnected_when_server_up_hides_wake_in_empty()
    {
        var f = SessionUiRules.For(SessionState.Disconnected, serverResponds: true);
        Assert.False(f.WakeEnabled);
        Assert.True(f.ShowEmpty);
    }

    [Fact]
    public void Connecting_disables_enter_and_end()
    {
        var f = SessionUiRules.For(SessionState.Connecting, serverResponds: true);
        Assert.False(f.EnterEnabled);
        Assert.Equal("Entrando…", f.EnterText);
        Assert.False(f.EndSessionEnabled);
        Assert.True(f.ShowEmpty);
        Assert.False(f.ShowDashboard);
        Assert.False(f.EnergyVisible);
    }

    [Fact]
    public void Connected_shows_dashboard_and_energy()
    {
        var f = SessionUiRules.For(SessionState.Connected, serverResponds: true);
        Assert.False(f.EnterEnabled);
        Assert.False(f.EnterPrimary);
        Assert.True(f.EndSessionEnabled);
        Assert.False(f.ShowEmpty);
        Assert.True(f.ShowDashboard);
        Assert.True(f.EnergyVisible);
        Assert.False(f.WakeEnabled);
        Assert.True(f.RebootEnabled);
        Assert.True(f.ShutdownEnabled);
    }

    [Fact]
    public void Connected_busy_disables_energy_and_end()
    {
        var f = SessionUiRules.For(SessionState.Connected, serverResponds: true, busy: true);
        Assert.False(f.EndSessionEnabled);
        Assert.False(f.RebootEnabled);
        Assert.False(f.ShutdownEnabled);
    }
}
