namespace ENet.NET.Primitives;

public class ENetChannel
{
    public ushort outgoingReliableSequenceNumber;
    public ushort outgoingUnreliableSequenceNumber;
    public ushort usedReliableWindows;
    public ushort reliableWindows[ENET_PEER_RELIABLE_WINDOWS];
    public ushort incomingReliableSequenceNumber;
    public ushort incomingUnreliableSequenceNumber;
    public ENetList    incomingReliableCommands;
    public ENetList    incomingUnreliableCommands;
}