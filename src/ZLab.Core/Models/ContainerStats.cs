namespace ZLab.Models;

public sealed record ContainerStats(
    string Name,
    string State,
    DateTimeOffset? StartedAt,
    int Restarts,
    double? CpuPercentOfHost,
    long? MemUsedBytes,
    long? MemLimitBytes);
