namespace ENet.NET;

public class ENetChannel
{
    public ushort outgoingReliableSequenceNumber;
    public ushort outgoingUnreliableSequenceNumber;
    public ushort usedReliableWindows;
    public ushort[] reliableWindows = new ushort[ENets.ENET_PEER_RELIABLE_WINDOWS];
    public ushort incomingReliableSequenceNumber;
    public ushort incomingUnreliableSequenceNumber;
    public ENetList<ENetIncomingCommand> incomingReliableCommands;
    public ENetList<ENetIncomingCommand> incomingUnreliableCommands;
}