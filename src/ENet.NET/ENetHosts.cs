using System;
using System.Net;
using static ENet.NET.ENets;
using static ENet.NET.ENetSockets;
using static ENet.NET.ENetTimes;
using static ENet.NET.ENetProtocols;
using static ENet.NET.ENetPeers;
using static ENet.NET.ENetAddresses;
using static ENet.NET.ENetDelegates;


namespace ENet.NET
{
    // =======================================================================//
    // !
    // ! Host
    // !
    // =======================================================================//
    public static class ENetHosts
    {
        /** Creates a host for communicating to peers.
         *
         *  @param address   the address at which other peers may connect to this host.  If null, then no peers may connect to the host.
         *  @param peerCount the maximum number of peers that should be allocated for the host.
         *  @param channelLimit the maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT
         *  @param incomingBandwidth downstream bandwidth of the host in bytes/second; if 0, ENet will assume unlimited bandwidth.
         *  @param outgoingBandwidth upstream bandwidth of the host in bytes/second; if 0, ENet will assume unlimited bandwidth.
         *
         *  @returns the host on success and null on failure
         *
         *  @remarks ENet will strategically drop packets on specific sides of a connection between hosts
         *  to ensure the host's bandwidth is not overwhelmed.  The bandwidth parameters also determine
         *  the window size of a connection which limits the amount of reliable packets that may be in transit
         *  at any given time.
         */
        public static ENetHost enet_host_create(ENetAddress address, int peerCount, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
        {
            if (peerCount > ENET_PROTOCOL_MAXIMUM_PEER_ID)
            {
                return null;
            }

            ENetHost host = new ENetHost();
            host.peers = enet_malloc<ENetPeer>(peerCount);
            if (host.peers == null)
            {
                enet_free(host);
                return null;
            }

            for (int i = 0; i < host.peers.Length; ++i)
            {
                host.peers[i].clear();
            }

            host.socket = enet_socket_create(ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM);
            if (host.socket != null)
            {
                enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY, 0);
            }

            if (host.socket == null || (address.host != null && enet_socket_bind(host.socket, address) < 0))
            {
                if (host.socket != null)
                {
                    enet_socket_destroy(host.socket);
                }

                enet_free(host.peers);
                enet_free(host);

                return null;
            }

            enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_NONBLOCK, 1);
            enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_BROADCAST, 1);
            enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_RCVBUF, ENET_HOST_RECEIVE_BUFFER_SIZE);
            enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_SNDBUF, ENET_HOST_SEND_BUFFER_SIZE);
            enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY, 0);

            if (address.host != null && enet_socket_get_address(host.socket, ref host.address) < 0)
            {
                host.address = address.Clone();
            }

            if (0 >= channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
            {
                channelLimit = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
            }

            host.randomSeed = (uint)host.GetHashCode() % uint.MaxValue;
            host.randomSeed += (uint)enet_host_random_seed();
            host.randomSeed = (host.randomSeed << 16) | (host.randomSeed >> 16);
            host.channelLimit = channelLimit;
            host.incomingBandwidth = incomingBandwidth;
            host.outgoingBandwidth = outgoingBandwidth;
            host.bandwidthThrottleEpoch = 0;
            host.recalculateBandwidthLimits = 0;
            host.mtu = ENET_HOST_DEFAULT_MTU;
            host.peerCount = peerCount;
            host.commandCount = 0;
            host.bufferCount = 0;
            host.checksum = null;
            host.receivedAddress = new ENetAddress(IPAddress.IPv6Any, 0, 0);
            host.receivedData = null;
            host.receivedDataLength = 0;
            host.totalSentData = 0;
            host.totalSentPackets = 0;
            host.totalReceivedData = 0;
            host.totalReceivedPackets = 0;
            host.totalQueued = 0;
            host.connectedPeers = 0;
            host.bandwidthLimitedPeers = 0;
            host.duplicatePeers = ENET_PROTOCOL_MAXIMUM_PEER_ID;
            host.maximumPacketSize = ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE;
            host.maximumWaitingData = ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA;
            host.compressor.context = null;
            host.compressor.compress = null;
            host.compressor.decompress = null;
            host.compressor.destroy = null;
            host.intercept = null;

            host.dispatchQueue.Clear();

            for (int i = 0; i < host.peerCount; ++i)
            {
                ENetPeer currentPeer = host.peers[i];
                currentPeer.host = host;
                currentPeer.incomingPeerID = (ushort)i;
                currentPeer.outgoingSessionID = currentPeer.incomingSessionID = 0xFF;
                currentPeer.data = null;

                currentPeer.acknowledgements.Clear();
                currentPeer.sentReliableCommands.Clear();
                currentPeer.outgoingCommands.Clear();
                currentPeer.outgoingSendReliableCommands.Clear();
                currentPeer.dispatchedCommands.Clear();

                enet_peer_reset(currentPeer);
            }

            return host;
        } /* enet_host_create */

        /** Destroys the host and all resources associated with it.
         *  @param host pointer to the host to destroy
         */
        public static void enet_host_destroy(ENetHost host)
        {
            if (host == null)
            {
                return;
            }

            enet_socket_destroy(host.socket);

            for (int currentPeer = 0; currentPeer < host.peerCount; ++currentPeer)
            {
                ENetPeer curPeer = host.peers[currentPeer];
                enet_peer_reset(curPeer);
            }

            if (host.compressor.context != null && null != host.compressor.destroy)
            {
                host.compressor.destroy(host.compressor.context);
            }

            enet_free(host.peers);
            enet_free(host);
        }

        public static uint enet_host_random(ENetHost host)
        {
            /* Mulberry32 by Tommy Ettinger */
            uint n = (host.randomSeed += 0x6D2B79F5U);
            n = (n ^ (n >> 15)) * (n | 1U);
            n ^= n + (n ^ (n >> 7)) * (n | 61U);
            return n ^ (n >> 14);
        }

        /** Initiates a connection to a foreign host.
         *  @param host host seeking the connection
         *  @param address destination for the connection
         *  @param channelCount number of channels to allocate
         *  @param data user data supplied to the receiving host
         *  @returns a peer representing the foreign host on success, null on failure
         *  @remarks The peer returned will have not completed the connection until enet_host_service()
         *  notifies of an ENET_EVENT_TYPE_CONNECT event for the peer.
         */
        public static ENetPeer enet_host_connect(ENetHost host, ENetAddress address, int channelCount, uint data)
        {
            ENetPeer currentPeer = null;
            ENetChannel channel = null;

            if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT)
            {
                channelCount = ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;
            }
            else if (channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
            {
                channelCount = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
            }

            int currentPeerIdx = 0;
            for (currentPeerIdx = 0; currentPeerIdx < host.peerCount; ++currentPeerIdx)
            {
                currentPeer = host.peers[currentPeerIdx];
                if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED)
                {
                    break;
                }
            }

            if (currentPeerIdx >= host.peerCount)
            {
                return null;
            }

            currentPeer.channels = enet_malloc<ENetChannel>(channelCount);
            if (currentPeer.channels == null)
            {
                return null;
            }

            currentPeer.channelCount = channelCount;
            currentPeer.state = ENetPeerState.ENET_PEER_STATE_CONNECTING;
            currentPeer.address = address;
            currentPeer.connectID = enet_host_random(host);
            currentPeer.mtu = host.mtu;

            if (host.outgoingBandwidth == 0)
            {
                currentPeer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }
            else
            {
                currentPeer.windowSize = (host.outgoingBandwidth / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }

            if (currentPeer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
            {
                currentPeer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else if (currentPeer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
            {
                currentPeer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }

            int channelIdx = 0;
            for (channelIdx = 0; channelIdx < channelCount; ++channelIdx)
            {
                channel = currentPeer.channels[channelIdx];
                channel.outgoingReliableSequenceNumber = 0;
                channel.outgoingUnreliableSequenceNumber = 0;
                channel.incomingReliableSequenceNumber = 0;
                channel.incomingUnreliableSequenceNumber = 0;

                channel.incomingReliableCommands.Clear();
                channel.incomingUnreliableCommands.Clear();

                channel.usedReliableWindows = 0;
                Array.Fill(channel.reliableWindows, (ushort)0);
            }

            ENetProtocol command = new ENetProtocol();
            command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            command.header.channelID = 0xFF;
            command.connect.outgoingPeerID = ENET_HOST_TO_NET_16(currentPeer.incomingPeerID);
            command.connect.incomingSessionID = currentPeer.incomingSessionID;
            command.connect.outgoingSessionID = currentPeer.outgoingSessionID;
            command.connect.mtu = ENET_HOST_TO_NET_32((uint)currentPeer.mtu);
            command.connect.windowSize = ENET_HOST_TO_NET_32((uint)currentPeer.windowSize);
            command.connect.channelCount = ENET_HOST_TO_NET_32((uint)channelCount);
            command.connect.incomingBandwidth = ENET_HOST_TO_NET_32((uint)host.incomingBandwidth);
            command.connect.outgoingBandwidth = ENET_HOST_TO_NET_32((uint)host.outgoingBandwidth);
            command.connect.packetThrottleInterval = ENET_HOST_TO_NET_32((uint)currentPeer.packetThrottleInterval);
            command.connect.packetThrottleAcceleration = ENET_HOST_TO_NET_32((uint)currentPeer.packetThrottleAcceleration);
            command.connect.packetThrottleDeceleration = ENET_HOST_TO_NET_32((uint)currentPeer.packetThrottleDeceleration);
            command.connect.connectID = currentPeer.connectID;
            command.connect.data = ENET_HOST_TO_NET_32(data);

            enet_peer_queue_outgoing_command(currentPeer, ref command, null, 0, 0);

            return currentPeer;
        } /* enet_host_connect */

        /** Queues a packet to be sent to all peers associated with the host.
         *  @param host host on which to broadcast the packet
         *  @param channelID channel on which to broadcast
         *  @param packet packet to broadcast
         */
        public static void enet_host_broadcast(ENetHost host, byte channelID, ENetPacket packet)
        {
            ENetPeer currentPeer = null;

            for (int currentPeerIdx = 0; currentPeerIdx < host.peerCount; ++currentPeerIdx)
            {
                currentPeer = host.peers[currentPeerIdx];
                if (currentPeer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED)
                {
                    continue;
                }

                enet_peer_send(currentPeer, channelID, packet);
            }

            if (packet.referenceCount == 0)
            {
                callbacks.packet_destroy(packet);
            }
        }

        /** Sends raw data to specified address. Useful when you want to send unconnected data using host's socket.
         *  @param host host sending data
         *  @param address destination address
         *  @param data data pointer
         *  @param dataLength length of data to send
         *  @retval >=0 bytes sent
         *  @retval <0 error
         *  @sa enet_socket_send
         */
        public static int enet_host_send_raw(ENetHost host, ref ENetAddress address, byte[] data, int dataLength)
        {
            ENetBuffer buffer = new ENetBuffer();
            buffer.data = data;
            buffer.dataLength = dataLength;
            return enet_socket_send(host.socket, ref address, buffer);
        }

        /** Sends raw data to specified address with extended arguments. Allows to send only part of data, handy for other programming languages.
         *  I.e. if you have data =- { 0, 1, 2, 3 } and call function as enet_host_send_raw_ex(data, 1, 2) then it will skip 1 byte and send 2 bytes { 1, 2 }.
         *  @param host host sending data
         *  @param address destination address
         *  @param data data pointer
         *  @param skipBytes number of bytes to skip from start of data
         *  @param bytesToSend number of bytes to send
         *  @retval >=0 bytes sent
         *  @retval <0 error
         *  @sa enet_socket_send
         */
        public static int enet_host_send_raw_ex(ENetHost host, ref ENetAddress address, byte[] data, int offset, int skipBytes, int bytesToSend)
        {
            // todo : @ikpil check
            enet_assert(false);

            ENetBuffer buffer;
            buffer.data = data;
            buffer.offset = offset;
            buffer.dataLength = bytesToSend;
            return enet_socket_send(host.socket, ref address, buffer);
        }

        /** Sets intercept callback for the host.
         *  @param host host to set a callback
         *  @param callback intercept callback
         */
        public static void enet_host_set_intercept(ENetHost host, ENetInterceptCallback callback)
        {
            host.intercept = callback;
        }

        /** Sets the packet compressor the host should use to compress and decompress packets.
         *  @param host host to enable or disable compression for
         *  @param compressor callbacks for for the packet compressor; if null, then compression is disabled
         */
        public static void enet_host_compress(ENetHost host, ref ENetCompressor compressor)
        {
            if (host.compressor.context != null && null != host.compressor.destroy)
            {
                host.compressor.destroy(host.compressor.context);
            }

            if (null != compressor.context)
            {
                host.compressor = compressor;
            }
            else
            {
                host.compressor.context = null;
            }
        }

        /** Limits the maximum allowed channels of future incoming connections.
         *  @param host host to limit
         *  @param channelLimit the maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT
         */
        public static void enet_host_channel_limit(ENetHost host, int channelLimit)
        {
            if (0 >= channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
            {
                channelLimit = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
            }

            host.channelLimit = channelLimit;
        }

        /** Adjusts the bandwidth limits of a host.
         *  @param host host to adjust
         *  @param incomingBandwidth new incoming bandwidth
         *  @param outgoingBandwidth new outgoing bandwidth
         *  @remarks the incoming and outgoing bandwidth parameters are identical in function to those
         *  specified in enet_host_create().
         */
        public static void enet_host_bandwidth_limit(ENetHost host, uint incomingBandwidth, uint outgoingBandwidth)
        {
            host.incomingBandwidth = incomingBandwidth;
            host.outgoingBandwidth = outgoingBandwidth;
            host.recalculateBandwidthLimits = 1;
        }

        public static void enet_host_bandwidth_throttle(ENetHost host)
        {
            long timeCurrent = enet_time_get();
            long elapsedTime = timeCurrent - host.bandwidthThrottleEpoch;
            long peersRemaining = host.connectedPeers;
            long dataTotal = uint.MaxValue;
            long bandwidth = uint.MaxValue;
            long throttle = 0;
            long bandwidthLimit = 0;

            int needsAdjustment = host.bandwidthLimitedPeers > 0 ? 1 : 0;
            ENetPeer peer;

            if (elapsedTime < ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL)
            {
                return;
            }

            if (host.outgoingBandwidth == 0 && host.incomingBandwidth == 0)
            {
                return;
            }

            host.bandwidthThrottleEpoch = timeCurrent;

            if (peersRemaining == 0)
            {
                return;
            }

            if (host.outgoingBandwidth != 0)
            {
                dataTotal = 0;
                bandwidth = (host.outgoingBandwidth * elapsedTime) / 1000;

                for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                {
                    peer = host.peers[peerIdx];
                    if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
                    {
                        continue;
                    }

                    dataTotal += peer.outgoingDataTotal;
                }
            }

            while (peersRemaining > 0 && needsAdjustment != 0)
            {
                needsAdjustment = 0;

                if (dataTotal <= bandwidth)
                {
                    throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
                }
                else
                {
                    throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
                }

                for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                {
                    peer = host.peers[peerIdx];
                    long peerBandwidth;

                    if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) ||
                        peer.incomingBandwidth == 0 ||
                        peer.outgoingBandwidthThrottleEpoch == timeCurrent
                       )
                    {
                        continue;
                    }

                    peerBandwidth = (peer.incomingBandwidth * elapsedTime) / 1000;
                    if ((throttle * peer.outgoingDataTotal) / ENET_PEER_PACKET_THROTTLE_SCALE <= peerBandwidth)
                    {
                        continue;
                    }

                    peer.packetThrottleLimit = (peerBandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / peer.outgoingDataTotal;

                    if (peer.packetThrottleLimit == 0)
                    {
                        peer.packetThrottleLimit = 1;
                    }

                    if (peer.packetThrottle > peer.packetThrottleLimit)
                    {
                        peer.packetThrottle = peer.packetThrottleLimit;
                    }

                    peer.outgoingBandwidthThrottleEpoch = timeCurrent;

                    peer.incomingDataTotal = 0;
                    peer.outgoingDataTotal = 0;

                    needsAdjustment = 1;
                    --peersRemaining;
                    bandwidth -= peerBandwidth;
                    dataTotal -= peerBandwidth;
                }
            }

            if (peersRemaining > 0)
            {
                if (dataTotal <= bandwidth)
                {
                    throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
                }
                else
                {
                    throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
                }

                for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                {
                    peer = host.peers[peerIdx];
                    if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) || peer.outgoingBandwidthThrottleEpoch == timeCurrent)
                    {
                        continue;
                    }

                    peer.packetThrottleLimit = throttle;

                    if (peer.packetThrottle > peer.packetThrottleLimit)
                    {
                        peer.packetThrottle = peer.packetThrottleLimit;
                    }

                    peer.incomingDataTotal = 0;
                    peer.outgoingDataTotal = 0;
                }
            }

            if (0 != host.recalculateBandwidthLimits)
            {
                host.recalculateBandwidthLimits = 0;

                peersRemaining = (uint)host.connectedPeers;
                bandwidth = host.incomingBandwidth;
                needsAdjustment = 1;

                if (bandwidth == 0)
                {
                    bandwidthLimit = 0;
                }
                else
                {
                    while (peersRemaining > 0 && needsAdjustment != 0)
                    {
                        needsAdjustment = 0;
                        bandwidthLimit = bandwidth / peersRemaining;

                        for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                        {
                            peer = host.peers[peerIdx];
                            if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) ||
                                peer.incomingBandwidthThrottleEpoch == timeCurrent
                               )
                            {
                                continue;
                            }

                            if (peer.outgoingBandwidth > 0 && peer.outgoingBandwidth >= bandwidthLimit)
                            {
                                continue;
                            }

                            peer.incomingBandwidthThrottleEpoch = timeCurrent;

                            needsAdjustment = 1;
                            --peersRemaining;
                            bandwidth -= peer.outgoingBandwidth;
                        }
                    }
                }

                for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                {
                    peer = host.peers[peerIdx];
                    if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
                    {
                        continue;
                    }

                    ENetProtocol command = new ENetProtocol();
                    command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                    command.header.channelID = 0xFF;
                    command.bandwidthLimit.outgoingBandwidth = ENET_HOST_TO_NET_32((uint)host.outgoingBandwidth);

                    if (peer.incomingBandwidthThrottleEpoch == timeCurrent)
                    {
                        command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32((uint)peer.outgoingBandwidth);
                    }
                    else
                    {
                        command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32((uint)bandwidthLimit);
                    }

                    enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
                }
            }
        } /* enet_host_bandwidth_throttle */

        /** Checks for any queued events on the host and dispatches one if available.
         *
         *  @param host    host to check for events
         *  @param event   an event structure where event details will be placed if available
         *  @retval > 0 if an event was dispatched
         *  @retval 0 if no events are available
         *  @retval < 0 on failure
         *  @ingroup host
         */
        public static int enet_host_check_events(ENetHost host, ENetEvent @event)
        {
            if (@event == null)
            {
                return -1;
            }

            @event.type = ENetEventType.ENET_EVENT_TYPE_NONE;
            @event.peer = null;
            @event.packet = null;

            return enet_protocol_dispatch_incoming_commands(host, @event);
        }

        /** Waits for events on the host specified and shuttles packets between
         *  the host and its peers.
         *
         *  @param host    host to service
         *  @param event   an event structure where event details will be placed if one occurs
         *                 if event == null then no events will be delivered
         *  @param timeout number of milliseconds that ENet should wait for events
         *  @retval > 0 if an event occurred within the specified time limit
         *  @retval 0 if no event occurred
         *  @retval < 0 on failure
         *  @remarks enet_host_service should be called fairly regularly for adequate performance
         *  @ingroup host
         */
        public static int enet_host_service(ENetHost host, ENetEvent @event, long timeout)
        {
            uint waitCondition;

            if (@event != null)
            {
                @event.type = ENetEventType.ENET_EVENT_TYPE_NONE;
                @event.peer = null;
                @event.packet = null;

                switch (enet_protocol_dispatch_incoming_commands(host, @event))
                {
                    case 1:
                        return 1;

                    case -1:
#if DEBUG
                        perror("Error dispatching incoming packets");
#endif

                        return -1;

                    default:
                        break;
                }
            }

            host.serviceTime = enet_time_get();
            timeout += host.serviceTime;

            do
            {
                if (ENET_TIME_DIFFERENCE(host.serviceTime, host.bandwidthThrottleEpoch) >= ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL)
                {
                    enet_host_bandwidth_throttle(host);
                }

                switch (enet_protocol_send_outgoing_commands(host, @event, 1))
                {
                    case 1:
                        return 1;

                    case -1:
#if DEBUG
                        perror("Error sending outgoing packets");
#endif

                        return -1;

                    default:
                        break;
                }

                switch (enet_protocol_receive_incoming_commands(host, @event))
                {
                    case 1:
                        return 1;

                    case -1:
#if DEBUG
                        perror("Error receiving incoming packets");
#endif

                        return -1;

                    default:
                        break;
                }

                switch (enet_protocol_send_outgoing_commands(host, @event, 1))
                {
                    case 1:
                        return 1;

                    case -1:
#if DEBUG
                        perror("Error sending outgoing packets");
#endif

                        return -1;

                    default:
                        break;
                }

                if (@event != null)
                {
                    switch (enet_protocol_dispatch_incoming_commands(host, @event))
                    {
                        case 1:
                            return 1;

                        case -1:
#if DEBUG
                            perror("Error dispatching incoming packets");
#endif

                            return -1;

                        default:
                            break;
                    }
                }

                if (ENET_TIME_GREATER_EQUAL(host.serviceTime, timeout))
                {
                    return 0;
                }

                do
                {
                    host.serviceTime = enet_time_get();

                    if (ENET_TIME_GREATER_EQUAL(host.serviceTime, timeout))
                    {
                        return 0;
                    }

                    waitCondition = (uint)ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE | (uint)ENetSocketWait.ENET_SOCKET_WAIT_INTERRUPT;
                    if (enet_socket_wait(host.socket, ref waitCondition, ENET_TIME_DIFFERENCE(timeout, host.serviceTime)) != 0)
                    {
                        return -1;
                    }
                } while (0 != (waitCondition & (uint)ENetSocketWait.ENET_SOCKET_WAIT_INTERRUPT));

                host.serviceTime = enet_time_get();
            } while (0 != (waitCondition & (uint)ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE));

            return 0;
        } /* enet_host_service */

        public static long enet_host_random_seed()
        {
            return DateTime.UtcNow.Ticks;
        }

        /** Sends any queued packets on the host specified to its designated peers.
        *
        *  @param host   host to flush
        *  @remarks this function need only be used in circumstances where one wishes to send queued packets earlier than in a call to enet_host_service().
        *  @ingroup host
        */
        public static void enet_host_flush(ENetHost host)
        {
            host.serviceTime = enet_time_get();
            enet_protocol_send_outgoing_commands(host, null, 0);
        }
    }
}