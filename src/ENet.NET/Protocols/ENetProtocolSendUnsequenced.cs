using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendUnsequenced
{
    public ENetProtocolCommandHeader header;
    public ushort unsequencedGroup;
    public ushort dataLength;
}