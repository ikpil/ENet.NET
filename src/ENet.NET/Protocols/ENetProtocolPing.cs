using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolPing
{
    public ENetProtocolCommandHeader header;
}