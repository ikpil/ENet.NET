using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetAcknowledgement
    {
        public readonly LinkedListNode<ENetAcknowledgement> acknowledgementList;
        public uint sentTime;
        public ENetProtocol command;

        public ENetAcknowledgement()
        {
            acknowledgementList = new LinkedListNode<ENetAcknowledgement>(this);
        }
    }
}