using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolCommandHeader
{
    public byte command;
    public byte channelID;
    public ushort reliableSequenceNumber;
}