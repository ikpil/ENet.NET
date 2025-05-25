using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetIncomingCommand
    {
        public readonly LinkedListNode<ENetIncomingCommand> incomingCommandList;
        public ushort reliableSequenceNumber;
        public ushort unreliableSequenceNumber;
        public ENetProtocol command;
        public long fragmentCount;
        public long fragmentsRemaining;
        public uint[] fragments;
        public ENetPacket packet;

        public ENetIncomingCommand()
        {
            incomingCommandList = new LinkedListNode<ENetIncomingCommand>(this);
        }
    }
}