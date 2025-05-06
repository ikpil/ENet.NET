using System.Collections.Generic;

namespace ENet.NET
{
    public class ENetAcknowledgement
    {
        public LinkedListNode<ENetAcknowledgement> acknowledgementList;
        public uint sentTime;
        public ENetProtocol command;
    }
}