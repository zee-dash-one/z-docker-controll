using ZLab.Models;
using ZLab.Services;

namespace ZLab.Tests;

public class ServiceCardPresenterTests
{
    private static readonly RemoteService Nine = new(
        "9router", "9Router", false, "http://192.168.2.37:20128", "https://9router-serve.mandrill-court.ts.net");

    [Fact]
    public void Cpu_of_host_divides_by_cores()
    {
        var pct = ServiceCardPresenter.CpuOfHost(3.21, 4);
        Assert.NotNull(pct);
        Assert.InRange(pct!.Value, 0.8024, 0.8026);
        Assert.Equal("0,8%", ServiceCardPresenter.CpuText(pct));
    }

    [Fact]
    public void Running_shows_online_and_monitor()
    {
        var c = new ContainerStats(
            "9router",
            "running",
            new DateTimeOffset(2026, 10, 16, 22, 14, 3, TimeSpan.Zero),
            0,
            3.21,
            85_200_000,
            15_500_000_000);
        var view = ServiceCardPresenter.From(Nine, c, cores: 4, nowUtc: new DateTimeOffset(2026, 10, 19, 2, 14, 3, TimeSpan.Zero), hasReading: true);
        Assert.Equal(StatusPillKindUi.Online, view.Pill);
        Assert.Equal(ServiceCardBodyKind.Monitor, view.Body);
        Assert.Equal("192.168.2.37:20128", view.AddressLabel);
        Assert.Contains("0 reinícios", view.StateLine);
        Assert.Equal("0,8%", view.CpuText);
        Assert.True(view.CanStop);
        Assert.False(view.CanStart);
    }

    [Fact]
    public void One_restart_is_singular()
    {
        Assert.Equal("1 reinício", ServiceCardPresenter.RestartsLabel(1));
        Assert.Equal("2 reinícios", ServiceCardPresenter.RestartsLabel(2));
    }

    [Fact]
    public void Exited_shows_stopped_box()
    {
        var c = new ContainerStats("9router", "exited", null, 2, null, null, null);
        var view = ServiceCardPresenter.From(Nine, c, 4, DateTimeOffset.UtcNow, true);
        Assert.Equal(StatusPillKindUi.Stopped, view.Pill);
        Assert.Equal(ServiceCardBodyKind.StoppedBox, view.Body);
        Assert.Equal("Container parado · sem consumo", view.StateLine);
        Assert.True(view.CanStart);
    }

    [Fact]
    public void Absent_container_prompts_create()
    {
        var view = ServiceCardPresenter.From(Nine, null, 4, DateTimeOffset.UtcNow, true);
        Assert.Equal(StatusPillKindUi.Unknown, view.Pill);
        Assert.Equal(ServiceCardBodyKind.AbsentBox, view.Body);
        Assert.Contains("Criar", view.StateLine);
        Assert.True(view.CanCreate);
    }
}
