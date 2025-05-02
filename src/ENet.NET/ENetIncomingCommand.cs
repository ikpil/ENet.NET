

namespace ENet.NET;

public class ENetIncomingCommand
{
    public ENetListNode incomingCommandList;
    public ushort  reliableSequenceNumber;
    public ushort  unreliableSequenceNumber;
    public ENetProtocol command;
    public uint  fragmentCount;
    public uint  fragmentsRemaining;
    public uint *fragments;
    public ENetPacket * packet;
}