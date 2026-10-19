namespace ZLab.Services;

/// <summary>
/// One of the six Compose services on z-server-01. The list is closed.
/// </summary>
public sealed record RemoteService(
    string Id,
    string DisplayName,
    bool TailnetProfile,
    string? LanUrl,
    string? HttpsLabel);
