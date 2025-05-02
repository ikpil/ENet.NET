namespace ENet.NET;

/**
 * An ENet event as returned by enet_host_service().
 *
 * @sa enet_host_service
 */
public struct ENetEvent
{
    ENetEventType type; /* type of the event */
    ENetPeer* peer; /* peer that generated a connect, disconnect or receive event */
    byte channelID; /* channel on the peer that generated the event, if appropriate */
    uint data; /* data associated with the event, if appropriate */
    ENetPacket* packet; /* packet associated with the event, if appropriate */
}