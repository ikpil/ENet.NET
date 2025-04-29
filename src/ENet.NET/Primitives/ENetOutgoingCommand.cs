using ENet.NET.Protocols;

namespace ENet.NET.Primitives;

public struct ENetOutgoingCommand
{
    public ENetListNode outgoingCommandList;
    public ushort  reliableSequenceNumber;
    public ushort  unreliableSequenceNumber;
    public uint  sentTime;
    public uint  roundTripTimeout;
    public uint  queueTime;
    public uint  fragmentOffset;
    public ushort  fragmentLength;
    public ushort  sendAttempts;
    public ENetProtocol command;
    public ENetPacket * packet;
}