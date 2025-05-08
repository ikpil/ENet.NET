using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetChannel
    {
        public ushort outgoingReliableSequenceNumber;
        public ushort outgoingUnreliableSequenceNumber;
        public ushort usedReliableWindows;
        public ushort[] reliableWindows = new ushort[ENets.ENET_PEER_RELIABLE_WINDOWS];
        public ushort incomingReliableSequenceNumber;
        public ushort incomingUnreliableSequenceNumber;
        public LinkedList<ENetIncomingCommand> incomingReliableCommands = new LinkedList<ENetIncomingCommand>();
        public LinkedList<ENetIncomingCommand> incomingUnreliableCommands = new LinkedList<ENetIncomingCommand>();
    }
}