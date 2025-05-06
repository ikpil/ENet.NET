using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ENet.NET
{
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
        public Socket socket;
        public ENetAddress address; /* Internet address of the host */
        public long incomingBandwidth; /* downstream bandwidth of the host */
        public long outgoingBandwidth; /* upstream bandwidth of the host */
        public long bandwidthThrottleEpoch;
        public long mtu;
        public uint randomSeed;
        public int recalculateBandwidthLimits;
        public ENetPeer[] peers; /* array of peers allocated for this host */
        public long peerCount; /* number of peers allocated for this host */
        public long channelLimit; /* maximum number of channels allowed for connected peers */
        public long serviceTime;
        public LinkedList<ENetPeer> dispatchQueue = new LinkedList<ENetPeer>();
        public uint totalQueued;
        public long packetSize;
        public ushort headerFlags;
        public ENetProtocol[] commands = new ENetProtocol[ENets.ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS];
        public int commandCount;
        public ENetBuffer[] buffers = new ENetBuffer[ENets.ENET_BUFFER_MAXIMUM];
        public int bufferCount;
        public ENetChecksumCallback checksum; /* callback the user can set to enable packet checksums for this host */
        public ENetCompressor compressor;

        public byte[][] packetData = new byte[2][]
        {
            new byte[ENets.ENET_PROTOCOL_MAXIMUM_MTU],
            new byte[ENets.ENET_PROTOCOL_MAXIMUM_MTU],
        };

        public ENetAddress receivedAddress;
        public ArraySegment<byte> receivedData;
        public int receivedDataLength;
        public long totalSentData; /* total data sent, user should reset to 0 as needed to prevent overflow */
        public long totalSentPackets; /* total UDP packets sent, user should reset to 0 as needed to prevent overflow */
        public long totalReceivedData; /* total data received, user should reset to 0 as needed to prevent overflow */
        public long totalReceivedPackets; /* total UDP packets received, user should reset to 0 as needed to prevent overflow */
        public ENetInterceptCallback intercept; /* callback the user can set to intercept received raw UDP packets */
        public long connectedPeers;
        public long bandwidthLimitedPeers;
        public long duplicatePeers; /* optional number of allowed peers from duplicate IPs, defaults to ENET_PROTOCOL_MAXIMUM_PEER_ID */
        public long maximumPacketSize; /* the maximum allowable packet size that may be sent or received on a peer */
        public long maximumWaitingData; /* the maximum aggregate amount of buffer space a peer may use waiting for packets to be delivered */
    }
}