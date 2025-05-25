using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetOutgoingCommand
    {
        public readonly LinkedListNode<ENetOutgoingCommand> outgoingCommandList;
        public ushort reliableSequenceNumber;
        public ushort unreliableSequenceNumber;
        public long sentTime;
        public uint roundTripTimeout;
        public uint queueTime;
        public int fragmentOffset;
        public ushort fragmentLength;
        public ushort sendAttempts;
        public ENetProtocol command;
        public ENetPacket packet;

        public ENetOutgoingCommand()
        {
            outgoingCommandList = new LinkedListNode<ENetOutgoingCommand>(this);
        }
    }
}