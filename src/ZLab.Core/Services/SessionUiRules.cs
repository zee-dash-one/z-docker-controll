namespace ZLab.Services;

public enum SessionState
{
    Disconnected,
    Connecting,
    Connected
}

public sealed record SessionUiFlags(
    bool EnterEnabled,
    bool EnterPrimary,
    string EnterText,
    bool EndSessionEnabled,
    bool ShowEmpty,
    bool ShowDashboard,
    bool EnergyVisible,
    bool WakeEnabled,
    bool RebootEnabled,
    bool ShutdownEnabled);

/// <summary>
/// Pure enable/visibility rules for the session shell (todo 02).
/// </summary>
public static class SessionUiRules
{
    public static SessionUiFlags For(SessionState state, bool serverResponds, bool busy = false)
    {
        return state switch
        {
            SessionState.Connecting => new SessionUiFlags(
                EnterEnabled: false,
                EnterPrimary: true,
                EnterText: "Entrando…",
                EndSessionEnabled: false,
                ShowEmpty: true,
                ShowDashboard: false,
                EnergyVisible: false,
                WakeEnabled: false,
                RebootEnabled: false,
                ShutdownEnabled: false),

            SessionState.Connected => new SessionUiFlags(
                EnterEnabled: false,
                EnterPrimary: false,
                EnterText: "Entrar",
                EndSessionEnabled: !busy,
                ShowEmpty: false,
                ShowDashboard: true,
                EnergyVisible: true,
                WakeEnabled: !busy && !serverResponds,
                RebootEnabled: !busy && serverResponds,
                ShutdownEnabled: !busy && serverResponds),

            _ => new SessionUiFlags(
                EnterEnabled: !busy,
                EnterPrimary: true,
                EnterText: "Entrar",
                EndSessionEnabled: false,
                ShowEmpty: true,
                ShowDashboard: false,
                EnergyVisible: false,
                WakeEnabled: !busy && !serverResponds,
                RebootEnabled: false,
                ShutdownEnabled: false)
        };
    }
}
