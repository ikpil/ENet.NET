using System;
using System.Runtime.InteropServices;

namespace ENet.NET;

public static class ENetProtocols
{
   
    // =======================================================================//
    // !
    // ! Protocol
    // !
    // =======================================================================//
    public static uint[] commandSizes = new uint[(int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT]
    {
        (uint)0,
        (uint)Marshal.SizeOf<ENetProtocolAcknowledge>(),
        (uint)Marshal.SizeOf<ENetProtocolConnect>(),
        (uint)Marshal.SizeOf<ENetProtocolVerifyConnect>(),
        (uint)Marshal.SizeOf<ENetProtocolDisconnect>(),
        (uint)Marshal.SizeOf<ENetProtocolPing>(),
        (uint)Marshal.SizeOf<ENetProtocolSendReliable>(),
        (uint)Marshal.SizeOf<ENetProtocolSendUnreliable>(),
        (uint)Marshal.SizeOf<ENetProtocolSendFragment>(),
        (uint)Marshal.SizeOf<ENetProtocolSendUnsequenced>(),
        (uint)Marshal.SizeOf<ENetProtocolBandwidthLimit>(),
        (uint)Marshal.SizeOf<ENetProtocolThrottleConfigure>(),
        (uint)Marshal.SizeOf<ENetProtocolSendFragment>(),
    };

    public static uint enet_protocol_command_size(byte commandNumber) {
        return commandSizes[commandNumber & (uint)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK];
    }

    public static void enet_protocol_change_state(ENetHost host, ENetPeer peer, ENetPeerState state)
    {
        ENET_UNUSED(host);

        if (state == ENetPeerState.ENET_PEER_STATE_CONNECTED || state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            enet_peer_on_connect(peer);
        } else {
            enet_peer_on_disconnect(peer);
        }

        peer.state = state;
    }

    public static void enet_protocol_dispatch_state(ENetHost host, ENetPeer peer, ENetPeerState state) {
        enet_protocol_change_state(host, peer, state);

        if (0 == (peer.flags & (uint)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH)) {
            enet_list_insert(enet_list_end(ref host.dispatchQueue), peer.dispatchList);
            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
        }
    }

    public static int enet_protocol_dispatch_incoming_commands(ENetHost host, ENetEvent @event) {
        while (!enet_list_empty(ref host.dispatchQueue)) {
            ENetPeer peer = enet_list_remove(enet_list_begin(ref host.dispatchQueue));
            peer.flags = (ushort)(peer.flags & ~(ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH);

            switch (peer.state) {
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
                    if (enet_list_empty(ref peer.dispatchedCommands)) {
                        continue;
                    }

                    @event.packet = enet_peer_receive(peer, ref @event.channelID);
                    if (@event.packet == null) {
                        continue;
                    }

                    @event.type = ENetEventType.ENET_EVENT_TYPE_RECEIVE;
                    @event.peer = peer;

                    if (!enet_list_empty(ref peer.dispatchedCommands)) {
                        peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                        enet_list_insert(enet_list_end(ref host.dispatchQueue), peer.dispatchList);
                    }

                    return 1;

                default:
                    break;
            }
        }

        return 0;
    } /* enet_protocol_dispatch_incoming_commands */

    public static void enet_protocol_notify_connect(ENetHost host, ENetPeer peer, ENetEvent @event) {
        host.recalculateBandwidthLimits = 1;

        if (@event != null) {
            enet_protocol_change_state(host, peer, ENetPeerState.ENET_PEER_STATE_CONNECTED);

            peer.totalDataSent     = 0;
            peer.totalDataReceived = 0;
            peer.totalPacketsSent  = 0;
            peer.totalPacketsLost  = 0;

            @event.type = ENetEventType.ENET_EVENT_TYPE_CONNECT;
            @event.peer = peer;
            @event.data = peer.eventData;
        } else {
            enet_protocol_dispatch_state(host, peer, peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTING ? ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED : ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING);
        }
    }

    public static void enet_protocol_notify_disconnect(ENetHost host, ENetPeer peer, ENetEvent @event) {
        if (peer.state >= ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING) {
            host.recalculateBandwidthLimits = 1;
        }

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && peer.state < ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED) {
            enet_peer_reset(peer);
        } else if (@event != null) {
            @event.type = ENetEventType.ENET_EVENT_TYPE_DISCONNECT;
            @event.peer = peer;
            @event.data = 0;

            enet_peer_reset(peer);
        } else {
            peer.eventData = 0;
            enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
        }
    }

    public static void enet_protocol_notify_disconnect_timeout (ENetHost  host, ENetPeer  peer, ENetEvent @event) {
        if (peer.state >= ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING) {
           host.recalculateBandwidthLimits = 1;
        }

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && peer.state < ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED) {
            enet_peer_reset (peer);
        }
        else if (@event != null) {
            @event.type = ENetEventType.ENET_EVENT_TYPE_DISCONNECT_TIMEOUT;
            @event.peer = peer;
            @event.data = 0;

            enet_peer_reset(peer);
        }
        else {
            peer.eventData = 0;
            enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
        }
    }

    public static void enet_protocol_remove_sent_unreliable_commands(ENetPeer peer, ref ENetList<ENetOutgoingCommand> sentUnreliableCommands) 
    {

        if (enet_list_empty (ref sentUnreliableCommands))
            return;

        do
        {
            ref ENetOutgoingCommand outgoingCommand = ref enet_list_front(ref sentUnreliableCommands);
            enet_list_remove(outgoingCommand.outgoingCommandList);

            if (outgoingCommand.packet != null) {
                --outgoingCommand.packet.referenceCount;

                if (outgoingCommand.packet.referenceCount == 0) {
                    outgoingCommand.packet.flags |= (ushort)ENetPacketFlag.ENET_PACKET_FLAG_SENT;
                    callbacks.packet_destroy(outgoingCommand.packet);
                }
            }

            enet_free(outgoingCommand);
        } while (!enet_list_empty(ref sentUnreliableCommands));

        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER && !enet_peer_has_outgoing_commands(peer)) {
            enet_peer_disconnect(peer, peer.eventData);
        }
    }

    public static ENetOutgoingCommand enet_protocol_find_sent_reliable_command (ENetList<ENetOutgoingCommand> list, ushort reliableSequenceNumber, byte channelID) {
        ENetListNode<ENetOutgoingCommand> currentCommand;

        for (currentCommand = enet_list_begin(ref list);
            currentCommand != enet_list_end(ref list);
            currentCommand = enet_list_next(currentCommand))
        {
            ENetOutgoingCommand outgoingCommand = currentCommand.value;

            if (0 == (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE)) {
                continue;
            }

            if (outgoingCommand.sendAttempts < 1) {
                break;
            }

            if (outgoingCommand.reliableSequenceNumber == reliableSequenceNumber && outgoingCommand.command.header.channelID == channelID) {
                return outgoingCommand;
            }
        }

        return null;
    }

    public static ENetProtocolCommand enet_protocol_remove_sent_reliable_command(ENetPeer peer, ushort reliableSequenceNumber, byte channelID) {
        ENetOutgoingCommand outgoingCommand = null;
        ENetListNode<ENetOutgoingCommand> currentCommand;
        ENetProtocolCommand commandNumber;
        int wasSent = 1;

        for (currentCommand = enet_list_begin(ref peer.sentReliableCommands);
            currentCommand != enet_list_end(ref peer.sentReliableCommands);
            currentCommand = enet_list_next(currentCommand))
        {

            outgoingCommand = currentCommand.value;
            if (outgoingCommand.reliableSequenceNumber == reliableSequenceNumber && outgoingCommand.command.header.channelID == channelID) {
                break;
            }
        }

        if (currentCommand == enet_list_end(ref peer.sentReliableCommands)) {
            outgoingCommand = enet_protocol_find_sent_reliable_command(peer.outgoingCommands, reliableSequenceNumber, channelID);
            if (outgoingCommand == null) {
                outgoingCommand = enet_protocol_find_sent_reliable_command(peer.outgoingSendReliableCommands, reliableSequenceNumber, channelID);
            }

            wasSent = 0;
        }

        if (outgoingCommand == null) {
            return ENetProtocolCommand.ENET_PROTOCOL_COMMAND_NONE;
        }

        if (channelID < peer.channelCount) 
        {
            ENetChannel channel       = peer.channels[channelID];
            ushort reliableWindow = (ushort)(reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);
            if (channel.reliableWindows[reliableWindow] > 0) 
            {
                --channel.reliableWindows[reliableWindow];
                if (0 != channel.reliableWindows[reliableWindow])
                {
                    channel.usedReliableWindows &= (ushort)(~(1u << reliableWindow));
                }
            }
        }

        commandNumber = (ENetProtocolCommand) (outgoingCommand.command.header.command & (ushort)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK);
        enet_list_remove(outgoingCommand.outgoingCommandList);

        if (outgoingCommand.packet != null) {
            if (0 != wasSent) {
                peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
            }

            --outgoingCommand.packet.referenceCount;

            if (outgoingCommand.packet.referenceCount == 0) {
                outgoingCommand.packet.flags |= (uint)ENetPacketFlag.ENET_PACKET_FLAG_SENT;
                callbacks.packet_destroy(outgoingCommand.packet);
            }
        }

        enet_free(outgoingCommand);

        if (enet_list_empty(ref peer.sentReliableCommands)) {
            return commandNumber;
        }

        outgoingCommand = enet_list_front(ref peer.sentReliableCommands);
        peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;

        return commandNumber;
    } /* enet_protocol_remove_sent_reliable_command */

    public static ENetPeer enet_protocol_handle_connect(ENetHost host, ref ENetProtocolHeader header, ref ENetProtocol command)
    {
        ENET_UNUSED(header);

        byte incomingSessionID, outgoingSessionID;
        long mtu, windowSize;
        ENetChannel channel = null;
        long channelCount, duplicatePeers = 0;
        ENetPeer currentPeer, peer = null;
        ENetProtocol verifyCommand = new ENetProtocol();

        channelCount = ENET_NET_TO_HOST_32(command.connect.channelCount);

        if (channelCount < ENets.ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            return null;
        }

        for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
        {
            currentPeer = host.peers[peerIdx];
            if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED) {
                if (peer == null) {
                    peer = currentPeer;
                }
            } else if (currentPeer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING && in6_equal(currentPeer.address.host, host.receivedAddress.host)) {
                if (currentPeer.address.port == host.receivedAddress.port && currentPeer.connectID == command.connect.connectID) {
                    return null;
                }

                ++duplicatePeers;
            }
        }

        if (peer == null || duplicatePeers >= host.duplicatePeers) {
            return null;
        }

        if (channelCount > host.channelLimit) {
            channelCount = host.channelLimit;
        }

        peer.channels = enet_malloc<ENetChannel>(channelCount);
        if (peer.channels == null) {
            return null;
        }
        peer.channelCount               = channelCount;
        peer.state                      = ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT;
        peer.connectID                  = command.connect.connectID;
        peer.address                    = host.receivedAddress;
        peer.mtu                        = host.mtu;
        peer.outgoingPeerID             = ENET_NET_TO_HOST_16(command.connect.outgoingPeerID);
        peer.incomingBandwidth          = ENET_NET_TO_HOST_32(command.connect.incomingBandwidth);
        peer.outgoingBandwidth          = ENET_NET_TO_HOST_32(command.connect.outgoingBandwidth);
        peer.packetThrottleInterval     = ENET_NET_TO_HOST_32(command.connect.packetThrottleInterval);
        peer.packetThrottleAcceleration = ENET_NET_TO_HOST_32(command.connect.packetThrottleAcceleration);
        peer.packetThrottleDeceleration = ENET_NET_TO_HOST_32(command.connect.packetThrottleDeceleration);
        peer.eventData                  = ENET_NET_TO_HOST_32(command.connect.data);

        incomingSessionID = command.connect.incomingSessionID == 0xFF ? peer.outgoingSessionID : command.connect.incomingSessionID;
        incomingSessionID = (incomingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        if (incomingSessionID == peer.outgoingSessionID) {
            incomingSessionID = (incomingSessionID + 1)
              & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        }
        peer.outgoingSessionID = incomingSessionID;

        outgoingSessionID = command.connect.outgoingSessionID == 0xFF ? peer.incomingSessionID : command.connect.outgoingSessionID;
        outgoingSessionID = (outgoingSessionID + 1) & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        if (outgoingSessionID == peer.incomingSessionID) {
            outgoingSessionID = (outgoingSessionID + 1)
              & (ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        }
        peer.incomingSessionID = outgoingSessionID;

        for (int channelIdx = 0; channelIdx < channelCount; ++channelIdx)
        {
            channel = peer.channels[channelIdx];
            channel.outgoingReliableSequenceNumber   = 0;
            channel.outgoingUnreliableSequenceNumber = 0;
            channel.incomingReliableSequenceNumber   = 0;
            channel.incomingUnreliableSequenceNumber = 0;

            enet_list_clear(ref channel.incomingReliableCommands);
            enet_list_clear(ref channel.incomingUnreliableCommands);

            channel.usedReliableWindows = 0;
            memset(channel.reliableWindows, 0, sizeof(channel.reliableWindows));
        }

        mtu = ENET_NET_TO_HOST_32(command.connect.mtu);

        if (mtu < ENets.ENET_PROTOCOL_MINIMUM_MTU) {
            mtu = ENets.ENET_PROTOCOL_MINIMUM_MTU;
        } else if (mtu > ENets.ENET_PROTOCOL_MAXIMUM_MTU) {
            mtu = ENets.ENET_PROTOCOL_MAXIMUM_MTU;
        }

        if (mtu < peer.mtu)
            peer.mtu = mtu;

        if (host.outgoingBandwidth == 0 && peer.incomingBandwidth == 0) {
            peer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else if (host.outgoingBandwidth == 0 || peer.incomingBandwidth == 0) {
            peer.windowSize = (Math.Max(host.outgoingBandwidth, peer.incomingBandwidth) / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else {
            peer.windowSize = (Math.Min(host.outgoingBandwidth, peer.incomingBandwidth) / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (peer.windowSize < ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            peer.windowSize = ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (peer.windowSize > ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            peer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        if (host.incomingBandwidth == 0) {
            windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else {
            windowSize = (host.incomingBandwidth / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (windowSize > ENET_NET_TO_HOST_32(command.connect.windowSize)) {
            windowSize = ENET_NET_TO_HOST_32(command.connect.windowSize);
        }

        if (windowSize < ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            windowSize = ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (windowSize > ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        verifyCommand.header.command                            = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        verifyCommand.header.channelID                          = 0xFF;
        verifyCommand.verifyConnect.outgoingPeerID              = ENET_HOST_TO_NET_16(peer.incomingPeerID);
        verifyCommand.verifyConnect.incomingSessionID           = incomingSessionID;
        verifyCommand.verifyConnect.outgoingSessionID           = outgoingSessionID;
        verifyCommand.verifyConnect.mtu                         = ENET_HOST_TO_NET_32(peer.mtu);
        verifyCommand.verifyConnect.windowSize                  = ENET_HOST_TO_NET_32(windowSize);
        verifyCommand.verifyConnect.channelCount                = ENET_HOST_TO_NET_32(channelCount);
        verifyCommand.verifyConnect.incomingBandwidth           = ENET_HOST_TO_NET_32(host.incomingBandwidth);
        verifyCommand.verifyConnect.outgoingBandwidth           = ENET_HOST_TO_NET_32(host.outgoingBandwidth);
        verifyCommand.verifyConnect.packetThrottleInterval      = ENET_HOST_TO_NET_32(peer.packetThrottleInterval);
        verifyCommand.verifyConnect.packetThrottleAcceleration  = ENET_HOST_TO_NET_32(peer.packetThrottleAcceleration);
        verifyCommand.verifyConnect.packetThrottleDeceleration  = ENET_HOST_TO_NET_32(peer.packetThrottleDeceleration);
        verifyCommand.verifyConnect.connectID                   = peer.connectID;

        enet_peer_queue_outgoing_command(peer, ref verifyCommand, null, 0, 0);
        return peer;
    } /* enet_protocol_handle_connect */

    public static int enet_protocol_handle_send_reliable(ENetHost host, ENetPeer peer, ref ENetProtocol command, byte **currentData) {
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendReliable.dataLength);
        *currentData += dataLength;

        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const byte *) command + sizeof(ENetProtocolSendReliable), dataLength, ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE, 0) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_send_unsequenced(ENetHost host, ENetPeer peer, ref ENetProtocol command, byte **currentData) {
        uint unsequencedGroup, index;
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendUnsequenced.dataLength);
        *currentData += dataLength;
        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        unsequencedGroup = ENET_NET_TO_HOST_16(command.sendUnsequenced.unsequencedGroup);
        index = unsequencedGroup % ENets.ENET_PEER_UNSEQUENCED_WINDOW_SIZE;

        if (unsequencedGroup < peer.incomingUnsequencedGroup) {
            unsequencedGroup += 0x10000;
        }

        if (unsequencedGroup >= (uint) peer.incomingUnsequencedGroup + ENets.ENET_PEER_FREE_UNSEQUENCED_WINDOWS * ENets.ENET_PEER_UNSEQUENCED_WINDOW_SIZE) {
            return 0;
        }

        unsequencedGroup &= 0xFFFF;

        if (unsequencedGroup - index != peer.incomingUnsequencedGroup) {
            peer.incomingUnsequencedGroup = unsequencedGroup - index;
            memset(peer.unsequencedWindow, 0, sizeof(peer.unsequencedWindow));
        } else if (peer.unsequencedWindow[index / 32] & (1u << (index % 32))) {
            return 0;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const byte *) command + sizeof(ENetProtocolSendUnsequenced), dataLength, ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED,0) == null) {
            return -1;
        }

        peer.unsequencedWindow[index / 32] |= 1u << (index % 32);

        return 0;
    } /* enet_protocol_handle_send_unsequenced */

    public static int enet_protocol_handle_send_unreliable(ENetHost host, ENetPeer peer, ref ENetProtocol command, byte **currentData) {
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount ||
          (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER))
        {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendUnreliable.dataLength);
        *currentData += dataLength;
        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const byte *) command + sizeof(ENetProtocolSendUnreliable), dataLength, 0, 0) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_send_fragment(ENetHost host, ENetPeer peer, ref ENetProtocol command, byte **currentData) {
        uint fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, startSequenceNumber, totalLength;
        ENetChannel channel = null;
        ushort startWindow, currentWindow;
        ENetListNode<ENetIncomingCommand> currentCommand = null;
        ENetIncomingCommand startCommand = null;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        fragmentLength = ENET_NET_TO_HOST_16(command.sendFragment.dataLength);
        *currentData  += fragmentLength;
        if (fragmentLength <= 0 ||
            fragmentLength > host.maximumPacketSize ||
            *currentData < host.receivedData ||
            *currentData > &host.receivedData[host.receivedDataLength]
        ) {
            return -1;
        }

        channel = peer.channels[command.header.channelID];
        startSequenceNumber = ENET_NET_TO_HOST_16(command.sendFragment.startSequenceNumber);
        startWindow         = startSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
        currentWindow       = channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;

        if (startSequenceNumber < channel.incomingReliableSequenceNumber) {
            startWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
        }

        if (startWindow < currentWindow || startWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
            return 0;
        }

        fragmentNumber = ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
        fragmentCount  = ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
        fragmentOffset = ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
        totalLength    = ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

        if (fragmentCount > ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
            fragmentNumber >= fragmentCount ||
            totalLength > host.maximumPacketSize ||
            totalLength < fragmentCount ||
            fragmentOffset >= totalLength ||
            fragmentLength > totalLength - fragmentOffset
        ) {
            return -1;
        }

        for (currentCommand = enet_list_previous(enet_list_end(ref channel.incomingReliableCommands));
            currentCommand != enet_list_end(ref channel.incomingReliableCommands);
            currentCommand = enet_list_previous(currentCommand)
        )
        {
            ENetIncomingCommand incomingCommand = currentCommand.value;

            if (startSequenceNumber >= channel.incomingReliableSequenceNumber) {
                if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                    continue;
                }
            } else if (incomingCommand.reliableSequenceNumber >= channel.incomingReliableSequenceNumber) {
                break;
            }

            if (incomingCommand.reliableSequenceNumber <= startSequenceNumber) {
                if (incomingCommand.reliableSequenceNumber < startSequenceNumber) {
                    break;
                }

                if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) !=
                    ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT ||
                    totalLength != incomingCommand.packet.dataLength ||
                    fragmentCount != incomingCommand.fragmentCount
                ) {
                    return -1;
                }

                startCommand = incomingCommand;
                break;
            }
        }

        if (startCommand == null) {
            ENetProtocol hostCommand = *command;
            hostCommand.header.reliableSequenceNumber = startSequenceNumber;
            startCommand = enet_peer_queue_incoming_command(peer, &hostCommand, null, totalLength, ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE, fragmentCount);
            if (startCommand == null) {
                return -1;
            }
        }

        if ((startCommand.fragments[fragmentNumber / 32] & (1u << (fragmentNumber % 32))) == 0) {
            --startCommand.fragmentsRemaining;
            startCommand.fragments[fragmentNumber / 32] |= (1u << (fragmentNumber % 32));

            if (fragmentOffset + fragmentLength > startCommand.packet.dataLength) {
                fragmentLength = startCommand.packet.dataLength - fragmentOffset;
            }

            memcpy(startCommand.packet.data + fragmentOffset, (byte *) command + sizeof(ENetProtocolSendFragment), fragmentLength);

            if (startCommand.fragmentsRemaining <= 0) {
                enet_peer_dispatch_incoming_reliable_commands(peer, channel, null);
            }
        }

        return 0;
    } /* enet_protocol_handle_send_fragment */

    public static int enet_protocol_handle_send_unreliable_fragment(ENetHost host, ENetPeer peer, ref ENetProtocol command, byte **currentData) {
        uint fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, reliableSequenceNumber, startSequenceNumber, totalLength;
        ushort reliableWindow, currentWindow;
        ENetChannel channel = null;
        ENetListNode<ENetIncomingCommand> currentCommand;
        ENetIncomingCommand startCommand = null;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        fragmentLength = ENET_NET_TO_HOST_16(command.sendFragment.dataLength);
        *currentData  += fragmentLength;
        if (fragmentLength <= 0 ||
            fragmentLength > host.maximumPacketSize ||
            *currentData < host.receivedData ||
            *currentData > &host.receivedData[host.receivedDataLength]
        ) {
            return -1;
        }

        channel = &peer.channels[command.header.channelID];
        reliableSequenceNumber = command.header.reliableSequenceNumber;
        startSequenceNumber    = ENET_NET_TO_HOST_16(command.sendFragment.startSequenceNumber);

        reliableWindow = reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
        currentWindow  = channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;

        if (reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
            reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
        }

        if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
            return 0;
        }

        if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && startSequenceNumber <= channel.incomingUnreliableSequenceNumber) {
            return 0;
        }

        fragmentNumber = ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
        fragmentCount  = ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
        fragmentOffset = ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
        totalLength    = ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

        if (fragmentCount > ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
            fragmentNumber >= fragmentCount ||
            totalLength > host.maximumPacketSize ||
            totalLength < fragmentCount ||
            fragmentOffset >= totalLength ||
            fragmentLength > totalLength - fragmentOffset
        ) {
            return -1;
        }

        for (currentCommand = enet_list_previous(enet_list_end(ref channel.incomingUnreliableCommands));
            currentCommand != enet_list_end(ref channel.incomingUnreliableCommands);
            currentCommand = enet_list_previous(currentCommand)
        )
        {
            ENetIncomingCommand incomingCommand = currentCommand.value;

            if (reliableSequenceNumber >= channel.incomingReliableSequenceNumber) {
                if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                    continue;
                }
            } else if (incomingCommand.reliableSequenceNumber >= channel.incomingReliableSequenceNumber) {
                break;
            }

            if (incomingCommand.reliableSequenceNumber < reliableSequenceNumber) {
                break;
            }

            if (incomingCommand.reliableSequenceNumber > reliableSequenceNumber) {
                continue;
            }

            if (incomingCommand.unreliableSequenceNumber <= startSequenceNumber) {
                if (incomingCommand.unreliableSequenceNumber < startSequenceNumber) {
                    break;
                }

                if ((incomingCommand.command.header.command & (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) !=
                    (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT ||
                    totalLength != incomingCommand.packet.dataLength ||
                    fragmentCount != incomingCommand.fragmentCount
                ) {
                    return -1;
                }

                startCommand = incomingCommand;
                break;
            }
        }

        if (startCommand == null) {
            startCommand = enet_peer_queue_incoming_command(peer, ref command, null, totalLength,
                ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT, fragmentCount);
            if (startCommand == null) {
                return -1;
            }
        }

        if ((startCommand.fragments[fragmentNumber / 32] & (1u << (fragmentNumber % 32))) == 0) {
            --startCommand.fragmentsRemaining;
            startCommand.fragments[fragmentNumber / 32] |= (1u << (fragmentNumber % 32));

            if (fragmentOffset + fragmentLength > startCommand.packet.dataLength) {
                fragmentLength = startCommand.packet.dataLength - fragmentOffset;
            }

            memcpy(startCommand.packet.data + fragmentOffset, (byte *) command + sizeof(ENetProtocolSendFragment), fragmentLength);

            if (startCommand.fragmentsRemaining <= 0) {
                enet_peer_dispatch_incoming_unreliable_commands(peer, channel, null);
            }
        }

        return 0;
    } /* enet_protocol_handle_send_unreliable_fragment */

    public static int enet_protocol_handle_ping(ENetHost host, ENetPeer peer, ref ENetProtocol command)
    {
        ENET_UNUSED(host);
        ENET_UNUSED(command);

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_bandwidth_limit(ENetHost host, ENetPeer peer, ref ENetProtocol command) {
        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            return -1;
        }
        if (peer.incomingBandwidth != 0) {
            --host.bandwidthLimitedPeers;
        }

        peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.bandwidthLimit.incomingBandwidth);
        if (peer.incomingBandwidth != 0) {
            ++host.bandwidthLimitedPeers;
        }

        peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.bandwidthLimit.outgoingBandwidth);

        if (peer.incomingBandwidth == 0 && host.outgoingBandwidth == 0) {
            peer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else if (peer.incomingBandwidth == 0 || host.outgoingBandwidth == 0) {
            peer.windowSize = (Math.Max(peer.incomingBandwidth, host.outgoingBandwidth)
              / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else {
            peer.windowSize = (Math.Min(peer.incomingBandwidth, host.outgoingBandwidth)
              / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (peer.windowSize < ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            peer.windowSize = ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (peer.windowSize > ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            peer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        return 0;
    } /* enet_protocol_handle_bandwidth_limit */

    public static int enet_protocol_handle_throttle_configure(ENetHost host, ENetPeer peer, ref ENetProtocol command)
    {
        ENET_UNUSED(host);

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            return -1;
        }

        peer.packetThrottleInterval     = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleInterval);
        peer.packetThrottleAcceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleAcceleration);
        peer.packetThrottleDeceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleDeceleration);

        return 0;
    }

    public static int enet_protocol_handle_disconnect(ENetHost host, ENetPeer peer, ref ENetProtocol command) {
        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE ||
            peer.state == ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT
        ) {
            return 0;
        }

        enet_peer_reset_queues(peer);

        if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTION_SUCCEEDED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTING || peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTING) {
            enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
        }
        else if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTION_PENDING) { host.recalculateBandwidthLimits = 1; }
            enet_peer_reset(peer);
        }
        else if (command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
            enet_protocol_change_state(host, peer, ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT);
        }
        else {
            enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
        }

        if (peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECTED) {
            peer.eventData = ENET_NET_TO_HOST_32(command.disconnect.data);
        }

        return 0;
    }

    public static int enet_protocol_handle_acknowledge(ENetHost host, ENetEvent @event, ENetPeer peer, ref ENetProtocol command) {
        uint roundTripTime, receivedSentTime, receivedReliableSequenceNumber;
        ENetProtocolCommand commandNumber;

        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE) {
            return 0;
        }

        receivedSentTime  = ENET_NET_TO_HOST_16(command.acknowledge.receivedSentTime);
        receivedSentTime |= host.serviceTime & 0xFFFF0000;
        if ((receivedSentTime & 0x8000) > (host.serviceTime & 0x8000)) {
            receivedSentTime -= 0x10000;
        }

        if (ENET_TIME_LESS(host.serviceTime, receivedSentTime)) {
            return 0;
        }

        roundTripTime = ENET_TIME_DIFFERENCE(host.serviceTime, receivedSentTime);
        roundTripTime = Math.Max(roundTripTime, 1);

        if (peer.lastReceiveTime > 0) {
            enet_peer_throttle(peer, roundTripTime);

            peer.roundTripTimeVariance -= peer.roundTripTimeVariance / 4;

            if (roundTripTime >= peer.roundTripTime) {
                uint diff = roundTripTime - peer.roundTripTime;
                peer.roundTripTimeVariance += diff / 4;
                peer.roundTripTime += diff / 8;
            } else {
                uint diff = peer.roundTripTime - roundTripTime;
                peer.roundTripTimeVariance += diff / 4;
                peer.roundTripTime -= diff / 8;
            }
        } else {
            peer.roundTripTime = roundTripTime;
            peer.roundTripTimeVariance = (roundTripTime + 1) / 2;
        }

        if (peer.roundTripTime < peer.lowestRoundTripTime) {
            peer.lowestRoundTripTime = peer.roundTripTime;
        }

        if (peer.roundTripTimeVariance > peer.highestRoundTripTimeVariance) {
            peer.highestRoundTripTimeVariance = peer.roundTripTimeVariance;
        }

        if (peer.packetThrottleEpoch == 0 ||
            ENET_TIME_DIFFERENCE(host.serviceTime, peer.packetThrottleEpoch) >= peer.packetThrottleInterval
        ) {
            peer.lastRoundTripTime            = peer.lowestRoundTripTime;
            peer.lastRoundTripTimeVariance    = Math.Max (peer.highestRoundTripTimeVariance, 1);
            peer.lowestRoundTripTime          = peer.roundTripTime;
            peer.highestRoundTripTimeVariance = peer.roundTripTimeVariance;
            peer.packetThrottleEpoch          = host.serviceTime;
        }

        peer.lastReceiveTime = Math.Max(host.serviceTime, 1);
        peer.earliestTimeout = 0;

        receivedReliableSequenceNumber = ENET_NET_TO_HOST_16(command.acknowledge.receivedReliableSequenceNumber);
        commandNumber = enet_protocol_remove_sent_reliable_command(peer, (ushort)receivedReliableSequenceNumber, command.header.channelID);

        switch (peer.state) {
            case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                if (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT) {
                    return -1;
                }

                enet_protocol_notify_connect(host, peer, @event);
                break;

            case ENetPeerState.ENET_PEER_STATE_DISCONNECTING:
                if (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT) {
                    return -1;
                }

                enet_protocol_notify_disconnect(host, peer, @event);
                break;

            case ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER:
                if (!enet_peer_has_outgoing_commands(peer)) {
                    enet_peer_disconnect(peer, peer.eventData);
                }
                break;

            default:
                break;
        }

        return 0;
    } /* enet_protocol_handle_acknowledge */

    public static int enet_protocol_handle_verify_connect(ENetHost host, ENetEvent @event, ENetPeer peer, ref ENetProtocol command) {
        uint mtu, windowSize;
        ulong channelCount;

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTING) {
            return 0;
        }

        channelCount = ENET_NET_TO_HOST_32(command.verifyConnect.channelCount);

        if (channelCount < ENets.ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleInterval) != peer.packetThrottleInterval ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleAcceleration) != peer.packetThrottleAcceleration ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleDeceleration) != peer.packetThrottleDeceleration ||
            command.verifyConnect.connectID != peer.connectID
        ) {
            peer.eventData = 0;
            enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            return -1;
        }

        enet_protocol_remove_sent_reliable_command(peer, 1, 0xFF);

        if (channelCount < peer.channelCount) {
            peer.channelCount = channelCount;
        }

        peer.outgoingPeerID    = ENET_NET_TO_HOST_16(command.verifyConnect.outgoingPeerID);
        peer.incomingSessionID = command.verifyConnect.incomingSessionID;
        peer.outgoingSessionID = command.verifyConnect.outgoingSessionID;

        mtu = ENET_NET_TO_HOST_32(command.verifyConnect.mtu);

        if (mtu < ENets.ENET_PROTOCOL_MINIMUM_MTU) {
            mtu = ENets.ENET_PROTOCOL_MINIMUM_MTU;
        } else if (mtu > ENets.ENET_PROTOCOL_MAXIMUM_MTU) {
            mtu = ENets.ENET_PROTOCOL_MAXIMUM_MTU;
        }

        if (mtu < peer.mtu) {
            peer.mtu = mtu;
        }

        windowSize = ENET_NET_TO_HOST_32(command.verifyConnect.windowSize);
        if (windowSize < ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            windowSize = ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (windowSize > ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        if (windowSize < peer.windowSize) {
            peer.windowSize = windowSize;
        }

        peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.incomingBandwidth);
        peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.outgoingBandwidth);

        enet_protocol_notify_connect(host, peer, @event);
        return 0;
    } /* enet_protocol_handle_verify_connect */

    public static int enet_protocol_handle_incoming_commands(ENetHost host, ENetEvent @event) {
        ENetProtocol command = new ENetProtocol();
        ENetPeer peer = null;
        byte *currentData;
        long headerSize;
        ushort peerID, flags;
        byte sessionID;

        if (host.receivedDataLength < Marshal.SizeOf<ENetProtocolHeaderMinimal>()) {
            return 0;
        }

        ref ENetProtocolHeader header = ref host.receivedData;

        peerID    = ENET_NET_TO_HOST_16(header.peerID);
        sessionID = (byte)((peerID & (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK) >> (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        flags = (ushort)(peerID & (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK);
        peerID = (ushort)(peerID & ~((ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK | (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK));

        headerSize = 0 != (flags & (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME) 
            ? Marshal.SizeOf<ENetProtocolHeader>() 
            : Marshal.SizeOf<ENetProtocolHeaderMinimal>();

        if (0 != (flags & (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA)) {
            if (host.receivedDataLength < headerSize + sizeof(byte)) {
                return 0;
            }

            byte * headerExtraPeerID = (byte *) & host.receivedData [headerSize];
            byte peerIDExtra = *headerExtraPeerID;
            peerID = (peerID & 0x07FF) | ((ushort)peerIDExtra << 11);

            headerSize += sizeof (byte);
        }

        if (host.checksum != null) {
            if (host.receivedDataLength < headerSize + sizeof(uint)) {
                return 0;
            }

            headerSize += sizeof(uint);
        }

        if (peerID == ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID) {
            peer = null;
        } else if (peerID >= host.peerCount) {
            return 0;
        } else {
            peer = host.peers[peerID];

            if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED ||
                peer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE ||
                ((!in6_equal(host.receivedAddress.host , peer.address.host) ||
                host.receivedAddress.port != peer.address.port) &&
                1 /* no broadcast in ipv6  !in6_equal(peer.address.host , ENET_HOST_BROADCAST)*/) ||
                (peer.outgoingPeerID < ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID &&
                sessionID != peer.incomingSessionID)
            ) {
                return 0;
            }
        }

        if (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_COMPRESSED) {
            ulong originalSize;
            if (host.compressor.context == null || host.compressor.decompress == null) {
                return 0;
            }

            originalSize = host.compressor.decompress(host.compressor.context,
                host.receivedData + headerSize,
                host.receivedDataLength - headerSize,
                host.packetData[1] + headerSize,
                sizeof(host.packetData[1]) - headerSize
            );

            if (originalSize <= 0 || originalSize > sizeof(host.packetData[1]) - headerSize) {
                return 0;
            }

            memcpy(host.packetData[1], header, headerSize);
            host.receivedData       = host.packetData[1];
            host.receivedDataLength = headerSize + originalSize;
        }

        if (host.checksum != null) {
            uint *checksum = (uint *) &host.receivedData[headerSize - sizeof(uint)];
            uint desiredChecksum = *checksum;
            ENetBuffer buffer;

            *checksum = peer != null ? peer.connectID : 0;

            buffer.data       = host.receivedData;
            buffer.dataLength = host.receivedDataLength;

            if (host.checksum(ref buffer, 1) != desiredChecksum) {
                return 0;
            }
        }

        if (peer != null) {
            peer.address.host       = host.receivedAddress.host;
            peer.address.port       = host.receivedAddress.port;
            peer.incomingDataTotal += (uint)host.receivedDataLength;
            peer.totalDataReceived += host.receivedDataLength;
        }

        currentData = host.receivedData + headerSize;

        while (currentData < &host.receivedData[host.receivedDataLength]) {
            byte commandNumber;
            ulong commandSize;

            command = (ENetProtocol *) currentData;

            if (currentData + sizeof(ENetProtocolCommandHeader) > &host.receivedData[host.receivedDataLength]) {
                break;
            }

            commandNumber = (byte)(command.header.command & (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK);
            if (commandNumber >= (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT) {
                break;
            }

            commandSize = commandSizes[commandNumber];
            if (commandSize == 0 || currentData + commandSize > &host.receivedData[host.receivedDataLength]) {
                break;
            }

            currentData += commandSize;

            if (peer == null && (commandNumber != (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT || currentData < &host.receivedData[host.receivedDataLength])) {
                break;
            }

            command.header.reliableSequenceNumber = ENET_NET_TO_HOST_16(command.header.reliableSequenceNumber);

            switch ((ENetProtocolCommand)commandNumber) {
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_ACKNOWLEDGE:
                    if (0 != enet_protocol_handle_acknowledge(host, @event, peer, ref command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT:
                    if (peer != null) {
                        goto commandError;
                    }
                    peer = enet_protocol_handle_connect(host, ref header, ref command);
                    if (peer == null) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_VERIFY_CONNECT:
                    if (0 != enet_protocol_handle_verify_connect(host, @event, peer, ref command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT:
                    if (0 != enet_protocol_handle_disconnect(host, peer, ref command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_PING:
                    if (0 != enet_protocol_handle_ping(host, peer, ref command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                    if (enet_protocol_handle_send_reliable(host, peer, ref command, currentData)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                    if (enet_protocol_handle_send_unreliable(host, peer, ref command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                    if (enet_protocol_handle_send_unsequenced(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
                    if (enet_protocol_handle_send_fragment(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT:
                    if (enet_protocol_handle_bandwidth_limit(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE:
                    if (enet_protocol_handle_throttle_configure(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                    if (enet_protocol_handle_send_unreliable_fragment(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                default:
                    goto commandError;
            }

            assert(peer);
            if ((command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0) {
                ushort sentTime;

                if (!(flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME)) {
                    break;
                }

                sentTime = ENET_NET_TO_HOST_16(header.sentTime);

                switch (peer.state) {
                    case ENetPeerState.ENET_PEER_STATE_DISCONNECTING:
                    case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                    case ENetPeerState.ENET_PEER_STATE_DISCONNECTED:
                    case ENetPeerState.ENET_PEER_STATE_ZOMBIE:
                        break;

                    case ENetPeerState.ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT:
                        if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT) {
                            enet_peer_queue_acknowledgement(peer, command, sentTime);
                        }
                        break;

                    default:
                        enet_peer_queue_acknowledgement(peer, command, sentTime);
                        break;
                }
            }
        }

    commandError:
        if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE) {
            return 1;
        }

        return 0;
    } /* enet_protocol_handle_incoming_commands */

    public static int enet_protocol_receive_incoming_commands(ENetHost host, ENetEvent @event) {
        int packets;

        for (packets = 0; packets < 256; ++packets) {
            int receivedLength;
            ENetBuffer buffer = new ENetBuffer();

            buffer.data       = host.packetData[0];
            // buffer.dataLength = sizeof (host.packetData[0]);
            buffer.dataLength = host.mtu;

            receivedLength    = enet_socket_receive(host.socket, ref host.receivedAddress, ref buffer, 1);

            if (receivedLength == -2)
                continue;

            if (receivedLength < 0) {
                return -1;
            }

            if (receivedLength == 0) {
                return 0;
            }

            host.receivedData       = host.packetData[0];
            host.receivedDataLength = receivedLength;

            host.totalReceivedData += receivedLength;
            host.totalReceivedPackets++;

            if (host.intercept != null) {
                switch (host.intercept(host, (void *)@event)) {
                    case 1:
                        if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE) {
                            return 1;
                        }

                        continue;

                    case -1:
                        return -1;

                    default:
                        break;
                }
            }

            switch (enet_protocol_handle_incoming_commands(host, @event)) {
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

    public static void enet_protocol_send_acknowledgements(ENetHost host, ENetPeer peer) {
        ref ENetProtocol command = ref host.commands[host.commandCount];
        ref ENetBuffer buffer    = ref host.buffers[host.bufferCount];
        ENetListNode<ENetAcknowledgement> currentAcknowledgement = null;
        ushort reliableSequenceNumber;

        currentAcknowledgement = enet_list_begin(ref peer.acknowledgements);

        while (currentAcknowledgement != enet_list_end(ref peer.acknowledgements)) {
            if (command >= &host.commands[sizeof(host.commands) / sizeof(ENetProtocol)] ||
                buffer >= &host.buffers[sizeof(host.buffers) / sizeof(ENetBuffer)] ||
                peer.mtu - host.packetSize < sizeof(ENetProtocolAcknowledge)
            ) {
                peer.flags |= ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING;
                break;
            }

            ref ENetAcknowledgement acknowledgement = ref currentAcknowledgement.value;
            currentAcknowledgement = enet_list_next(currentAcknowledgement);

            buffer.data       = command;
            buffer.dataLength = sizeof(ENetProtocolAcknowledge);
            host.packetSize += buffer.dataLength;

            reliableSequenceNumber = ENET_HOST_TO_NET_16(acknowledgement.command.header.reliableSequenceNumber);

            command.header.command   = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_ACKNOWLEDGE;
            command.header.channelID = acknowledgement.command.header.channelID;
            command.header.reliableSequenceNumber = reliableSequenceNumber;
            command.acknowledge.receivedReliableSequenceNumber = reliableSequenceNumber;
            command.acknowledge.receivedSentTime = ENET_HOST_TO_NET_16(acknowledgement.sentTime);

            if ((acknowledgement.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT) {
                enet_protocol_dispatch_state(host, peer, ENetPeerState.ENET_PEER_STATE_ZOMBIE);
            }

            enet_list_remove(&acknowledgement.acknowledgementList);
            enet_free(acknowledgement);

            ++command;
            ++buffer;
        }

        host.commandCount = command - host.commands;
        host.bufferCount  = buffer - host.buffers;
    } /* enet_protocol_send_acknowledgements */

    public static int enet_protocol_check_timeouts(ENetHost host, ENetPeer peer, ENetEvent @event)
    {
        ENetOutgoingCommand outgoingCommand = null;
        ENetListNode<ENetOutgoingCommand> currentCommand, insertPosition, insertSendReliablePosition;

        currentCommand = enet_list_begin(ref peer.sentReliableCommands);
        insertPosition = enet_list_begin(ref peer.outgoingCommands);
        insertSendReliablePosition = enet_list_begin(ref peer.outgoingSendReliableCommands);

        while (currentCommand != enet_list_end(ref peer.sentReliableCommands)) {
            outgoingCommand = currentCommand.value;

            currentCommand = enet_list_next(currentCommand);

            if (ENET_TIME_DIFFERENCE(host.serviceTime, outgoingCommand.sentTime) < outgoingCommand.roundTripTimeout) {
                continue;
            }

            if (peer.earliestTimeout == 0 || ENET_TIME_LESS(outgoingCommand.sentTime, peer.earliestTimeout)) {
                peer.earliestTimeout = outgoingCommand.sentTime;
            }

            if (peer.earliestTimeout != 0 &&
                (ENET_TIME_DIFFERENCE(host.serviceTime, peer.earliestTimeout) >= peer.timeoutMaximum ||
                ((1u << (outgoingCommand.sendAttempts - 1)) >= peer.timeoutLimit &&
                ENET_TIME_DIFFERENCE(host.serviceTime, peer.earliestTimeout) >= peer.timeoutMinimum))
            ) {
                enet_protocol_notify_disconnect_timeout(host, peer, @event);
                return 1;
            }

            ++peer.packetsLost;
            ++peer.totalPacketsLost;

            /* Replaced exponential backoff time with something more linear */
            /* Source: http://lists.cubik.org/pipermail/enet-discuss/2014-May/002308.html */
            outgoingCommand.roundTripTimeout = peer.roundTripTime + 4 * peer.roundTripTimeVariance;

            if (outgoingCommand.packet != null) {
                peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
                enet_list_insert(insertSendReliablePosition, enet_list_remove (outgoingCommand.outgoingCommandList));
            } else {
                enet_list_insert(insertPosition, enet_list_remove (outgoingCommand.outgoingCommandList));
            }

            if (currentCommand == enet_list_begin(ref peer.sentReliableCommands) && !enet_list_empty(ref peer.sentReliableCommands)) {
                outgoingCommand = currentCommand.value;
                peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;
            }
        }

        return 0;
    } /* enet_protocol_check_timeouts */

    public static int enet_protocol_check_outgoing_commands(ENetHost host, ENetPeer peer, ENetList<ENetOutgoingCommand> sentUnreliableCommands) {
        ref ENetProtocol command = ref host.commands[host.commandCount];
        ref ENetBuffer buffer    = ref host.buffers[host.bufferCount];
        ENetOutgoingCommand outgoingCommand = null;
        ENetListNode<ENetOutgoingCommand> currentCommand, currentSendReliableCommand = null;
        ENetChannel channel = null;
        ushort reliableWindow = 0;
        ulong commandSize=0;
        int windowWrap = 0, canPing = 1;

        currentCommand = enet_list_begin(ref peer.outgoingCommands);
        currentSendReliableCommand = enet_list_begin (peer.outgoingSendReliableCommands);

        for (;;) {
            if (currentCommand != enet_list_end (ref peer.outgoingCommands))
            {
                outgoingCommand = currentCommand.value;
                if (currentSendReliableCommand != enet_list_end (ref  peer.outgoingSendReliableCommands) && ENET_TIME_LESS (currentSendReliableCommand.value.queueTime, outgoingCommand.queueTime)) {
                    goto useSendReliableCommand;
                }
                currentCommand = enet_list_next (currentCommand);
            } else if (currentSendReliableCommand != enet_list_end (ref  peer.outgoingSendReliableCommands)) {
                useSendReliableCommand:
                outgoingCommand = currentSendReliableCommand.value;
                currentSendReliableCommand = enet_list_next (currentSendReliableCommand);
            } else {
                break;
            }


            if (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
                channel = outgoingCommand.command.header.channelID < peer.channelCount ? & peer.channels [outgoingCommand.command.header.channelID] : null;
                reliableWindow = outgoingCommand.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
                if (channel != null) {
                    if (windowWrap) {
                        continue;
                    } else if (outgoingCommand.sendAttempts < 1 && 
                            !(outgoingCommand.reliableSequenceNumber % ENets.ENET_PEER_RELIABLE_WINDOW_SIZE) &&
                            (channel.reliableWindows [(reliableWindow + ENets.ENET_PEER_RELIABLE_WINDOWS - 1) % ENets.ENET_PEER_RELIABLE_WINDOWS] >= ENets.ENET_PEER_RELIABLE_WINDOW_SIZE ||
                            channel.usedReliableWindows & ((((1u << (ENets.ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) << reliableWindow) | 
                            (((1u << (ENets.ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) >> (ENets.ENET_PEER_RELIABLE_WINDOWS - reliableWindow))))) 
                    {
                        windowWrap = 1;
                        currentSendReliableCommand = enet_list_end(ref peer.outgoingSendReliableCommands);
                        continue;
                    }
                }

                if (outgoingCommand.packet != null) {
                    uint windowSize = (peer.packetThrottle * peer.windowSize) / ENets.ENET_PEER_PACKET_THROTTLE_SCALE;

                    if (peer.reliableDataInTransit + outgoingCommand.fragmentLength > Math.Max (windowSize, peer.mtu))
                    {
                        currentSendReliableCommand = enet_list_end(ref peer.outgoingSendReliableCommands);
                        continue;
                    }
                }

                canPing = 0;
            }

            commandSize = commandSizes[outgoingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK];
            if (command >= &host.commands[sizeof(host.commands) / sizeof(ENetProtocol)] ||
                buffer + 1 >= &host.buffers[sizeof(host.buffers) / sizeof(ENetBuffer)] ||
                peer.mtu - host.packetSize < commandSize ||
                (outgoingCommand.packet != null &&
                (ushort) (peer.mtu - host.packetSize) < (ushort) (commandSize + outgoingCommand.fragmentLength))
            ) {
                peer.flags |= ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING;
                break;
            }

            if (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
                channel = outgoingCommand.command.header.channelID < peer.channelCount ? & peer.channels [outgoingCommand.command.header.channelID] : null;
                reliableWindow = outgoingCommand.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
                if (channel != null && outgoingCommand.sendAttempts < 1) {
                    channel.usedReliableWindows |= 1u << reliableWindow;
                    ++channel.reliableWindows[reliableWindow];
                }

                ++outgoingCommand.sendAttempts;

                if (outgoingCommand.roundTripTimeout == 0) {
                    outgoingCommand.roundTripTimeout = peer.roundTripTime + 4 * peer.roundTripTimeVariance;
                }

                if (enet_list_empty(ref peer.sentReliableCommands)) {
                    peer.nextTimeout = host.serviceTime + outgoingCommand.roundTripTimeout;
                }

                enet_list_insert(enet_list_end(ref peer.sentReliableCommands), enet_list_remove(outgoingCommand.outgoingCommandList));

                outgoingCommand.sentTime = host.serviceTime;

                host.headerFlags |= (ushort)ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME;
                peer.reliableDataInTransit += outgoingCommand.fragmentLength;
            } else {
                if (outgoingCommand.packet != null && outgoingCommand.fragmentOffset == 0) {
                    peer.packetThrottleCounter += ENets.ENET_PEER_PACKET_THROTTLE_COUNTER;
                    peer.packetThrottleCounter %= ENets.ENET_PEER_PACKET_THROTTLE_SCALE;

                    if (peer.packetThrottleCounter > peer.packetThrottle) {
                        ushort reliableSequenceNumber = outgoingCommand.reliableSequenceNumber,
                                    unreliableSequenceNumber = outgoingCommand.unreliableSequenceNumber;
                        for (;;)
                        {
                            --outgoingCommand.packet.referenceCount;

                            if (outgoingCommand.packet.referenceCount == 0) {
                                enet_packet_destroy(outgoingCommand.packet);
                            }

                            enet_list_remove(outgoingCommand.outgoingCommandList);
                            enet_free(outgoingCommand);

                            if (currentCommand == enet_list_end (ref peer.outgoingCommands)) {
                                break;
                            }

                            outgoingCommand = currentCommand.value;
                            if (outgoingCommand.reliableSequenceNumber != reliableSequenceNumber ||
                                outgoingCommand.unreliableSequenceNumber != unreliableSequenceNumber) {
                                break;
                            }

                            currentCommand = enet_list_next(currentCommand);
                        }

                        continue;
                    }
                }

                enet_list_remove(&outgoingCommand.outgoingCommandList);

                if (outgoingCommand.packet != null) {
                    enet_list_insert(enet_list_end (ref sentUnreliableCommands), outgoingCommand);
                }
            }


            buffer.data       = command;
            buffer.dataLength = commandSize;

            host.packetSize  += buffer.dataLength;

            command = outgoingCommand.command;

            if (outgoingCommand.packet != null) {
                ++buffer;
                buffer.data       = outgoingCommand.packet.data + outgoingCommand.fragmentOffset;
                buffer.dataLength = outgoingCommand.fragmentLength;
                host.packetSize += outgoingCommand.fragmentLength;
            } else if(! (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE)) {
                enet_free(outgoingCommand);
            }

            ++peer.packetsSent;
            ++peer.totalPacketsSent;

            ++command;
            ++buffer;
        }

        host.commandCount = command - host.commands;
        host.bufferCount  = buffer - host.buffers;

        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER &&
            !enet_peer_has_outgoing_commands (peer) &&
            enet_list_empty (sentUnreliableCommands)) {
                enet_peer_disconnect (peer, peer.eventData);
            }

        return canPing;
    } /* enet_protocol_send_reliable_outgoing_commands */

    public static int enet_protocol_send_outgoing_commands(ENetHost host, ENetEvent @event, int checkForTimeouts) {
        Span<byte> headerData = stackalloc byte[
            Marshal.SizeOf<ENetProtocolHeader>()
            + sizeof(byte) // additional peer id byte
            + sizeof(uint)
        ];
        ref ENetProtocolHeader header = (ENetProtocolHeader) headerData;
        int sentLength = 0;
        ulong shouldCompress = 0;
        ENetList<ENetOutgoingCommand> sentUnreliableCommands;
        int sendPass = 0, continueSending = 0;
        ENetPeer currentPeer = null;

        enet_list_clear(ref sentUnreliableCommands);

        for (; sendPass <= continueSending; ++ sendPass)
            for(currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
                if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED || currentPeer.state == ENetPeerState.ENET_PEER_STATE_ZOMBIE || (sendPass > 0 && ! (currentPeer.flags & ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING))) {
                    continue;
                }

                currentPeer.flags &= ~ ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING;

                host.headerFlags  = 0;
                host.commandCount = 0;
                host.bufferCount  = 1;
                host.packetSize   = sizeof(ENetProtocolHeader);

                if (!enet_list_empty(ref currentPeer.acknowledgements)) {
                    enet_protocol_send_acknowledgements(host, currentPeer);
                }

                if (checkForTimeouts != 0 &&
                    !enet_list_empty(ref currentPeer.sentReliableCommands) &&
                    ENET_TIME_GREATER_EQUAL(host.serviceTime, currentPeer.nextTimeout) &&
                    enet_protocol_check_timeouts(host, currentPeer, @event) == 1
                ) {
                    if (@event != null && @event.type != ENetEventType.ENET_EVENT_TYPE_NONE) {
                        return 1;
                    } else {
                        goto nextPeer;
                    }
                }

                if (((enet_list_empty (& currentPeer.outgoingCommands) &&
                    enet_list_empty (& currentPeer.outgoingSendReliableCommands)) ||
                    enet_protocol_check_outgoing_commands (host, currentPeer, sentUnreliableCommands)) &&
                    enet_list_empty(ref currentPeer.sentReliableCommands) &&
                    ENET_TIME_DIFFERENCE(host.serviceTime, currentPeer.lastReceiveTime) >= currentPeer.pingInterval &&
                    currentPeer.mtu - host.packetSize >= sizeof(ENetProtocolPing)
                ) {
                    enet_peer_ping(currentPeer);
                    enet_protocol_check_outgoing_commands(host, currentPeer, &sentUnreliableCommands);
                }

                if (host.commandCount == 0) {
                    goto nextPeer;
                }

                if (currentPeer.packetLossEpoch == 0) {
                    currentPeer.packetLossEpoch = host.serviceTime;
                } else if (ENET_TIME_DIFFERENCE(host.serviceTime, currentPeer.packetLossEpoch) >= ENets.ENET_PEER_PACKET_LOSS_INTERVAL && currentPeer.packetsSent > 0) {
                    uint packetLoss = currentPeer.packetsLost * ENets.ENET_PEER_PACKET_LOSS_SCALE / currentPeer.packetsSent;

#if ENET_DEBUG
                    printf(
                        "peer %u: %f%%+-%f%% packet loss, %u+-%u ms round trip time, %f%% throttle, %llu outgoing, %llu/%llu incoming\n", currentPeer.incomingPeerID,
                        currentPeer.packetLoss / (float) ENets.ENET_PEER_PACKET_LOSS_SCALE,
                        currentPeer.packetLossVariance / (float) ENets.ENET_PEER_PACKET_LOSS_SCALE, currentPeer.roundTripTime, currentPeer.roundTripTimeVariance,
                        currentPeer.packetThrottle / (float) ENets.ENET_PEER_PACKET_THROTTLE_SCALE,
                        enet_list_size(currentPeer.outgoingCommands),
                        currentPeer.channels != null ? enet_list_size(currentPeer.channels[0].incomingReliableCommands) : 0,
                        currentPeer.channels != null ? enet_list_size(currentPeer.channels[0].incomingUnreliableCommands) : 0
                    );
#endif

                    currentPeer.packetLossVariance = (currentPeer.packetLossVariance * 3 + ENET_DIFFERENCE(packetLoss, currentPeer.packetLoss)) / 4;
                    currentPeer.packetLoss = (currentPeer.packetLoss * 7 + packetLoss) / 8;

                    currentPeer.packetLossEpoch = host.serviceTime;
                    currentPeer.packetsSent     = 0;
                    currentPeer.packetsLost     = 0;
                }

                host.buffers[0].data = headerData;
                if (host.headerFlags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME) {
                    header.sentTime = ENET_HOST_TO_NET_16(host.serviceTime & 0xFFFF);
                    host.buffers[0].dataLength = sizeof(ENetProtocolHeader);
                } else {
                    host.buffers[0].dataLength = sizeof(ENetProtocolHeaderMinimal);
                }

                shouldCompress = 0;
                if (host.compressor.context != null && host.compressor.CompressorCompress != null) {
                    ulong originalSize = host.packetSize - sizeof(ENetProtocolHeader),
                      compressedSize    = host.compressor.CompressorCompress(host.compressor.context, &host.buffers[1], host.bufferCount - 1, originalSize, host.packetData[1], originalSize);
                    if (compressedSize > 0 && compressedSize < originalSize) {
                        host.headerFlags |= ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_COMPRESSED;
                        shouldCompress     = compressedSize;
                        #if ENET_DEBUG_COMPRESS
                        printf("peer %u: compressed %u.%u (%u%%)\n", currentPeer.incomingPeerID, originalSize, compressedSize, (compressedSize * 100) / originalSize);
                        #endif
                    }
                }

                if (currentPeer.outgoingPeerID < ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID) {
                    host.headerFlags |= currentPeer.outgoingSessionID << ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT;
                }

                {
                    ushort basePeerID = (ushort)(currentPeer.outgoingPeerID & 0x07FF);
                    ushort flagsAndSession = (ushort)(host.headerFlags & 0xF800); /* top bits only */

                    if (currentPeer.outgoingPeerID > 0x07FF)
                    {
                        flagsAndSession |= ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA;
                        header.peerID = ENET_HOST_TO_NET_16(basePeerID | flagsAndSession);
                        {
                            byte overflowByte = (byte)((currentPeer.outgoingPeerID >> 11) & 0xFF);
                            byte *extraPeerIDByte   = &headerData[host.buffers[0].dataLength];
                            *extraPeerIDByte             = overflowByte;
                            host.buffers[0].dataLength += sizeof(byte);
                        }
                    }
                    else
                    {
                        header.peerID = ENET_HOST_TO_NET_16(basePeerID | flagsAndSession);
                    }
                }

                if (host.checksum != null) {
                    uint *checksum = (uint *) &headerData[host.buffers[0].dataLength];
                    *checksum = currentPeer.outgoingPeerID < ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID ? currentPeer.connectID : 0;
                    host.buffers[0].dataLength += sizeof(uint);
                    *checksum = host.checksum(host.buffers, host.bufferCount);
                }

                if (shouldCompress > 0) {
                    host.buffers[1].data       = host.packetData[1];
                    host.buffers[1].dataLength = shouldCompress;
                    host.bufferCount = 2;
                }

                currentPeer.lastSendTime = host.serviceTime;
                sentLength = enet_socket_send(host.socket, &currentPeer.address, host.buffers, host.bufferCount);
                enet_protocol_remove_sent_unreliable_commands(currentPeer, &sentUnreliableCommands);

                if (sentLength < 0) {
                    // The local 'headerData' array (to which 'data' is assigned) goes out
                    // of scope on return from this function, so ensure we no longer point to it.
                    host.buffers[0].data = null;
                    return -1;
                }

                host.totalSentData += sentLength;
                currentPeer.totalDataSent += sentLength;
                host.totalSentPackets++;
            nextPeer:
                if (currentPeer.flags & ENetPeerFlag.ENET_PEER_FLAG_CONTINUE_SENDING)
                    continueSending = sendPass + 1;
            }

        // The local 'headerData' array (to which 'data' is assigned) goes out
        // of scope on return from this function, so ensure we no longer point to it.
        host.buffers[0].data = null;

        return 0;
    } /* enet_protocol_send_outgoing_commands */
 
    /** Sends any queued packets on the host specified to its designated peers.
*
*  @param host   host to flush
*  @remarks this function need only be used in circumstances where one wishes to send queued packets earlier than in a call to enet_host_service().
*  @ingroup host
*/
    public static void enet_host_flush(ENetHost host) {
        host.serviceTime = enet_time_get();
        enet_protocol_send_outgoing_commands(host, null, 0);
    }

}