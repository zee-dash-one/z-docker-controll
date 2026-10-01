namespace ZLab.Models;

public sealed class ServicesDocument
{
    public int Version { get; set; } = 1;
    public AppSettings Settings { get; set; } = new();
    public List<ServiceItem> Services { get; set; } = new();
}
