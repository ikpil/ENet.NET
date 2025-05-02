namespace ENet.NET;

/** An ENet host for communicating with peers.
 *
 * No fields should be modified unless otherwise stated.
 *
 *  @sa enet_host_create()
 *  @sa enet_host_destroy()
 *  @sa enet_host_connect()
 *  @sa enet_host_service()
 *  @sa enet_host_flush()
 *  @sa enet_host_broadcast()
 *  @sa enet_host_compress()
 *  @sa enet_host_channel_limit()
 *  @sa enet_host_bandwidth_limit()
 *  @sa enet_host_bandwidth_throttle()
 */
public class ENetHost
{
    ENetSocket socket;
    ENetAddress address; /* Internet address of the host */
    uint incomingBandwidth; /* downstream bandwidth of the host */
    uint outgoingBandwidth; /* upstream bandwidth of the host */
    uint bandwidthThrottleEpoch;
    uint mtu;
    uint randomSeed;
    int recalculateBandwidthLimits;
    ENetPeer* peers; /* array of peers allocated for this host */
    ulong peerCount; /* number of peers allocated for this host */
    ulong channelLimit; /* maximum number of channels allowed for connected peers */
    uint serviceTime;
    ENetList dispatchQueue;
    uint totalQueued;
    ulong packetSize;
    ushort headerFlags;
    ENetProtocol commands[ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS];
    ulong commandCount;
    ENetBuffer buffers[ENET_BUFFER_MAXIMUM];
    ulong bufferCount;
    ENetChecksumCallback checksum; /* callback the user can set to enable packet checksums for this host */
    ENetCompressor compressor;
    byte packetData[2][ENET_PROTOCOL_MAXIMUM_MTU];
    ENetAddress receivedAddress;
    byte* receivedData;
    ulong receivedDataLength;
    uint totalSentData; /* total data sent, user should reset to 0 as needed to prevent overflow */
    uint totalSentPackets; /* total UDP packets sent, user should reset to 0 as needed to prevent overflow */
    uint totalReceivedData; /* total data received, user should reset to 0 as needed to prevent overflow */
    uint totalReceivedPackets; /* total UDP packets received, user should reset to 0 as needed to prevent overflow */
    ENetInterceptCallback intercept; /* callback the user can set to intercept received raw UDP packets */
    ulong connectedPeers;
    ulong bandwidthLimitedPeers;
    ulong duplicatePeers; /* optional number of allowed peers from duplicate IPs, defaults to ENET_PROTOCOL_MAXIMUM_PEER_ID */
    ulong maximumPacketSize; /* the maximum allowable packet size that may be sent or received on a peer */
    ulong maximumWaitingData; /* the maximum aggregate amount of buffer space a peer may use waiting for packets to be delivered */
}