namespace ENet.NET
{
    /**
     * An ENet event as returned by enet_host_service().
     *
     * @sa enet_host_service
     */
    public class ENetEvent
    {
        public ENetEventType type; /* type of the event */
        public ENetPeer peer; /* peer that generated a connect, disconnect or receive event */
        public byte channelID; /* channel on the peer that generated the event, if appropriate */
        public uint data; /* data associated with the event, if appropriate */
        public ENetPacket packet; /* packet associated with the event, if appropriate */
    }
}