using System.Runtime.InteropServices;

namespace ENet.NET;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolThrottleConfigure
{
    public ENetProtocolCommandHeader header;
    public uint packetThrottleInterval;
    public uint packetThrottleAcceleration;
    public uint packetThrottleDeceleration;
}