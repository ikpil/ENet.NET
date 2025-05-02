namespace ENet.NET;

public class ENetChannel
{
    public ushort outgoingReliableSequenceNumber;
    public ushort outgoingUnreliableSequenceNumber;
    public ushort usedReliableWindows;
    public ushort reliableWindows[ENET_PEER_RELIABLE_WINDOWS];
    public ushort incomingReliableSequenceNumber;
    public ushort incomingUnreliableSequenceNumber;
    public ENetList<ENetIncomingCommand> incomingReliableCommands;
    public ENetList<ENetIncomingCommand> incomingUnreliableCommands;
}