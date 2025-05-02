using System.Runtime.InteropServices;

namespace ENet.NET;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendReliable
{
    public ENetProtocolCommandHeader header;
    public ushort dataLength;
}