using System.Runtime.InteropServices;

namespace ENet.NET.Protocols;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolAcknowledge
{
    public ENetProtocolCommandHeader header;
    public ushort receivedReliableSequenceNumber;
    public ushort receivedSentTime;
}