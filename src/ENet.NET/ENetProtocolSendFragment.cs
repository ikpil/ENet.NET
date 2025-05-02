using System.Runtime.InteropServices;

namespace ENet.NET;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendFragment
{
    public ENetProtocolCommandHeader header;
    public ushort startSequenceNumber;
    public ushort dataLength;
    public uint fragmentCount;
    public uint fragmentNumber;
    public uint totalLength;
    public uint fragmentOffset;
}