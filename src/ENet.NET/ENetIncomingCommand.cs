using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetIncomingCommand
    {
        public LinkedListNode<ENetIncomingCommand> incomingCommandList;
        public ushort reliableSequenceNumber;
        public ushort unreliableSequenceNumber;
        public ENetProtocol command;
        public long fragmentCount;
        public long fragmentsRemaining;
        public uint[] fragments;
        public ENetPacket packet;
    }
}