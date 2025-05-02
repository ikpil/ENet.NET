namespace ENet.NET;

/**
 * An ENet peer which data packets may be sent or received from.
 *
 * No fields should be modified unless otherwise specified.
 */
public class ENetPeer
{
    public ENetListNode dispatchList;

    public ENetHost host;
    public ushort outgoingPeerID;
    public ushort incomingPeerID;
    public uint connectID;
    public byte outgoingSessionID;
    public byte incomingSessionID;
    public ENetAddress address; /* Internet address of the peer */
    public void* data; /* Application private data, may be freely modified */
    public ENetPeerState state;

    public ENetChannel* channels;
    public ulong channelCount; /* Number of channels allocated for communication with peer */
    public uint incomingBandwidth; /* Downstream bandwidth of the client in bytes/second */
    public uint outgoingBandwidth; /* Upstream bandwidth of the client in bytes/second */
    public uint incomingBandwidthThrottleEpoch;

    public uint outgoingBandwidthThrottleEpoch;
    public uint incomingDataTotal;
    public ulong totalDataReceived;
    public uint outgoingDataTotal;
    public ulong totalDataSent;
    public uint lastSendTime;
    public uint lastReceiveTime;
    public uint nextTimeout;
    public uint earliestTimeout;
    public uint packetLossEpoch;
    public uint packetsSent;
    public ulong totalPacketsSent; /* total number of packets sent during a session */
    public uint packetsLost;
    public uint totalPacketsLost; /* total number of packets lost during a session */
    public uint packetLoss; /* mean packet loss of reliable packets as a ratio with respect to the constant ENET_PEER_PACKET_LOSS_SCALE */
    public uint packetLossVariance;
    public uint packetThrottle;
    public uint packetThrottleLimit;
    public uint packetThrottleCounter;
    public uint packetThrottleEpoch;
    public uint packetThrottleAcceleration;
    public uint packetThrottleDeceleration;
    public uint packetThrottleInterval;
    public uint pingInterval;
    public uint timeoutLimit;
    public uint timeoutMinimum;
    public uint timeoutMaximum;
    public uint lastRoundTripTime;
    public uint lowestRoundTripTime;
    public uint lastRoundTripTimeVariance;
    public uint highestRoundTripTimeVariance;
    public uint roundTripTime; /* mean round trip time (RTT), in milliseconds, between sending a reliable packet and receiving its acknowledgement */
    public uint roundTripTimeVariance;
    public uint mtu;
    public uint windowSize;
    public uint reliableDataInTransit;
    public ushort outgoingReliableSequenceNumber;
    public ENetList acknowledgements;
    public ENetList sentReliableCommands;
    public ENetList outgoingCommands;
    public ENetList outgoingSendReliableCommands;
    public ENetList dispatchedCommands;
    public ushort flags;
    public ushort reserved;
    public ushort incomingUnsequencedGroup;
    public ushort outgoingUnsequencedGroup;
    public uint unsequencedWindow[ENET_PEER_UNSEQUENCED_WINDOW_SIZE / 32];
    public uint eventData;
    public ulong totalWaitingData;
}