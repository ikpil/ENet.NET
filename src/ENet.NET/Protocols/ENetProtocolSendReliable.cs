using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendReliable
{
    public ENetProtocolCommandHeader header;
    public ushort dataLength;
}