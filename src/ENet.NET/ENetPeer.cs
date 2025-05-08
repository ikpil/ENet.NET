using System.Collections.Generic;

namespace ENet.NET
{
    /**
     * An ENet peer which data packets may be sent or received from.
     *
     * No fields should be modified unless otherwise specified.
     */
    public class ENetPeer
    {
        public LinkedListNode<ENetPeer> dispatchList;

        public ENetHost host;
        public ushort outgoingPeerID;
        public ushort incomingPeerID;
        public uint connectID;
        public byte outgoingSessionID;
        public byte incomingSessionID;
        public ENetAddress address; /* Internet address of the peer */
        public object data; /* Application private data, may be freely modified */
        public ENetPeerState state;

        public ENetChannel[] channels;
        public long channelCount; /* Number of channels allocated for communication with peer */
        public long incomingBandwidth; /* Downstream bandwidth of the client in bytes/second */
        public long outgoingBandwidth; /* Upstream bandwidth of the client in bytes/second */
        public long incomingBandwidthThrottleEpoch;

        public long outgoingBandwidthThrottleEpoch;
        public long incomingDataTotal;
        public long totalDataReceived;
        public long outgoingDataTotal;
        public long totalDataSent;
        public long lastSendTime;
        public long lastReceiveTime;
        public long nextTimeout;
        public long earliestTimeout;
        public long packetLossEpoch;
        public long packetsSent;
        public long totalPacketsSent; /* total number of packets sent during a session */
        public long packetsLost;
        public long totalPacketsLost; /* total number of packets lost during a session */
        public long packetLoss; /* mean packet loss of reliable packets as a ratio with respect to the constant ENET_PEER_PACKET_LOSS_SCALE */
        public long packetLossVariance;
        public long packetThrottle;
        public long packetThrottleLimit;
        public long packetThrottleCounter;
        public long packetThrottleEpoch;
        public long packetThrottleAcceleration;
        public long packetThrottleDeceleration;
        public long packetThrottleInterval;
        public long pingInterval;
        public long timeoutLimit;
        public long timeoutMinimum;
        public long timeoutMaximum;
        public long lastRoundTripTime;
        public long lowestRoundTripTime;
        public long lastRoundTripTimeVariance;
        public long highestRoundTripTimeVariance;
        public long roundTripTime; /* mean round trip time (RTT), in milliseconds, between sending a reliable packet and receiving its acknowledgement */
        public long roundTripTimeVariance;
        public long mtu;
        public long windowSize;
        public long reliableDataInTransit;
        public ushort outgoingReliableSequenceNumber;
        public LinkedList<ENetAcknowledgement> acknowledgements = new LinkedList<ENetAcknowledgement>();
        public LinkedList<ENetOutgoingCommand> sentReliableCommands = new LinkedList<ENetOutgoingCommand>();
        public LinkedList<ENetOutgoingCommand> outgoingCommands = new LinkedList<ENetOutgoingCommand>();
        public LinkedList<ENetOutgoingCommand> outgoingSendReliableCommands = new LinkedList<ENetOutgoingCommand>();
        public LinkedList<ENetIncomingCommand> dispatchedCommands = new LinkedList<ENetIncomingCommand>();
        public ushort flags;
        public ushort reserved;
        public ushort incomingUnsequencedGroup;
        public ushort outgoingUnsequencedGroup;
        public uint[] unsequencedWindow = new uint[ENets.ENET_PEER_UNSEQUENCED_WINDOW_SIZE / 32];
        public uint eventData;
        public long totalWaitingData;

        public void clear()
        {
            // ..
        }
    }
}