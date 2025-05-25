using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static ENet.NET.ENets;
using static ENet.NET.ENetPeers;
using static ENet.NET.ENetPackets;
using static ENet.NET.ENetSockets;
using static ENet.NET.ENetTimes;
using static ENet.NET.ENetAddresses;

namespace ENet.NET
{
    public static class ENetProtocols
    {
        // =======================================================================//
        // !
        // ! Protocol
        // !
        // =======================================================================//
        public static int[] commandSizes = new int[ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT]
        {
            0,
            Marshal.SizeOf<ENetProtocolAcknowledge>(),
            Marshal.SizeOf<ENetProtocolConnect>(),
            Marshal.SizeOf<ENetProtocolVerifyConnect>(),
            Marshal.SizeOf<ENetProtocolDisconnect>(),
            Marshal.SizeOf<ENetProtocolPing>(),
            Marshal.SizeOf<ENetProtocolSendReliable>(),
            Marshal.SizeOf<ENetProtocolSendUnreliable>(),
            Marshal.SizeOf<ENetProtocolSendFragment>(),
            Marshal.SizeOf<ENetProtocolSendUnsequenced>(),
            Marshal.SizeOf<ENetProtocolBandwidthLimit>(),
            Marshal.SizeOf<ENetProtocolThrottleConfigure>(),
            Marshal.SizeOf<ENetProtocolSendFragment>(),
        };

        public static int enet_protocol_command_size(byte commandNumber)
        {
            return commandSizes[commandNumber & (uint)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK];
        }

        public static void enet_protocol_change_state(ENetHost host, ENetPeer peer, ENetPeerState state)
        {
            ENET_UNUSED(host);

            if (state == ENetPeerState.ENET_PEER_STATE_CONNECTED || state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                enet_peer_on_connect(peer);
            }
            else
            {
                enet_peer_on_disconnect(peer);
            }

            peer.state = state;
        }

        public static void enet_protocol_dispatch_state(ENetHost host, ENetPeer peer, ENetPeerState state)
        {
            enet_protocol_change_state(host, peer, state);

            if (0 == (peer.flags & (uint)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH))
            {
                host.dispatchQueue.AddLast(peer.dispatchList);
                peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
            }
        }

        public static int enet_protocol_dispatch_incoming_commands(ENetHost host, ENetEvent @event)
        {
            while (!host.dispatchQueue.IsEmpty())
            {
                ENetPeer peer = host.dispatchQueue.First.RemoveAndGet();
                peer.flags = (ushort)(peer.flags & ~(ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH);

                switch (peer.state)
                {
                    case ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING:
                    case ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED:
                        enet_protocol_change_state(host, peer, ENetPeerState.ENET_PEER_STATE_CONNECTED);

                        @event.type = ENetEventType.ENET_EVENT_TYPE_CONNECT;
                        @event.peer = peer;
                        @event.data = peer.eventData;

                        return 1;

                    case ENetPeerState.ENET_PEER_STATE_ZOMBIE:
                        host.recalculateBandwidthLimits = 1;

                        @event.type = ENetEventType.ENET_EVENT_TYPE_DISCONNECT;
                        @event.peer = peer;
                        @event.data = peer.eventData;

                        enet_peer_reset(peer);

                        return 1;

                    case ENetPeerState.ENET_PEER_STATE_CONNECTED:
                        if (peer.dispatchedCommands.IsEmpty())
                        {
                            continue;
                        }

                        @event.packet = enet_peer_receive(peer, ref @event.channelID);
                        if (@event.packet == null)
                        {
                            continue;
                        }

                        @event.type = ENetEventType.ENET_EVENT_TYPE_RECEIVE;
                        @event.peer = peer;

                        if (!peer.dispatchedCommands.IsEmpty())
                        {
                            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                            host.dispatchQueue.AddLast(peer.dispatchList);
                        }

                        return 1;

                    default:
                        break;
                }
            }

            return 0;
        } /* enet_protocol_dispatch_incoming_commands */

        public static void enet_protocol_notify_connect(ENetHost host, ENetPeer peer, ENetEvent @event)
        {
            host.recalculateBandwidthLimits = 1;

            if (@event != null)
            {
                enet_protocol_change_state(host, peer, ENetPeerState.ENET_PEER_STATE_CONNECTED);

                peer.totalDataSent = 0;
                peer.totalDataReceived = 0;
                peer.totalPacketsSent = 0;
                peer.totalPacketsLost = 0;

                @event.type = ENetEventType.ENET_EVENT_TYPE_CONNECT;
                @event.peer = peer;
                @event.data = peer.eventData;
            }
            else
            {
                enet_protocol_dispatch_state(host, peer, peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTING ? ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED : ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING);
            }
        }

        public static void enet_protocol_notify_disconnect(ENetHost host, ENetPeer peer, ENetEvent @event)
        {
            if (peer.state >= ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING)
            {
                host.recalculateBandwidthLimits = 1;
            }

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && peer.state < ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED)
            {
                enet_peer_reset(peer);
            }
            else if (@event != null)
            {
                @event.type = ENetEventType.ENET_EVENT_TYPE_DISCONNECT;
                @event.peer = peer;
                @event.data = 0;

                enet_peer_reset(peer);
            }
            else
            {
                peer.eventData = 0;
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            }
        }

        public static void enet_protocol_notify_disconnect_timeout(ENetHost host, ENetPeer peer, ENetEvent @event)
        {
            if (peer.state >= ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING)
            {
                host.recalculateBandwidthLimits = 1;
            }

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && peer.state < ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED)
            {
                enet_peer_reset(peer);
            }
            else if (@event != null)
            {
                @event.type = ENetEventType.ENET_EVENT_TYPE_DISCONNECT_TIMEOUT;
                @event.peer = peer;
                @event.data = 0;

                enet_peer_reset(peer);
            }
            else
            {
                peer.eventData = 0;
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            }
        }

        public static void enet_protocol_remove_sent_unreliable_commands(ENetPeer peer, LinkedList<ENetOutgoingCommand> sentUnreliableCommands)
        {
            if (sentUnreliableCommands.IsEmpty())
                return;

            do
            {
                ENetOutgoingCommand outgoingCommand = sentUnreliableCommands.First.Value;
                outgoingCommand.outgoingCommandList.RemoveAndGet();

                if (outgoingCommand.packet != null)
                {
                    --outgoingCommand.packet.referenceCount;

                    if (outgoingCommand.packet.referenceCount == 0)
                    {
                        outgoingCommand.packet.flags |= (ushort)ENetPacketFlag.ENET_PACKET_FLAG_SENT;
                        callbacks.packet_destroy(outgoingCommand.packet);
                    }
                }

                enet_free(outgoingCommand);
            } while (!sentUnreliableCommands.IsEmpty());

            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER && !enet_peer_has_outgoing_commands(peer))
            {
                enet_peer_disconnect(peer, peer.eventData);
            }
        }

        public static ENetOutgoingCommand enet_protocol_find_sent_reliable_command(LinkedList<ENetOutgoingCommand> list, ushort reliableSequenceNumber, byte channelID)
        {
            LinkedListNode<ENetOutgoingCommand> currentCommand;

            for (currentCommand = list.First;
                 currentCommand != null;
                 currentCommand = currentCommand.Next)
            {
                ENetOutgoingCommand outgoingCommand = currentCommand.Value;

                if (0 == (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
                {
                    continue;
                }

                if (outgoingCommand.sendAttempts < 1)
                {
                    break;
                }

                if (outgoingCommand.reliableSequenceNumber == reliableSequenceNumber && outgoingCommand.command.header.channelID == channelID)
                {
                    return outgoingCommand;
                }
            }

            return null;
        }

        public static byte enet_protocol_remove_sent_reliable_command(ENetPeer peer, ushort reliableSequenceNumber, byte channelID)
        {
            ENetOutgoingCommand outgoingCommand = null;
            LinkedListNode<ENetOutgoingCommand> currentCommand;
            byte commandNumber;
            int wasSent = 1;

            for (currentCommand = peer.sentReliableCommands.First;
                 currentCommand != null;
                 currentCommand = currentCommand.Next)
            {
                outgoingCommand = currentCommand.Value;
                if (outgoingCommand.reliableSequenceNumber == reliableSequenceNumber && outgoingCommand.command.header.channelID == channelID)
                {
                    break;
                }
            }

            if (currentCommand == null)
            {
                outgoingCommand = enet_protocol_find_sent_reliable_command(peer.outgoingCommands, reliableSequenceNumber, channelID);
                if (outgoingCommand == null)
                {
                    outgoingCommand = enet_protocol_find_sent_reliable_command(peer.outgoingSendReliableCommands, reliableSequenceNumber, channelID);
                }

                wasSent = 0;
            }

            if (outgoingCommand == null)
            {
                return ENetProtocolCommand.ENET_PROTOCOL_COMMAND_NONE;
            }

            if (channelID < peer.channelCount)
            {
                ENetChannel channel = peer.channels[channelID];
                ushort reliableWindow = (ushort)(reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE);
                if (channel.reliableWindows[reliableWindow] > 0)
                {
                    --channel.reliableWindows[reliableWindow];
                    if (0 != channel.reliableWindows[reliableWindow])
                    {
                        channel.usedReliableWindows &= (ushort)(~(1u << reliableWindow));
                    }
                }
            }

            commandNumber = (byte)(outgoingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK);
            outgoingCommand.outgoingCommandList.RemoveAndGet();

            if (outgoingCommand.packet != null)
            {
                if (0 != wasSent)
                {
                    peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
                }

                --outgoingCommand.packet.referenceCount;

                if (outgoingCommand.packet.referenceCount == 0)
                {
                    outgoingCommand.packet.flags |= ENetPacketFlag.ENET_PACKET_FLAG_SENT;
                    callbacks.packet_destroy(outgoingCommand.packet);
                }
            }

            enet_free(outgoingCommand);

            if (peer.sentReliableCommands.IsEmpty())
            {
                return commandNumber;
            }

            outgoingCommand = peer.sentReliableCommands.First.Value;
            peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;

            return commandNumber;
        } /* enet_protocol_remove_sent_reliable_command */

        public static ENetPeer enet_protocol_handle_connect(ENetHost host, ref ENetProtocolHeader header, ref ENetProtocol command)
        {
            ENET_UNUSED(header);

            byte incomingSessionID, outgoingSessionID;
            long mtu, windowSize;
            ENetChannel channel = null;
            int channelCount, duplicatePeers = 0;
            ENetPeer currentPeer, peer = null;
            ENetProtocol verifyCommand = new ENetProtocol();

            channelCount = (int)ENET_NET_TO_HOST_32(command.connect.channelCount);

            if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
            {
                return null;
            }

            for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
            {
                currentPeer = host.peers[peerIdx];
                if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED)
                {
                    if (peer == null)
                    {
                        peer = currentPeer;
                    }
                }
                else if (currentPeer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && in6_equal(currentPeer.address.host, host.receivedAddress.host))
                {
                    if (currentPeer.address.port == host.receivedAddress.port && currentPeer.connectID == command.connect.connectID)
                    {
                        return null;
                    }

                    ++duplicatePeers;
                }
            }

            if (peer == null || duplicatePeers >= host.duplicatePeers)
            {
                return null;
            }

            if (channelCount > host.channelLimit)
            {
                channelCount = host.channelLimit;
            }

            peer.channels = enet_malloc<ENetChannel>(channelCount);
            if (peer.channels == null)
            {
                return null;
            }

            peer.channelCount = channelCount;
            peer.state = ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT;
            peer.connectID = command.connect.connectID;
            peer.address = host.receivedAddress;
            peer.mtu = host.mtu;
            peer.outgoingPeerID = ENET_NET_TO_HOST_16(command.connect.outgoingPeerID);
            peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.connect.incomingBandwidth);
            peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.connect.outgoingBandwidth);
            peer.packetThrottleInterval = ENET_NET_TO_HOST_32(command.connect.packetThrottleInterval);
            peer.packetThrottleAcceleration = ENET_NET_TO_HOST_32(command.connect.packetThrottleAcceleration);
            peer.packetThrottleDeceleration = ENET_NET_TO_HOST_32(command.connect.packetThrottleDeceleration);
            peer.eventData = ENET_NET_TO_HOST_32(command.connect.data);

            incomingSessionID = command.connect.incomingSessionID == 0xFF ? peer.outgoingSessionID : command.connect.incomingSessionID;
            incomingSessionID = (byte)((incomingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT));
            if (incomingSessionID == peer.outgoingSessionID)
            {
                incomingSessionID = (byte)((incomingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT));
            }

            peer.outgoingSessionID = incomingSessionID;

            outgoingSessionID = command.connect.outgoingSessionID == 0xFF ? peer.incomingSessionID : command.connect.outgoingSessionID;
            outgoingSessionID = (byte)((outgoingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT));
            if (outgoingSessionID == peer.incomingSessionID)
            {
                outgoingSessionID = (byte)((outgoingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT));
            }

            peer.incomingSessionID = outgoingSessionID;

            for (int channelIdx = 0; channelIdx < channelCount; ++channelIdx)
            {
                channel = peer.channels[channelIdx];
                channel.outgoingReliableSequenceNumber = 0;
                channel.outgoingUnreliableSequenceNumber = 0;
                channel.incomingReliableSequenceNumber = 0;
                channel.incomingUnreliableSequenceNumber = 0;

                channel.incomingReliableCommands.Clear();
                channel.incomingUnreliableCommands.Clear();

                channel.usedReliableWindows = 0;
                Array.Fill(channel.reliableWindows, (ushort)0);
            }

            mtu = ENET_NET_TO_HOST_32(command.connect.mtu);

            if (mtu < ENET_PROTOCOL_MINIMUM_MTU)
            {
                mtu = ENET_PROTOCOL_MINIMUM_MTU;
            }
            else if (mtu > ENET_PROTOCOL_MAXIMUM_MTU)
            {
                mtu = ENET_PROTOCOL_MAXIMUM_MTU;
            }

            if (mtu < peer.mtu)
                peer.mtu = mtu;

            if (host.outgoingBandwidth == 0 && peer.incomingBandwidth == 0)
            {
                peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }
            else if (host.outgoingBandwidth == 0 || peer.incomingBandwidth == 0)
            {
                peer.windowSize = (Math.Max(host.outgoingBandwidth, peer.incomingBandwidth) / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else
            {
                peer.windowSize = (Math.Min(host.outgoingBandwidth, peer.incomingBandwidth) / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }

            if (peer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
            {
                peer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else if (peer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
            {
                peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }

            if (host.incomingBandwidth == 0)
            {
                windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }
            else
            {
                windowSize = (host.incomingBandwidth / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }

            if (windowSize > ENET_NET_TO_HOST_32(command.connect.windowSize))
            {
                windowSize = ENET_NET_TO_HOST_32(command.connect.windowSize);
            }

            if (windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
            {
                windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else if (windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
            {
                windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }

            verifyCommand.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            verifyCommand.header.channelID = 0xFF;
            verifyCommand.verifyConnect.outgoingPeerID = ENET_HOST_TO_NET_16(peer.incomingPeerID);
            verifyCommand.verifyConnect.incomingSessionID = incomingSessionID;
            verifyCommand.verifyConnect.outgoingSessionID = outgoingSessionID;
            verifyCommand.verifyConnect.mtu = ENET_HOST_TO_NET_32((uint)peer.mtu);
            verifyCommand.verifyConnect.windowSize = ENET_HOST_TO_NET_32((uint)windowSize);
            verifyCommand.verifyConnect.channelCount = ENET_HOST_TO_NET_32((uint)channelCount);
            verifyCommand.verifyConnect.incomingBandwidth = ENET_HOST_TO_NET_32((uint)host.incomingBandwidth);
            verifyCommand.verifyConnect.outgoingBandwidth = ENET_HOST_TO_NET_32((uint)host.outgoingBandwidth);
            verifyCommand.verifyConnect.packetThrottleInterval = ENET_HOST_TO_NET_32((uint)peer.packetThrottleInterval);
            verifyCommand.verifyConnect.packetThrottleAcceleration = ENET_HOST_TO_NET_32((uint)peer.packetThrottleAcceleration);
            verifyCommand.verifyConnect.packetThrottleDeceleration = ENET_HOST_TO_NET_32((uint)peer.packetThrottleDeceleration);
            verifyCommand.verifyConnect.connectID = peer.connectID;

            enet_peer_queue_outgoing_command(peer, ref verifyCommand, null, 0, 0);
            return peer;
        } /* enet_protocol_handle_connect */

        public static int enet_protocol_handle_send_reliable(ENetHost host, ENetPeer peer, ref ENetProtocol command, ref ArraySegment<byte> currentData)
        {
            int dataLength;

            if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
            {
                return -1;
            }

            ArraySegment<byte> data = currentData.Slice(Marshal.SizeOf<ENetProtocolSendReliable>());
            dataLength = ENET_NET_TO_HOST_16(command.sendReliable.dataLength);
            currentData = currentData.Slice(0, dataLength);

            if (dataLength > host.maximumPacketSize || currentData.Offset < host.receivedData.Offset || currentData.Offset > host.receivedDataLength)
            {
                return -1;
            }

            if (enet_peer_queue_incoming_command(peer, ref command, data, dataLength, ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE, 0) == null)
            {
                return -1;
            }

            return 0;
        }

        public static int enet_protocol_handle_send_unsequenced(ENetHost host, ENetPeer peer, ref ENetProtocol command, ref ArraySegment<byte> currentData)
        {
            uint unsequencedGroup, index;

            if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
            {
                return -1;
            }

            ArraySegment<byte> data = currentData.Slice(Marshal.SizeOf<ENetProtocolSendUnsequenced>());
            int dataLength = ENET_NET_TO_HOST_16(command.sendUnsequenced.dataLength);
            currentData = currentData.Slice(0, dataLength);
            if (dataLength > host.maximumPacketSize || currentData.Offset < host.receivedData.Offset || currentData.Offset > host.receivedDataLength)
            {
                return -1;
            }

            unsequencedGroup = ENET_NET_TO_HOST_16(command.sendUnsequenced.unsequencedGroup);
            index = unsequencedGroup % ENET_PEER_UNSEQUENCED_WINDOW_SIZE;

            if (unsequencedGroup < peer.incomingUnsequencedGroup)
            {
                unsequencedGroup += 0x10000;
            }

            if (unsequencedGroup >= (uint)peer.incomingUnsequencedGroup + ENET_PEER_FREE_UNSEQUENCED_WINDOWS * ENET_PEER_UNSEQUENCED_WINDOW_SIZE)
            {
                return 0;
            }

            unsequencedGroup &= 0xFFFF;

            if (unsequencedGroup - index != peer.incomingUnsequencedGroup)
            {
                peer.incomingUnsequencedGroup = (ushort)(unsequencedGroup - index);
                Array.Fill(peer.unsequencedWindow, (uint)0);
            }
            else if (0 != (peer.unsequencedWindow[index / 32] & (1u << (int)(index % 32))))
            {
                return 0;
            }

            if (enet_peer_queue_incoming_command(peer, ref command, data, dataLength, ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED, 0) == null)
            {
                return -1;
            }

            peer.unsequencedWindow[index / 32] |= 1u << (int)(index % 32);

            return 0;
        } /* enet_protocol_handle_send_unsequenced */

        public static int enet_protocol_handle_send_unreliable(ENetHost host, ENetPeer peer, ref ENetProtocol command, ref ArraySegment<byte> currentData)
        {
            if (command.header.channelID >= peer.channelCount ||
                (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
            {
                return -1;
            }


            ArraySegment<byte> data = currentData.Slice(Marshal.SizeOf<ENetProtocolSendUnreliable>());
            int dataLength = ENET_NET_TO_HOST_16(command.sendUnreliable.dataLength);
            currentData = currentData.Slice(dataLength);
            if (dataLength > host.maximumPacketSize || currentData.Offset < host.receivedData.Offset || currentData.Offset > host.receivedDataLength)
            {
                return -1;
            }

            if (enet_peer_queue_incoming_command(peer, ref command, data, dataLength, 0, 0) == null)
            {
                return -1;
            }

            return 0;
        }

        public static int enet_protocol_handle_send_fragment(ENetHost host, ENetPeer peer, ref ENetProtocol command, ref ArraySegment<byte> currentData)
        {
            int fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, startSequenceNumber, totalLength;
            ENetChannel channel = null;
            long startWindow, currentWindow;
            LinkedListNode<ENetIncomingCommand> currentCommand = null;
            ENetIncomingCommand startCommand = null;

            if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
            {
                return -1;
            }

            fragmentLength = ENET_NET_TO_HOST_16(command.sendFragment.dataLength);
            currentData = currentData.Slice((int)fragmentLength);
            if (fragmentLength <= 0 ||
                fragmentLength > host.maximumPacketSize ||
                currentData.Offset < host.receivedData.Offset ||
                currentData.Offset > host.receivedDataLength
               )
            {
                return -1;
            }

            channel = peer.channels[command.header.channelID];
            startSequenceNumber = ENET_NET_TO_HOST_16(command.sendFragment.startSequenceNumber);
            startWindow = startSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
            currentWindow = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

            if (startSequenceNumber < channel.incomingReliableSequenceNumber)
            {
                startWindow += ENET_PEER_RELIABLE_WINDOWS;
            }

            if (startWindow < currentWindow || startWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1)
            {
                return 0;
            }

            fragmentNumber = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
            fragmentCount = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
            fragmentOffset = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
            totalLength = (int)ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

            if (fragmentCount > ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
                fragmentNumber >= fragmentCount ||
                totalLength > host.maximumPacketSize ||
                totalLength < fragmentCount ||
                fragmentOffset >= totalLength ||
                fragmentLength > totalLength - fragmentOffset
               )
            {
                return -1;
            }

            for (currentCommand = channel.incomingReliableCommands.Last;
                 currentCommand != null;
                 currentCommand = currentCommand.Previous
                )
            {
                ENetIncomingCommand incomingCommand = currentCommand.Value;

                if (startSequenceNumber >= channel.incomingReliableSequenceNumber)
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

                if (incomingCommand.reliableSequenceNumber <= startSequenceNumber)
                {
                    if (incomingCommand.reliableSequenceNumber < startSequenceNumber)
                    {
                        break;
                    }

                    if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) !=
                        ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT ||
                        totalLength != incomingCommand.packet.dataLength ||
                        fragmentCount != incomingCommand.fragmentCount
                       )
                    {
                        return -1;
                    }

                    startCommand = incomingCommand;
                    break;
                }
            }

            if (startCommand == null)
            {
                ENetProtocol hostCommand = command;
                hostCommand.header.reliableSequenceNumber = (ushort)startSequenceNumber;
                startCommand = enet_peer_queue_incoming_command(peer, ref hostCommand, null, totalLength, ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE, fragmentCount);
                if (startCommand == null)
                {
                    return -1;
                }
            }

            if ((startCommand.fragments[fragmentNumber / 32] & (1u << (int)(fragmentNumber % 32))) == 0)
            {
                --startCommand.fragmentsRemaining;
                startCommand.fragments[fragmentNumber / 32] |= (1u << (int)(fragmentNumber % 32));

                if (fragmentOffset + fragmentLength > startCommand.packet.dataLength)
                {
                    fragmentLength = (int)(startCommand.packet.dataLength - fragmentOffset);
                }

                // todo : @ikpil check
                enet_assert(false);
                //memcpy(startCommand.packet.data + fragmentOffset, (byte *) command + Marshal.SizeOf<ENetProtocolSendFragment>(), fragmentLength);

                if (startCommand.fragmentsRemaining <= 0)
                {
                    enet_peer_dispatch_incoming_reliable_commands(peer, channel, null);
                }
            }

            return 0;
        } /* enet_protocol_handle_send_fragment */

        public static int enet_protocol_handle_send_unreliable_fragment(ENetHost host, ENetPeer peer, ref ENetProtocol command, ref ArraySegment<byte> currentData)
        {
            int fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, reliableSequenceNumber, startSequenceNumber, totalLength;
            ushort reliableWindow, currentWindow;
            ENetChannel channel = null;
            LinkedListNode<ENetIncomingCommand> currentCommand;
            ENetIncomingCommand startCommand = null;

            if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
            {
                return -1;
            }

            fragmentLength = ENET_NET_TO_HOST_16(command.sendFragment.dataLength);
            currentData = currentData.Slice((int)fragmentLength);
            if (fragmentLength <= 0 ||
                fragmentLength > host.maximumPacketSize ||
                currentData.Offset < host.receivedData.Offset ||
                currentData.Offset > host.receivedDataLength
               )
            {
                return -1;
            }

            channel = peer.channels[command.header.channelID];
            reliableSequenceNumber = command.header.reliableSequenceNumber;
            startSequenceNumber = ENET_NET_TO_HOST_16(command.sendFragment.startSequenceNumber);

            reliableWindow = (ushort)(reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE);
            currentWindow = (ushort)(channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE);

            if (reliableSequenceNumber < channel.incomingReliableSequenceNumber)
            {
                reliableWindow += ENET_PEER_RELIABLE_WINDOWS;
            }

            if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1)
            {
                return 0;
            }

            if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && startSequenceNumber <= channel.incomingUnreliableSequenceNumber)
            {
                return 0;
            }

            fragmentNumber = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
            fragmentCount = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
            fragmentOffset = (int)ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
            totalLength = (int)ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

            if (fragmentCount > ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
                fragmentNumber >= fragmentCount ||
                totalLength > host.maximumPacketSize ||
                totalLength < fragmentCount ||
                fragmentOffset >= totalLength ||
                fragmentLength > totalLength - fragmentOffset
               )
            {
                return -1;
            }

            for (currentCommand = channel.incomingUnreliableCommands.Last;
                 currentCommand != null;
                 currentCommand = currentCommand.Previous
                )
            {
                ENetIncomingCommand incomingCommand = currentCommand.Value;

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

                if (incomingCommand.unreliableSequenceNumber <= startSequenceNumber)
                {
                    if (incomingCommand.unreliableSequenceNumber < startSequenceNumber)
                    {
                        break;
                    }

                    if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) !=
                        ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT ||
                        totalLength != incomingCommand.packet.dataLength ||
                        fragmentCount != incomingCommand.fragmentCount
                       )
                    {
                        return -1;
                    }

                    startCommand = incomingCommand;
                    break;
                }
            }

            if (startCommand == null)
            {
                startCommand = enet_peer_queue_incoming_command(peer, ref command, null, totalLength,
                    ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT, fragmentCount);
                if (startCommand == null)
                {
                    return -1;
                }
            }

            if ((startCommand.fragments[fragmentNumber / 32] & (1u << (int)(fragmentNumber % 32))) == 0)
            {
                --startCommand.fragmentsRemaining;
                startCommand.fragments[fragmentNumber / 32] |= (1u << (int)(fragmentNumber % 32));

                if (fragmentOffset + fragmentLength > startCommand.packet.dataLength)
                {
                    fragmentLength = (int)(startCommand.packet.dataLength - fragmentOffset);
                }

                // todo : @ikpil check
                enet_assert(false);
                //memcpy(startCommand.packet.data + fragmentOffset, (byte *) command + Marshal.SizeOf<ENetProtocolSendFragment>(), fragmentLength);

                if (startCommand.fragmentsRemaining <= 0)
                {
                    enet_peer_dispatch_incoming_unreliable_commands(peer, channel, null);
                }
            }

            return 0;
        } /* enet_protocol_handle_send_unreliable_fragment */

        public static int enet_protocol_handle_ping(ENetHost host, ENetPeer peer, ref ENetProtocol command)
        {
            ENET_UNUSED(host);
            ENET_UNUSED(command);

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                return -1;
            }

            return 0;
        }

        public static int enet_protocol_handle_bandwidth_limit(ENetHost host, ENetPeer peer, ref ENetProtocol command)
        {
            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                return -1;
            }

            if (peer.incomingBandwidth != 0)
            {
                --host.bandwidthLimitedPeers;
            }

            peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.bandwidthLimit.incomingBandwidth);
            if (peer.incomingBandwidth != 0)
            {
                ++host.bandwidthLimitedPeers;
            }

            peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.bandwidthLimit.outgoingBandwidth);

            if (peer.incomingBandwidth == 0 && host.outgoingBandwidth == 0)
            {
                peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }
            else if (peer.incomingBandwidth == 0 || host.outgoingBandwidth == 0)
            {
                peer.windowSize = (Math.Max(peer.incomingBandwidth, host.outgoingBandwidth)
                                   / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else
            {
                peer.windowSize = (Math.Min(peer.incomingBandwidth, host.outgoingBandwidth)
                                   / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }

            if (peer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
            {
                peer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }
            else if (peer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
            {
                peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }

            return 0;
        } /* enet_protocol_handle_bandwidth_limit */

        public static int enet_protocol_handle_throttle_configure(ENetHost host, ENetPeer peer, ref ENetProtocol command)
        {
            ENET_UNUSED(host);

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                return -1;
            }

            peer.packetThrottleInterval = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleInterval);
            peer.packetThrottleAcceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleAcceleration);
            peer.packetThrottleDeceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleDeceleration);

            return 0;
        }

        public static int enet_protocol_handle_disconnect(ENetHost host, ENetPeer peer, ref ENetProtocol command)
        {
            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE ||
                peer.state == ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT
               )
            {
                return 0;
            }

            enet_peer_reset_queues(peer);

            if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTING || peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTING)
            {
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            }
            else if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)
            {
                if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING) { host.recalculateBandwidthLimits = 1; }

                enet_peer_reset(peer);
            }
            else if (0 != (command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
            {
                enet_protocol_change_state(host, peer, ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT);
            }
            else
            {
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            }

            if (peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECTED)
            {
                peer.eventData = ENET_NET_TO_HOST_32(command.disconnect.data);
            }

            return 0;
        }

        public static int enet_protocol_handle_acknowledge(ENetHost host, ENetEvent @event, ENetPeer peer, ref ENetProtocol command)
        {
            uint roundTripTime, receivedSentTime, receivedReliableSequenceNumber;
            byte commandNumber;

            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE)
            {
                return 0;
            }

            receivedSentTime = ENET_NET_TO_HOST_16(command.acknowledge.receivedSentTime);
            receivedSentTime |= (uint)(host.serviceTime & 0xFFFF0000);
            if ((receivedSentTime & 0x8000) > (host.serviceTime & 0x8000))
            {
                receivedSentTime -= 0x10000;
            }

            if (ENET_TIME_LESS(host.serviceTime, receivedSentTime))
            {
                return 0;
            }

            roundTripTime = (uint)ENET_TIME_DIFFERENCE(host.serviceTime, receivedSentTime);
            roundTripTime = Math.Max(roundTripTime, 1);

            if (peer.lastReceiveTime > 0)
            {
                enet_peer_throttle(peer, roundTripTime);

                peer.roundTripTimeVariance -= peer.roundTripTimeVariance / 4;

                if (roundTripTime >= peer.roundTripTime)
                {
                    uint diff = (uint)(roundTripTime - peer.roundTripTime);
                    peer.roundTripTimeVariance += diff / 4;
                    peer.roundTripTime += diff / 8;
                }
                else
                {
                    uint diff = (uint)(peer.roundTripTime - roundTripTime);
                    peer.roundTripTimeVariance += diff / 4;
                    peer.roundTripTime -= diff / 8;
                }
            }
            else
            {
                peer.roundTripTime = roundTripTime;
                peer.roundTripTimeVariance = (roundTripTime + 1) / 2;
            }

            if (peer.roundTripTime < peer.lowestRoundTripTime)
            {
                peer.lowestRoundTripTime = peer.roundTripTime;
            }

            if (peer.roundTripTimeVariance > peer.highestRoundTripTimeVariance)
            {
                peer.highestRoundTripTimeVariance = peer.roundTripTimeVariance;
            }

            if (peer.packetThrottleEpoch == 0 ||
                ENET_TIME_DIFFERENCE(host.serviceTime, peer.packetThrottleEpoch) >= peer.packetThrottleInterval
               )
            {
                peer.lastRoundTripTime = peer.lowestRoundTripTime;
                peer.lastRoundTripTimeVariance = Math.Max(peer.highestRoundTripTimeVariance, 1);
                peer.lowestRoundTripTime = peer.roundTripTime;
                peer.highestRoundTripTimeVariance = peer.roundTripTimeVariance;
                peer.packetThrottleEpoch = host.serviceTime;
            }

            peer.lastReceiveTime = Math.Max(host.serviceTime, 1);
            peer.earliestTimeout = 0;

            receivedReliableSequenceNumber = ENET_NET_TO_HOST_16(command.acknowledge.receivedReliableSequenceNumber);
            commandNumber = enet_protocol_remove_sent_reliable_command(peer, (ushort)receivedReliableSequenceNumber, command.header.channelID);

            switch (peer.state)
            {
                case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                    if (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT)
                    {
                        return -1;
                    }

                    enet_protocol_notify_connect(host, peer, @event);
                    break;

                case ENetPeerState.ENET_PEER_STATE_DISCONNECTING:
                    if (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT)
                    {
                        return -1;
                    }

                    enet_protocol_notify_disconnect(host, peer, @event);
                    break;

                case ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER:
                    if (!enet_peer_has_outgoing_commands(peer))
                    {
                        enet_peer_disconnect(peer, peer.eventData);
                    }

                    break;

                default:
                    break;
            }

            return 0;
        } /* enet_protocol_handle_acknowledge */

        public static int enet_protocol_handle_verify_connect(ENetHost host, ENetEvent @event, ENetPeer peer, ref ENetProtocol command)
        {
            uint mtu, windowSize;
            long channelCount;

            if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING)
            {
                return 0;
            }

            channelCount = ENET_NET_TO_HOST_32(command.verifyConnect.channelCount);

            if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT ||
                ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleInterval) != peer.packetThrottleInterval ||
                ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleAcceleration) != peer.packetThrottleAcceleration ||
                ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleDeceleration) != peer.packetThrottleDeceleration ||
                command.verifyConnect.connectID != peer.connectID
               )
            {
                peer.eventData = 0;
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
                return -1;
            }

            enet_protocol_remove_sent_reliable_command(peer, 1, 0xFF);

            if (channelCount < peer.channelCount)
            {
                peer.channelCount = channelCount;
            }

            peer.outgoingPeerID = ENET_NET_TO_HOST_16(command.verifyConnect.outgoingPeerID);
            peer.incomingSessionID = command.verifyConnect.incomingSessionID;
            peer.outgoingSessionID = command.verifyConnect.outgoingSessionID;

            mtu = ENET_NET_TO_HOST_32(command.verifyConnect.mtu);

            if (mtu < ENET_PROTOCOL_MINIMUM_MTU)
            {
                mtu = ENET_PROTOCOL_MINIMUM_MTU;
            }
            else if (mtu > ENET_PROTOCOL_MAXIMUM_MTU)
            {
                mtu = ENET_PROTOCOL_MAXIMUM_MTU;
            }

            if (mtu < peer.mtu)
            {
                peer.mtu = mtu;
            }

            windowSize = ENET_NET_TO_HOST_32(command.verifyConnect.windowSize);
            if (windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
            {
                windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
            }

            if (windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
            {
                windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
            }

            if (windowSize < peer.windowSize)
            {
                peer.windowSize = windowSize;
            }

            peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.incomingBandwidth);
            peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.outgoingBandwidth);

            enet_protocol_notify_connect(host, peer, @event);
            return 0;
        } /* enet_protocol_handle_verify_connect */

        public static int enet_protocol_handle_incoming_commands(ENetHost host, ENetEvent @event)
        {
            ENetPeer peer = null;
            ArraySegment<byte> currentData;
            int headerSize;
            ushort peerID, flags;
            byte sessionID;

            if (host.receivedDataLength < Marshal.SizeOf<ENetProtocolHeaderMinimal>())
            {
                return 0;
            }

            ENetProtocolHeader header = new ENetProtocolHeader(host.receivedData);

            peerID = ENET_NET_TO_HOST_16(header.peerID);
            sessionID = (byte)((peerID & ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK) >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
            flags = (ushort)(peerID & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK);
            peerID = (ushort)(peerID & ~(ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK | ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK));

            headerSize = 0 != (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME)
                ? Marshal.SizeOf<ENetProtocolHeader>()
                : Marshal.SizeOf<ENetProtocolHeaderMinimal>();

            if (0 != (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA))
            {
                if (host.receivedDataLength < headerSize + sizeof(byte))
                {
                    return 0;
                }

                ArraySegment<byte> headerExtraPeerID = host.receivedData.Slice(headerSize, 1);
                byte peerIDExtra = headerExtraPeerID[0];
                peerID = (ushort)((peerID & 0x07FF) | ((ushort)peerIDExtra << 11));

                headerSize += sizeof(byte);
            }

            if (host.checksum != null)
            {
                if (host.receivedDataLength < headerSize + sizeof(uint))
                {
                    return 0;
                }

                headerSize += Marshal.SizeOf<uint>();
            }

            if (peerID == ENET_PROTOCOL_MAXIMUM_PEER_ID)
            {
                peer = null;
            }
            else if (peerID >= host.peerCount)
            {
                return 0;
            }
            else
            {
                peer = host.peers[peerID];

                if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED ||
                    peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE ||
                    ((!in6_equal(host.receivedAddress.host, peer.address.host) ||
                      host.receivedAddress.port != peer.address.port) &&
                     1 == 1 /* no broadcast in ipv6  !in6_equal(peer.address.host , ENET_HOST_BROADCAST)*/) ||
                    (peer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID &&
                     sessionID != peer.incomingSessionID)
                   )
                {
                    return 0;
                }
            }

            if (0 != (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_COMPRESSED))
            {
                long originalSize;
                if (host.compressor.context == null || host.compressor.decompress == null)
                {
                    return 0;
                }

                // todo : @ikpil check
                enet_assert(false);
                originalSize = host.compressor.decompress(host.compressor.context,
                    host.receivedData.Slice(headerSize),
                    host.receivedDataLength - headerSize,
                    new ArraySegment<byte>(host.packetData[1], headerSize, host.packetData[1].Length - headerSize),
                    host.packetData[1].Length - headerSize
                );

                if (originalSize <= 0 || originalSize > host.packetData[1].Length - headerSize)
                {
                    return 0;
                }

                // todo : @ikpil check
                enet_assert(false);
                // memcpy(host.packetData[1], header, headerSize);
                // host.receivedData       = host.packetData[1];
                // host.receivedDataLength = headerSize + originalSize;
            }

            if (host.checksum != null)
            {
                // todo : @ikpil check
                enet_assert(false);
                // uint *checksum = (uint *) &host.receivedData[headerSize - Marshal.SizeOf<uint>()];
                // uint desiredChecksum = *checksum;
                // ENetBuffer buffer;
                //
                // *checksum = peer != null ? peer.connectID : 0;
                //
                // buffer.data       = host.receivedData;
                // buffer.dataLength = host.receivedDataLength;
                //
                // if (host.checksum(ref buffer, 1) != desiredChecksum) {
                //     return 0;
                // }
            }

            if (peer != null)
            {
                peer.address = new ENetAddress(host.receivedAddress.host, host.receivedAddress.port, host.receivedAddress.sin6_scope_id);
                peer.incomingDataTotal += (uint)host.receivedDataLength;
                peer.totalDataReceived += host.receivedDataLength;
            }

            currentData = host.receivedData.Slice(headerSize, host.receivedDataLength - headerSize);

            while (currentData.Offset < host.receivedDataLength)
            {
                byte commandNumber;
                int commandSize;

                ENetProtocol command = new ENetProtocol();
                command.MergeForm(currentData);

                if (currentData.Offset + Marshal.SizeOf<ENetProtocolCommandHeader>() > host.receivedDataLength)
                {
                    break;
                }

                commandNumber = (byte)(command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK);
                if (commandNumber >= ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT)
                {
                    break;
                }

                commandSize = commandSizes[commandNumber];
                if (commandSize == 0 || currentData.Offset + commandSize > host.receivedDataLength)
                {
                    break;
                }

                currentData = currentData.Slice(commandSize);

                if (peer == null && (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT || currentData.Offset < host.receivedDataLength))
                {
                    break;
                }

                command.header.reliableSequenceNumber = ENET_NET_TO_HOST_16(command.header.reliableSequenceNumber);

                switch (commandNumber)
                {
                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_ACKNOWLEDGE:
                        if (0 != enet_protocol_handle_acknowledge(host, @event, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT:
                        if (peer != null)
                        {
                            goto commandError;
                        }

                        peer = enet_protocol_handle_connect(host, ref header, ref command);
                        if (peer == null)
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT:
                        if (0 != enet_protocol_handle_verify_connect(host, @event, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT:
                        if (0 != enet_protocol_handle_disconnect(host, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_PING:
                        if (0 != enet_protocol_handle_ping(host, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                        if (0 != enet_protocol_handle_send_reliable(host, peer, ref command, ref currentData))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                        if (0 != enet_protocol_handle_send_unreliable(host, peer, ref command, ref currentData))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                        if (0 != enet_protocol_handle_send_unsequenced(host, peer, ref command, ref currentData))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
                        if (0 != enet_protocol_handle_send_fragment(host, peer, ref command, ref currentData))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT:
                        if (0 != enet_protocol_handle_bandwidth_limit(host, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE:
                        if (0 != enet_protocol_handle_throttle_configure(host, peer, ref command))
                        {
                            goto commandError;
                        }

                        break;

                    case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                        if (0 != enet_protocol_handle_send_unreliable_fragment(host, peer, ref command, ref currentData))
                        {
                            goto commandError;
                        }

                        break;

                    default:
                        goto commandError;
                }

                enet_assert(null != peer);
                if ((command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0)
                {
                    ushort sentTime;

                    if (0 == (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME))
                    {
                        break;
                    }

                    sentTime = ENET_NET_TO_HOST_16(header.sentTime);

                    switch (peer.state)
                    {
                        case ENetPeerState.ENET_PEER_STATE_DISCONNECTING:
                        case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                        case ENetPeerState.ENET_PEER_STATE_DISCONNECTED:
                        case ENetPeerState.ENET_PEER_STATE_ZOMBIE:
                            break;

                        case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT:
                            if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT)
                            {
                                enet_peer_queue_acknowledgement(peer, ref command, sentTime);
                            }

                            break;

                        default:
                            enet_peer_queue_acknowledgement(peer, ref command, sentTime);
                            break;
                    }
                }
            }

            commandError:
            if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE)
            {
                return 1;
            }

            return 0;
        } /* enet_protocol_handle_incoming_commands */

        public static int enet_protocol_receive_incoming_commands(ENetHost host, ENetEvent @event)
        {
            int packets;

            for (packets = 0; packets < 256; ++packets)
            {
                int receivedLength;
                ENetBuffer buffer = new ENetBuffer();

                buffer.data = host.packetData[0];
                // buffer.dataLength = sizeof (host.packetData[0]);
                buffer.dataLength = host.mtu;

                receivedLength = enet_socket_receive(host.socket, ref host.receivedAddress, ref buffer);

                if (receivedLength == -2)
                    continue;

                if (receivedLength < 0)
                {
                    return -1;
                }

                if (receivedLength == 0)
                {
                    return 0;
                }

                host.receivedData = host.packetData[0];
                host.receivedDataLength = receivedLength;

                host.totalReceivedData += receivedLength;
                host.totalReceivedPackets++;

                if (host.intercept != null)
                {
                    switch (host.intercept(host, @event))
                    {
                        case 1:
                            if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE)
                            {
                                return 1;
                            }

                            continue;

                        case -1:
                            return -1;

                        default:
                            break;
                    }
                }

                switch (enet_protocol_handle_incoming_commands(host, @event))
                {
                    case 1:
                        return 1;

                    case -1:
                        return -1;

                    default:
                        break;
                }
            }

            return -1;
        } /* enet_protocol_receive_incoming_commands */

        public static void enet_protocol_send_acknowledgements(ENetHost host, ENetPeer peer)
        {
            int ncommand = host.commandCount;
            int nbuffer = host.bufferCount;
            LinkedListNode<ENetAcknowledgement> currentAcknowledgement = null;
            ushort reliableSequenceNumber;

            currentAcknowledgement = peer.acknowledgements.First;

            while (currentAcknowledgement != null)
            {
                ref ENetProtocol command = ref host.commands[ncommand];
                ref ENetBuffer buffer = ref host.buffers[nbuffer];

                if (ncommand >= host.commands.Length ||
                    nbuffer >= host.buffers.Length ||
                    peer.mtu - host.packetSize < Marshal.SizeOf<ENetProtocolAcknowledge>()
                   )
                {
                    peer.flags |= ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING;
                    break;
                }

                ENetAcknowledgement acknowledgement = currentAcknowledgement.Value;
                currentAcknowledgement = currentAcknowledgement.Next;

                // todo : @ikpil check
                enet_assert(false);
                //buffer.data       = command.dummyBytes.AsSpan();
                buffer.dataLength = Marshal.SizeOf<ENetProtocolAcknowledge>();
                host.packetSize += buffer.dataLength;

                reliableSequenceNumber = ENET_HOST_TO_NET_16(acknowledgement.command.header.reliableSequenceNumber);

                command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_ACKNOWLEDGE;
                command.header.channelID = acknowledgement.command.header.channelID;
                command.header.reliableSequenceNumber = reliableSequenceNumber;
                command.acknowledge.receivedReliableSequenceNumber = reliableSequenceNumber;
                command.acknowledge.receivedSentTime = ENET_HOST_TO_NET_16((ushort)acknowledgement.sentTime);

                if ((acknowledgement.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT)
                {
                    enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
                }

                acknowledgement.acknowledgementList.RemoveAndGet();
                enet_free(acknowledgement);

                ++ncommand;
                ++nbuffer;
            }

            host.commandCount = ncommand;
            host.bufferCount = nbuffer;
        } /* enet_protocol_send_acknowledgements */

        public static int enet_protocol_check_timeouts(ENetHost host, ENetPeer peer, ENetEvent @event)
        {
            ENetOutgoingCommand outgoingCommand = null;
            LinkedListNode<ENetOutgoingCommand> currentCommand, insertPosition, insertSendReliablePosition;

            currentCommand = peer.sentReliableCommands.First;
            insertPosition = peer.outgoingCommands.First;
            insertSendReliablePosition = peer.outgoingSendReliableCommands.First;

            while (currentCommand != null)
            {
                outgoingCommand = currentCommand.Value;

                currentCommand = currentCommand.Next;

                if (ENET_TIME_DIFFERENCE(host.serviceTime, outgoingCommand.sentTime) < outgoingCommand.roundTripTimeout)
                {
                    continue;
                }

                if (peer.earliestTimeout == 0 || ENET_TIME_LESS(outgoingCommand.sentTime, peer.earliestTimeout))
                {
                    peer.earliestTimeout = outgoingCommand.sentTime;
                }

                if (peer.earliestTimeout != 0 &&
                    (ENET_TIME_DIFFERENCE(host.serviceTime, peer.earliestTimeout) >= peer.timeoutMaximum ||
                     ((1u << (outgoingCommand.sendAttempts - 1)) >= peer.timeoutLimit &&
                      ENET_TIME_DIFFERENCE(host.serviceTime, peer.earliestTimeout) >= peer.timeoutMinimum))
                   )
                {
                    enet_protocol_notify_disconnect_timeout(host, peer, @event);
                    return 1;
                }

                ++peer.packetsLost;
                ++peer.totalPacketsLost;

                /* Replaced exponential backoff time with something more linear */
                /* Source: http://lists.cubik.org/pipermail/enet-discuss/2014-May/002308.html */
                outgoingCommand.roundTripTimeout = (uint)(peer.roundTripTime + 4 * peer.roundTripTimeVariance);

                if (outgoingCommand.packet != null)
                {
                    peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
                    insertSendReliablePosition.AddAfter(outgoingCommand.outgoingCommandList.RemoveAndGet());
                }
                else
                {
                    insertPosition.AddAfter(outgoingCommand.outgoingCommandList.RemoveAndGet());
                }

                if (currentCommand == peer.sentReliableCommands.First && !peer.sentReliableCommands.IsEmpty())
                {
                    outgoingCommand = currentCommand.Value;
                    peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;
                }
            }

            return 0;
        } /* enet_protocol_check_timeouts */

        public static int enet_protocol_check_outgoing_commands(ENetHost host, ENetPeer peer, LinkedList<ENetOutgoingCommand> sentUnreliableCommands)
        {
            int ncommand = host.commandCount;
            int nbuffer = host.bufferCount;

            ENetOutgoingCommand outgoingCommand = null;
            LinkedListNode<ENetOutgoingCommand> currentCommand, currentSendReliableCommand = null;
            ENetChannel channel = null;
            int reliableWindow = 0;
            int commandSize = 0;
            int windowWrap = 0, canPing = 1;

            currentCommand = peer.outgoingCommands.First;
            currentSendReliableCommand = peer.outgoingSendReliableCommands.First;

            for (;;)
            {
                if (currentCommand != null)
                {
                    outgoingCommand = currentCommand.Value;
                    if (currentSendReliableCommand != null && ENET_TIME_LESS(currentSendReliableCommand.Value.queueTime, outgoingCommand.queueTime))
                    {
                        // todo : @ikpil check
                        enet_assert(false);
                        //goto useSendReliableCommand;
                    }

                    currentCommand = currentCommand.Next;
                }
                else if (currentSendReliableCommand != null)
                {
                    useSendReliableCommand:
                    outgoingCommand = currentSendReliableCommand.Value;
                    currentSendReliableCommand = currentSendReliableCommand.Next;
                }
                else
                {
                    break;
                }


                if (0 != (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
                {
                    channel = outgoingCommand.command.header.channelID < peer.channelCount ? peer.channels[outgoingCommand.command.header.channelID] : null;
                    reliableWindow = outgoingCommand.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
                    if (channel != null)
                    {
                        if (0 != windowWrap)
                        {
                            continue;
                        }
                        else if (outgoingCommand.sendAttempts < 1 &&
                                 0 == (outgoingCommand.reliableSequenceNumber % ENET_PEER_RELIABLE_WINDOW_SIZE) &&
                                 (channel.reliableWindows[(reliableWindow + ENET_PEER_RELIABLE_WINDOWS - 1) % ENET_PEER_RELIABLE_WINDOWS] >= ENET_PEER_RELIABLE_WINDOW_SIZE ||
                                  0 != (channel.usedReliableWindows & ((((1u << (ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) << reliableWindow)) |
                                        (((1u << (ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) >> (ENET_PEER_RELIABLE_WINDOWS - reliableWindow)))))
                        {
                            windowWrap = 1;
                            currentSendReliableCommand = null;
                            continue;
                        }
                    }

                    if (outgoingCommand.packet != null)
                    {
                        long windowSize = (peer.packetThrottle * peer.windowSize) / ENET_PEER_PACKET_THROTTLE_SCALE;

                        if (peer.reliableDataInTransit + outgoingCommand.fragmentLength > Math.Max(windowSize, peer.mtu))
                        {
                            currentSendReliableCommand = null;
                            continue;
                        }
                    }

                    canPing = 0;
                }

                ref ENetProtocol command = ref host.commands[host.commandCount];
                ref ENetBuffer buffer = ref host.buffers[host.bufferCount];

                commandSize = commandSizes[outgoingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK];
                if (ncommand >= host.commands.Length ||
                    nbuffer + 1 >= host.buffers.Length ||
                    peer.mtu - host.packetSize < commandSize ||
                    (outgoingCommand.packet != null &&
                     (ushort)(peer.mtu - host.packetSize) < (ushort)(commandSize + outgoingCommand.fragmentLength))
                   )
                {
                    peer.flags |= ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING;
                    break;
                }

                if (0 != (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
                {
                    channel = outgoingCommand.command.header.channelID < peer.channelCount ? peer.channels[outgoingCommand.command.header.channelID] : null;
                    reliableWindow = outgoingCommand.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
                    if (channel != null && outgoingCommand.sendAttempts < 1)
                    {
                        channel.usedReliableWindows |= (ushort)(1u << reliableWindow);
                        ++channel.reliableWindows[reliableWindow];
                    }

                    ++outgoingCommand.sendAttempts;

                    if (outgoingCommand.roundTripTimeout == 0)
                    {
                        outgoingCommand.roundTripTimeout = (uint)(peer.roundTripTime + 4 * peer.roundTripTimeVariance);
                    }

                    if (peer.sentReliableCommands.IsEmpty())
                    {
                        peer.nextTimeout = host.serviceTime + outgoingCommand.roundTripTimeout;
                    }

                    peer.sentReliableCommands.AddLast(outgoingCommand.outgoingCommandList.RemoveAndGet());

                    outgoingCommand.sentTime = host.serviceTime;

                    host.headerFlags |= ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME;
                    peer.reliableDataInTransit += outgoingCommand.fragmentLength;
                }
                else
                {
                    if (outgoingCommand.packet != null && outgoingCommand.fragmentOffset == 0)
                    {
                        peer.packetThrottleCounter += ENET_PEER_PACKET_THROTTLE_COUNTER;
                        peer.packetThrottleCounter %= ENET_PEER_PACKET_THROTTLE_SCALE;

                        if (peer.packetThrottleCounter > peer.packetThrottle)
                        {
                            ushort reliableSequenceNumber = outgoingCommand.reliableSequenceNumber,
                                unreliableSequenceNumber = outgoingCommand.unreliableSequenceNumber;
                            for (;;)
                            {
                                --outgoingCommand.packet.referenceCount;

                                if (outgoingCommand.packet.referenceCount == 0)
                                {
                                    enet_packet_destroy(outgoingCommand.packet);
                                }

                                outgoingCommand.outgoingCommandList.RemoveAndGet();
                                enet_free(outgoingCommand);

                                if (currentCommand == null)
                                {
                                    break;
                                }

                                outgoingCommand = currentCommand.Value;
                                if (outgoingCommand.reliableSequenceNumber != reliableSequenceNumber ||
                                    outgoingCommand.unreliableSequenceNumber != unreliableSequenceNumber)
                                {
                                    break;
                                }

                                currentCommand = currentCommand.Next;
                            }

                            continue;
                        }
                    }

                    outgoingCommand.outgoingCommandList.RemoveAndGet();

                    if (outgoingCommand.packet != null)
                    {
                        sentUnreliableCommands.AddLast(outgoingCommand);
                    }
                }


                enet_assert(false);
                //buffer.data = command;
                buffer.dataLength = commandSize;

                host.packetSize += buffer.dataLength;

                command = outgoingCommand.command;

                if (outgoingCommand.packet != null)
                {
                    ++nbuffer;
                    enet_assert(false);
                    //buffer.data = new ArraySegment<byte>(outgoingCommand.packet.data, outgoingCommand.fragmentOffset, outgoingCommand.packet.data.Length - outgoingCommand.fragmentOffset);
                    buffer.dataLength = outgoingCommand.fragmentLength;
                    host.packetSize += outgoingCommand.fragmentLength;
                }
                else if (0 == (outgoingCommand.command.header.command & ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE))
                {
                    enet_free(outgoingCommand);
                }

                ++peer.packetsSent;
                ++peer.totalPacketsSent;

                ++ncommand;
                ++nbuffer;
            }

            host.commandCount = ncommand;
            host.bufferCount = nbuffer;

            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER &&
                !enet_peer_has_outgoing_commands(peer) &&
                sentUnreliableCommands.IsEmpty())
            {
                enet_peer_disconnect(peer, peer.eventData);
            }

            return canPing;
        } /* enet_protocol_send_reliable_outgoing_commands */


        public static int enet_protocol_send_outgoing_commands(ENetHost host, ENetEvent @event, int checkForTimeouts)
        {
            ENetProtocolSendOutgoingCommand headerData = new ENetProtocolSendOutgoingCommand();
            ref ENetProtocolHeader header = ref headerData.header;
            int sentLength = 0;
            int shouldCompress = 0;
            LinkedList<ENetOutgoingCommand> sentUnreliableCommands = new LinkedList<ENetOutgoingCommand>();
            int sendPass = 0, continueSending = 0;
            ENetPeer currentPeer = null;

            sentUnreliableCommands.Clear();

            for (; sendPass <= continueSending; ++sendPass)
            {
                for (int i = 0; i < host.peerCount; ++i)
                {
                    currentPeer = host.peers[i];
                    if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || currentPeer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE || (sendPass > 0 && 0 == (currentPeer.flags & ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING)))
                    {
                        continue;
                    }

                    currentPeer.flags &= unchecked((ushort)~ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING);

                    host.headerFlags = 0;
                    host.commandCount = 0;
                    host.bufferCount = 1;
                    host.packetSize = Marshal.SizeOf<ENetProtocolHeader>();

                    if (!currentPeer.acknowledgements.IsEmpty())
                    {
                        enet_protocol_send_acknowledgements(host, currentPeer);
                    }

                    if (checkForTimeouts != 0 &&
                        !currentPeer.sentReliableCommands.IsEmpty() &&
                        ENET_TIME_GREATER_EQUAL(host.serviceTime, currentPeer.nextTimeout) &&
                        enet_protocol_check_timeouts(host, currentPeer, @event) == 1
                       )
                    {
                        if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE)
                        {
                            return 1;
                        }
                        else
                        {
                            goto nextPeer;
                        }
                    }

                    if (((currentPeer.outgoingCommands.IsEmpty() &&
                          currentPeer.outgoingSendReliableCommands.IsEmpty()) ||
                         0 != enet_protocol_check_outgoing_commands(host, currentPeer, sentUnreliableCommands)) &&
                        currentPeer.sentReliableCommands.IsEmpty() &&
                        ENET_TIME_DIFFERENCE(host.serviceTime, currentPeer.lastReceiveTime) >= currentPeer.pingInterval &&
                        currentPeer.mtu - host.packetSize >= Marshal.SizeOf<ENetProtocolPing>()
                       )
                    {
                        enet_peer_ping(currentPeer);
                        enet_protocol_check_outgoing_commands(host, currentPeer, sentUnreliableCommands);
                    }

                    if (host.commandCount == 0)
                    {
                        goto nextPeer;
                    }

                    if (currentPeer.packetLossEpoch == 0)
                    {
                        currentPeer.packetLossEpoch = host.serviceTime;
                    }
                    else if (ENET_TIME_DIFFERENCE(host.serviceTime, currentPeer.packetLossEpoch) >= ENET_PEER_PACKET_LOSS_INTERVAL && currentPeer.packetsSent > 0)
                    {
                        long packetLoss = currentPeer.packetsLost * ENET_PEER_PACKET_LOSS_SCALE / currentPeer.packetsSent;

#if DEBUG
                        float packetLoss1 = currentPeer.packetLoss / (float)ENET_PEER_PACKET_LOSS_SCALE;
                        float packetLoss2 = currentPeer.packetLossVariance / (float)ENET_PEER_PACKET_LOSS_SCALE;
                        float throttle = currentPeer.packetThrottle / (float)ENET_PEER_PACKET_THROTTLE_SCALE;
                        int outgoing = currentPeer.outgoingCommands.Count;
                        int incoming1 = currentPeer.channels != null ? currentPeer.channels[0].incomingReliableCommands.Count : 0;
                        int incoming2 = currentPeer.channels != null ? currentPeer.channels[0].incomingUnreliableCommands.Count : 0;
                        print($"peer {currentPeer.incomingPeerID}: {packetLoss1}+-{packetLoss2} packet loss, {currentPeer.roundTripTime}+-{currentPeer.roundTripTimeVariance} ms round trip time, {throttle} throttle, {outgoing} outgoing, {incoming1}/{incoming2} incoming");
#endif

                        currentPeer.packetLossVariance = (currentPeer.packetLossVariance * 3 + ENET_DIFFERENCE(packetLoss, currentPeer.packetLoss)) / 4;
                        currentPeer.packetLoss = (currentPeer.packetLoss * 7 + packetLoss) / 8;

                        currentPeer.packetLossEpoch = host.serviceTime;
                        currentPeer.packetsSent = 0;
                        currentPeer.packetsLost = 0;
                    }

                    // todo : @ikpil check
                    enet_assert(false);
                    //host.buffers[0].data = headerData;
                    if (0 != (host.headerFlags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME))
                    {
                        header.sentTime = ENET_HOST_TO_NET_16((ushort)(host.serviceTime & 0xFFFF));
                        host.buffers[0].dataLength = Marshal.SizeOf<ENetProtocolHeader>();
                    }
                    else
                    {
                        host.buffers[0].dataLength = Marshal.SizeOf<ENetProtocolHeaderMinimal>();
                    }

                    shouldCompress = 0;
                    if (host.compressor.context != null && host.compressor.compress != null)
                    {
                        int originalSize = host.packetSize - Marshal.SizeOf<ENetProtocolHeader>(),
                            compressedSize = host.compressor.compress(host.compressor.context, ref host.buffers[1], host.bufferCount - 1, originalSize, host.packetData[1], originalSize);
                        if (compressedSize > 0 && compressedSize < originalSize)
                        {
                            host.headerFlags |= ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_COMPRESSED;
                            shouldCompress = compressedSize;
#if DEBUG
                            print($"peer {currentPeer.incomingPeerID}: compressed {originalSize}.{compressedSize} ({(compressedSize * 100) / originalSize})");
#endif
                        }
                    }

                    if (currentPeer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID)
                    {
                        host.headerFlags |= (ushort)(currentPeer.outgoingSessionID << ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
                    }

                    {
                        ushort basePeerID = (ushort)(currentPeer.outgoingPeerID & 0x07FF);
                        ushort flagsAndSession = (ushort)(host.headerFlags & 0xF800); /* top bits only */

                        if (currentPeer.outgoingPeerID > 0x07FF)
                        {
                            flagsAndSession |= ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA;
                            header.peerID = ENET_HOST_TO_NET_16((ushort)(basePeerID | flagsAndSession));
                            {
                                // todo : @ikpil check
                                enet_assert(false);
                                // byte overflowByte = (byte)((currentPeer.outgoingPeerID >> 11) & 0xFF);
                                // byte *extraPeerIDByte   = &headerData[host.buffers[0].dataLength];
                                // *extraPeerIDByte             = overflowByte;
                                // host.buffers[0].dataLength += Marshal.SizeOf<byte>();
                            }
                        }
                        else
                        {
                            header.peerID = ENET_HOST_TO_NET_16((ushort)(basePeerID | flagsAndSession));
                        }
                    }

                    if (host.checksum != null)
                    {
                        // todo : @ikpil check
                        enet_assert(false);
                        // uint *checksum = (uint *) &headerData[host.buffers[0].dataLength];
                        // *checksum = currentPeer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID ? currentPeer.connectID : 0;
                        // host.buffers[0].dataLength += Marshal.SizeOf<uint>();
                        // *checksum = host.checksum(host.buffers, host.bufferCount);
                    }

                    if (shouldCompress > 0)
                    {
                        host.buffers[1].data = host.packetData[1];
                        host.buffers[1].dataLength = shouldCompress;
                        host.bufferCount = 2;
                    }

                    currentPeer.lastSendTime = host.serviceTime;
                    sentLength = enet_socket_send(host.socket, ref currentPeer.address, host.buffers, host.bufferCount);
                    enet_protocol_remove_sent_unreliable_commands(currentPeer, sentUnreliableCommands);

                    if (sentLength < 0)
                    {
                        // The local 'headerData' array (to which 'data' is assigned) goes out
                        // of scope on return from this function, so ensure we no longer point to it.
                        host.buffers[0].data = null;
                        return -1;
                    }

                    host.totalSentData += sentLength;
                    currentPeer.totalDataSent += sentLength;
                    host.totalSentPackets++;
                    nextPeer:
                    if (0 != (currentPeer.flags & ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING))
                        continueSending = sendPass + 1;
                }
            }

            // The local 'headerData' array (to which 'data' is assigned) goes out
            // of scope on return from this function, so ensure we no longer point to it.
            host.buffers[0].data = null;

            return 0;
        } /* enet_protocol_send_outgoing_commands */
    }
}