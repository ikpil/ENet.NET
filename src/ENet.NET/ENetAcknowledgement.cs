namespace ENet.NET
{
    public class ENetAcknowledgement
    {
        public ENetListNode<ENetAcknowledgement> acknowledgementList;
        public uint sentTime;
        public ENetProtocol command;
    }
}