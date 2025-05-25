using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static ENet.NET.ENets;
using static ENet.NET.ENetAddresses;
using static ENet.NET.ENetHosts;
using static ENet.NET.ENetProtocols;
using static ENet.NET.ENetDelegates;


namespace ENet.NET
{
    public static class ENetPeers
    {
        // =======================================================================//
        // !
        // ! Peer
        // !
        // =======================================================================//

        /** Configures throttle parameter for a peer.
         *
         *  Unreliable packets are dropped by ENet in response to the varying conditions
         *  of the Internet connection to the peer.  The throttle represents a probability
         *  that an unreliable packet should not be dropped and thus sent by ENet to the peer.
         *  The lowest mean round trip time from the sending of a reliable packet to the
         *  receipt of its acknowledgement is measured over an amount of time specified by
         *  the interval parameter in milliseconds.  If a measured round trip time happens to
         *  be significantly less than the mean round trip time measured over the interval,
         *  then the throttle probability is increased to allow more traffic by an amount
         *  specified in the acceleration parameter, which is a ratio to the ENET_PEER_PACKET_THROTTLE_SCALE
         *  constant.  If a measured round trip time happens to be significantly greater than
         *  the mean round trip time measured over the interval, then the throttle probability
         *  is decreased to limit traffic by an amount specified in the deceleration parameter, which
         *  is a ratio to the ENET_PEER_PACKET_THROTTLE_SCALE constant.  When the throttle has
         *  a value of ENET_PEER_PACKET_THROTTLE_SCALE, no unreliable packets are dropped by
         *  ENet, and so 100% of all unreliable packets will be sent.  When the throttle has a
         *  value of 0, all unreliable packets are dropped by ENet, and so 0% of all unreliable
         *  packets will be sent.  Intermediate values for the throttle represent intermediate
         *  probabilities between 0% and 100% of unreliable packets being sent.  The bandwidth
         *  limits of the local and foreign hosts are taken into account to determine a
         *  sensible limit for the throttle probability above which it should not raise even in
         *  the best of conditions.
         *
         *  @param peer peer to configure
         *  @param interval interval, in milliseconds, over which to measure lowest mean RTT; the default value is ENET_PEER_PACKET_THROTTLE_INTERVAL.
         *  @param acceleration rate at which to increase the throttle probability as mean RTT declines
         *  @param deceleration rate at which to decrease the throttle probability as mean RTT increases
         */
        public static void enet_peer_throttle_configure(ENetPeer peer, uint interval, uint acceleration, uint deceleration)
        {
            ENetProtocol command = new ENetProtocol();

            peer.packetThrottleInterval = interval;
            peer.packetThrottleAcceleration = acceleration;
            peer.packetThrottleDeceleration = deceleration;

            command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            command.header.channelID = 0xFF;

            command.throttleConfigure.packetThrottleInterval = ENET_HOST_TO_NET_32(interval);
            command.throttleConfigure.packetThrottleAcceleration = ENET_HOST_TO_NET_32(acceleration);
            command.throttleConfigure.packetThrottleDeceleration = ENET_HOST_TO_NET_32(deceleration);

            enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
        }

        public static int enet_peer_throttle(ENetPeer peer, uint rtt)
        {
            if (peer.lastRoundTripTime <= peer.lastRoundTripTimeVariance)
            {
                peer.packetThrottle = peer.packetThrottleLimit;
            }
            else if (rtt <= peer.lastRoundTripTime)
            {
                peer.packetThrottle += peer.packetThrottleAcceleration;

                if (peer.packetThrottle > peer.packetThrottleLimit)
                {
                    peer.packetThrottle = peer.packetThrottleLimit;
                }

                return 1;
            }
            else if (rtt > peer.lastRoundTripTime + 2 * peer.lastRoundTripTimeVariance)
            {
                if (peer.packetThrottle > peer.packetThrottleDeceleration)
                {
                    peer.packetThrottle -= peer.packetThrottleDeceleration;
                }
                else
                {
                    peer.packetThrottle = 0;
                }

                return -1;
            }

            return 0;
        }

        /* Extended functionality for easier binding in other programming languages */
        public static long enet_host_get_peers_count(ENetHost host)
        {
            return host.connectedPeers;
        }

        public static long enet_host_get_packets_sent(ENetHost host)
        {
            return host.totalSentPackets;
        }

        public static long enet_host_get_packets_received(ENetHost host)
        {
            return host.totalReceivedPackets;
        }

        public static long enet_host_get_bytes_sent(ENetHost host)
        {
            return host.totalSentData;
        }

        public static long enet_host_get_bytes_received(ENetHost host)
        {
            return host.totalReceivedData;
        }

        /** Gets received data buffer. Returns buffer length.
         *  @param host host to access recevie buffer
         *  @param data ouput parameter for recevied data
         *  @retval buffer length
         */
        public static long enet_host_get_received_data(ENetHost host, /*out*/ ref ArraySegment<byte> data)
        {
            data = host.receivedData;
            return host.receivedDataLength;
        }

        public static int enet_host_get_mtu(ENetHost host)
        {
            return host.mtu;
        }

        public static uint enet_peer_get_id(ENetPeer peer)
        {
            return peer.connectID;
        }

        public static int enet_peer_get_ip(ENetPeer peer, out string ip)
        {
            return enet_address_get_host_ip(ref peer.address, out ip);
        }

        public static ushort enet_peer_get_port(ENetPeer peer)
        {
            return peer.address.port;
        }

        public static ENetPeerState enet_peer_get_state(ENetPeer peer)
        {
            return peer.state;
        }

        public static long enet_peer_get_rtt(ENetPeer peer)
        {
            return peer.roundTripTime;
        }

        public static long enet_peer_get_packets_sent(ENetPeer peer)
        {
            return peer.totalPacketsSent;
        }

        public static long enet_peer_get_packets_lost(ENetPeer peer)
        {
            return peer.totalPacketsLost;
        }

        public static long enet_peer_get_bytes_sent(ENetPeer peer)
        {
            return peer.totalDataSent;
        }

        public static long enet_peer_get_bytes_received(ENetPeer peer)
        {
            return peer.totalDataReceived;
        }

        public static object enet_peer_get_data(ENetPeer peer)
        {
            return peer.data;
        }

        public static void enet_peer_set_data(ENetPeer peer, object data)
        {
            peer.data = data;
        }

        public static object enet_packet_get_data(ENetPacket packet)
        {
            return packet.data;
        }

        public static long enet_packet_get_length(ENetPacket packet)
        {
            return packet.dataLength;
        }

        public static void enet_packet_set_free_callback(ENetPacket packet, ENetPacketFreeCallback callback)
        {
            packet.freeCallback = callback;
        }

        /** Queues a packet to be sent.
         *  On success, ENet will assume ownership of the packet, and so enet_packet_destroy
         *  should not be called on it thereafter. On failure, the caller still must destroy
         *  the packet on its own as ENet has not queued the packet. The caller can also
         *  check the packet's referenceCount field after sending to check if ENet queued
         *  the packet and thus incremented the referenceCount.
         *  @param peer destination for the packet
         *  @param channelID channel on which to send
         *  @param packet packet to send
         *  @retval 0 on success
         *  @retval < 0 on failure
         */
        public static int enet_peer_send(ENetPeer peer, byte channelID, ENetPacket packet)
        {
            ENetChannel channel = peer.channels[channelID];

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED || channelID >= peer.channelCount || packet.dataLength > peer.host.maximumPacketSize)
            {
                return -1;
            }

            long fragmentLength = peer.mtu - Marshal.SizeOf<ENetProtocolHeader>() - Marshal.SizeOf<ENetProtocolSendFragment>();
            if (peer.host.checksum != null)
            {
                fragmentLength -= Marshal.SizeOf<uint>();
            }

            if (packet.dataLength > fragmentLength)
            {
                long fragmentCount = (packet.dataLength + fragmentLength - 1) / fragmentLength, fragmentNumber, fragmentOffset;
                byte commandNumber;
                ushort startSequenceNumber;
                LinkedList<ENetOutgoingCommand> fragments = new LinkedList<ENetOutgoingCommand>();
                ENetOutgoingCommand fragment;

                if (fragmentCount > ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT)
                {
                    return -1;
                }

                if ((packet.flags & (ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE | ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT)) ==
                    ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT &&
                    channel.outgoingUnreliableSequenceNumber < 0xFFFF)
                {
                    commandNumber = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT;
                    startSequenceNumber = ENET_HOST_TO_NET_16((ushort)(channel.outgoingUnreliableSequenceNumber + 1));
                }
                else
                {
                    commandNumber = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                    startSequenceNumber = ENET_HOST_TO_NET_16((ushort)(channel.outgoingReliableSequenceNumber + 1));
                }

                fragments.Clear();

                for (fragmentNumber = 0, fragmentOffset = 0; fragmentOffset < packet.dataLength; ++fragmentNumber, fragmentOffset += fragmentLength)
                {
                    if (packet.dataLength - fragmentOffset < fragmentLength)
                    {
                        fragmentLength = packet.dataLength - fragmentOffset;
                    }

                    fragment = enet_malloc<ENetOutgoingCommand>(1)[0];

                    if (fragment == null)
                    {
                        while (!fragments.IsEmpty())
                        {
                            fragment = fragments.First.RemoveAndGet();

                            enet_free(fragment);
                        }

                        return -1;
                    }

                    fragment.fragmentOffset = (int)fragmentOffset;
                    fragment.fragmentLength = (ushort)fragmentLength;
                    fragment.packet = packet;
                    fragment.command.header.command = commandNumber;
                    fragment.command.header.channelID = channelID;

                    fragment.command.sendFragment.startSequenceNumber = startSequenceNumber;

                    fragment.command.sendFragment.dataLength = ENET_HOST_TO_NET_16((ushort)fragmentLength);
                    fragment.command.sendFragment.fragmentCount = ENET_HOST_TO_NET_32((ushort)fragmentCount);
                    fragment.command.sendFragment.fragmentNumber = ENET_HOST_TO_NET_32((ushort)fragmentNumber);
                    fragment.command.sendFragment.totalLength = ENET_HOST_TO_NET_32((uint)packet.dataLength);
                    fragment.command.sendFragment.fragmentOffset = ENET_NET_TO_HOST_32((ushort)fragmentOffset);

                    fragments.AddLast(fragment);
                }

                packet.referenceCount += fragmentNumber;

                while (!fragments.IsEmpty())
                {
                    fragment = fragments.First.RemoveAndGet();
                    enet_peer_setup_outgoing_command(peer, fragment);
                }

                return 0;
            }

            ENetProtocol command = new ENetProtocol();
            command.header.channelID = channelID;

            if ((packet.flags & (ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE | ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED)) == ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED)
            {
                command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
                command.sendUnsequenced.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
            }
            else if (0 != (packet.flags & ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE) || channel.outgoingUnreliableSequenceNumber >= 0xFFFF)
            {
                command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                command.sendReliable.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
            }
            else
            {
                command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE;
                command.sendUnreliable.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
            }

            if (enet_peer_queue_outgoing_command(peer, ref command, packet, 0, packet.dataLength) == null)
            {
                return -1;
            }

            return 0;
        } // enet_peer_send

        /** Attempts to dequeue any incoming queued packet.
         *  @param peer peer to dequeue packets from
         *  @param channelID holds the channel ID of the channel the packet was received on success
         *  @returns a pointer to the packet, or null if there are no available incoming queued packets
         */
        public static ENetPacket enet_peer_receive(ENetPeer peer, ref byte channelID)
        {
            ENetIncomingCommand incomingCommand;
            ENetPacket packet;

            if (peer.dispatchedCommands.IsEmpty())
            {
                return null;
            }

            incomingCommand = peer.dispatchedCommands.First.RemoveAndGet();

            //if (channelID != null) {
            channelID = incomingCommand.command.header.channelID;
            //}

            packet = incomingCommand.packet;
            --packet.referenceCount;

            if (incomingCommand.fragments != null)
            {
                enet_free(incomingCommand.fragments);
            }

            enet_free(incomingCommand);
            peer.totalWaitingData -= Math.Min(peer.totalWaitingData, packet.dataLength);

            return packet;
        }

        public static void enet_peer_reset_outgoing_commands(ENetPeer peer, LinkedList<ENetOutgoingCommand> queue)
        {
            ENetOutgoingCommand outgoingCommand;

            while (!queue.IsEmpty())
            {
                outgoingCommand = queue.First.RemoveAndGet();

                if (outgoingCommand.packet != null)
                {
                    --outgoingCommand.packet.referenceCount;

                    if (outgoingCommand.packet.referenceCount == 0)
                    {
                        callbacks.packet_destroy(outgoingCommand.packet);
                    }
                }

                enet_free(outgoingCommand);
            }
        }

        public static void enet_peer_remove_incoming_commands(ENetPeer peer, LinkedList<ENetIncomingCommand> queue, LinkedListNode<ENetIncomingCommand> startCommand, LinkedListNode<ENetIncomingCommand> endCommand, ENetIncomingCommand excludeCommand)
        {
            ENET_UNUSED(queue);

            LinkedListNode<ENetIncomingCommand> currentCommand;

            for (currentCommand = startCommand; currentCommand != endCommand;)
            {
                ENetIncomingCommand incomingCommand = currentCommand.Value;

                currentCommand = currentCommand.Next;

                if (incomingCommand == excludeCommand)
                    continue;

                incomingCommand.incomingCommandList.RemoveAndGet();

                if (incomingCommand.packet != null)
                {
                    --incomingCommand.packet.referenceCount;

                    peer.totalWaitingData -= Math.Min(peer.totalWaitingData, incomingCommand.packet.dataLength);

                    if (incomingCommand.packet.referenceCount == 0)
                    {
                        callbacks.packet_destroy(incomingCommand.packet);
                    }
                }

                if (incomingCommand.fragments != null)
                {
                    enet_free(incomingCommand.fragments);
                }

                enet_free(incomingCommand);
            }
        }

        public static void enet_peer_reset_incoming_commands(ENetPeer peer, LinkedList<ENetIncomingCommand> queue)
        {
            enet_peer_remove_incoming_commands(peer, queue, queue.First, queue.Last, null);
        }

        public static void enet_peer_reset_queues(ENetPeer peer)
        {
            if (0 != (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
            {
                peer.dispatchList.RemoveAndGet();
                peer.flags = (ushort)(peer.flags & ~(ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH);
            }

            while (!peer.acknowledgements.IsEmpty())
            {
                enet_free(peer.acknowledgements.First.RemoveAndGet());
            }

            enet_peer_reset_outgoing_commands(peer, peer.sentReliableCommands);
            enet_peer_reset_outgoing_commands(peer, peer.outgoingCommands);
            enet_peer_reset_outgoing_commands(peer, peer.outgoingSendReliableCommands);
            enet_peer_reset_incoming_commands(peer, peer.dispatchedCommands);

            if (peer.channels != null && peer.channelCount > 0)
            {
                for (int channel = 0; channel < peer.channelCount; ++channel)
                {
                    enet_peer_reset_incoming_commands(peer, peer.channels[channel].incomingReliableCommands);
                    enet_peer_reset_incoming_commands(peer, peer.channels[channel].incomingUnreliableCommands);
                }

                enet_free(peer.channels);
            }

            peer.channels = null;
            peer.channelCount = 0;
        }

        public static void enet_peer_on_connect(ENetPeer peer)
        {
            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                if (peer.incomingBandwidth != 0)
                {
                    ++peer.host.bandwidthLimitedPeers;
                }

                ++peer.host.connectedPeers;
            }
        }

        public static void enet_peer_on_disconnect(ENetPeer peer)
        {
            if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                if (peer.incomingBandwidth != 0)
                {
                    --peer.host.bandwidthLimitedPeers;
                }

                --peer.host.connectedPeers;
            }
        }

        /** Forcefully disconnects a peer.
         *  @param peer peer to forcefully disconnect
         *  @remarks The foreign host represented by the peer is not notified of the disconnection and will timeout
         *  on its connection to the local host.
         */
        public static void enet_peer_reset(ENetPeer peer)
        {
            enet_peer_on_disconnect(peer);

            // We don't want to reset connectID here, otherwise, we can't get it in the Disconnect event
            // peer.connectID                     = 0;
            peer.outgoingPeerID = ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID;
            peer.state = ENetPeerState.ENET_PEER_STATE_DISCONNECTED;
            peer.incomingBandwidth = 0;
            peer.outgoingBandwidth = 0;
            peer.incomingBandwidthThrottleEpoch = 0;
            peer.outgoingBandwidthThrottleEpoch = 0;
            peer.incomingDataTotal = 0;
            peer.totalDataReceived = 0;
            peer.outgoingDataTotal = 0;
            peer.totalDataSent = 0;
            peer.lastSendTime = 0;
            peer.lastReceiveTime = 0;
            peer.nextTimeout = 0;
            peer.earliestTimeout = 0;
            peer.packetLossEpoch = 0;
            peer.packetsSent = 0;
            peer.totalPacketsSent = 0;
            peer.packetsLost = 0;
            peer.totalPacketsLost = 0;
            peer.packetLoss = 0;
            peer.packetLossVariance = 0;
            peer.packetThrottle = ENets.ENET_PEER_DEFAULT_PACKET_THROTTLE;
            peer.packetThrottleLimit = ENets.ENET_PEER_PACKET_THROTTLE_SCALE;
            peer.packetThrottleCounter = 0;
            peer.packetThrottleEpoch = 0;
            peer.packetThrottleAcceleration = ENets.ENET_PEER_PACKET_THROTTLE_ACCELERATION;
            peer.packetThrottleDeceleration = ENets.ENET_PEER_PACKET_THROTTLE_DECELERATION;
            peer.packetThrottleInterval = ENets.ENET_PEER_PACKET_THROTTLE_INTERVAL;
            peer.pingInterval = ENets.ENET_PEER_PING_INTERVAL;
            peer.timeoutLimit = ENets.ENET_PEER_TIMEOUT_LIMIT;
            peer.timeoutMinimum = ENets.ENET_PEER_TIMEOUT_MINIMUM;
            peer.timeoutMaximum = ENets.ENET_PEER_TIMEOUT_MAXIMUM;
            peer.lastRoundTripTime = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
            peer.lowestRoundTripTime = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
            peer.lastRoundTripTimeVariance = 0;
            peer.highestRoundTripTimeVariance = 0;
            peer.roundTripTime = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
            peer.roundTripTimeVariance = 0;
            peer.mtu = peer.host.mtu;
            peer.reliableDataInTransit = 0;
            peer.outgoingReliableSequenceNumber = 0;
            peer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            peer.incomingUnsequencedGroup = 0;
            peer.outgoingUnsequencedGroup = 0;
            peer.eventData = 0;
            peer.totalWaitingData = 0;
            peer.flags = 0;

            //memset(peer.unsequencedWindow, 0, sizeof(peer.unsequencedWindow));
            Array.Fill(peer.unsequencedWindow, 0u);
            enet_peer_reset_queues(peer);
        }

        /** Sends a ping request to a peer.
         *  @param peer destination for the ping request
         *  @remarks ping requests factor into the mean round trip time as designated by the
         *  roundTripTime field in the ENetPeer structure.  ENet automatically pings all connected
         *  peers at regular intervals, however, this function may be called to ensure more
         *  frequent ping requests.
         */
        public static void enet_peer_ping(ENetPeer peer)
        {
            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED)
            {
                return;
            }

            ENetProtocol command = new ENetProtocol();
            command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_PING | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            command.header.channelID = 0xFF;

            enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
        }

        /** Sets the interval at which pings will be sent to a peer.
         *
         *  Pings are used both to monitor the liveness of the connection and also to dynamically
         *  adjust the throttle during periods of low traffic so that the throttle has reasonable
         *  responsiveness during traffic spikes.
         *
         *  @param peer the peer to adjust
         *  @param pingInterval the interval at which to send pings; defaults to ENET_PEER_PING_INTERVAL if 0
         */
        public static void enet_peer_ping_interval(ENetPeer peer, uint pingInterval)
        {
            peer.pingInterval = 0 >= pingInterval ? pingInterval : ENET_PEER_PING_INTERVAL;
        }

        /** Sets the timeout parameters for a peer.
         *
         *  The timeout parameter control how and when a peer will timeout from a failure to acknowledge
         *  reliable traffic. Timeout values use an exponential backoff mechanism, where if a reliable
         *  packet is not acknowledge within some multiple of the average RTT plus a variance tolerance,
         *  the timeout will be doubled until it reaches a set limit. If the timeout is thus at this
         *  limit and reliable packets have been sent but not acknowledged within a certain minimum time
         *  period, the peer will be disconnected. Alternatively, if reliable packets have been sent
         *  but not acknowledged for a certain maximum time period, the peer will be disconnected regardless
         *  of the current timeout limit value.
         *
         *  @param peer the peer to adjust
         *  @param timeoutLimit the timeout limit; defaults to ENET_PEER_TIMEOUT_LIMIT if 0
         *  @param timeoutMinimum the timeout minimum; defaults to ENET_PEER_TIMEOUT_MINIMUM if 0
         *  @param timeoutMaximum the timeout maximum; defaults to ENET_PEER_TIMEOUT_MAXIMUM if 0
         */
        public static void enet_peer_timeout(ENetPeer peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum)
        {
            peer.timeoutLimit = 0 < timeoutLimit ? timeoutLimit : (uint)ENets.ENET_PEER_TIMEOUT_LIMIT;
            peer.timeoutMinimum = 0 < timeoutMinimum ? timeoutMinimum : (uint)ENets.ENET_PEER_TIMEOUT_MINIMUM;
            peer.timeoutMaximum = 0 < timeoutMaximum ? timeoutMaximum : (uint)ENets.ENET_PEER_TIMEOUT_MAXIMUM;
        }

        /** Force an immediate disconnection from a peer.
         *  @param peer peer to disconnect
         *  @param data data describing the disconnection
         *  @remarks No ENET_EVENT_DISCONNECT event will be generated. The foreign peer is not
         *  guaranteed to receive the disconnect notification, and is reset immediately upon
         *  return from this function.
         */
        public static void enet_peer_disconnect_now(ENetPeer peer, uint data)
        {
            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED)
            {
                return;
            }

            if (peer.state != ENetPeerState.ENET_PEER_STATE_ZOMBIE && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECTING)
            {
                enet_peer_reset_queues(peer);

                ENetProtocol command = new ENetProtocol();
                command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
                command.header.channelID = 0xFF;
                command.disconnect.data = ENET_HOST_TO_NET_32(data);

                enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
                enet_host_flush(peer.host);
            }

            enet_peer_reset(peer);
        }

        /** Request a disconnection from a peer.
         *  @param peer peer to request a disconnection
         *  @param data data describing the disconnection
         *  @remarks An ENET_EVENT_DISCONNECT event will be generated by enet_host_service()
         *  once the disconnection is complete.
         */
        public static void enet_peer_disconnect(ENetPeer peer, uint data)
        {
            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTING ||
                peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED ||
                peer.state == ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT ||
                peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE
               )
            {
                return;
            }

            enet_peer_reset_queues(peer);

            ENetProtocol command = new ENetProtocol();
            command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT;
            command.header.channelID = 0xFF;
            command.disconnect.data = ENET_HOST_TO_NET_32(data);

            if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                command.header.command |= ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            }
            else
            {
                command.header.command |= ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
            }

            enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);

            if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                enet_peer_on_disconnect(peer);

                peer.state = ENetPeerState.ENET_PEER_STATE_DISCONNECTING;
            }
            else
            {
                enet_host_flush(peer.host);
                enet_peer_reset(peer);
            }
        }

        public static bool enet_peer_has_outgoing_commands(ENetPeer peer)
        {
            if (peer.outgoingCommands.IsEmpty() &&
                peer.outgoingSendReliableCommands.IsEmpty() &&
                peer.sentReliableCommands.IsEmpty())
            {
                return false;
            }

            return true;
        }

        /** Request a disconnection from a peer, but only after all queued outgoing packets are sent.
         *  @param peer peer to request a disconnection
         *  @param data data describing the disconnection
         *  @remarks An ENET_EVENT_DISCONNECT event will be generated by enet_host_service()
         *  once the disconnection is complete.
         */
        public static void enet_peer_disconnect_later(ENetPeer peer, uint data)
        {
            if ((peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) &&
                enet_peer_has_outgoing_commands(peer)
               )
            {
                peer.state = ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER;
                peer.eventData = data;
            }
            else
            {
                enet_peer_disconnect(peer, data);
            }
        }

        public static ENetAcknowledgement enet_peer_queue_acknowledgement(ENetPeer peer, ref ENetProtocol command, ushort sentTime)
        {
            if (command.header.channelID < peer.channelCount)
            {
                ENetChannel channel = peer.channels[command.header.channelID];
                ushort reliableWindow = (ushort)(command.header.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);
                ushort currentWindow = (ushort)(channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);

                if (command.header.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
                {
                    reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
                }

                if (reliableWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1 && reliableWindow <= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS)
                {
                    return null;
                }
            }

            ENetAcknowledgement acknowledgement = enet_malloc<ENetAcknowledgement>(1)[0];
            if (acknowledgement == null)
            {
                return null;
            }

            peer.outgoingDataTotal += (uint)Marshal.SizeOf<ENetProtocolAcknowledge>();

            acknowledgement.sentTime = sentTime;
            acknowledgement.command = command;

            peer.acknowledgements.AddLast(acknowledgement);
            return acknowledgement;
        }

        public static void enet_peer_setup_outgoing_command(ENetPeer peer, ENetOutgoingCommand outgoingCommand)
        {
            ENetChannel channel = null;
            if (outgoingCommand.command.header.channelID < peer.channelCount)
            {
                channel = peer.channels[outgoingCommand.command.header.channelID];
            }

            peer.outgoingDataTotal += enet_protocol_command_size(outgoingCommand.command.header.command) + outgoingCommand.fragmentLength;

            if (outgoingCommand.command.header.channelID == 0xFF)
            {
                ++peer.outgoingReliableSequenceNumber;

                outgoingCommand.reliableSequenceNumber = peer.outgoingReliableSequenceNumber;
                outgoingCommand.unreliableSequenceNumber = 0;
            }
            else if (0 != (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
            {
                ++channel.outgoingReliableSequenceNumber;
                channel.outgoingUnreliableSequenceNumber = 0;

                outgoingCommand.reliableSequenceNumber = channel.outgoingReliableSequenceNumber;
                outgoingCommand.unreliableSequenceNumber = 0;
            }
            else if (0 != (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED))
            {
                ++peer.outgoingUnsequencedGroup;

                outgoingCommand.reliableSequenceNumber = 0;
                outgoingCommand.unreliableSequenceNumber = 0;
            }
            else
            {
                if (outgoingCommand.fragmentOffset == 0)
                {
                    ++channel.outgoingUnreliableSequenceNumber;
                }

                outgoingCommand.reliableSequenceNumber = channel.outgoingReliableSequenceNumber;
                outgoingCommand.unreliableSequenceNumber = channel.outgoingUnreliableSequenceNumber;
            }

            outgoingCommand.sendAttempts = 0;
            outgoingCommand.sentTime = 0;
            outgoingCommand.roundTripTimeout = 0;
            outgoingCommand.command.header.reliableSequenceNumber = ENET_HOST_TO_NET_16(outgoingCommand.reliableSequenceNumber);
            outgoingCommand.queueTime = ++peer.host.totalQueued;

            switch (outgoingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK)
            {
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                    outgoingCommand.command.sendUnreliable.unreliableSequenceNumber = ENET_HOST_TO_NET_16(outgoingCommand.unreliableSequenceNumber);
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                    outgoingCommand.command.sendUnsequenced.unsequencedGroup = ENET_HOST_TO_NET_16(peer.outgoingUnsequencedGroup);
                    break;

                default:
                    break;
            }

            if ((outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0 && outgoingCommand.packet != null)
            {
                peer.outgoingSendReliableCommands.AddLast(outgoingCommand);
            }
            else
            {
                peer.outgoingCommands.AddLast(outgoingCommand);
            }
        }

        public static ENetOutgoingCommand enet_peer_queue_outgoing_command(ENetPeer peer, ref ENetProtocol command, ENetPacket packet, long offset, long length)
        {
            ENetOutgoingCommand outgoingCommand = enet_malloc<ENetOutgoingCommand>(1)[0];

            if (outgoingCommand == null)
            {
                return null;
            }

            outgoingCommand.command = command;
            outgoingCommand.fragmentOffset = (int)offset;
            outgoingCommand.fragmentLength = (ushort)length;
            outgoingCommand.packet = packet;
            if (packet != null)
            {
                ++packet.referenceCount;
            }

            enet_peer_setup_outgoing_command(peer, outgoingCommand);
            return outgoingCommand;
        }

        public static void enet_peer_dispatch_incoming_unreliable_commands(ENetPeer peer, ENetChannel channel, ENetIncomingCommand queuedCommand)
        {
            LinkedListNode<ENetIncomingCommand> droppedCommand, startCommand, currentCommand = null;

            for (droppedCommand = startCommand = currentCommand = channel.incomingUnreliableCommands.First;
                 currentCommand != null;
                 currentCommand = currentCommand.Next
                )
            {
                ENetIncomingCommand incomingCommand = currentCommand.Value;

                if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED)
                {
                    continue;
                }

                if (incomingCommand.reliableSequenceNumber == channel.incomingReliableSequenceNumber)
                {
                    if (incomingCommand.fragmentsRemaining <= 0)
                    {
                        channel.incomingUnreliableSequenceNumber = incomingCommand.unreliableSequenceNumber;
                        continue;
                    }

                    if (startCommand != currentCommand)
                    {
                        peer.dispatchedCommands.Last.MoveBefore(startCommand, currentCommand.Previous);

                        if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
                        {
                            peer.host.dispatchQueue.AddLast(peer.dispatchList);
                            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                        }

                        droppedCommand = currentCommand;
                    }
                    else if (droppedCommand != currentCommand)
                    {
                        droppedCommand = currentCommand.Previous;
                    }
                }
                else
                {
                    ushort reliableWindow = (ushort)(incomingCommand.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);
                    ushort currentWindow = (ushort)(channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);

                    if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
                    {
                        reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
                    }

                    if (reliableWindow >= currentWindow && reliableWindow < currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1)
                    {
                        break;
                    }

                    droppedCommand = currentCommand.Next;

                    if (startCommand != currentCommand)
                    {
                        peer.dispatchedCommands.Last.MoveBefore(startCommand, currentCommand.Previous);

                        if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
                        {
                            peer.host.dispatchQueue.AddLast(peer.dispatchList);
                            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                        }
                    }
                }

                startCommand = currentCommand.Next;
            }

            if (startCommand != currentCommand)
            {
                peer.dispatchedCommands.Last.MoveBefore(startCommand, currentCommand.Previous);

                if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
                {
                    peer.host.dispatchQueue.AddLast(peer.dispatchList);
                    peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                }

                droppedCommand = currentCommand;
            }

            enet_peer_remove_incoming_commands(peer, channel.incomingUnreliableCommands, channel.incomingUnreliableCommands.First, droppedCommand, queuedCommand);
        }

        public static void enet_peer_dispatch_incoming_reliable_commands(ENetPeer peer, ENetChannel channel, ENetIncomingCommand queuedCommand)
        {
            LinkedListNode<ENetIncomingCommand> currentCommand;

            for (currentCommand = channel.incomingReliableCommands.First;
                 currentCommand != null;
                 currentCommand = currentCommand.Next
                )
            {
                ENetIncomingCommand incomingCommand = currentCommand.Value;

                if (incomingCommand.fragmentsRemaining > 0 || incomingCommand.reliableSequenceNumber != (ushort)(channel.incomingReliableSequenceNumber + 1))
                {
                    break;
                }

                channel.incomingReliableSequenceNumber = incomingCommand.reliableSequenceNumber;

                if (incomingCommand.fragmentCount > 0)
                {
                    channel.incomingReliableSequenceNumber += (ushort)(incomingCommand.fragmentCount - 1);
                }
            }

            if (currentCommand == channel.incomingReliableCommands.First)
            {
                return;
            }

            channel.incomingUnreliableSequenceNumber = 0;
            peer.dispatchedCommands.Last.MoveBefore(channel.incomingReliableCommands.First, currentCommand.Previous);

            if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
            {
                peer.host.dispatchQueue.AddLast(peer.dispatchList);
                peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
            }

            if (!channel.incomingUnreliableCommands.IsEmpty())
            {
                enet_peer_dispatch_incoming_unreliable_commands(peer, channel, queuedCommand);
            }
        }

        private static ENetIncomingCommand dummyCommand;

        public static ENetIncomingCommand enet_peer_queue_incoming_command(ENetPeer peer, ref ENetProtocol command, ArraySegment<byte> data, int dataLength, uint flags, int fragmentCount)
        {
            ENetChannel channel = peer.channels[command.header.channelID];
            int unreliableSequenceNumber = 0, reliableSequenceNumber = 0;
            int reliableWindow, currentWindow;
            ENetIncomingCommand incomingCommand = null;
            LinkedListNode<ENetIncomingCommand> currentCommand;
            ENetPacket packet = null;

            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                goto discardCommand;
            }

            if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED)
            {
                reliableSequenceNumber = command.header.reliableSequenceNumber;
                reliableWindow = reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
                currentWindow = channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;

                if (reliableSequenceNumber < channel.incomingReliableSequenceNumber)
                {
                    reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
                }

                if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1)
                {
                    goto discardCommand;
                }
            }

            switch (command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK)
            {
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                    if (reliableSequenceNumber == channel.incomingReliableSequenceNumber)
                    {
                        goto discardCommand;
                    }

                    for (currentCommand = channel.incomingReliableCommands.Last;
                         currentCommand != null;
                         currentCommand = currentCommand.Previous
                        )
                    {
                        incomingCommand = currentCommand.Value;

                        if (reliableSequenceNumber >= channel.incomingReliableSequenceNumber)
                        {
                            if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
                            {
                                continue;
                            }
                        }
                        else if (incomingCommand.reliableSequenceNumber >= channel.incomingReliableSequenceNumber)
                        {
                            break;
                        }

                        if (incomingCommand.reliableSequenceNumber <= reliableSequenceNumber)
                        {
                            if (incomingCommand.reliableSequenceNumber < reliableSequenceNumber)
                            {
                                break;
                            }

                            goto discardCommand;
                        }
                    }

                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                    unreliableSequenceNumber = ENET_NET_TO_HOST_16(command.sendUnreliable.unreliableSequenceNumber);

                    if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber)
                    {
                        goto discardCommand;
                    }

                    for (currentCommand = channel.incomingUnreliableCommands.Last;
                         currentCommand != null;
                         currentCommand = currentCommand.Previous
                        )
                    {
                        incomingCommand = currentCommand.Value;

                        if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED)
                        {
                            continue;
                        }

                        if (reliableSequenceNumber >= channel.incomingReliableSequenceNumber)
                        {
                            if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber)
                            {
                                continue;
                            }
                        }
                        else if (incomingCommand.reliableSequenceNumber >= channel.incomingReliableSequenceNumber)
                        {
                            break;
                        }

                        if (incomingCommand.reliableSequenceNumber < reliableSequenceNumber)
                        {
                            break;
                        }

                        if (incomingCommand.reliableSequenceNumber > reliableSequenceNumber)
                        {
                            continue;
                        }

                        if (incomingCommand.unreliableSequenceNumber <= unreliableSequenceNumber)
                        {
                            if (incomingCommand.unreliableSequenceNumber < unreliableSequenceNumber)
                            {
                                break;
                            }

                            goto discardCommand;
                        }
                    }

                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                    currentCommand = null;
                    break;

                default:
                    goto discardCommand;
            }

            if (peer.totalWaitingData >= peer.host.maximumWaitingData)
            {
                goto notifyError;
            }

            packet = callbacks.packet_create(data, dataLength, flags);
            if (packet == null)
            {
                goto notifyError;
            }

            incomingCommand = enet_malloc<ENetIncomingCommand>(1)[0];
            if (incomingCommand == null)
            {
                goto notifyError;
            }

            incomingCommand.reliableSequenceNumber = command.header.reliableSequenceNumber;
            incomingCommand.unreliableSequenceNumber = (ushort)(unreliableSequenceNumber & 0xFFFF);
            incomingCommand.command = command;
            incomingCommand.fragmentCount = fragmentCount;
            incomingCommand.fragmentsRemaining = fragmentCount;
            incomingCommand.packet = packet;
            incomingCommand.fragments = null;

            if (fragmentCount > 0)
            {
                if (fragmentCount <= ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT)
                {
                    incomingCommand.fragments = enet_malloc<uint>((fragmentCount + 31) / 32);
                }

                if (incomingCommand.fragments == null)
                {
                    enet_free(incomingCommand);

                    goto notifyError;
                }

                Array.Fill(incomingCommand.fragments, 0u, 0, (int)((fragmentCount + 31) / 32 * sizeof(uint)));
            }

            enet_assert(packet != null);
            ++packet.referenceCount;
            peer.totalWaitingData += packet.dataLength;

            currentCommand.Next.AddAfter(incomingCommand);

            switch (command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK)
            {
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                    enet_peer_dispatch_incoming_reliable_commands(peer, channel, incomingCommand);
                    break;

                default:
                    enet_peer_dispatch_incoming_unreliable_commands(peer, channel, incomingCommand);
                    break;
            }

            return incomingCommand;

            discardCommand:
            if (fragmentCount > 0)
            {
                goto notifyError;
            }

            if (packet != null && packet.referenceCount == 0)
            {
                callbacks.packet_destroy(packet);
            }

            return dummyCommand;

            notifyError:
            if (packet != null && packet.referenceCount == 0)
            {
                callbacks.packet_destroy(packet);
            }

            return null;
        } /* enet_peer_queue_incoming_command */
    }
}