namespace ENet.NET.Primitives;

public enum ENetPeerFlag
{
    ENET_PEER_FLAG_NEEDS_DISPATCH = (1 << 0),
    ENET_PEER_FLAG_CONTINUE_SENDING = (1 << 1)
}