using System.Runtime.InteropServices;

namespace ENet.NET;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolBandwidthLimit
{
    public ENetProtocolCommandHeader header;
    public uint incomingBandwidth;
    public uint outgoingBandwidth;
}