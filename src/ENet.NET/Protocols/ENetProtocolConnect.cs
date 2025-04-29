using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolConnect
{
    public ENetProtocolCommandHeader header;
    public ushort outgoingPeerID;
    public byte incomingSessionID;
    public byte outgoingSessionID;
    public uint mtu;
    public uint windowSize;
    public uint channelCount;
    public uint incomingBandwidth;
    public uint outgoingBandwidth;
    public uint packetThrottleInterval;
    public uint packetThrottleAcceleration;
    public uint packetThrottleDeceleration;
    public uint connectID;
    public uint data;
}