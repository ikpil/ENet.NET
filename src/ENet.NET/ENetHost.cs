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
    public ENetSocket socket;
    public ENetAddress address; /* Internet address of the host */
    public uint incomingBandwidth; /* downstream bandwidth of the host */
    public uint outgoingBandwidth; /* upstream bandwidth of the host */
    public uint bandwidthThrottleEpoch;
    public uint mtu;
    public uint randomSeed;
    public int recalculateBandwidthLimits;
    public ENetPeer[] peers; /* array of peers allocated for this host */
    public ulong peerCount; /* number of peers allocated for this host */
    public ulong channelLimit; /* maximum number of channels allowed for connected peers */
    public uint serviceTime;
    public ENetList<ENetPeer> dispatchQueue;
    public uint totalQueued;
    public ulong packetSize;
    public ushort headerFlags;
    public ENetProtocol commands[ENetProtocolConst.ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS];
    public ulong commandCount;
    public ENetBuffer buffers[ENET_BUFFER_MAXIMUM];
    public ulong bufferCount;
    public ENetChecksumCallback checksum; /* callback the user can set to enable packet checksums for this host */
    public ENetCompressor compressor;
    public byte packetData[2][ENetProtocolConst.ENET_PROTOCOL_MAXIMUM_MTU];
    public ENetAddress receivedAddress;
    public byte* receivedData;
    public ulong receivedDataLength;
    public uint totalSentData; /* total data sent, user should reset to 0 as needed to prevent overflow */
    public uint totalSentPackets; /* total UDP packets sent, user should reset to 0 as needed to prevent overflow */
    public uint totalReceivedData; /* total data received, user should reset to 0 as needed to prevent overflow */
    public uint totalReceivedPackets; /* total UDP packets received, user should reset to 0 as needed to prevent overflow */
    public ENetInterceptCallback intercept; /* callback the user can set to intercept received raw UDP packets */
    public ulong connectedPeers;
    public ulong bandwidthLimitedPeers;
    public ulong duplicatePeers; /* optional number of allowed peers from duplicate IPs, defaults to ENET_PROTOCOL_MAXIMUM_PEER_ID */
    public ulong maximumPacketSize; /* the maximum allowable packet size that may be sent or received on a peer */
    public ulong maximumWaitingData; /* the maximum aggregate amount of buffer space a peer may use waiting for packets to be delivered */
}