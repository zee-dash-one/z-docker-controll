using System.Net;
using System.Net.Sockets;

namespace ZLab.Services;

/// <summary>
/// Magic packet used by z-server-01 scripts/wake-zserver.ps1.
/// </summary>
public static class WakeOnLan
{
    public const string MacAddress = "FC:AA:14:F9:80:D4";
    public const string BroadcastAddress = "192.168.0.255";
    public const int UdpPort = 9;

    public static byte[] BuildMagicPacket(string mac)
    {
        var parts = mac.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6)
            throw new ArgumentException("MAC precisa de 6 octetos.", nameof(mac));

        var macBytes = new byte[6];
        for (var i = 0; i < 6; i++)
            macBytes[i] = Convert.ToByte(parts[i], 16);

        var packet = new byte[102];
        for (var i = 0; i < 6; i++)
            packet[i] = 0xFF;
        for (var rep = 0; rep < 16; rep++)
            Buffer.BlockCopy(macBytes, 0, packet, 6 + (rep * 6), 6);
        return packet;
    }

    public static int Send(string? mac = null, string? broadcast = null, int port = UdpPort)
    {
        var packet = BuildMagicPacket(mac ?? MacAddress);
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        return udp.Send(packet, packet.Length, broadcast ?? BroadcastAddress, port);
    }
}
