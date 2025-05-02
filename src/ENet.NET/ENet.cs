using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ENet.NET;

namespace ENet.NET;

public delegate void ENetPacketFreeCallback(object _);

public static class ENet
{
    public static void ENET_UNUSED<T>(T x)
    {
        //(void)x;
    }

    // #define ENET_DIFFERENCE(x, y) ((x) < (y) ? (y) - (x) : (x) - (y))

    
    // =======================================================================//
    // !
    // ! Public API
    // !
    // =======================================================================//
    public static ENetListNode<T> enet_list_begin<T>(ENetList<T> list)
    {
        return list.sentinel.next;
    }
    
    public static ENetListNode<T> enet_list_end<T>(ENetList<T> list)
    {
        return list.sentinel;
    }
    
    public static bool enet_list_empty<T>(ENetList<T> list)
    {
        return enet_list_begin(list) == enet_list_end(list);
    }
    
    public static ENetListNode<T> enet_list_next<T>(ENetListNode<T> iterator)
    {
        return iterator.next;
    }
    
    public static ENetListNode<T> enet_list_previous<T>(ENetListNode<T> iterator)
    {
        return iterator.previous;
    }
    
    public static ENetListNode<T> enet_list_front<T>(ENetList<T> list)
    {
        return list.sentinel.next;
    }
    
    public static ENetListNode<T> enet_list_back<T>(ENetList<T> list)
    {
        return list.sentinel.previous;
    }


// =======================================================================//
// !
// ! Callbacks
// !
// =======================================================================//

    public static ENetCallbacks callbacks = { malloc, free, abort, enet_packet_create, enet_packet_destroy };

    public const int ENET_VERSION_MAJOR = 2;
    public const int ENET_VERSION_MINOR = 6;
    public const int ENET_VERSION_PATCH = 2;

    public static uint ENET_VERSION_CREATE(uint major, uint minor, uint patch)
    {
        return (((major) << 16) | ((minor) << 8) | (patch));
    }

    public static uint ENET_VERSION_GET_MAJOR(uint version) 
    {
        return (((version)>>16)&0xFF);
    }
    
    public static uint ENET_VERSION_GET_MINOR(uint version) 
    {
        return (((version)>>8)&0xFF);
    }
    
    public static uint ENET_VERSION_GET_PATCH(uint version) 
    {
        return ((version)&0xFF);
    }

    public static readonly uint ENET_VERSION = ENET_VERSION_CREATE(ENET_VERSION_MAJOR, ENET_VERSION_MINOR, ENET_VERSION_PATCH);

    /**
     * Initializes ENet globally and supplies user-overridden callbacks. Must be called prior to using any functions in ENet. Do not use enet_initialize() if you use this variant. Make sure the ENetCallbacks structure is zeroed out so that any additional callbacks added in future versions will be properly ignored.
     *
     * @param version the constant ENET_VERSION should be supplied so ENet knows which version of ENetCallbacks struct to use
     * @param inits user-overridden callbacks where any null callbacks will use ENet's defaults
     * @returns 0 on success, < 0 on failure
     */
    public static int enet_initialize_with_callbacks(uint version, ENetCallbacks inits) 
    {
        if (version < ENET_VERSION_CREATE(1, 3, 0)) {
            return -1;
        }

        if (inits.malloc != null || inits.free != null) {
            if (inits.malloc == null || inits.free == null) {
                return -1;
            }

            callbacks.malloc = inits.malloc;
            callbacks.free   = inits.free;
        }

        if (inits.no_memory != null) {
            callbacks.no_memory = inits.no_memory;
        }

        if (inits.packet_create != null || inits.packet_destroy != null) {
            if (inits.packet_create == null || inits.packet_destroy == null) {
                return -1;
            }

            callbacks.packet_create = inits.packet_create;
            callbacks.packet_destroy = inits.packet_destroy;
        }

        return enet_initialize();
    }

    public static uint enet_linked_version() 
    {
        return ENET_VERSION;
    }

    public static ENetPacket enet_malloc_packet(ulong bufferSize)
    {
        // TODO : check ikpil
        object memory = callbacks.malloc(bufferSize);

        if (memory == null) {
            callbacks.no_memory();
        }

        return (ENetPacket)memory;
    }

    public static T enet_malloc<T>(ulong size) 
    {
        object memory = callbacks.malloc(size);

        if (memory == null) {
            callbacks.no_memory();
        }

        return (T)memory;
    }

    public static void enet_free(object memory) 
    {
        callbacks.free(memory);
    }

    // =======================================================================//
    // !
    // ! List
    // !
    // =======================================================================//

    public static void enet_list_clear<T>(ENetList<T> list) {
        list.sentinel.next     = list.sentinel;
        list.sentinel.previous = list.sentinel;
    }

    public static ENetListNode<T> enet_list_insert<T>(ENetListNode<T> position, object data) {
        ENetListNode<T> result = (ENetListNode<T>)data;

        result.previous = position.previous;
        result.next     = position;

        result.previous.next = result;
        position.previous     = result;

        return result;
    }

    public static T enet_list_remove<T>(ENetListNode<T> position) {
        position.previous.next = position.next;
        position.next.previous = position.previous;

        return position.value;
    }

    public static ENetListNode<T> enet_list_move<T>(ENetListNode<T> position, object dataFirst, object dataLast) {
        ENetListNode<T> first = (ENetListNode<T>)dataFirst;
        ENetListNode<T> last  = (ENetListNode<T>)dataLast;

        first.previous.next = last.next;
        last.next.previous  = first.previous;

        first.previous = position.previous;
        last.next      = position;

        first.previous.next = first;
        position.previous    = last;

        return first;
    }

    public static ulong enet_list_size<T>(ENetList<T> list) {
        ulong size = 0;
        ENetListNode<T> position;

        for (position = enet_list_begin(list); position != enet_list_end(list); position = enet_list_next(position)) {
            ++size;
        }

        return size;
    }

    // =======================================================================//
    // !
    // ! Packet
    // !
    // =======================================================================//

    /**
     * Creates a packet that may be sent to a peer.
     * @param data         initial contents of the packet's data; the packet's data will remain uninitialized if data is null.
     * @param dataLength   size of the data allocated for this packet
     * @param flags        flags for this packet as described for the ENetPacket structure.
     * @returns the packet on success, null on failure
     */
    public static ENetPacket enet_packet_create(byte[] data, ulong dataLength, uint flags) {
        ENetPacket packet;
        if (0 != (flags & (uint)ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
        {
            packet = enet_malloc_packet(0);
            if (packet == null) {
                return null;
            }

            packet.data = data;
        }
        else {
            packet = enet_malloc_packet(dataLength);
            if (packet == null) {
                return null;
            }

            //packet.data = (enet_uint8 *)packet + sizeof(ENetPacket);

            if (data != null) {
                memcpy(packet.data, data, dataLength);
            }
        }

        packet.referenceCount = 0;
        packet.flags        = flags;
        packet.dataLength   = dataLength;
        packet.freeCallback = null;
        packet.userData     = null;

        return packet;
    }

    /** Attempts to resize the data in the packet to length specified in the
        dataLength parameter
        @param packet packet to resize
        @param dataLength new size for the packet data
        @returns new packet pointer on success, null on failure
    */
    public static ENetPacket enet_packet_resize(ENetPacket packet, ulong dataLength)
    {
        ENetPacket newPacket = null;

        if (dataLength <= packet.dataLength || 0 != (packet.flags & (uint)ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
        {
           packet.dataLength = dataLength;

           return packet;
        }

        newPacket = enet_malloc_packet(dataLength);
        if (newPacket == null)
          return null;

        memcpy(newPacket, packet, sizeof(ENetPacket) + packet.dataLength);
        newPacket.data = (enet_uint8 *)newPacket + sizeof(ENetPacket);
        newPacket.dataLength = dataLength;
        enet_free(packet);

        return newPacket;
    }

    public static ENetPacket enet_packet_create_offset(byte[] data, ulong dataLength, ulong dataOffset, uint flags) {
        ENetPacket packet;
        if (0 != (flags & (uint)ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE)) 
        {
            packet = enet_malloc_packet(0);
            if (packet == null) {
                return null;
            }

            packet.data = data;
        }
        else {
            packet = enet_malloc_packet( dataLength + dataOffset);
            if (packet == null) {
                return null;
            }

            packet.data = (enet_uint8 *)packet + sizeof(ENetPacket);

            if (data != null) {
                memcpy(packet.data + dataOffset, data, dataLength);
            }
        }

        packet.referenceCount = 0;
        packet.flags        = flags;
        packet.dataLength   = dataLength + dataOffset;
        packet.freeCallback = null;
        packet.userData     = null;

        return packet;
    }

    public static ENetPacket enet_packet_copy(ENetPacket packet) {
        return enet_packet_create(packet.data, packet.dataLength, packet.flags);
    }

    /**
     * Destroys the packet and deallocates its data.
     * @param packet packet to be destroyed
     */
    public static void enet_packet_destroy(ENetPacket packet) {
        if (packet == null) {
            return;
        }

        if (packet.freeCallback != null) {
            packet.freeCallback(packet);
        }

        enet_free(packet);
    }

    private static int initializedCRC32 = 0;
    private static uint[] crcTable = new uint[256];

    public static uint reflect_crc(int val, int bits) {
        int result = 0, bit;

        for (bit = 0; bit < bits; bit++) {
            if (0 != (val & 1))
            {
                result |= 1 << (bits - 1 - bit);
            }
            val >>= 1;
        }

        return (uint)result;
    }

    private static  void initialize_crc32() {
        int @byte;

        for (@byte = 0; @byte < 256; ++@byte) {
            uint crc = reflect_crc(@byte, 8) << 24;
            int offset;

            for (offset = 0; offset < 8; ++offset) {
                if (0 != (crc & 0x80000000)) {
                    crc = (crc << 1) ^ 0x04c11db7;
                } else {
                    crc <<= 1;
                }
            }

            crcTable[@byte] = reflect_crc((int)crc, 32);
        }

        initializedCRC32 = 1;
    }

    public static uint enet_crc32(Span<ENetBuffer> buffers, ulong bufferCount) {
        uint crc = 0xFFFFFFFF;

        if (0 == initializedCRC32)
        {
            initialize_crc32();
        }

        int n = 0;

        while (bufferCount-- > 0) {
            byte[] data = buffers[n].data;
            byte[] dataEnd = data[buffers[n].dataLength];

            while (data < dataEnd) {
                crc = (crc >> 8) ^ crcTable[(crc & 0xFF) ^ *data++];
            }

            ++n;
        }

        return ENET_HOST_TO_NET_32(~crc);
    }

// =======================================================================//
// !
// ! Protocol
// !
// =======================================================================//

    public static ulong[] commandSizes = new ulong[(int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT]
    {
        (ulong)0,
        (ulong)Marshal.SizeOf<ENetProtocolAcknowledge>(),
        (ulong)Marshal.SizeOf<ENetProtocolConnect>(),
        (ulong)Marshal.SizeOf<ENetProtocolVerifyConnect>(),
        (ulong)Marshal.SizeOf<ENetProtocolDisconnect>(),
        (ulong)Marshal.SizeOf<ENetProtocolPing>(),
        (ulong)Marshal.SizeOf<ENetProtocolSendReliable>(),
        (ulong)Marshal.SizeOf<ENetProtocolSendUnreliable>(),
        (ulong)Marshal.SizeOf<ENetProtocolSendFragment>(),
        (ulong)Marshal.SizeOf<ENetProtocolSendUnsequenced>(),
        (ulong)Marshal.SizeOf<ENetProtocolBandwidthLimit>(),
        (ulong)Marshal.SizeOf<ENetProtocolThrottleConfigure>(),
        (ulong)Marshal.SizeOf<ENetProtocolSendFragment>(),
    };

    public static ulong enet_protocol_command_size(byte commandNumber) {
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
            enet_list_insert(enet_list_end(host.dispatchQueue), peer.dispatchList);
            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
        }
    }

    public static int enet_protocol_dispatch_incoming_commands(ENetHost host, ENetEvent @event) {
        while (!enet_list_empty(host.dispatchQueue)) {
            ENetPeer peer = enet_list_remove(enet_list_begin(host.dispatchQueue));
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
                    if (enet_list_empty(peer.dispatchedCommands)) {
                        continue;
                    }

                    @event.packet = enet_peer_receive(peer, ref @event.channelID);
                    if (@event.packet == null) {
                        continue;
                    }

                    @event.type = ENetEventType.ENET_EVENT_TYPE_RECEIVE;
                    @event.peer = peer;

                    if (!enet_list_empty(peer.dispatchedCommands)) {
                        peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                        enet_list_insert(enet_list_end(host.dispatchQueue), peer.dispatchList);
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

    public static void enet_protocol_remove_sent_unreliable_commands(ENetPeer *peer, ENetList *sentUnreliableCommands) {
        ENetOutgoingCommand *outgoingCommand;

        if (enet_list_empty (sentUnreliableCommands))
            return;

        do {
            outgoingCommand = (ENetOutgoingCommand *) enet_list_front(sentUnreliableCommands);
            enet_list_remove(&outgoingCommand.outgoingCommandList);

            if (outgoingCommand.packet != null) {
                --outgoingCommand.packet.referenceCount;

                if (outgoingCommand.packet.referenceCount == 0) {
                    outgoingCommand.packet.flags |= ENET_PACKET_FLAG_SENT;
                    callbacks.packet_destroy(outgoingCommand.packet);
                }
            }

            enet_free(outgoingCommand);
        } while (!enet_list_empty(sentUnreliableCommands));

        if (peer.state == ENET_PEER_STATE_DISCONNECT_LATER && !enet_peer_has_outgoing_commands(peer)) {
            enet_peer_disconnect(peer, peer.eventData);
        }
    }

    public static ENetOutgoingCommand *enet_protocol_find_sent_reliable_command (ENetList * list, enet_uint16 reliableSequenceNumber, enet_uint8 channelID) {
        ENetListNode currentCommand;

        for (currentCommand = enet_list_begin(list);
            currentCommand != enet_list_end(list);
            currentCommand = enet_list_next(currentCommand)) {
            ENetOutgoingCommand * outgoingCommand = (ENetOutgoingCommand *)currentCommand;

            if (! (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE)) {
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

    public static ENetProtocolCommand enet_protocol_remove_sent_reliable_command(ENetPeer *peer, enet_uint16 reliableSequenceNumber, enet_uint8 channelID) {
        ENetOutgoingCommand *outgoingCommand = null;
        ENetListNode currentCommand;
        ENetProtocolCommand commandNumber;
        int wasSent = 1;

        for (currentCommand = enet_list_begin(&peer.sentReliableCommands);
            currentCommand != enet_list_end(&peer.sentReliableCommands);
            currentCommand = enet_list_next(currentCommand)) {

            outgoingCommand = (ENetOutgoingCommand *)currentCommand;
            if (outgoingCommand.reliableSequenceNumber == reliableSequenceNumber && outgoingCommand.command.header.channelID == channelID) {
                break;
            }
        }

        if (currentCommand == enet_list_end(&peer.sentReliableCommands)) {
            outgoingCommand = enet_protocol_find_sent_reliable_command(&peer.outgoingCommands, reliableSequenceNumber, channelID);
            if (outgoingCommand == null) {
                outgoingCommand = enet_protocol_find_sent_reliable_command(&peer.outgoingSendReliableCommands, reliableSequenceNumber, channelID);
            }

            wasSent = 0;
        }

        if (outgoingCommand == null) {
            return ENET_PROTOCOL_COMMAND_NONE;
        }

        if (channelID < peer.channelCount) {
            ENetChannel *channel       = &peer.channels[channelID];
            enet_uint16 reliableWindow = reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
            if (channel.reliableWindows[reliableWindow] > 0) {
                --channel.reliableWindows[reliableWindow];
                if (!channel.reliableWindows[reliableWindow]) {
                    channel.usedReliableWindows &= ~(1u << reliableWindow);
                }
            }
        }

        commandNumber = (ENetProtocolCommand) (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK);
        enet_list_remove(&outgoingCommand.outgoingCommandList);

        if (outgoingCommand.packet != null) {
            if (wasSent) {
                peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
            }

            --outgoingCommand.packet.referenceCount;

            if (outgoingCommand.packet.referenceCount == 0) {
                outgoingCommand.packet.flags |= ENET_PACKET_FLAG_SENT;
                callbacks.packet_destroy(outgoingCommand.packet);
            }
        }

        enet_free(outgoingCommand);

        if (enet_list_empty(&peer.sentReliableCommands)) {
            return commandNumber;
        }

        outgoingCommand = (ENetOutgoingCommand *) enet_list_front(&peer.sentReliableCommands);
        peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;

        return commandNumber;
    } /* enet_protocol_remove_sent_reliable_command */

    public static ENetPeer * enet_protocol_handle_connect(ENetHost *host, ENetProtocolHeader *header, ENetProtocol *command) {
        ENET_UNUSED(header)

        enet_uint8 incomingSessionID, outgoingSessionID;
        uint mtu, windowSize;
        ENetChannel *channel;
        ulong channelCount, duplicatePeers = 0;
        ENetPeer *currentPeer, *peer = null;
        ENetProtocol verifyCommand;

        channelCount = ENET_NET_TO_HOST_32(command.connect.channelCount);

        if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            return null;
        }

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            if (currentPeer.state == ENET_PEER_STATE_DISCONNECTED) {
                if (peer == null) {
                    peer = currentPeer;
                }
            } else if (currentPeer.state != ENET_PEER_STATE_CONNECTING && in6_equal(currentPeer.address.host, host.receivedAddress.host)) {
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
        peer.channels = (ENetChannel *) enet_malloc(channelCount * sizeof(ENetChannel));
        if (peer.channels == null) {
            return null;
        }
        peer.channelCount               = channelCount;
        peer.state                      = ENET_PEER_STATE_ACKNOWLEDGING_CONNECT;
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
        incomingSessionID = (incomingSessionID + 1) & (ENET_PROTOCOL_HEADER_SESSION_MASK >> ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        if (incomingSessionID == peer.outgoingSessionID) {
            incomingSessionID = (incomingSessionID + 1)
              & (ENET_PROTOCOL_HEADER_SESSION_MASK >> ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        }
        peer.outgoingSessionID = incomingSessionID;

        outgoingSessionID = command.connect.outgoingSessionID == 0xFF ? peer.incomingSessionID : command.connect.outgoingSessionID;
        outgoingSessionID = (outgoingSessionID + 1) & (ENET_PROTOCOL_HEADER_SESSION_MASK >> ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        if (outgoingSessionID == peer.incomingSessionID) {
            outgoingSessionID = (outgoingSessionID + 1)
              & (ENET_PROTOCOL_HEADER_SESSION_MASK >> ENET_PROTOCOL_HEADER_SESSION_SHIFT);
        }
        peer.incomingSessionID = outgoingSessionID;

        for (channel = peer.channels; channel < &peer.channels[channelCount]; ++channel) {
            channel.outgoingReliableSequenceNumber   = 0;
            channel.outgoingUnreliableSequenceNumber = 0;
            channel.incomingReliableSequenceNumber   = 0;
            channel.incomingUnreliableSequenceNumber = 0;

            enet_list_clear(&channel.incomingReliableCommands);
            enet_list_clear(&channel.incomingUnreliableCommands);

            channel.usedReliableWindows = 0;
            memset(channel.reliableWindows, 0, sizeof(channel.reliableWindows));
        }

        mtu = ENET_NET_TO_HOST_32(command.connect.mtu);

        if (mtu < ENET_PROTOCOL_MINIMUM_MTU) {
            mtu = ENET_PROTOCOL_MINIMUM_MTU;
        } else if (mtu > ENET_PROTOCOL_MAXIMUM_MTU) {
            mtu = ENET_PROTOCOL_MAXIMUM_MTU;
        }

        if (mtu < peer.mtu)
            peer.mtu = mtu;

        if (host.outgoingBandwidth == 0 && peer.incomingBandwidth == 0) {
            peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else if (host.outgoingBandwidth == 0 || peer.incomingBandwidth == 0) {
            peer.windowSize = (Math.Max(host.outgoingBandwidth, peer.incomingBandwidth) / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else {
            peer.windowSize = (Math.Min(host.outgoingBandwidth, peer.incomingBandwidth) / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (peer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            peer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (peer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        if (host.incomingBandwidth == 0) {
            windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else {
            windowSize = (host.incomingBandwidth / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (windowSize > ENET_NET_TO_HOST_32(command.connect.windowSize)) {
            windowSize = ENET_NET_TO_HOST_32(command.connect.windowSize);
        }

        if (windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        verifyCommand.header.command                            = (int)ENET_PROTOCOL_COMMAND_VERIFY_CONNECT | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
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

        enet_peer_queue_outgoing_command(peer, &verifyCommand, null, 0, 0);
        return peer;
    } /* enet_protocol_handle_connect */

    public static int enet_protocol_handle_send_reliable(ENetHost *host, ENetPeer *peer, const ENetProtocol *command, enet_uint8 **currentData) {
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendReliable.dataLength);
        *currentData += dataLength;

        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const enet_uint8 *) command + sizeof(ENetProtocolSendReliable), dataLength, ENET_PACKET_FLAG_RELIABLE, 0) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_send_unsequenced(ENetHost *host, ENetPeer *peer, const ENetProtocol *command, enet_uint8 **currentData) {
        uint unsequencedGroup, index;
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER)) {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendUnsequenced.dataLength);
        *currentData += dataLength;
        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        unsequencedGroup = ENET_NET_TO_HOST_16(command.sendUnsequenced.unsequencedGroup);
        index = unsequencedGroup % ENET_PEER_UNSEQUENCED_WINDOW_SIZE;

        if (unsequencedGroup < peer.incomingUnsequencedGroup) {
            unsequencedGroup += 0x10000;
        }

        if (unsequencedGroup >= (uint) peer.incomingUnsequencedGroup + ENET_PEER_FREE_UNSEQUENCED_WINDOWS * ENET_PEER_UNSEQUENCED_WINDOW_SIZE) {
            return 0;
        }

        unsequencedGroup &= 0xFFFF;

        if (unsequencedGroup - index != peer.incomingUnsequencedGroup) {
            peer.incomingUnsequencedGroup = unsequencedGroup - index;
            memset(peer.unsequencedWindow, 0, sizeof(peer.unsequencedWindow));
        } else if (peer.unsequencedWindow[index / 32] & (1u << (index % 32))) {
            return 0;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const enet_uint8 *) command + sizeof(ENetProtocolSendUnsequenced), dataLength, ENET_PACKET_FLAG_UNSEQUENCED,0) == null) {
            return -1;
        }

        peer.unsequencedWindow[index / 32] |= 1u << (index % 32);

        return 0;
    } /* enet_protocol_handle_send_unsequenced */

    public static int enet_protocol_handle_send_unreliable(ENetHost *host, ENetPeer *peer, const ENetProtocol *command,
      enet_uint8 **currentData) {
        ulong dataLength;

        if (command.header.channelID >= peer.channelCount ||
          (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER))
        {
            return -1;
        }

        dataLength    = ENET_NET_TO_HOST_16(command.sendUnreliable.dataLength);
        *currentData += dataLength;
        if (dataLength > host.maximumPacketSize || *currentData < host.receivedData || *currentData > &host.receivedData[host.receivedDataLength]) {
            return -1;
        }

        if (enet_peer_queue_incoming_command(peer, command, (const enet_uint8 *) command + sizeof(ENetProtocolSendUnreliable), dataLength, 0, 0) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_send_fragment(ENetHost *host, ENetPeer *peer, const ENetProtocol *command, enet_uint8 **currentData) {
        uint fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, startSequenceNumber, totalLength;
        ENetChannel *channel;
        enet_uint16 startWindow, currentWindow;
        ENetListNode currentCommand;
        ENetIncomingCommand *startCommand = null;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER)) {
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
        startSequenceNumber = ENET_NET_TO_HOST_16(command.sendFragment.startSequenceNumber);
        startWindow         = startSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
        currentWindow       = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

        if (startSequenceNumber < channel.incomingReliableSequenceNumber) {
            startWindow += ENET_PEER_RELIABLE_WINDOWS;
        }

        if (startWindow < currentWindow || startWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
            return 0;
        }

        fragmentNumber = ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
        fragmentCount  = ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
        fragmentOffset = ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
        totalLength    = ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

        if (fragmentCount > ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
            fragmentNumber >= fragmentCount ||
            totalLength > host.maximumPacketSize ||
            totalLength < fragmentCount ||
            fragmentOffset >= totalLength ||
            fragmentLength > totalLength - fragmentOffset
        ) {
            return -1;
        }

        for (currentCommand = enet_list_previous(enet_list_end(&channel.incomingReliableCommands));
            currentCommand != enet_list_end(&channel.incomingReliableCommands);
            currentCommand = enet_list_previous(currentCommand)
        ) {
            ENetIncomingCommand *incomingCommand = (ENetIncomingCommand *) currentCommand;

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

                if ((incomingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK) !=
                    ENET_PROTOCOL_COMMAND_SEND_FRAGMENT ||
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
            startCommand = enet_peer_queue_incoming_command(peer, &hostCommand, null, totalLength, ENET_PACKET_FLAG_RELIABLE, fragmentCount);
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

            memcpy(startCommand.packet.data + fragmentOffset, (enet_uint8 *) command + sizeof(ENetProtocolSendFragment), fragmentLength);

            if (startCommand.fragmentsRemaining <= 0) {
                enet_peer_dispatch_incoming_reliable_commands(peer, channel, null);
            }
        }

        return 0;
    } /* enet_protocol_handle_send_fragment */

    public static int enet_protocol_handle_send_unreliable_fragment(ENetHost *host, ENetPeer *peer, const ENetProtocol *command, enet_uint8 **currentData) {
        uint fragmentNumber, fragmentCount, fragmentOffset, fragmentLength, reliableSequenceNumber, startSequenceNumber, totalLength;
        enet_uint16 reliableWindow, currentWindow;
        ENetChannel *channel;
        ENetListNode currentCommand;
        ENetIncomingCommand *startCommand = null;

        if (command.header.channelID >= peer.channelCount || (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER)) {
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

        reliableWindow = reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
        currentWindow  = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

        if (reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
            reliableWindow += ENET_PEER_RELIABLE_WINDOWS;
        }

        if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
            return 0;
        }

        if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && startSequenceNumber <= channel.incomingUnreliableSequenceNumber) {
            return 0;
        }

        fragmentNumber = ENET_NET_TO_HOST_32(command.sendFragment.fragmentNumber);
        fragmentCount  = ENET_NET_TO_HOST_32(command.sendFragment.fragmentCount);
        fragmentOffset = ENET_NET_TO_HOST_32(command.sendFragment.fragmentOffset);
        totalLength    = ENET_NET_TO_HOST_32(command.sendFragment.totalLength);

        if (fragmentCount > ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT ||
            fragmentNumber >= fragmentCount ||
            totalLength > host.maximumPacketSize ||
            totalLength < fragmentCount ||
            fragmentOffset >= totalLength ||
            fragmentLength > totalLength - fragmentOffset
        ) {
            return -1;
        }

        for (currentCommand = enet_list_previous(enet_list_end(&channel.incomingUnreliableCommands));
            currentCommand != enet_list_end(&channel.incomingUnreliableCommands);
            currentCommand = enet_list_previous(currentCommand)
        ) {
            ENetIncomingCommand *incomingCommand = (ENetIncomingCommand *) currentCommand;

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

                if ((incomingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK) !=
                    ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT ||
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
            startCommand = enet_peer_queue_incoming_command(peer, command, null, totalLength,
                ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT, fragmentCount);
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

            memcpy(startCommand.packet.data + fragmentOffset, (enet_uint8 *) command + sizeof(ENetProtocolSendFragment), fragmentLength);

            if (startCommand.fragmentsRemaining <= 0) {
                enet_peer_dispatch_incoming_unreliable_commands(peer, channel, null);
            }
        }

        return 0;
    } /* enet_protocol_handle_send_unreliable_fragment */

    public static int enet_protocol_handle_ping(ENetHost *host, ENetPeer *peer, const ENetProtocol *command) {
        ENET_UNUSED(host)
        ENET_UNUSED(command)

        if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
            return -1;
        }

        return 0;
    }

    public static int enet_protocol_handle_bandwidth_limit(ENetHost *host, ENetPeer *peer, const ENetProtocol *command) {
        if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
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
            peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else if (peer.incomingBandwidth == 0 || host.outgoingBandwidth == 0) {
            peer.windowSize = (Math.Max(peer.incomingBandwidth, host.outgoingBandwidth)
              / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else {
            peer.windowSize = (Math.Min(peer.incomingBandwidth, host.outgoingBandwidth)
              / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (peer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            peer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (peer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            peer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        return 0;
    } /* enet_protocol_handle_bandwidth_limit */

    public static int enet_protocol_handle_throttle_configure(ENetHost *host, ENetPeer *peer, const ENetProtocol *command) {
        ENET_UNUSED(host)

        if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
            return -1;
        }

        peer.packetThrottleInterval     = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleInterval);
        peer.packetThrottleAcceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleAcceleration);
        peer.packetThrottleDeceleration = ENET_NET_TO_HOST_32(command.throttleConfigure.packetThrottleDeceleration);

        return 0;
    }

    public static int enet_protocol_handle_disconnect(ENetHost *host, ENetPeer *peer, const ENetProtocol *command) {
        if (peer.state == ENET_PEER_STATE_DISCONNECTED || peer.state == ENET_PEER_STATE_ZOMBIE ||
            peer.state == ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT
        ) {
            return 0;
        }

        enet_peer_reset_queues(peer);

        if (peer.state == ENET_PEER_STATE_CONNECTION_SUCCEEDED || peer.state == ENET_PEER_STATE_DISCONNECTING || peer.state == ENET_PEER_STATE_CONNECTING) {
            enet_protocol_dispatch_state(host, peer, ENET_PEER_STATE_ZOMBIE);
        }
        else if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
            if (peer.state == ENET_PEER_STATE_CONNECTION_PENDING) { host.recalculateBandwidthLimits = 1; }
            enet_peer_reset(peer);
        }
        else if (command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
            enet_protocol_change_state(host, peer, ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT);
        }
        else {
            enet_protocol_dispatch_state(host, peer, ENET_PEER_STATE_ZOMBIE);
        }

        if (peer.state != ENET_PEER_STATE_DISCONNECTED) {
            peer.eventData = ENET_NET_TO_HOST_32(command.disconnect.data);
        }

        return 0;
    }

    public static int enet_protocol_handle_acknowledge(ENetHost *host, ENetEvent *event, ENetPeer *peer, const ENetProtocol *command) {
        uint roundTripTime, receivedSentTime, receivedReliableSequenceNumber;
        ENetProtocolCommand commandNumber;

        if (peer.state == ENET_PEER_STATE_DISCONNECTED || peer.state == ENET_PEER_STATE_ZOMBIE) {
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
        commandNumber = enet_protocol_remove_sent_reliable_command(peer, receivedReliableSequenceNumber, command.header.channelID);

        switch (peer.state) {
            case ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                if (commandNumber != ENET_PROTOCOL_COMMAND_VERIFY_CONNECT) {
                    return -1;
                }

                enet_protocol_notify_connect(host, peer, event);
                break;

            case ENET_PEER_STATE_DISCONNECTING:
                if (commandNumber != ENET_PROTOCOL_COMMAND_DISCONNECT) {
                    return -1;
                }

                enet_protocol_notify_disconnect(host, peer, event);
                break;

            case ENET_PEER_STATE_DISCONNECT_LATER:
                if (!enet_peer_has_outgoing_commands(peer)) {
                    enet_peer_disconnect(peer, peer.eventData);
                }
                break;

            default:
                break;
        }

        return 0;
    } /* enet_protocol_handle_acknowledge */

    public static int enet_protocol_handle_verify_connect(ENetHost *host, ENetEvent *event, ENetPeer *peer, const ENetProtocol *command) {
        uint mtu, windowSize;
        ulong channelCount;

        if (peer.state != ENET_PEER_STATE_CONNECTING) {
            return 0;
        }

        channelCount = ENET_NET_TO_HOST_32(command.verifyConnect.channelCount);

        if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleInterval) != peer.packetThrottleInterval ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleAcceleration) != peer.packetThrottleAcceleration ||
            ENET_NET_TO_HOST_32(command.verifyConnect.packetThrottleDeceleration) != peer.packetThrottleDeceleration ||
            command.verifyConnect.connectID != peer.connectID
        ) {
            peer.eventData = 0;
            enet_protocol_dispatch_state(host, peer, ENET_PEER_STATE_ZOMBIE);
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

        if (mtu < ENET_PROTOCOL_MINIMUM_MTU) {
            mtu = ENET_PROTOCOL_MINIMUM_MTU;
        } else if (mtu > ENET_PROTOCOL_MAXIMUM_MTU) {
            mtu = ENET_PROTOCOL_MAXIMUM_MTU;
        }

        if (mtu < peer.mtu) {
            peer.mtu = mtu;
        }

        windowSize = ENET_NET_TO_HOST_32(command.verifyConnect.windowSize);
        if (windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        if (windowSize < peer.windowSize) {
            peer.windowSize = windowSize;
        }

        peer.incomingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.incomingBandwidth);
        peer.outgoingBandwidth = ENET_NET_TO_HOST_32(command.verifyConnect.outgoingBandwidth);

        enet_protocol_notify_connect(host, peer, event);
        return 0;
    } /* enet_protocol_handle_verify_connect */

    public static int enet_protocol_handle_incoming_commands(ENetHost *host, ENetEvent *event) {
        ENetProtocolHeader *header;
        ENetProtocol *command;
        ENetPeer *peer;
        enet_uint8 *currentData;
        ulong headerSize;
        enet_uint16 peerID, flags;
        enet_uint8 sessionID;

        if (host.receivedDataLength < sizeof(ENetProtocolHeaderMinimal)) {
            return 0;
        }

        header = (ENetProtocolHeader *) host.receivedData;

        peerID    = ENET_NET_TO_HOST_16(header.peerID);
        sessionID = (peerID & ENET_PROTOCOL_HEADER_SESSION_MASK) >> ENET_PROTOCOL_HEADER_SESSION_SHIFT;
        flags     = peerID & ENET_PROTOCOL_HEADER_FLAG_MASK;
        peerID   &= ~(ENET_PROTOCOL_HEADER_FLAG_MASK | ENET_PROTOCOL_HEADER_SESSION_MASK);

        headerSize = (flags & ENET_PROTOCOL_HEADER_FLAG_SENT_TIME ? sizeof(ENetProtocolHeader) : sizeof(ENetProtocolHeaderMinimal));

#ifdef ENET_USE_MORE_PEERS
        if (flags & ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA) {
            if (host.receivedDataLength < headerSize + sizeof(enet_uint8)) {
                return 0;
            }

            enet_uint8 * headerExtraPeerID = (enet_uint8 *) & host.receivedData [headerSize];
            enet_uint8 peerIDExtra = *headerExtraPeerID;
            peerID = (peerID & 0x07FF) | ((enet_uint16)peerIDExtra << 11);

            headerSize += sizeof (enet_uint8);
        }
#endif

        if (host.checksum != null) {
            if (host.receivedDataLength < headerSize + sizeof(uint)) {
                return 0;
            }

            headerSize += sizeof(uint);
        }

        if (peerID == ENET_PROTOCOL_MAXIMUM_PEER_ID) {
            peer = null;
        } else if (peerID >= host.peerCount) {
            return 0;
        } else {
            peer = &host.peers[peerID];

            if (peer.state == ENET_PEER_STATE_DISCONNECTED ||
                peer.state == ENET_PEER_STATE_ZOMBIE ||
                ((!in6_equal(host.receivedAddress.host , peer.address.host) ||
                host.receivedAddress.port != peer.address.port) &&
                1 /* no broadcast in ipv6  !in6_equal(peer.address.host , ENET_HOST_BROADCAST)*/) ||
                (peer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID &&
                sessionID != peer.incomingSessionID)
            ) {
                return 0;
            }
        }

        if (flags & ENET_PROTOCOL_HEADER_FLAG_COMPRESSED) {
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

            if (host.checksum(&buffer, 1) != desiredChecksum) {
                return 0;
            }
        }

        if (peer != null) {
            peer.address.host       = host.receivedAddress.host;
            peer.address.port       = host.receivedAddress.port;
            peer.incomingDataTotal += host.receivedDataLength;
            peer.totalDataReceived += host.receivedDataLength;
        }

        currentData = host.receivedData + headerSize;

        while (currentData < &host.receivedData[host.receivedDataLength]) {
            enet_uint8 commandNumber;
            ulong commandSize;

            command = (ENetProtocol *) currentData;

            if (currentData + sizeof(ENetProtocolCommandHeader) > &host.receivedData[host.receivedDataLength]) {
                break;
            }

            commandNumber = command.header.command & ENET_PROTOCOL_COMMAND_MASK;
            if (commandNumber >= ENET_PROTOCOL_COMMAND_COUNT) {
                break;
            }

            commandSize = commandSizes[commandNumber];
            if (commandSize == 0 || currentData + commandSize > &host.receivedData[host.receivedDataLength]) {
                break;
            }

            currentData += commandSize;

            if (peer == null && (commandNumber != ENET_PROTOCOL_COMMAND_CONNECT || currentData < &host.receivedData[host.receivedDataLength])) {
                break;
            }

            command.header.reliableSequenceNumber = ENET_NET_TO_HOST_16(command.header.reliableSequenceNumber);

            switch (commandNumber) {
                case ENET_PROTOCOL_COMMAND_ACKNOWLEDGE:
                    if (enet_protocol_handle_acknowledge(host, event, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_CONNECT:
                    if (peer != null) {
                        goto commandError;
                    }
                    peer = enet_protocol_handle_connect(host, header, command);
                    if (peer == null) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_VERIFY_CONNECT:
                    if (enet_protocol_handle_verify_connect(host, event, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_DISCONNECT:
                    if (enet_protocol_handle_disconnect(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_PING:
                    if (enet_protocol_handle_ping(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                    if (enet_protocol_handle_send_reliable(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                    if (enet_protocol_handle_send_unreliable(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                    if (enet_protocol_handle_send_unsequenced(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
                    if (enet_protocol_handle_send_fragment(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT:
                    if (enet_protocol_handle_bandwidth_limit(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE:
                    if (enet_protocol_handle_throttle_configure(host, peer, command)) {
                        goto commandError;
                    }
                    break;

                case ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                    if (enet_protocol_handle_send_unreliable_fragment(host, peer, command, &currentData)) {
                        goto commandError;
                    }
                    break;

                default:
                    goto commandError;
            }

            assert(peer);
            if ((command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0) {
                enet_uint16 sentTime;

                if (!(flags & ENET_PROTOCOL_HEADER_FLAG_SENT_TIME)) {
                    break;
                }

                sentTime = ENET_NET_TO_HOST_16(header.sentTime);

                switch (peer.state) {
                    case ENET_PEER_STATE_DISCONNECTING:
                    case ENET_PEER_STATE_ACKNOWLEDGING_CONNECT:
                    case ENET_PEER_STATE_DISCONNECTED:
                    case ENET_PEER_STATE_ZOMBIE:
                        break;

                    case ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT:
                        if ((command.header.command & ENET_PROTOCOL_COMMAND_MASK) == ENET_PROTOCOL_COMMAND_DISCONNECT) {
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
        if (event != null && event.type != ENET_EVENT_TYPE_NONE) {
            return 1;
        }

        return 0;
    } /* enet_protocol_handle_incoming_commands */

    public static int enet_protocol_receive_incoming_commands(ENetHost *host, ENetEvent *event) {
        int packets;

        for (packets = 0; packets < 256; ++packets) {
            int receivedLength;
            ENetBuffer buffer;

            buffer.data       = host.packetData[0];
            // buffer.dataLength = sizeof (host.packetData[0]);
            buffer.dataLength = host.mtu;

            receivedLength    = enet_socket_receive(host.socket, &host.receivedAddress, &buffer, 1);

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
                switch (host.intercept(host, (void *)event)) {
                    case 1:
                        if (event != null && event.type != ENET_EVENT_TYPE_NONE) {
                            return 1;
                        }

                        continue;

                    case -1:
                        return -1;

                    default:
                        break;
                }
            }

            switch (enet_protocol_handle_incoming_commands(host, event)) {
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

    public static void enet_protocol_send_acknowledgements(ENetHost *host, ENetPeer *peer) {
        ENetProtocol *command = &host.commands[host.commandCount];
        ENetBuffer *buffer    = &host.buffers[host.bufferCount];
        ENetAcknowledgement *acknowledgement;
        ENetListNode currentAcknowledgement;
        enet_uint16 reliableSequenceNumber;

        currentAcknowledgement = enet_list_begin(&peer.acknowledgements);

        while (currentAcknowledgement != enet_list_end(&peer.acknowledgements)) {
            if (command >= &host.commands[sizeof(host.commands) / sizeof(ENetProtocol)] ||
                buffer >= &host.buffers[sizeof(host.buffers) / sizeof(ENetBuffer)] ||
                peer.mtu - host.packetSize < sizeof(ENetProtocolAcknowledge)
            ) {
                peer.flags |= ENET_PEER_FLAG_CONTINUE_SENDING;
                break;
            }

            acknowledgement = (ENetAcknowledgement *) currentAcknowledgement;
            currentAcknowledgement = enet_list_next(currentAcknowledgement);

            buffer.data       = command;
            buffer.dataLength = sizeof(ENetProtocolAcknowledge);
            host.packetSize += buffer.dataLength;

            reliableSequenceNumber = ENET_HOST_TO_NET_16(acknowledgement.command.header.reliableSequenceNumber);

            command.header.command   = ENET_PROTOCOL_COMMAND_ACKNOWLEDGE;
            command.header.channelID = acknowledgement.command.header.channelID;
            command.header.reliableSequenceNumber = reliableSequenceNumber;
            command.acknowledge.receivedReliableSequenceNumber = reliableSequenceNumber;
            command.acknowledge.receivedSentTime = ENET_HOST_TO_NET_16(acknowledgement.sentTime);

            if ((acknowledgement.command.header.command & ENET_PROTOCOL_COMMAND_MASK) == ENET_PROTOCOL_COMMAND_DISCONNECT) {
                enet_protocol_dispatch_state(host, peer, ENET_PEER_STATE_ZOMBIE);
            }

            enet_list_remove(&acknowledgement.acknowledgementList);
            enet_free(acknowledgement);

            ++command;
            ++buffer;
        }

        host.commandCount = command - host.commands;
        host.bufferCount  = buffer - host.buffers;
    } /* enet_protocol_send_acknowledgements */

    public static int enet_protocol_check_timeouts(ENetHost *host, ENetPeer *peer, ENetEvent *event) {
        ENetOutgoingCommand *outgoingCommand;
        ENetListNode currentCommand, insertPosition, insertSendReliablePosition;

        currentCommand = enet_list_begin(&peer.sentReliableCommands);
        insertPosition = enet_list_begin(&peer.outgoingCommands);
        insertSendReliablePosition = enet_list_begin(&peer.outgoingSendReliableCommands);

        while (currentCommand != enet_list_end(&peer.sentReliableCommands)) {
            outgoingCommand = (ENetOutgoingCommand *) currentCommand;

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
                enet_protocol_notify_disconnect_timeout(host, peer, event);
                return 1;
            }

            ++peer.packetsLost;
            ++peer.totalPacketsLost;

            /* Replaced exponential backoff time with something more linear */
            /* Source: http://lists.cubik.org/pipermail/enet-discuss/2014-May/002308.html */
            outgoingCommand.roundTripTimeout = peer.roundTripTime + 4 * peer.roundTripTimeVariance;

            if (outgoingCommand.packet != null) {
                peer.reliableDataInTransit -= outgoingCommand.fragmentLength;
                enet_list_insert(insertSendReliablePosition, enet_list_remove (& outgoingCommand.outgoingCommandList));
            } else {
                enet_list_insert(insertPosition, enet_list_remove (& outgoingCommand.outgoingCommandList));
            }

            if (currentCommand == enet_list_begin(&peer.sentReliableCommands) && !enet_list_empty(&peer.sentReliableCommands)) {
                outgoingCommand = (ENetOutgoingCommand *) currentCommand;
                peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;
            }
        }

        return 0;
    } /* enet_protocol_check_timeouts */

    public static int enet_protocol_check_outgoing_commands(ENetHost *host, ENetPeer *peer, ENetList *sentUnreliableCommands) {
        ENetProtocol *command = &host.commands[host.commandCount];
        ENetBuffer *buffer    = &host.buffers[host.bufferCount];
        ENetOutgoingCommand *outgoingCommand = null;
        ENetListNode currentCommand, currentSendReliableCommand;
        ENetChannel *channel = null;
        enet_uint16 reliableWindow = 0;
        ulong commandSize=0;
        int windowWrap = 0, canPing = 1;

        currentCommand = enet_list_begin(&peer.outgoingCommands);
        currentSendReliableCommand = enet_list_begin (&peer.outgoingSendReliableCommands);

        for (;;) {
            if (currentCommand != enet_list_end (& peer.outgoingCommands))
            {
                outgoingCommand = (ENetOutgoingCommand *) currentCommand;
                if (currentSendReliableCommand != enet_list_end (& peer.outgoingSendReliableCommands) && ENET_TIME_LESS (((ENetOutgoingCommand *) currentSendReliableCommand).queueTime, outgoingCommand.queueTime)) {
                    goto useSendReliableCommand;
                }
                currentCommand = enet_list_next (currentCommand);
            } else if (currentSendReliableCommand != enet_list_end (& peer.outgoingSendReliableCommands)) {
                useSendReliableCommand:
                outgoingCommand = (ENetOutgoingCommand *) currentSendReliableCommand;
                currentSendReliableCommand = enet_list_next (currentSendReliableCommand);
            } else {
                break;
            }


            if (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
                channel = outgoingCommand.command.header.channelID < peer.channelCount ? & peer.channels [outgoingCommand.command.header.channelID] : null;
                reliableWindow = outgoingCommand.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
                if (channel != null) {
                    if (windowWrap) {
                        continue;
                    } else if (outgoingCommand.sendAttempts < 1 && 
                            !(outgoingCommand.reliableSequenceNumber % ENET_PEER_RELIABLE_WINDOW_SIZE) &&
                            (channel.reliableWindows [(reliableWindow + ENET_PEER_RELIABLE_WINDOWS - 1) % ENET_PEER_RELIABLE_WINDOWS] >= ENET_PEER_RELIABLE_WINDOW_SIZE ||
                            channel.usedReliableWindows & ((((1u << (ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) << reliableWindow) | 
                            (((1u << (ENET_PEER_FREE_RELIABLE_WINDOWS + 2)) - 1) >> (ENET_PEER_RELIABLE_WINDOWS - reliableWindow))))) 
                    {
                        windowWrap = 1;
                        currentSendReliableCommand = enet_list_end (& peer.outgoingSendReliableCommands);
                        continue;
                    }
                }

                if (outgoingCommand.packet != null) {
                    uint windowSize = (peer.packetThrottle * peer.windowSize) / ENET_PEER_PACKET_THROTTLE_SCALE;

                    if (peer.reliableDataInTransit + outgoingCommand.fragmentLength > Math.Max (windowSize, peer.mtu))
                    {
                        currentSendReliableCommand = enet_list_end (& peer.outgoingSendReliableCommands);
                        continue;
                    }
                }

                canPing = 0;
            }

            commandSize = commandSizes[outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK];
            if (command >= &host.commands[sizeof(host.commands) / sizeof(ENetProtocol)] ||
                buffer + 1 >= &host.buffers[sizeof(host.buffers) / sizeof(ENetBuffer)] ||
                peer.mtu - host.packetSize < commandSize ||
                (outgoingCommand.packet != null &&
                (enet_uint16) (peer.mtu - host.packetSize) < (enet_uint16) (commandSize + outgoingCommand.fragmentLength))
            ) {
                peer.flags |= ENET_PEER_FLAG_CONTINUE_SENDING;
                break;
            }

            if (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
                channel = outgoingCommand.command.header.channelID < peer.channelCount ? & peer.channels [outgoingCommand.command.header.channelID] : null;
                reliableWindow = outgoingCommand.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
                if (channel != null && outgoingCommand.sendAttempts < 1) {
                    channel.usedReliableWindows |= 1u << reliableWindow;
                    ++channel.reliableWindows[reliableWindow];
                }

                ++outgoingCommand.sendAttempts;

                if (outgoingCommand.roundTripTimeout == 0) {
                    outgoingCommand.roundTripTimeout = peer.roundTripTime + 4 * peer.roundTripTimeVariance;
                }

                if (enet_list_empty(&peer.sentReliableCommands)) {
                    peer.nextTimeout = host.serviceTime + outgoingCommand.roundTripTimeout;
                }

                enet_list_insert(enet_list_end(&peer.sentReliableCommands), enet_list_remove(&outgoingCommand.outgoingCommandList));

                outgoingCommand.sentTime = host.serviceTime;

                host.headerFlags |= ENET_PROTOCOL_HEADER_FLAG_SENT_TIME;
                peer.reliableDataInTransit += outgoingCommand.fragmentLength;
            } else {
                if (outgoingCommand.packet != null && outgoingCommand.fragmentOffset == 0) {
                    peer.packetThrottleCounter += ENET_PEER_PACKET_THROTTLE_COUNTER;
                    peer.packetThrottleCounter %= ENET_PEER_PACKET_THROTTLE_SCALE;

                    if (peer.packetThrottleCounter > peer.packetThrottle) {
                        enet_uint16 reliableSequenceNumber = outgoingCommand.reliableSequenceNumber,
                                    unreliableSequenceNumber = outgoingCommand.unreliableSequenceNumber;
                        for (;;)
                        {
                            --outgoingCommand.packet.referenceCount;

                            if (outgoingCommand.packet.referenceCount == 0) {
                                enet_packet_destroy(outgoingCommand.packet);
                            }

                            enet_list_remove(& outgoingCommand.outgoingCommandList);
                            enet_free(outgoingCommand);

                            if (currentCommand == enet_list_end (& peer.outgoingCommands)) {
                                break;
                            }

                            outgoingCommand = (ENetOutgoingCommand *) currentCommand;
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
                    enet_list_insert(enet_list_end (sentUnreliableCommands), outgoingCommand);
                }
            }


            buffer.data       = command;
            buffer.dataLength = commandSize;

            host.packetSize  += buffer.dataLength;

            *command = outgoingCommand.command;

            if (outgoingCommand.packet != null) {
                ++buffer;
                buffer.data       = outgoingCommand.packet.data + outgoingCommand.fragmentOffset;
                buffer.dataLength = outgoingCommand.fragmentLength;
                host.packetSize += outgoingCommand.fragmentLength;
            } else if(! (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE)) {
                enet_free(outgoingCommand);
            }

            ++peer.packetsSent;
            ++peer.totalPacketsSent;

            ++command;
            ++buffer;
        }

        host.commandCount = command - host.commands;
        host.bufferCount  = buffer - host.buffers;

        if (peer.state == ENET_PEER_STATE_DISCONNECT_LATER &&
            !enet_peer_has_outgoing_commands (peer) &&
            enet_list_empty (sentUnreliableCommands)) {
                enet_peer_disconnect (peer, peer.eventData);
            }

        return canPing;
    } /* enet_protocol_send_reliable_outgoing_commands */

    public static int enet_protocol_send_outgoing_commands(ENetHost *host, ENetEvent *event, int checkForTimeouts) {
        enet_uint8 headerData[
            sizeof(ENetProtocolHeader) 
#ifdef ENET_USE_MORE_PEERS
            + sizeof(enet_uint8) // additional peer id byte
#endif
            + sizeof(uint)
        ];
        ENetProtocolHeader *header = (ENetProtocolHeader *) headerData;
        int sentLength = 0;
        ulong shouldCompress = 0;
        ENetList sentUnreliableCommands;
        int sendPass = 0, continueSending = 0;
        ENetPeer *currentPeer;

        enet_list_clear (&sentUnreliableCommands);

        for (; sendPass <= continueSending; ++ sendPass)
            for(currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
                if (currentPeer.state == ENET_PEER_STATE_DISCONNECTED || currentPeer.state == ENET_PEER_STATE_ZOMBIE || (sendPass > 0 && ! (currentPeer.flags & ENET_PEER_FLAG_CONTINUE_SENDING))) {
                    continue;
                }

                currentPeer.flags &= ~ ENET_PEER_FLAG_CONTINUE_SENDING;

                host.headerFlags  = 0;
                host.commandCount = 0;
                host.bufferCount  = 1;
                host.packetSize   = sizeof(ENetProtocolHeader);

                if (!enet_list_empty(&currentPeer.acknowledgements)) {
                    enet_protocol_send_acknowledgements(host, currentPeer);
                }

                if (checkForTimeouts != 0 &&
                    !enet_list_empty(&currentPeer.sentReliableCommands) &&
                    ENET_TIME_GREATER_EQUAL(host.serviceTime, currentPeer.nextTimeout) &&
                    enet_protocol_check_timeouts(host, currentPeer, event) == 1
                ) {
                    if (event != null && event.type != ENET_EVENT_TYPE_NONE) {
                        return 1;
                    } else {
                        goto nextPeer;
                    }
                }

                if (((enet_list_empty (& currentPeer.outgoingCommands) &&
                    enet_list_empty (& currentPeer.outgoingSendReliableCommands)) ||
                    enet_protocol_check_outgoing_commands (host, currentPeer, &sentUnreliableCommands)) &&
                    enet_list_empty(&currentPeer.sentReliableCommands) &&
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
                } else if (ENET_TIME_DIFFERENCE(host.serviceTime, currentPeer.packetLossEpoch) >= ENET_PEER_PACKET_LOSS_INTERVAL && currentPeer.packetsSent > 0) {
                    uint packetLoss = currentPeer.packetsLost * ENET_PEER_PACKET_LOSS_SCALE / currentPeer.packetsSent;

                    #ifdef ENET_DEBUG
                    printf(
                        "peer %u: %f%%+-%f%% packet loss, %u+-%u ms round trip time, %f%% throttle, %llu outgoing, %llu/%llu incoming\n", currentPeer.incomingPeerID,
                        currentPeer.packetLoss / (float) ENET_PEER_PACKET_LOSS_SCALE,
                        currentPeer.packetLossVariance / (float) ENET_PEER_PACKET_LOSS_SCALE, currentPeer.roundTripTime, currentPeer.roundTripTimeVariance,
                        currentPeer.packetThrottle / (float) ENET_PEER_PACKET_THROTTLE_SCALE,
                        enet_list_size(&currentPeer.outgoingCommands),
                        currentPeer.channels != null ? enet_list_size( &currentPeer.channels.incomingReliableCommands) : 0llu,
                        currentPeer.channels != null ? enet_list_size(&currentPeer.channels.incomingUnreliableCommands) : 0llu
                    );
                    #endif

                    currentPeer.packetLossVariance = (currentPeer.packetLossVariance * 3 + ENET_DIFFERENCE(packetLoss, currentPeer.packetLoss)) / 4;
                    currentPeer.packetLoss = (currentPeer.packetLoss * 7 + packetLoss) / 8;

                    currentPeer.packetLossEpoch = host.serviceTime;
                    currentPeer.packetsSent     = 0;
                    currentPeer.packetsLost     = 0;
                }

                host.buffers[0].data = headerData;
                if (host.headerFlags & ENET_PROTOCOL_HEADER_FLAG_SENT_TIME) {
                    header.sentTime = ENET_HOST_TO_NET_16(host.serviceTime & 0xFFFF);
                    host.buffers[0].dataLength = sizeof(ENetProtocolHeader);
                } else {
                    host.buffers[0].dataLength = sizeof(ENetProtocolHeaderMinimal);
                }

                shouldCompress = 0;
                if (host.compressor.context != null && host.compressor.compress != null) {
                    ulong originalSize = host.packetSize - sizeof(ENetProtocolHeader),
                      compressedSize    = host.compressor.compress(host.compressor.context, &host.buffers[1], host.bufferCount - 1, originalSize, host.packetData[1], originalSize);
                    if (compressedSize > 0 && compressedSize < originalSize) {
                        host.headerFlags |= ENET_PROTOCOL_HEADER_FLAG_COMPRESSED;
                        shouldCompress     = compressedSize;
                        #ifdef ENET_DEBUG_COMPRESS
                        printf("peer %u: compressed %u.%u (%u%%)\n", currentPeer.incomingPeerID, originalSize, compressedSize, (compressedSize * 100) / originalSize);
                        #endif
                    }
                }

                if (currentPeer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID) {
                    host.headerFlags |= currentPeer.outgoingSessionID << ENET_PROTOCOL_HEADER_SESSION_SHIFT;
                }

#ifdef ENET_USE_MORE_PEERS
                {
                    enet_uint16 basePeerID = (enet_uint16)(currentPeer.outgoingPeerID & 0x07FF);
                    enet_uint16 flagsAndSession = (enet_uint16)(host.headerFlags & 0xF800); /* top bits only */

                    if (currentPeer.outgoingPeerID > 0x07FF)
                    {
                        flagsAndSession |= ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA;
                        header.peerID = ENET_HOST_TO_NET_16(basePeerID | flagsAndSession);
                        {
                            enet_uint8 overflowByte = (enet_uint8)((currentPeer.outgoingPeerID >> 11) & 0xFF);
                            enet_uint8 *extraPeerIDByte   = &headerData[host.buffers[0].dataLength];
                            *extraPeerIDByte             = overflowByte;
                            host.buffers[0].dataLength += sizeof(enet_uint8);
                        }
                    }
                    else
                    {
                        header.peerID = ENET_HOST_TO_NET_16(basePeerID | flagsAndSession);
                    }
                }
#else
                header.peerID = ENET_HOST_TO_NET_16(currentPeer.outgoingPeerID | host.headerFlags);
#endif

                if (host.checksum != null) {
                    uint *checksum = (uint *) &headerData[host.buffers[0].dataLength];
                    *checksum = currentPeer.outgoingPeerID < ENET_PROTOCOL_MAXIMUM_PEER_ID ? currentPeer.connectID : 0;
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
                if (currentPeer.flags & ENET_PEER_FLAG_CONTINUE_SENDING)
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
public static void enet_host_flush(ENetHost *host) {
        host.serviceTime = enet_time_get();
        enet_protocol_send_outgoing_commands(host, null, 0);
    }

    /** Checks for any queued events on the host and dispatches one if available.
     *
     *  @param host    host to check for events
     *  @param event   an event structure where event details will be placed if available
     *  @retval > 0 if an event was dispatched
     *  @retval 0 if no events are available
     *  @retval < 0 on failure
     *  @ingroup host
     */
    public static int enet_host_check_events(ENetHost *host, ENetEvent *event) {
        if (event == null) { return -1; }

        event.type   = ENET_EVENT_TYPE_NONE;
        event.peer   = null;
        event.packet = null;

        return enet_protocol_dispatch_incoming_commands(host, event);
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
    public static int enet_host_service(ENetHost *host, ENetEvent *event, uint timeout) {
        uint waitCondition;

        if (event != null) {
            event.type   = ENET_EVENT_TYPE_NONE;
            event.peer   = null;
            event.packet = null;

            switch (enet_protocol_dispatch_incoming_commands(host, event)) {
                case 1:
                    return 1;

                case -1:
                    #ifdef ENET_DEBUG
                    perror("Error dispatching incoming packets");
                    #endif

                    return -1;

                default:
                    break;
            }
        }

        host.serviceTime = enet_time_get();
        timeout += host.serviceTime;

        do {
            if (ENET_TIME_DIFFERENCE(host.serviceTime, host.bandwidthThrottleEpoch) >= ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL) {
                enet_host_bandwidth_throttle(host);
            }

            switch (enet_protocol_send_outgoing_commands(host, event, 1)) {
                case 1:
                    return 1;

                case -1:
                    #ifdef ENET_DEBUG
                    perror("Error sending outgoing packets");
                    #endif

                    return -1;

                default:
                    break;
            }

            switch (enet_protocol_receive_incoming_commands(host, event)) {
                case 1:
                    return 1;

                case -1:
                    #ifdef ENET_DEBUG
                    perror("Error receiving incoming packets");
                    #endif

                    return -1;

                default:
                    break;
            }

            switch (enet_protocol_send_outgoing_commands(host, event, 1)) {
                case 1:
                    return 1;

                case -1:
                    #ifdef ENET_DEBUG
                    perror("Error sending outgoing packets");
                    #endif

                    return -1;

                default:
                    break;
            }

            if (event != null) {
                switch (enet_protocol_dispatch_incoming_commands(host, event)) {
                    case 1:
                        return 1;

                    case -1:
                        #ifdef ENET_DEBUG
                        perror("Error dispatching incoming packets");
                        #endif

                        return -1;

                    default:
                        break;
                }
            }

            if (ENET_TIME_GREATER_EQUAL(host.serviceTime, timeout)) {
                return 0;
            }

            do {
                host.serviceTime = enet_time_get();

                if (ENET_TIME_GREATER_EQUAL(host.serviceTime, timeout)) {
                    return 0;
                }

                waitCondition = ENET_SOCKET_WAIT_RECEIVE | ENET_SOCKET_WAIT_INTERRUPT;
                if (enet_socket_wait(host.socket, &waitCondition, ENET_TIME_DIFFERENCE(timeout, host.serviceTime)) != 0) {
                    return -1;
                }
            } while (waitCondition & ENET_SOCKET_WAIT_INTERRUPT);

            host.serviceTime = enet_time_get();
        } while (waitCondition & ENET_SOCKET_WAIT_RECEIVE);

        return 0;
    } /* enet_host_service */


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
public static void enet_peer_throttle_configure(ENetPeer *peer, uint interval, uint acceleration, uint deceleration) {
        ENetProtocol command;

        peer.packetThrottleInterval     = interval;
        peer.packetThrottleAcceleration = acceleration;
        peer.packetThrottleDeceleration = deceleration;

        command.header.command   = (int)ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        command.header.channelID = 0xFF;

        command.throttleConfigure.packetThrottleInterval     = ENET_HOST_TO_NET_32(interval);
        command.throttleConfigure.packetThrottleAcceleration = ENET_HOST_TO_NET_32(acceleration);
        command.throttleConfigure.packetThrottleDeceleration = ENET_HOST_TO_NET_32(deceleration);

        enet_peer_queue_outgoing_command(peer, &command, null, 0, 0);
    }

    public static int enet_peer_throttle(ENetPeer *peer, uint rtt) {
        if (peer.lastRoundTripTime <= peer.lastRoundTripTimeVariance) {
            peer.packetThrottle = peer.packetThrottleLimit;
        }
        else if (rtt <= peer.lastRoundTripTime) {
            peer.packetThrottle += peer.packetThrottleAcceleration;

            if (peer.packetThrottle > peer.packetThrottleLimit) {
                peer.packetThrottle = peer.packetThrottleLimit;
            }

            return 1;
        }
        else if (rtt > peer.lastRoundTripTime + 2 * peer.lastRoundTripTimeVariance) {
            if (peer.packetThrottle > peer.packetThrottleDeceleration) {
                peer.packetThrottle -= peer.packetThrottleDeceleration;
            } else {
                peer.packetThrottle = 0;
            }

            return -1;
        }

        return 0;
    }

    /* Extended functionality for easier binding in other programming languages */
    uint enet_host_get_peers_count(ENetHost *host) {
        return host.connectedPeers;
    }

    uint enet_host_get_packets_sent(ENetHost *host) {
        return host.totalSentPackets;
    }

    uint enet_host_get_packets_received(ENetHost *host) {
        return host.totalReceivedPackets;
    }

    uint enet_host_get_bytes_sent(ENetHost *host) {
        return host.totalSentData;
    }

    uint enet_host_get_bytes_received(ENetHost *host) {
        return host.totalReceivedData;
    }

    /** Gets received data buffer. Returns buffer length.
     *  @param host host to access recevie buffer
     *  @param data ouput parameter for recevied data
     *  @retval buffer length
     */
    uint enet_host_get_received_data(ENetHost *host, /*out*/ enet_uint8** data) {
        *data = host.receivedData;
        return host.receivedDataLength;
    }

    uint enet_host_get_mtu(ENetHost *host) {
        return host.mtu;
    }

    uint enet_peer_get_id(ENetPeer *peer) {
        return peer.connectID;
    }

    uint enet_peer_get_ip(ENetPeer *peer, char *ip, ulong ipLength) {
        return enet_address_get_host_ip(&peer.address, ip, ipLength);
    }

    enet_uint16 enet_peer_get_port(ENetPeer *peer) {
        return peer.address.port;
    }

    ENetPeerState enet_peer_get_state(ENetPeer *peer) {
        return peer.state;
    }

    uint enet_peer_get_rtt(ENetPeer *peer) {
        return peer.roundTripTime;
    }

    ulong enet_peer_get_packets_sent(ENetPeer *peer) {
        return peer.totalPacketsSent;
    }

    uint enet_peer_get_packets_lost(ENetPeer *peer) {
        return peer.totalPacketsLost;
    }

    ulong enet_peer_get_bytes_sent(ENetPeer *peer) {
        return peer.totalDataSent;
    }

    ulong enet_peer_get_bytes_received(ENetPeer *peer) {
        return peer.totalDataReceived;
    }

    void * enet_peer_get_data(ENetPeer *peer) {
        return (void *) peer.data;
    }

public static void enet_peer_set_data(ENetPeer *peer, const void *data) {
        peer.data = (uint *) data;
    }

    void * enet_packet_get_data(ENetPacket *packet) {
        return (void *) packet.data;
    }

    uint enet_packet_get_length(ENetPacket *packet) {
        return packet.dataLength;
    }

public static void enet_packet_set_free_callback(ENetPacket *packet, void *callback) {
        packet.freeCallback = (ENetPacketFreeCallback)callback;
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
    public static int enet_peer_send(ENetPeer *peer, enet_uint8 channelID, ENetPacket *packet) {
        ENetChannel *channel = &peer.channels[channelID];
        ENetProtocol command;
        ulong fragmentLength;

        if (peer.state != ENET_PEER_STATE_CONNECTED || channelID >= peer.channelCount || packet.dataLength > peer.host.maximumPacketSize) {
            return -1;
        }

        fragmentLength = peer.mtu - sizeof(ENetProtocolHeader) - sizeof(ENetProtocolSendFragment);
        if (peer.host.checksum != null) {
            fragmentLength -= sizeof(uint);
        }

        if (packet.dataLength > fragmentLength) {
            uint fragmentCount = (packet.dataLength + fragmentLength - 1) / fragmentLength, fragmentNumber, fragmentOffset;
            enet_uint8 commandNumber;
            enet_uint16 startSequenceNumber;
            ENetList fragments;
            ENetOutgoingCommand *fragment;

            if (fragmentCount > ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT) {
                return -1;
            }

            if ((packet.flags & (ENET_PACKET_FLAG_RELIABLE | ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT)) ==
                ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT &&
                channel.outgoingUnreliableSequenceNumber < 0xFFFF)
            {
                commandNumber       = ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT;
                startSequenceNumber = ENET_HOST_TO_NET_16(channel.outgoingUnreliableSequenceNumber + 1);
            } else {
                commandNumber       = ENET_PROTOCOL_COMMAND_SEND_FRAGMENT | ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                startSequenceNumber = ENET_HOST_TO_NET_16(channel.outgoingReliableSequenceNumber + 1);
            }

            enet_list_clear(&fragments);

            for (fragmentNumber = 0, fragmentOffset = 0; fragmentOffset < packet.dataLength; ++fragmentNumber, fragmentOffset += fragmentLength) {
                if (packet.dataLength - fragmentOffset < fragmentLength) {
                    fragmentLength = packet.dataLength - fragmentOffset;
                }

                fragment = (ENetOutgoingCommand *) enet_malloc(sizeof(ENetOutgoingCommand));

                if (fragment == null) {
                    while (!enet_list_empty(&fragments)) {
                        fragment = (ENetOutgoingCommand *) enet_list_remove(enet_list_begin(&fragments));

                        enet_free(fragment);
                    }

                    return -1;
                }

                fragment.fragmentOffset           = fragmentOffset;
                fragment.fragmentLength           = fragmentLength;
                fragment.packet                   = packet;
                fragment.command.header.command   = commandNumber;
                fragment.command.header.channelID = channelID;

                fragment.command.sendFragment.startSequenceNumber = startSequenceNumber;

                fragment.command.sendFragment.dataLength     = ENET_HOST_TO_NET_16(fragmentLength);
                fragment.command.sendFragment.fragmentCount  = ENET_HOST_TO_NET_32(fragmentCount);
                fragment.command.sendFragment.fragmentNumber = ENET_HOST_TO_NET_32(fragmentNumber);
                fragment.command.sendFragment.totalLength    = ENET_HOST_TO_NET_32(packet.dataLength);
                fragment.command.sendFragment.fragmentOffset = ENET_NET_TO_HOST_32(fragmentOffset);

                enet_list_insert(enet_list_end(&fragments), fragment);
            }

            packet.referenceCount += fragmentNumber;

            while (!enet_list_empty(&fragments)) {
                fragment = (ENetOutgoingCommand *) enet_list_remove(enet_list_begin(&fragments));
                enet_peer_setup_outgoing_command(peer, fragment);
            }

            return 0;
        }

        command.header.channelID = channelID;

        if ((packet.flags & (ENET_PACKET_FLAG_RELIABLE | ENET_PACKET_FLAG_UNSEQUENCED)) == ENET_PACKET_FLAG_UNSEQUENCED) {
            command.header.command = (int)ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED | (int)ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
            command.sendUnsequenced.dataLength = ENET_HOST_TO_NET_16(packet.dataLength);
        }
        else if (packet.flags & ENET_PACKET_FLAG_RELIABLE || channel.outgoingUnreliableSequenceNumber >= 0xFFFF) {
            command.header.command = (int)ENET_PROTOCOL_COMMAND_SEND_RELIABLE | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            command.sendReliable.dataLength = ENET_HOST_TO_NET_16(packet.dataLength);
        }
        else {
            command.header.command = ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE;
            command.sendUnreliable.dataLength = ENET_HOST_TO_NET_16(packet.dataLength);
        }

        if (enet_peer_queue_outgoing_command(peer, &command, packet, 0, packet.dataLength) == null) {
            return -1;
        }

        return 0;
    } // enet_peer_send

    /** Attempts to dequeue any incoming queued packet.
     *  @param peer peer to dequeue packets from
     *  @param channelID holds the channel ID of the channel the packet was received on success
     *  @returns a pointer to the packet, or null if there are no available incoming queued packets
     */
    public static ENetPacket enet_peer_receive(ENetPeer peer, ref byte channelID) {
        ENetIncomingCommand incomingCommand;
        ENetPacket packet;

        if (enet_list_empty(peer.dispatchedCommands)) {
            return null;
        }

        incomingCommand = enet_list_remove(enet_list_begin(peer.dispatchedCommands));

        //if (channelID != null) {
            channelID = incomingCommand.command.header.channelID;
        //}

        packet = incomingCommand.packet;
        --packet.referenceCount;

        if (incomingCommand.fragments != null) {
            enet_free(incomingCommand.fragments);
        }

        enet_free(incomingCommand);
        peer.totalWaitingData -= Math.Min(peer.totalWaitingData, packet.dataLength);

        return packet;
    }

    public static void enet_peer_reset_outgoing_commands(ENetPeer peer, ENetList<ENetOutgoingCommand> queue) {
        ENetOutgoingCommand outgoingCommand;

        while (!enet_list_empty(queue)) {
            outgoingCommand = enet_list_remove(enet_list_begin(queue));

            if (outgoingCommand.packet != null) {
                --outgoingCommand.packet.referenceCount;

                if (outgoingCommand.packet.referenceCount == 0) {
                    callbacks.packet_destroy(outgoingCommand.packet);
                }
            }

            enet_free(outgoingCommand);
        }
    }

    public static void enet_peer_remove_incoming_commands(ENetPeer peer, ENetList<ENetIncomingCommand> queue, ENetListNode<ENetIncomingCommand> startCommand, ENetListNode<ENetIncomingCommand> endCommand, ENetIncomingCommand excludeCommand)
    {
        ENET_UNUSED(queue);

        ENetListNode<ENetIncomingCommand> currentCommand;

        for (currentCommand = startCommand; currentCommand != endCommand;) {
            ENetIncomingCommand incomingCommand = currentCommand.value;

            currentCommand = enet_list_next(currentCommand);

            if (incomingCommand == excludeCommand)
                continue;

            enet_list_remove(incomingCommand.incomingCommandList);

            if (incomingCommand.packet != null) {
                --incomingCommand.packet.referenceCount;

                peer.totalWaitingData -= Math.Min(peer.totalWaitingData, incomingCommand.packet.dataLength);

                if (incomingCommand.packet.referenceCount == 0) {
                    callbacks.packet_destroy(incomingCommand.packet);
                }
            }

            if (incomingCommand.fragments != null) {
                enet_free(incomingCommand.fragments);
            }

            enet_free(incomingCommand);
        }
    }

    public static void enet_peer_reset_incoming_commands(ENetPeer peer, ENetList<ENetIncomingCommand> queue) {
        enet_peer_remove_incoming_commands(peer, queue, enet_list_begin(queue), enet_list_end(queue), null);
    }

    public static void enet_peer_reset_queues(ENetPeer peer) {
        ulong channel;

        if (0 != (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH) )
        {
            enet_list_remove(peer.dispatchList);
            peer.flags = (ushort)(peer.flags & ~(ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH);
        }

        while (!enet_list_empty(peer.acknowledgements)) {
            enet_free(enet_list_remove(enet_list_begin(peer.acknowledgements)));
        }

        enet_peer_reset_outgoing_commands(peer, peer.sentReliableCommands);
        enet_peer_reset_outgoing_commands(peer, peer.outgoingCommands);
        enet_peer_reset_outgoing_commands(peer, peer.outgoingSendReliableCommands);
        enet_peer_reset_incoming_commands(peer, peer.dispatchedCommands);

        if (peer.channels != null && peer.channelCount > 0) {
            for (channel = 0; channel < peer.channelCount; ++channel) {
                enet_peer_reset_incoming_commands(peer, peer.channels[channel].incomingReliableCommands);
                enet_peer_reset_incoming_commands(peer, peer.channels[channel].incomingUnreliableCommands);
            }

            enet_free(peer.channels);
        }

        peer.channels     = null;
        peer.channelCount = 0;
    }

    public static void enet_peer_on_connect(ENetPeer peer) {
        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            if (peer.incomingBandwidth != 0) {
                ++peer.host.bandwidthLimitedPeers;
            }

            ++peer.host.connectedPeers;
        }
    }

    public static void enet_peer_on_disconnect(ENetPeer peer) {
        if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            if (peer.incomingBandwidth != 0) {
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
    public static void enet_peer_reset(ENetPeer peer) {
        enet_peer_on_disconnect(peer);

        // We don't want to reset connectID here, otherwise, we can't get it in the Disconnect event
        // peer.connectID                     = 0;
        peer.outgoingPeerID                = ENetProtocolConst.ENET_PROTOCOL_MAXIMUM_PEER_ID;
        peer.state                         = ENetPeerState.ENET_PEER_STATE_DISCONNECTED;
        peer.incomingBandwidth             = 0;
        peer.outgoingBandwidth             = 0;
        peer.incomingBandwidthThrottleEpoch = 0;
        peer.outgoingBandwidthThrottleEpoch = 0;
        peer.incomingDataTotal             = 0;
        peer.totalDataReceived             = 0;
        peer.outgoingDataTotal             = 0;
        peer.totalDataSent                 = 0;
        peer.lastSendTime                  = 0;
        peer.lastReceiveTime               = 0;
        peer.nextTimeout                   = 0;
        peer.earliestTimeout               = 0;
        peer.packetLossEpoch               = 0;
        peer.packetsSent                   = 0;
        peer.totalPacketsSent              = 0;
        peer.packetsLost                   = 0;
        peer.totalPacketsLost              = 0;
        peer.packetLoss                    = 0;
        peer.packetLossVariance            = 0;
        peer.packetThrottle                = ENetPeerConst.ENET_PEER_DEFAULT_PACKET_THROTTLE;
        peer.packetThrottleLimit           = ENetPeerConst.ENET_PEER_PACKET_THROTTLE_SCALE;
        peer.packetThrottleCounter         = 0;
        peer.packetThrottleEpoch           = 0;
        peer.packetThrottleAcceleration    = ENetPeerConst.ENET_PEER_PACKET_THROTTLE_ACCELERATION;
        peer.packetThrottleDeceleration    = ENetPeerConst.ENET_PEER_PACKET_THROTTLE_DECELERATION;
        peer.packetThrottleInterval        = ENetPeerConst.ENET_PEER_PACKET_THROTTLE_INTERVAL;
        peer.pingInterval                  = ENetPeerConst.ENET_PEER_PING_INTERVAL;
        peer.timeoutLimit                  = ENetPeerConst.ENET_PEER_TIMEOUT_LIMIT;
        peer.timeoutMinimum                = ENetPeerConst.ENET_PEER_TIMEOUT_MINIMUM;
        peer.timeoutMaximum                = ENetPeerConst.ENET_PEER_TIMEOUT_MAXIMUM;
        peer.lastRoundTripTime             = ENetPeerConst.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.lowestRoundTripTime           = ENetPeerConst.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.lastRoundTripTimeVariance     = 0;
        peer.highestRoundTripTimeVariance  = 0;
        peer.roundTripTime                 = ENetPeerConst.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.roundTripTimeVariance         = 0;
        peer.mtu                           = peer.host.mtu;
        peer.reliableDataInTransit         = 0;
        peer.outgoingReliableSequenceNumber = 0;
        peer.windowSize                    = ENetProtocolConst.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        peer.incomingUnsequencedGroup      = 0;
        peer.outgoingUnsequencedGroup      = 0;
        peer.eventData                     = 0;
        peer.totalWaitingData              = 0;
        peer.flags                         = 0;

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
public static void enet_peer_ping(ENetPeer *peer) {
        ENetProtocol command;

        if (peer.state != ENET_PEER_STATE_CONNECTED) {
            return;
        }

        command.header.command   = (int)ENET_PROTOCOL_COMMAND_PING | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        command.header.channelID = 0xFF;

        enet_peer_queue_outgoing_command(peer, &command, null, 0, 0);
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
public static void enet_peer_ping_interval(ENetPeer *peer, uint pingInterval) {
        peer.pingInterval = pingInterval ? pingInterval : (uint)ENET_PEER_PING_INTERVAL;
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

public static void enet_peer_timeout(ENetPeer *peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum) {
        peer.timeoutLimit   = timeoutLimit ? timeoutLimit : (uint)ENET_PEER_TIMEOUT_LIMIT;
        peer.timeoutMinimum = timeoutMinimum ? timeoutMinimum : (uint)ENET_PEER_TIMEOUT_MINIMUM;
        peer.timeoutMaximum = timeoutMaximum ? timeoutMaximum : (uint)ENET_PEER_TIMEOUT_MAXIMUM;
    }

    /** Force an immediate disconnection from a peer.
     *  @param peer peer to disconnect
     *  @param data data describing the disconnection
     *  @remarks No ENET_EVENT_DISCONNECT event will be generated. The foreign peer is not
     *  guaranteed to receive the disconnect notification, and is reset immediately upon
     *  return from this function.
     */
public static void enet_peer_disconnect_now(ENetPeer *peer, uint data) {
        ENetProtocol command;

        if (peer.state == ENET_PEER_STATE_DISCONNECTED) {
            return;
        }

        if (peer.state != ENET_PEER_STATE_ZOMBIE && peer.state != ENET_PEER_STATE_DISCONNECTING) {
            enet_peer_reset_queues(peer);

            command.header.command   = (int)ENET_PROTOCOL_COMMAND_DISCONNECT | (int)ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
            command.header.channelID = 0xFF;
            command.disconnect.data  = ENET_HOST_TO_NET_32(data);

            enet_peer_queue_outgoing_command(peer, &command, null, 0, 0);
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
public static void enet_peer_disconnect(ENetPeer *peer, uint data) {
        ENetProtocol command;

        if (peer.state == ENET_PEER_STATE_DISCONNECTING ||
            peer.state == ENET_PEER_STATE_DISCONNECTED ||
            peer.state == ENET_PEER_STATE_ACKNOWLEDGING_DISCONNECT ||
            peer.state == ENET_PEER_STATE_ZOMBIE
        ) {
            return;
        }

        enet_peer_reset_queues(peer);

        command.header.command   = ENET_PROTOCOL_COMMAND_DISCONNECT;
        command.header.channelID = 0xFF;
        command.disconnect.data  = ENET_HOST_TO_NET_32(data);

        if (peer.state == ENET_PEER_STATE_CONNECTED || peer.state == ENET_PEER_STATE_DISCONNECT_LATER) {
            command.header.command |= ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        } else {
            command.header.command |= ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
        }

        enet_peer_queue_outgoing_command(peer, &command, null, 0, 0);

        if (peer.state == ENET_PEER_STATE_CONNECTED || peer.state == ENET_PEER_STATE_DISCONNECT_LATER) {
            enet_peer_on_disconnect(peer);

            peer.state = ENET_PEER_STATE_DISCONNECTING;
        } else {
            enet_host_flush(peer.host);
            enet_peer_reset(peer);
        }
    }

    public static int enet_peer_has_outgoing_commands (ENetPeer * peer)
    {
        if (enet_list_empty (&peer.outgoingCommands) &&
            enet_list_empty (&peer.outgoingSendReliableCommands) &&
            enet_list_empty (&peer.sentReliableCommands)) {
                return 0;
            }

        return 1;
    }

    /** Request a disconnection from a peer, but only after all queued outgoing packets are sent.
     *  @param peer peer to request a disconnection
     *  @param data data describing the disconnection
     *  @remarks An ENET_EVENT_DISCONNECT event will be generated by enet_host_service()
     *  once the disconnection is complete.
     */
public static void enet_peer_disconnect_later(ENetPeer *peer, uint data) {
        if ((peer.state == ENET_PEER_STATE_CONNECTED || peer.state == ENET_PEER_STATE_DISCONNECT_LATER) &&
            enet_peer_has_outgoing_commands(peer)
        ) {
            peer.state     = ENET_PEER_STATE_DISCONNECT_LATER;
            peer.eventData = data;
        } else {
            enet_peer_disconnect(peer, data);
        }
    }

    ENetAcknowledgement *enet_peer_queue_acknowledgement(ENetPeer *peer, const ENetProtocol *command, enet_uint16 sentTime) {
        ENetAcknowledgement *acknowledgement;

        if (command.header.channelID < peer.channelCount) {
            ENetChannel *channel       = &peer.channels[command.header.channelID];
            enet_uint16 reliableWindow = command.header.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
            enet_uint16 currentWindow  = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

            if (command.header.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                reliableWindow += ENET_PEER_RELIABLE_WINDOWS;
            }

            if (reliableWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1 && reliableWindow <= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS) {
                return null;
            }
        }

        acknowledgement = (ENetAcknowledgement *) enet_malloc(sizeof(ENetAcknowledgement));
        if (acknowledgement == null) {
            return null;
        }

        peer.outgoingDataTotal += sizeof(ENetProtocolAcknowledge);

        acknowledgement.sentTime = sentTime;
        acknowledgement.command  = *command;

        enet_list_insert(enet_list_end(&peer.acknowledgements), acknowledgement);
        return acknowledgement;
    }

public static void enet_peer_setup_outgoing_command(ENetPeer *peer, ENetOutgoingCommand *outgoingCommand) {
        ENetChannel *channel = &peer.channels[outgoingCommand.command.header.channelID];
        peer.outgoingDataTotal += enet_protocol_command_size(outgoingCommand.command.header.command) + outgoingCommand.fragmentLength;

        if (outgoingCommand.command.header.channelID == 0xFF) {
            ++peer.outgoingReliableSequenceNumber;

            outgoingCommand.reliableSequenceNumber   = peer.outgoingReliableSequenceNumber;
            outgoingCommand.unreliableSequenceNumber = 0;
        }
        else if (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) {
            ++channel.outgoingReliableSequenceNumber;
            channel.outgoingUnreliableSequenceNumber = 0;

            outgoingCommand.reliableSequenceNumber   = channel.outgoingReliableSequenceNumber;
            outgoingCommand.unreliableSequenceNumber = 0;
        }
        else if (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED) {
            ++peer.outgoingUnsequencedGroup;

            outgoingCommand.reliableSequenceNumber   = 0;
            outgoingCommand.unreliableSequenceNumber = 0;
        }
        else {
            if (outgoingCommand.fragmentOffset == 0) {
                ++channel.outgoingUnreliableSequenceNumber;
            }

            outgoingCommand.reliableSequenceNumber   = channel.outgoingReliableSequenceNumber;
            outgoingCommand.unreliableSequenceNumber = channel.outgoingUnreliableSequenceNumber;
        }

        outgoingCommand.sendAttempts          = 0;
        outgoingCommand.sentTime              = 0;
        outgoingCommand.roundTripTimeout      = 0;
        outgoingCommand.command.header.reliableSequenceNumber = ENET_HOST_TO_NET_16(outgoingCommand.reliableSequenceNumber);
        outgoingCommand.queueTime             = ++peer.host.totalQueued;

        switch (outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK) {
            case ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                outgoingCommand.command.sendUnreliable.unreliableSequenceNumber = ENET_HOST_TO_NET_16(outgoingCommand.unreliableSequenceNumber);
                break;

            case ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                outgoingCommand.command.sendUnsequenced.unsequencedGroup = ENET_HOST_TO_NET_16(peer.outgoingUnsequencedGroup);
                break;

            default:
                break;
        }

        if ((outgoingCommand.command.header.command & ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0 && outgoingCommand.packet != null) {
            enet_list_insert(enet_list_end(&peer.outgoingSendReliableCommands), outgoingCommand);
        } else {
            enet_list_insert(enet_list_end(&peer.outgoingCommands), outgoingCommand);
        }
    }

    ENetOutgoingCommand * enet_peer_queue_outgoing_command(ENetPeer *peer, const ENetProtocol *command, ENetPacket *packet, uint offset, enet_uint16 length) {
        ENetOutgoingCommand *outgoingCommand = (ENetOutgoingCommand *) enet_malloc(sizeof(ENetOutgoingCommand));

        if (outgoingCommand == null) {
            return null;
        }

        outgoingCommand.command        = *command;
        outgoingCommand.fragmentOffset = offset;
        outgoingCommand.fragmentLength = length;
        outgoingCommand.packet         = packet;
        if (packet != null) {
            ++packet.referenceCount;
        }

        enet_peer_setup_outgoing_command(peer, outgoingCommand);
        return outgoingCommand;
    }

public static void enet_peer_dispatch_incoming_unreliable_commands(ENetPeer *peer, ENetChannel *channel, ENetIncomingCommand * queuedCommand) {
        ENetListNode droppedCommand, startCommand, currentCommand;

        for (droppedCommand = startCommand = currentCommand = enet_list_begin(&channel.incomingUnreliableCommands);
            currentCommand != enet_list_end(&channel.incomingUnreliableCommands);
            currentCommand = enet_list_next(currentCommand)
        ) {
            ENetIncomingCommand *incomingCommand = (ENetIncomingCommand *) currentCommand;

            if ((incomingCommand.command.header.command & ENET_PROTOCOL_COMMAND_MASK) == ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
                continue;
            }

            if (incomingCommand.reliableSequenceNumber == channel.incomingReliableSequenceNumber) {
                if (incomingCommand.fragmentsRemaining <= 0) {
                    channel.incomingUnreliableSequenceNumber = incomingCommand.unreliableSequenceNumber;
                    continue;
                }

                if (startCommand != currentCommand) {
                    enet_list_move(enet_list_end(&peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

                    if (!(peer.flags & ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                        enet_list_insert(enet_list_end(&peer.host.dispatchQueue), &peer.dispatchList);
                        peer.flags |= ENET_PEER_FLAG_NEEDS_DISPATCH;
                    }

                    droppedCommand = currentCommand;
                } else if (droppedCommand != currentCommand) {
                    droppedCommand = enet_list_previous(currentCommand);
                }
            } else {
                enet_uint16 reliableWindow = incomingCommand.reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
                enet_uint16 currentWindow  = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

                if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                    reliableWindow += ENET_PEER_RELIABLE_WINDOWS;
                }

                if (reliableWindow >= currentWindow && reliableWindow < currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
                    break;
                }

                droppedCommand = enet_list_next(currentCommand);

                if (startCommand != currentCommand) {
                    enet_list_move(enet_list_end(&peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

                    if (!(peer.flags & ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                        enet_list_insert(enet_list_end(&peer.host.dispatchQueue), &peer.dispatchList);
                        peer.flags |= ENET_PEER_FLAG_NEEDS_DISPATCH;
                    }
                }
            }

            startCommand = enet_list_next(currentCommand);
        }

        if (startCommand != currentCommand) {
            enet_list_move(enet_list_end(&peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

            if (!(peer.flags & ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                enet_list_insert(enet_list_end(&peer.host.dispatchQueue), &peer.dispatchList);
                peer.flags |= ENET_PEER_FLAG_NEEDS_DISPATCH;
            }

            droppedCommand = currentCommand;
        }

        enet_peer_remove_incoming_commands(peer, &channel.incomingUnreliableCommands, enet_list_begin(&channel.incomingUnreliableCommands), droppedCommand, queuedCommand);
    }

public static void enet_peer_dispatch_incoming_reliable_commands(ENetPeer *peer, ENetChannel *channel, ENetIncomingCommand *queuedCommand) {
        ENetListNode currentCommand;

        for (currentCommand = enet_list_begin(&channel.incomingReliableCommands);
            currentCommand != enet_list_end(&channel.incomingReliableCommands);
            currentCommand = enet_list_next(currentCommand)
        ) {
            ENetIncomingCommand *incomingCommand = (ENetIncomingCommand *) currentCommand;

            if (incomingCommand.fragmentsRemaining > 0 || incomingCommand.reliableSequenceNumber != (enet_uint16) (channel.incomingReliableSequenceNumber + 1)) {
                break;
            }

            channel.incomingReliableSequenceNumber = incomingCommand.reliableSequenceNumber;

            if (incomingCommand.fragmentCount > 0) {
                channel.incomingReliableSequenceNumber += incomingCommand.fragmentCount - 1;
            }
        }

        if (currentCommand == enet_list_begin(&channel.incomingReliableCommands)) {
            return;
        }

        channel.incomingUnreliableSequenceNumber = 0;
        enet_list_move(enet_list_end(&peer.dispatchedCommands), enet_list_begin(&channel.incomingReliableCommands), enet_list_previous(currentCommand));

        if (!(peer.flags & ENET_PEER_FLAG_NEEDS_DISPATCH)) {
            enet_list_insert(enet_list_end(&peer.host.dispatchQueue), &peer.dispatchList);
            peer.flags |= ENET_PEER_FLAG_NEEDS_DISPATCH;
        }

        if (!enet_list_empty(&channel.incomingUnreliableCommands)) {
            enet_peer_dispatch_incoming_unreliable_commands(peer, channel, queuedCommand);
        }
    }

    ENetIncomingCommand * enet_peer_queue_incoming_command(ENetPeer *peer, const ENetProtocol *command, const void *data, ulong dataLength, uint flags, uint fragmentCount) {
        static ENetIncomingCommand dummyCommand;

        ENetChannel *channel = &peer.channels[command.header.channelID];
        uint unreliableSequenceNumber = 0, reliableSequenceNumber = 0;
        enet_uint16 reliableWindow, currentWindow;
        ENetIncomingCommand *incomingCommand;
        ENetListNode currentCommand;
        ENetPacket *packet = null;

        if (peer.state == ENET_PEER_STATE_DISCONNECT_LATER) {
            goto discardCommand;
        }

        if ((command.header.command & ENET_PROTOCOL_COMMAND_MASK) != ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
            reliableSequenceNumber = command.header.reliableSequenceNumber;
            reliableWindow         = reliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;
            currentWindow = channel.incomingReliableSequenceNumber / ENET_PEER_RELIABLE_WINDOW_SIZE;

            if (reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                reliableWindow += ENET_PEER_RELIABLE_WINDOWS;
            }

            if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
                goto discardCommand;
            }
        }

        switch (command.header.command & ENET_PROTOCOL_COMMAND_MASK) {
            case ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
            case ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                if (reliableSequenceNumber == channel.incomingReliableSequenceNumber) {
                    goto discardCommand;
                }

                for (currentCommand = enet_list_previous(enet_list_end(&channel.incomingReliableCommands));
                    currentCommand != enet_list_end(&channel.incomingReliableCommands);
                    currentCommand = enet_list_previous(currentCommand)
                ) {
                    incomingCommand = (ENetIncomingCommand *) currentCommand;

                    if (reliableSequenceNumber >= channel.incomingReliableSequenceNumber) {
                        if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                            continue;
                        }
                    } else if (incomingCommand.reliableSequenceNumber >= channel.incomingReliableSequenceNumber) {
                        break;
                    }

                    if (incomingCommand.reliableSequenceNumber <= reliableSequenceNumber) {
                        if (incomingCommand.reliableSequenceNumber < reliableSequenceNumber) {
                            break;
                        }

                        goto discardCommand;
                    }
                }
                break;

            case ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
            case ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                unreliableSequenceNumber = ENET_NET_TO_HOST_16(command.sendUnreliable.unreliableSequenceNumber);

                if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber) {
                    goto discardCommand;
                }

                for (currentCommand = enet_list_previous(enet_list_end(&channel.incomingUnreliableCommands));
                    currentCommand != enet_list_end(&channel.incomingUnreliableCommands);
                    currentCommand = enet_list_previous(currentCommand)
                ) {
                    incomingCommand = (ENetIncomingCommand *) currentCommand;

                    if ((command.header.command & ENET_PROTOCOL_COMMAND_MASK) == ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
                        continue;
                    }

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

                    if (incomingCommand.unreliableSequenceNumber <= unreliableSequenceNumber) {
                        if (incomingCommand.unreliableSequenceNumber < unreliableSequenceNumber) {
                            break;
                        }

                        goto discardCommand;
                    }
                }
                break;

            case ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                currentCommand = enet_list_end(&channel.incomingUnreliableCommands);
                break;

            default:
                goto discardCommand;
        }

        if (peer.totalWaitingData >= peer.host.maximumWaitingData) {
            goto notifyError;
        }

        packet = callbacks.packet_create(data, dataLength, flags);
        if (packet == null) {
            goto notifyError;
        }

        incomingCommand = (ENetIncomingCommand *) enet_malloc(sizeof(ENetIncomingCommand));
        if (incomingCommand == null) {
            goto notifyError;
        }

        incomingCommand.reliableSequenceNumber     = command.header.reliableSequenceNumber;
        incomingCommand.unreliableSequenceNumber   = unreliableSequenceNumber & 0xFFFF;
        incomingCommand.command                    = *command;
        incomingCommand.fragmentCount              = fragmentCount;
        incomingCommand.fragmentsRemaining         = fragmentCount;
        incomingCommand.packet                     = packet;
        incomingCommand.fragments                  = null;

        if (fragmentCount > 0) {
            if (fragmentCount <= ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT) {
                incomingCommand.fragments = (uint *) enet_malloc((fragmentCount + 31) / 32 * sizeof(uint));
            }

            if (incomingCommand.fragments == null) {
                enet_free(incomingCommand);

                goto notifyError;
            }

            memset(incomingCommand.fragments, 0, (fragmentCount + 31) / 32 * sizeof(uint));
        }

        assert(packet != null);
        ++packet.referenceCount;
        peer.totalWaitingData += packet.dataLength;

        enet_list_insert(enet_list_next(currentCommand), incomingCommand);

        switch (command.header.command & ENET_PROTOCOL_COMMAND_MASK) {
            case ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
            case ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                enet_peer_dispatch_incoming_reliable_commands(peer, channel, incomingCommand);
                break;

            default:
                enet_peer_dispatch_incoming_unreliable_commands(peer, channel, incomingCommand);
                break;
        }

        return incomingCommand;

    discardCommand:
        if (fragmentCount > 0) {
            goto notifyError;
        }

        if (packet != null  && packet.referenceCount == 0) {
            callbacks.packet_destroy(packet);
        }

        return &dummyCommand;

    notifyError:
        if (packet != null && packet.referenceCount == 0) {
            callbacks.packet_destroy(packet);
        }

        return null;
    } /* enet_peer_queue_incoming_command */

// =======================================================================//
// !
// ! Host
// !
// =======================================================================//

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
    ENetHost * enet_host_create(const ENetAddress *address, ulong peerCount, ulong channelLimit, uint incomingBandwidth, uint outgoingBandwidth) {
        ENetHost *host;
        ENetPeer *currentPeer;

        if (peerCount > ENET_PROTOCOL_MAXIMUM_PEER_ID) {
            return null;
        }

        host = (ENetHost *) enet_malloc(sizeof(ENetHost));
        if (host == null) { return null; }
        memset(host, 0, sizeof(ENetHost));

        host.peers = (ENetPeer *) enet_malloc(peerCount * sizeof(ENetPeer));
        if (host.peers == null) {
            enet_free(host);
            return null;
        }

        memset(host.peers, 0, peerCount * sizeof(ENetPeer));

        host.socket = enet_socket_create(ENET_SOCKET_TYPE_DATAGRAM);
        if (host.socket != ENET_SOCKET_NULL) {
            enet_socket_set_option (host.socket, ENET_SOCKOPT_IPV6_V6ONLY, 0);
        }

        if (host.socket == ENET_SOCKET_NULL || (address != null && enet_socket_bind(host.socket, address) < 0)) {
            if (host.socket != ENET_SOCKET_NULL) {
                enet_socket_destroy(host.socket);
            }

            enet_free(host.peers);
            enet_free(host);

            return null;
        }

        enet_socket_set_option(host.socket, ENET_SOCKOPT_NONBLOCK, 1);
        enet_socket_set_option(host.socket, ENET_SOCKOPT_BROADCAST, 1);
        enet_socket_set_option(host.socket, ENET_SOCKOPT_RCVBUF, ENET_HOST_RECEIVE_BUFFER_SIZE);
        enet_socket_set_option(host.socket, ENET_SOCKOPT_SNDBUF, ENET_HOST_SEND_BUFFER_SIZE);
        enet_socket_set_option(host.socket, ENET_SOCKOPT_IPV6_V6ONLY, 0);

        if (address != null && enet_socket_get_address(host.socket, &host.address) < 0) {
            host.address = *address;
        }

        if (!channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            channelLimit = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
        }

        host.randomSeed                    = (uint) ((uintptr_t) host % UINT32_MAX);
        host.randomSeed                    += enet_host_random_seed();
        host.randomSeed                    = (host.randomSeed << 16) | (host.randomSeed >> 16);
        host.channelLimit                  = channelLimit;
        host.incomingBandwidth             = incomingBandwidth;
        host.outgoingBandwidth             = outgoingBandwidth;
        host.bandwidthThrottleEpoch        = 0;
        host.recalculateBandwidthLimits    = 0;
        host.mtu                           = ENET_HOST_DEFAULT_MTU;
        host.peerCount                     = peerCount;
        host.commandCount                  = 0;
        host.bufferCount                   = 0;
        host.checksum                      = null;
        host.receivedAddress.host          = ENET_HOST_ANY;
        host.receivedAddress.port          = 0;
        host.receivedData                  = null;
        host.receivedDataLength            = 0;
        host.totalSentData                 = 0;
        host.totalSentPackets              = 0;
        host.totalReceivedData             = 0;
        host.totalReceivedPackets          = 0;
        host.totalQueued                   = 0;
        host.connectedPeers                = 0;
        host.bandwidthLimitedPeers         = 0;
        host.duplicatePeers                = ENET_PROTOCOL_MAXIMUM_PEER_ID;
        host.maximumPacketSize             = ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE;
        host.maximumWaitingData            = ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA;
        host.compressor.context            = null;
        host.compressor.compress           = null;
        host.compressor.decompress         = null;
        host.compressor.destroy            = null;
        host.intercept                     = null;

        enet_list_clear(&host.dispatchQueue);

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            currentPeer.host = host;
            currentPeer.incomingPeerID    = currentPeer - host.peers;
            currentPeer.outgoingSessionID = currentPeer.incomingSessionID = 0xFF;
            currentPeer.data = null;

            enet_list_clear(&currentPeer.acknowledgements);
            enet_list_clear(&currentPeer.sentReliableCommands);
            enet_list_clear(&currentPeer.outgoingCommands);
            enet_list_clear(&currentPeer.outgoingSendReliableCommands);
            enet_list_clear(&currentPeer.dispatchedCommands);

            enet_peer_reset(currentPeer);
        }

        return host;
    } /* enet_host_create */

    /** Destroys the host and all resources associated with it.
     *  @param host pointer to the host to destroy
     */
public static void enet_host_destroy(ENetHost *host) {
        ENetPeer *currentPeer;

        if (host == null) {
            return;
        }

        enet_socket_destroy(host.socket);

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            enet_peer_reset(currentPeer);
        }

        if (host.compressor.context != null && host.compressor.destroy) {
            (*host.compressor.destroy)(host.compressor.context);
        }

        enet_free(host.peers);
        enet_free(host);
    }

    uint enet_host_random(ENetHost * host) {
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
    ENetPeer * enet_host_connect(ENetHost *host, const ENetAddress *address, ulong channelCount, uint data) {
        ENetPeer *currentPeer;
        ENetChannel *channel;
        ENetProtocol command;

        if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT) {
            channelCount = ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;
        } else if (channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            channelCount = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
        }

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            if (currentPeer.state == ENET_PEER_STATE_DISCONNECTED) {
                break;
            }
        }

        if (currentPeer >= &host.peers[host.peerCount]) {
            return null;
        }

        currentPeer.channels = (ENetChannel *) enet_malloc(channelCount * sizeof(ENetChannel));
        if (currentPeer.channels == null) {
            return null;
        }

        currentPeer.channelCount = channelCount;
        currentPeer.state        = ENET_PEER_STATE_CONNECTING;
        currentPeer.address      = *address;
        currentPeer.connectID    = enet_host_random(host);
        currentPeer.mtu          = host.mtu;

        if (host.outgoingBandwidth == 0) {
            currentPeer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else {
            currentPeer.windowSize = (host.outgoingBandwidth / ENET_PEER_WINDOW_SIZE_SCALE) * ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (currentPeer.windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            currentPeer.windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (currentPeer.windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            currentPeer.windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        for (channel = currentPeer.channels; channel < &currentPeer.channels[channelCount]; ++channel) {
            channel.outgoingReliableSequenceNumber   = 0;
            channel.outgoingUnreliableSequenceNumber = 0;
            channel.incomingReliableSequenceNumber   = 0;
            channel.incomingUnreliableSequenceNumber = 0;

            enet_list_clear(&channel.incomingReliableCommands);
            enet_list_clear(&channel.incomingUnreliableCommands);

            channel.usedReliableWindows = 0;
            memset(channel.reliableWindows, 0, sizeof(channel.reliableWindows));
        }

        command.header.command                     = (int)ENET_PROTOCOL_COMMAND_CONNECT | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        command.header.channelID                   = 0xFF;
        command.connect.outgoingPeerID             = ENET_HOST_TO_NET_16(currentPeer.incomingPeerID);
        command.connect.incomingSessionID          = currentPeer.incomingSessionID;
        command.connect.outgoingSessionID          = currentPeer.outgoingSessionID;
        command.connect.mtu                        = ENET_HOST_TO_NET_32(currentPeer.mtu);
        command.connect.windowSize                 = ENET_HOST_TO_NET_32(currentPeer.windowSize);
        command.connect.channelCount               = ENET_HOST_TO_NET_32(channelCount);
        command.connect.incomingBandwidth          = ENET_HOST_TO_NET_32(host.incomingBandwidth);
        command.connect.outgoingBandwidth          = ENET_HOST_TO_NET_32(host.outgoingBandwidth);
        command.connect.packetThrottleInterval     = ENET_HOST_TO_NET_32(currentPeer.packetThrottleInterval);
        command.connect.packetThrottleAcceleration = ENET_HOST_TO_NET_32(currentPeer.packetThrottleAcceleration);
        command.connect.packetThrottleDeceleration = ENET_HOST_TO_NET_32(currentPeer.packetThrottleDeceleration);
        command.connect.connectID                  = currentPeer.connectID;
        command.connect.data                       = ENET_HOST_TO_NET_32(data);

        enet_peer_queue_outgoing_command(currentPeer, &command, null, 0, 0);

        return currentPeer;
    } /* enet_host_connect */

    /** Queues a packet to be sent to all peers associated with the host.
     *  @param host host on which to broadcast the packet
     *  @param channelID channel on which to broadcast
     *  @param packet packet to broadcast
     */
    public static void enet_host_broadcast(ENetHost *host, enet_uint8 channelID, ENetPacket *packet) {
        ENetPeer *currentPeer;

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            if (currentPeer.state != ENET_PEER_STATE_CONNECTED) {
                continue;
            }

            enet_peer_send(currentPeer, channelID, packet);
        }

        if (packet.referenceCount == 0) {
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
    public static int enet_host_send_raw(ENetHost *host, const ENetAddress* address, enet_uint8* data, ulong dataLength) {
        ENetBuffer buffer;
        buffer.data = data;
        buffer.dataLength = dataLength;
        return enet_socket_send(host.socket, address, &buffer, 1);
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
    public static int enet_host_send_raw_ex(ENetHost *host, const ENetAddress* address, enet_uint8* data, ulong skipBytes, ulong bytesToSend) {
        ENetBuffer buffer;
        buffer.data = data + skipBytes;
        buffer.dataLength = bytesToSend;
        return enet_socket_send(host.socket, address, &buffer, 1);
    }

    /** Sets intercept callback for the host.
     *  @param host host to set a callback
     *  @param callback intercept callback
     */
    public static void enet_host_set_intercept(ENetHost *host, const ENetInterceptCallback callback) {
        host.intercept = callback;
    }

    /** Sets the packet compressor the host should use to compress and decompress packets.
     *  @param host host to enable or disable compression for
     *  @param compressor callbacks for for the packet compressor; if null, then compression is disabled
     */
    public static void enet_host_compress(ENetHost *host, const ENetCompressor *compressor) {
        if (host.compressor.context != null && host.compressor.destroy) {
            (*host.compressor.destroy)(host.compressor.context);
        }

        if (compressor) {
            host.compressor = *compressor;
        } else {
            host.compressor.context = null;
        }
    }

    /** Limits the maximum allowed channels of future incoming connections.
     *  @param host host to limit
     *  @param channelLimit the maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT
     */
    public static void enet_host_channel_limit(ENetHost *host, ulong channelLimit) {
        if (!channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
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
    public static void enet_host_bandwidth_limit(ENetHost *host, uint incomingBandwidth, uint outgoingBandwidth) {
        host.incomingBandwidth = incomingBandwidth;
        host.outgoingBandwidth = outgoingBandwidth;
        host.recalculateBandwidthLimits = 1;
    }

    public static void enet_host_bandwidth_throttle(ENetHost *host) {
        uint timeCurrent       = enet_time_get();
        uint elapsedTime       = timeCurrent - host.bandwidthThrottleEpoch;
        uint peersRemaining    = (uint) host.connectedPeers;
        uint dataTotal         = ~0;
        uint bandwidth         = ~0;
        uint throttle          = 0;
        uint bandwidthLimit    = 0;

        int needsAdjustment = host.bandwidthLimitedPeers > 0 ? 1 : 0;
        ENetPeer *peer;
        ENetProtocol command;

        if (elapsedTime < ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL) {
            return;
        }

        if (host.outgoingBandwidth == 0 && host.incomingBandwidth == 0) {
            return;
        }

        host.bandwidthThrottleEpoch = timeCurrent;

        if (peersRemaining == 0) {
            return;
        }

        if (host.outgoingBandwidth != 0) {
            dataTotal = 0;
            bandwidth = (host.outgoingBandwidth * elapsedTime) / 1000;

            for (peer = host.peers; peer < &host.peers[host.peerCount]; ++peer) {
                if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
                    continue;
                }

                dataTotal += peer.outgoingDataTotal;
            }
        }

        while (peersRemaining > 0 && needsAdjustment != 0) {
            needsAdjustment = 0;

            if (dataTotal <= bandwidth) {
                throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
            } else {
                throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
            }

            for (peer = host.peers; peer < &host.peers[host.peerCount]; ++peer) {
                uint peerBandwidth;

                if ((peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) ||
                    peer.incomingBandwidth == 0 ||
                    peer.outgoingBandwidthThrottleEpoch == timeCurrent
                ) {
                    continue;
                }

                peerBandwidth = (peer.incomingBandwidth * elapsedTime) / 1000;
                if ((throttle * peer.outgoingDataTotal) / ENET_PEER_PACKET_THROTTLE_SCALE <= peerBandwidth) {
                    continue;
                }

                peer.packetThrottleLimit = (peerBandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / peer.outgoingDataTotal;

                if (peer.packetThrottleLimit == 0) {
                    peer.packetThrottleLimit = 1;
                }

                if (peer.packetThrottle > peer.packetThrottleLimit) {
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

        if (peersRemaining > 0) {
            if (dataTotal <= bandwidth) {
                throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
            } else {
                throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
            }

            for (peer = host.peers;
              peer < &host.peers[host.peerCount];
              ++peer)
            {
                if ((peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) || peer.outgoingBandwidthThrottleEpoch == timeCurrent) {
                    continue;
                }

                peer.packetThrottleLimit = throttle;

                if (peer.packetThrottle > peer.packetThrottleLimit) {
                    peer.packetThrottle = peer.packetThrottleLimit;
                }

                peer.incomingDataTotal = 0;
                peer.outgoingDataTotal = 0;
            }
        }

        if (host.recalculateBandwidthLimits) {
            host.recalculateBandwidthLimits = 0;

            peersRemaining  = (uint) host.connectedPeers;
            bandwidth       = host.incomingBandwidth;
            needsAdjustment = 1;

            if (bandwidth == 0) {
                bandwidthLimit = 0;
            } else {
                while (peersRemaining > 0 && needsAdjustment != 0) {
                    needsAdjustment = 0;
                    bandwidthLimit  = bandwidth / peersRemaining;

                    for (peer = host.peers; peer < &host.peers[host.peerCount]; ++peer) {
                        if ((peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) ||
                            peer.incomingBandwidthThrottleEpoch == timeCurrent
                        ) {
                            continue;
                        }

                        if (peer.outgoingBandwidth > 0 && peer.outgoingBandwidth >= bandwidthLimit) {
                            continue;
                        }

                        peer.incomingBandwidthThrottleEpoch = timeCurrent;

                        needsAdjustment = 1;
                        --peersRemaining;
                        bandwidth -= peer.outgoingBandwidth;
                    }
                }
            }

            for (peer = host.peers; peer < &host.peers[host.peerCount]; ++peer) {
                if (peer.state != ENET_PEER_STATE_CONNECTED && peer.state != ENET_PEER_STATE_DISCONNECT_LATER) {
                    continue;
                }

                command.header.command   = (int)ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT | (int)ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                command.header.channelID = 0xFF;
                command.bandwidthLimit.outgoingBandwidth = ENET_HOST_TO_NET_32(host.outgoingBandwidth);

                if (peer.incomingBandwidthThrottleEpoch == timeCurrent) {
                    command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32(peer.outgoingBandwidth);
                } else {
                    command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32(bandwidthLimit);
                }

                enet_peer_queue_outgoing_command(peer, &command, null, 0, 0);
            }
        }
    } /* enet_host_bandwidth_throttle */

// =======================================================================//
// !
// ! Time
// !
// =======================================================================//

    #ifdef _WIN32
        static LARGE_INTEGER getFILETIMEoffset() {
            SYSTEMTIME s;
            FILETIME f;
            LARGE_INTEGER t;

            s.wYear = 1970;
            s.wMonth = 1;
            s.wDay = 1;
            s.wHour = 0;
            s.wMinute = 0;
            s.wSecond = 0;
            s.wMilliseconds = 0;
            SystemTimeToFileTime(&s, &f);
            t.QuadPart = f.dwHighDateTime;
            t.QuadPart <<= 32;
            t.QuadPart |= f.dwLowDateTime;
            return (t);
        }
        int clock_gettime(int X, struct timespec *tv) {
            (void)X;
            LARGE_INTEGER t;
            FILETIME f;
            double microseconds;
            static LARGE_INTEGER offset;
            static double frequencyToMicroseconds;
            public static int initialized = 0;
            static BOOL usePerformanceCounter = 0;

            if (!initialized) {
                LARGE_INTEGER performanceFrequency;
                initialized = 1;
                usePerformanceCounter = QueryPerformanceFrequency(&performanceFrequency);
                if (usePerformanceCounter) {
                    QueryPerformanceCounter(&offset);
                    frequencyToMicroseconds = (double)performanceFrequency.QuadPart / 1000000.;
                } else {
                    offset = getFILETIMEoffset();
                    frequencyToMicroseconds = 10.;
                }
            }
            if (usePerformanceCounter) {
                QueryPerformanceCounter(&t);
            } else {
                GetSystemTimeAsFileTime(&f);
                t.QuadPart = f.dwHighDateTime;
                t.QuadPart <<= 32;
                t.QuadPart |= f.dwLowDateTime;
            }

            t.QuadPart -= offset.QuadPart;
            microseconds = (double)t.QuadPart / frequencyToMicroseconds;
            t.QuadPart = (LONGLONG)microseconds;
            tv.tv_sec = (long)(t.QuadPart / 1000000);
            tv.tv_nsec = t.QuadPart % 1000000 * 1000;
            return (0);
        }
    #elif __APPLE__ && __MAC_OS_X_VERSION_MIN_REQUIRED < 101200
        #define CLOCK_MONOTONIC 0

        int clock_gettime(int X, struct timespec *ts) {
            clock_serv_t cclock;
            mach_timespec_t mts;

            host_get_clock_service(mach_host_self(), SYSTEM_CLOCK, &cclock);
            clock_get_time(cclock, &mts);
            mach_port_deallocate(mach_task_self(), cclock);

            ts.tv_sec = mts.tv_sec;
            ts.tv_nsec = mts.tv_nsec;

            return 0;
        }
    #endif

    uint enet_time_get() {
        // TODO enet uses 32 bit timestamps. We should modify it to use
        // 64 bit timestamps, but this is not trivial since we'd end up
        // changing half the structs in enet. For now, retain 32 bits, but
        // use an offset so we don't run out of bits. Basically, the first
        // call of enet_time_get() will always return 1, and follow-up calls
        // indicate elapsed time since the first call.
        //
        // Note that we don't want to return 0 from the first call, in case
        // some part of enet uses 0 as a special value (meaning time not set
        // for example).
        static uint64_t start_time_ns = 0;

        struct timespec ts;
    #if defined(CLOCK_MONOTONIC_RAW)
        clock_gettime(CLOCK_MONOTONIC_RAW, &ts);
    #else
        clock_gettime(CLOCK_MONOTONIC, &ts);
    #endif

        static const uint64_t ns_in_s = 1000 * 1000 * 1000;
        static const uint64_t ns_in_ms = 1000 * 1000;
        uint64_t current_time_ns = ts.tv_nsec + (uint64_t)ts.tv_sec * ns_in_s;

        // Most of the time we just want to atomically read the start time. We
        // could just use a single CAS instruction instead of this if, but it
        // would be slower in the average case.
        //
        // Note that statics are auto-initialized to zero, and starting a thread
        // implies a memory barrier. So we know that whatever thread calls this,
        // it correctly sees the start_time_ns as 0 initially.
        uint64_t offset_ns = ENET_ATOMIC_READ(&start_time_ns);
        if (offset_ns == 0) {
            // We still need to CAS, since two different threads can get here
            // at the same time.
            //
            // We assume that current_time_ns is > 1ms.
            //
            // Set the value of the start_time_ns, such that the first timestamp
            // is at 1ms. This ensures 0 remains a special value.
            uint64_t want_value = current_time_ns - 1 * ns_in_ms;
            #if defined(__GNUC__) // Ignore warning.
            #pragma GCC diagnostic push
            #pragma GCC diagnostic ignored "-Wpedantic"
            #endif
            uint64_t old_value = ENET_ATOMIC_CAS(&start_time_ns, 0, want_value);
            #if defined(__GNUC__)
            #pragma GCC diagnostic pop
            #endif
            offset_ns = old_value == 0 ? want_value : old_value;
        }

        uint64_t result_in_ns = current_time_ns - offset_ns;
        return (uint)(result_in_ns / ns_in_ms);
    }

    public static void enet_inaddr_map4to6(struct in_addr in, struct in6_addr *out)
    {
        if (in.s_addr == 0x00000000) { /* 0.0.0.0 */
            *out = enet_v6_anyaddr;
        } else if (in.s_addr == 0xFFFFFFFF) { /* 255.255.255.255 */
            *out = enet_v6_noaddr;
        } else {
            *out = enet_v4_anyaddr;
            out.s6_addr[10] = 0xFF;
            out.s6_addr[11] = 0xFF;
            out.s6_addr[12] = ((uint8_t *)&in.s_addr)[0];
            out.s6_addr[13] = ((uint8_t *)&in.s_addr)[1];
            out.s6_addr[14] = ((uint8_t *)&in.s_addr)[2];
            out.s6_addr[15] = ((uint8_t *)&in.s_addr)[3];
        }
    }
    public static void enet_inaddr_map6to4(const struct in6_addr *in, struct in_addr *out)
    {
        memset(out, 0, sizeof(struct in_addr));
        ((uint8_t *)&out.s_addr)[0] = in.s6_addr[12];
        ((uint8_t *)&out.s_addr)[1] = in.s6_addr[13];
        ((uint8_t *)&out.s_addr)[2] = in.s6_addr[14];
        ((uint8_t *)&out.s_addr)[3] = in.s6_addr[15];
    }

    public static int enet_in6addr_lookup_host(const char *name, bool nodns, ENetAddress *out) {
        struct addrinfo hints, *resultList = null, *result = null;

        memset(&hints, 0, sizeof(hints));
        hints.ai_family = AF_UNSPEC;

        if (nodns)
        {
            hints.ai_flags = AI_NUMERICHOST; /* prevent actual DNS lookups! */
        }

        if (getaddrinfo(name, null, &hints, &resultList) != 0) {
            freeaddrinfo(resultList);
            return -1;
        }

        for (result = resultList; result != null; result = result.ai_next) {
            if (result.ai_addr != null) {
                if (result.ai_family == AF_INET || (result.ai_family == AF_UNSPEC && result.ai_addrlen == sizeof(struct sockaddr_in))) {
                    enet_inaddr_map4to6(((struct sockaddr_in*)result.ai_addr).sin_addr, &out.host);
                    out.sin6_scope_id = 0;
                    freeaddrinfo(resultList);
                    return 0;

                } else if (result.ai_family == AF_INET6 || (result.ai_family == AF_UNSPEC && result.ai_addrlen == sizeof(struct sockaddr_in6))) {
                    memcpy(&out.host, &((struct sockaddr_in6*)result.ai_addr).sin6_addr, sizeof(struct ENetAddress.in6_addr));
                    out.sin6_scope_id = (enet_uint16) ((struct sockaddr_in6*)result.ai_addr).sin6_scope_id;
                    freeaddrinfo(resultList);
                    return 0;
                }
            }
        }
        freeaddrinfo(resultList);
        return -1;
    }

    public static int enet_address_set_host_ip_new(ENetAddress *address, const char *name) {
        return enet_in6addr_lookup_host(name, true, address);
    }

    public static int enet_address_set_host_new(ENetAddress *address, const char *name) {
        return enet_in6addr_lookup_host(name, false, address);
    }

    public static int enet_address_get_host_ip_new(const ENetAddress *address, char *name, ulong nameLength) {
        if (IN6_IS_ADDR_V4MAPPED(&address.host)) {
            struct in_addr buf;
            enet_inaddr_map6to4(&address.host, &buf);

            if (inet_ntop(AF_INET, &buf, name, nameLength) == null) {
                return -1;
            }
        }
        else {
            if (inet_ntop(AF_INET6, (void*)&address.host, name, nameLength) == null) {
                return -1;
            }
        }

        return 0;
    } /* enet_address_get_host_ip_new */

    public static int enet_address_get_host_new(const ENetAddress *address, char *name, ulong nameLength) {
        struct sockaddr_in6 sin;
        memset(&sin, 0, sizeof(struct sockaddr_in6));

        int err;


        sin.sin6_family = AF_INET6;
        sin.sin6_port = ENET_HOST_TO_NET_16 (address.port);
        sin.sin6_addr = address.host;
        sin.sin6_scope_id = address.sin6_scope_id;

        err = getnameinfo((struct sockaddr *) &sin, sizeof(sin), name, nameLength, null, 0, NI_NAMEREQD);
        if (!err) {
            if (name != null && nameLength > 0 && !memchr(name, '\0', nameLength)) {
                return -1;
            }
            return 0;
        }
        if (err != EAI_NONAME) {
            return -1;
        }

        return enet_address_get_host_ip_new(address, name, nameLength);
    } /* enet_address_get_host_new */

// =======================================================================//
// !
// ! Platform Specific (Unix)
// !
// =======================================================================//

    #ifndef _WIN32

        #if defined(__MINGW32__) && defined(Math.MinGW_COMPAT)
        // inet_ntop/inet_pton for MinGW from http://mingw-users.1079350.n2.nabble.com/IPv6-getaddrinfo-amp-inet-ntop-td5891996.html
        const char *inet_ntop(int af, const void *src, char *dst, socklen_t cnt) {
            if (af == AF_INET) {
                struct sockaddr_in in;
                memset(&in, 0, sizeof(in));
                in.sin_family = AF_INET;
                memcpy(&in.sin_addr, src, sizeof(struct in_addr));
                getnameinfo((struct sockaddr *)&in, sizeof(struct sockaddr_in), dst, cnt, null, 0, NI_NUMERICHOST);
                return dst;
            }
            else if (af == AF_INET6) {
                struct sockaddr_in6 in;
                memset(&in, 0, sizeof(in));
                in.sin6_family = AF_INET6;
                memcpy(&in.sin6_addr, src, sizeof(struct in_addr6));
                getnameinfo((struct sockaddr *)&in, sizeof(struct sockaddr_in6), dst, cnt, null, 0, NI_NUMERICHOST);
                return dst;
            }

            return null;
        }

        #define NS_INADDRSZ  4
        #define NS_IN6ADDRSZ 16
        #define NS_INT16SZ   2

        int inet_pton4(const char *src, char *dst) {
            uint8_t tmp[NS_INADDRSZ], *tp;

            int saw_digit = 0;
            int octets = 0;
            *(tp = tmp) = 0;

            int ch;
            while ((ch = *src++) != '\0')
            {
                if (ch >= '0' && ch <= '9')
                {
                    uint32_t n = *tp * 10 + (ch - '0');

                    if (saw_digit && *tp == 0)
                        return 0;

                    if (n > 255)
                        return 0;

                    *tp = n;
                    if (!saw_digit)
                    {
                        if (++octets > 4)
                            return 0;
                        saw_digit = 1;
                    }
                }
                else if (ch == '.' && saw_digit)
                {
                    if (octets == 4)
                        return 0;
                    *++tp = 0;
                    saw_digit = 0;
                }
                else
                    return 0;
            }
            if (octets < 4)
                return 0;

            memcpy(dst, tmp, NS_INADDRSZ);

            return 1;
        }

        int inet_pton6(const char *src, char *dst) {
            static const char xdigits[] = "0123456789abcdef";
            uint8_t tmp[NS_IN6ADDRSZ];

            uint8_t *tp = (uint8_t*) memset(tmp, '\0', NS_IN6ADDRSZ);
            uint8_t *endp = tp + NS_IN6ADDRSZ;
            uint8_t *colonp = null;

            /* Leading :: requires some special handling. */
            if (*src == ':')
            {
                if (*++src != ':')
                    return 0;
            }

            const char *curtok = src;
            int saw_xdigit = 0;
            uint32_t val = 0;
            int ch;
            while ((ch = tolower(*src++)) != '\0')
            {
                const char *pch = strchr(xdigits, ch);
                if (pch != null)
                {
                    val <<= 4;
                    val |= (pch - xdigits);
                    if (val > 0xffff)
                        return 0;
                    saw_xdigit = 1;
                    continue;
                }
                if (ch == ':')
                {
                    curtok = src;
                    if (!saw_xdigit)
                    {
                        if (colonp)
                            return 0;
                        colonp = tp;
                        continue;
                    }
                    else if (*src == '\0')
                    {
                        return 0;
                    }
                    if (tp + NS_INT16SZ > endp)
                        return 0;
                    *tp++ = (uint8_t) (val >> 8) & 0xff;
                    *tp++ = (uint8_t) val & 0xff;
                    saw_xdigit = 0;
                    val = 0;
                    continue;
                }
                if (ch == '.' && ((tp + NS_INADDRSZ) <= endp) &&
                        inet_pton4(curtok, (char*) tp) > 0)
                {
                    tp += NS_INADDRSZ;
                    saw_xdigit = 0;
                    break; /* '\0' was seen by inet_pton4(). */
                }
                return 0;
            }
            if (saw_xdigit)
            {
                if (tp + NS_INT16SZ > endp)
                    return 0;
                *tp++ = (uint8_t) (val >> 8) & 0xff;
                *tp++ = (uint8_t) val & 0xff;
            }
            if (colonp != null)
            {
                /*
                 * Since some memmove()'s erroneously fail to handle
                 * overlapping regions, we'll do the shift by hand.
                 */
                const int n = tp - colonp;

                if (tp == endp)
                    return 0;

                for (int i = 1; i <= n; i++)
                {
                    endp[-i] = colonp[n - i];
                    colonp[n - i] = 0;
                }
                tp = endp;
            }
            if (tp != endp)
                return 0;

            memcpy(dst, tmp, NS_IN6ADDRSZ);

            return 1;
        }


        int inet_pton(int af, const char *src, struct in6_addr *dst) {
            switch (af)
            {
            case AF_INET:
                return inet_pton4(src, (char *)dst);
            case AF_INET6:
                return inet_pton6(src, (char *)dst);
            default:
                return -1;
            }
        }
    #endif // __MINGW__

    public static ulong enet_host_random_seed(void) {
        return (ulong) time(null);
    }

    public static int enet_address_set_host_ip_old(ENetAddress *address, const char *name) {
        if (!inet_pton(AF_INET6, name, &address.host)) {
            return -1;
        }

        return 0;
    }

    public static int enet_address_set_host_old(ENetAddress *address, const char *name) {
        struct addrinfo hints, *resultList = null, *result = null;

        memset(&hints, 0, sizeof(hints));
        hints.ai_family = AF_UNSPEC;

        if (getaddrinfo(name, null, &hints, &resultList) != 0) {
            return -1;
        }

        for (result = resultList; result != null; result = result.ai_next) {
            if (result.ai_addr != null && result.ai_addrlen >= sizeof(struct sockaddr_in)) {
                if (result.ai_family == AF_INET) {
                    struct sockaddr_in * sin = (struct sockaddr_in *) result.ai_addr;

                    ((uint32_t *)&address.host.s6_addr)[0] = 0;
                    ((uint32_t *)&address.host.s6_addr)[1] = 0;
                    ((uint32_t *)&address.host.s6_addr)[2] = htonl(0xffff);
                    ((uint32_t *)&address.host.s6_addr)[3] = sin.sin_addr.s_addr;

                    freeaddrinfo(resultList);
                    return 0;
                }
                else if(result.ai_family == AF_INET6) {
                    struct sockaddr_in6 * sin = (struct sockaddr_in6 *)result.ai_addr;

                    address.host = sin.sin6_addr;
                    address.sin6_scope_id = sin.sin6_scope_id;

                    freeaddrinfo(resultList);
                    return 0;
                }
            }
        }
        freeaddrinfo(resultList);

        return enet_address_set_host_ip(address, name);
    } /* enet_address_set_host_old */

    public static int enet_address_get_host_ip_old(const ENetAddress *address, char *name, ulong nameLength) {
        if (inet_ntop(AF_INET6, &address.host, name, nameLength) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_address_get_host_old(const ENetAddress *address, char *name, ulong nameLength) {
        struct sockaddr_in6 sin;
        int err;
        memset(&sin, 0, sizeof(struct sockaddr_in6));
        sin.sin6_family = AF_INET6;
        sin.sin6_port = ENET_HOST_TO_NET_16 (address.port);
        sin.sin6_addr = address.host;
        sin.sin6_scope_id = address.sin6_scope_id;
        err = getnameinfo((struct sockaddr *) &sin, sizeof(sin), name, nameLength, null, 0, NI_NAMEREQD);
        if (!err) {
            if (name != null && nameLength > 0 && !memchr(name, '\0', nameLength)) {
                return -1;
            }
            return 0;
        }
        if (err != EAI_NONAME) {
            return -1;
        }
        return enet_address_get_host_ip(address, name, nameLength);
    } /* enet_address_get_host_old */

    public static int enet_socket_bind(ENetSocket socket, const ENetAddress *address) {
        struct sockaddr_in6 sin;
        memset(&sin, 0, sizeof(struct sockaddr_in6));
        sin.sin6_family = AF_INET6;

        if (address != null) {
            sin.sin6_port       = ENET_HOST_TO_NET_16(address.port);
            sin.sin6_addr       = address.host;
            sin.sin6_scope_id   = address.sin6_scope_id;
        } else {
            sin.sin6_port       = 0;
            sin.sin6_addr       = ENET_HOST_ANY;
            sin.sin6_scope_id   = 0;
        }

        return bind(socket, (struct sockaddr *)&sin, sizeof(struct sockaddr_in6));
    }

    public static int enet_socket_get_address(ENetSocket socket, ENetAddress *address) {
        struct sockaddr_in6 sin;
        socklen_t sinLength = sizeof(struct sockaddr_in6);

        if (getsockname(socket, (struct sockaddr *) &sin, &sinLength) == -1) {
            return -1;
        }

        address.host           = sin.sin6_addr;
        address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
        address.sin6_scope_id  = sin.sin6_scope_id;

        return 0;
    }

    public static int enet_socket_listen(ENetSocket socket, int backlog) {
        return listen(socket, backlog < 0 ? SOMAXCONN : backlog);
    }

    ENetSocket enet_socket_create(ENetSocketType type) {
        return socket(PF_INET6, (int)type == ENET_SOCKET_TYPE_DATAGRAM ? SOCK_DGRAM : SOCK_STREAM, 0);
    }

    public static int enet_socket_set_option(ENetSocket socket, ENetSocketOption option, int value) {
        int result = -1;

        switch (option) {
            case ENET_SOCKOPT_NONBLOCK:
                result = fcntl(socket, F_SETFL, (value ? O_NONBLOCK : 0) | (fcntl(socket, F_GETFL) & ~O_NONBLOCK));
                break;

            case ENET_SOCKOPT_BROADCAST:
                result = setsockopt(socket, SOL_SOCKET, SO_BROADCAST, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_REUSEADDR:
                result = setsockopt(socket, SOL_SOCKET, SO_REUSEADDR, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_RCVBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVBUF, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_SNDBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDBUF, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_RCVTIMEO: {
                struct timeval timeVal;
                timeVal.tv_sec  = value / 1000;
                timeVal.tv_usec = (value % 1000) * 1000;
                result = setsockopt(socket, SOL_SOCKET, SO_RCVTIMEO, (char *)&timeVal, sizeof(struct timeval));
                break;
            }

            case ENET_SOCKOPT_SNDTIMEO: {
                struct timeval timeVal;
                timeVal.tv_sec  = value / 1000;
                timeVal.tv_usec = (value % 1000) * 1000;
                result = setsockopt(socket, SOL_SOCKET, SO_SNDTIMEO, (char *)&timeVal, sizeof(struct timeval));
                break;
            }

            case ENET_SOCKOPT_NODELAY:
                result = setsockopt(socket, IPPROTO_TCP, TCP_NODELAY, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_IPV6_V6ONLY:
                result = setsockopt(socket, IPPROTO_IPV6, IPV6_V6ONLY, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_TTL:
                result = setsockopt(socket, IPPROTO_IP, IP_TTL, (char *)&value, sizeof(int));
                break;

            default:
                break;
        }
        return result == -1 ? -1 : 0;
    } /* enet_socket_set_option */

    public static int enet_socket_get_option(ENetSocket socket, ENetSocketOption option, int *value) {
        int result = -1;
        socklen_t len;

        switch (option) {
            case ENET_SOCKOPT_ERROR:
                len    = sizeof(int);
                result = getsockopt(socket, SOL_SOCKET, SO_ERROR, value, &len);
                break;

            case ENET_SOCKOPT_TTL:
                len = sizeof (int);
                result = getsockopt(socket, IPPROTO_IP, IP_TTL, (char *)value, &len);
                break;

            default:
                break;
        }
        return result == -1 ? -1 : 0;
    }

    public static int enet_socket_connect(ENetSocket socket, const ENetAddress *address) {
        struct sockaddr_in6 sin;
        int result;

        memset(&sin, 0, sizeof(struct sockaddr_in6));

        sin.sin6_family     = AF_INET6;
        sin.sin6_port       = ENET_HOST_TO_NET_16(address.port);
        sin.sin6_addr       = address.host;
        sin.sin6_scope_id   = address.sin6_scope_id;

        result = connect(socket, (struct sockaddr *)&sin, sizeof(struct sockaddr_in6));
        if (result == -1 && errno == EINPROGRESS) {
            return 0;
        }

        return result;
    }

    ENetSocket enet_socket_accept(ENetSocket socket, ENetAddress *address) {
        int result;
        struct sockaddr_in6 sin;
        socklen_t sinLength = sizeof(struct sockaddr_in6);

        result = accept(socket,address != null ? (struct sockaddr *) &sin : null, address != null ? &sinLength : null);

        if (result == -1) {
            return ENET_SOCKET_NULL;
        }

        if (address != null) {
            address.host = sin.sin6_addr;
            address.port = ENET_NET_TO_HOST_16 (sin.sin6_port);
            address.sin6_scope_id = sin.sin6_scope_id;
        }

        return result;
    }

    public static int enet_socket_shutdown(ENetSocket socket, ENetSocketShutdown how) {
        return shutdown(socket, (int) how);
    }

    public static void enet_socket_destroy(ENetSocket socket) {
        if (socket != -1) {
            close(socket);
        }
    }

    public static int enet_socket_send(ENetSocket socket, const ENetAddress *address, const ENetBuffer *buffers, ulong bufferCount) {
        struct msghdr msgHdr;
        struct sockaddr_in6 sin;
        int sentLength;

        memset(&msgHdr, 0, sizeof(struct msghdr));

        if (address != null) {
            memset(&sin, 0, sizeof(struct sockaddr_in6));

            sin.sin6_family     = AF_INET6;
            sin.sin6_port       = ENET_HOST_TO_NET_16(address.port);
            sin.sin6_addr       = address.host;
            sin.sin6_scope_id   = address.sin6_scope_id;

            msgHdr.msg_name    = &sin;
            msgHdr.msg_namelen = sizeof(struct sockaddr_in6);
        }

        msgHdr.msg_iov    = (struct iovec *) buffers;
        msgHdr.msg_iovlen = bufferCount;

        sentLength = sendmsg(socket, &msgHdr, MSG_NOSIGNAL);

        if (sentLength == -1) {
            switch (errno)
            {
                case EWOULDBLOCK:
                    return 0;
                case EINTR:
                case EMSGSIZE:
                    return -2;
                default:
                    return -1;
            }

            return -1;
        }

        return sentLength;
    } /* enet_socket_send */

    public static int enet_socket_receive(ENetSocket socket, ENetAddress *address, ENetBuffer *buffers, ulong bufferCount) {
        struct msghdr msgHdr;
        struct sockaddr_in6 sin;
        int recvLength;

        memset(&msgHdr, 0, sizeof(struct msghdr));

        if (address != null) {
            msgHdr.msg_name    = &sin;
            msgHdr.msg_namelen = sizeof(struct sockaddr_in6);
        }

        msgHdr.msg_iov    = (struct iovec *) buffers;
        msgHdr.msg_iovlen = bufferCount;

        recvLength = recvmsg(socket, &msgHdr, MSG_NOSIGNAL);

        if (recvLength == -1) {
            if (errno == EWOULDBLOCK) {
                return 0;
            }

            return -1;
        }

        if (msgHdr.msg_flags & MSG_TRUNC) {
            return -2;
        }

        if (address != null) {
            address.host           = sin.sin6_addr;
            address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id  = sin.sin6_scope_id;
        }

        return recvLength;
    } /* enet_socket_receive */

    public static int enet_socketset_select(ENetSocket maxSocket, ENetSocketSet *readSet, ENetSocketSet *writeSet, uint timeout) {
        struct timeval timeVal;

        timeVal.tv_sec  = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        return select(maxSocket + 1, readSet, writeSet, null, &timeVal);
    }

    public static int enet_socket_wait(ENetSocket socket, uint *condition, ulong timeout) {
        struct pollfd pollSocket;
        int pollCount;

        pollSocket.fd     = socket;
        pollSocket.events = 0;

        if (*condition & ENET_SOCKET_WAIT_SEND) {
            pollSocket.events |= POLLOUT;
        }

        if (*condition & ENET_SOCKET_WAIT_RECEIVE) {
            pollSocket.events |= POLLIN;
        }

        pollCount = poll(&pollSocket, 1, timeout);

        if (pollCount < 0) {
            if (errno == EINTR && *condition & ENET_SOCKET_WAIT_INTERRUPT) {
                *condition = ENET_SOCKET_WAIT_INTERRUPT;

                return 0;
            }

            return -1;
        }

        *condition = ENET_SOCKET_WAIT_NONE;

        if (pollCount == 0) {
            return 0;
        }

        if (pollSocket.revents & POLLOUT) {
            *condition |= ENET_SOCKET_WAIT_SEND;
        }

        if (pollSocket.revents & POLLIN) {
            *condition |= ENET_SOCKET_WAIT_RECEIVE;
        }

        return 0;
    } /* enet_socket_wait */



// =======================================================================//
// !
// ! Platform Specific (Win)
// !
// =======================================================================//

    public static int enet_initialize(void) {
        WORD versionRequested = MAKEWORD(1, 1);
        WSADATA wsaData = {0};

        if (WSAStartup(versionRequested, &wsaData)) {
            return -1;
        }

        if (LOBYTE(wsaData.wVersion) != 1 || HIBYTE(wsaData.wVersion) != 1) {
            WSACleanup();
            return -1;
        }

        timeBeginPeriod(1);
        return 0;
    }

    public static void enet_deinitialize(void) {
        timeEndPeriod(1);
        WSACleanup();
    }

    ulong enet_host_random_seed(void) {
        return (ulong) timeGetTime();
    }

    public static int enet_address_set_host_ip_old(ENetAddress *address, const char *name) {
        enet_uint8 vals[4] = { 0, 0, 0, 0 };
        int i;

        for (i = 0; i < 4; ++i) {
            const char *next = name + 1;
            if (*name != '0') {
                long val = strtol(name, (char **) &next, 10);
                if (val < 0 || val > 255 || next == name || next - name > 3) {
                    return -1;
                }
                vals[i] = (enet_uint8) val;
            }

            if (*next != (i < 3 ? '.' : '\0')) {
                return -1;
            }
            name = next + 1;
        }

        memcpy(&address.host, vals, sizeof(uint));
        return 0;
    }

    public static int enet_address_set_host_old(ENetAddress *address, const char *name) {
        struct hostent *hostEntry = null;
        hostEntry = gethostbyname(name);

        if (hostEntry == null || hostEntry.h_addrtype != AF_INET) {
            if (!inet_pton(AF_INET6, name, &address.host)) {
                return -1;
            }

            return 0;
        }

        ((uint *)&address.host.s6_addr)[0] = 0;
        ((uint *)&address.host.s6_addr)[1] = 0;
        ((uint *)&address.host.s6_addr)[2] = htonl(0xffff);
        ((uint *)&address.host.s6_addr)[3] = *(uint *)hostEntry.h_addr_list[0];

        return 0;
    }

    public static int enet_address_get_host_ip_old(const ENetAddress *address, char *name, ulong nameLength) {
        if (inet_ntop(AF_INET6, (PVOID)&address.host, name, nameLength) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_address_get_host_old(const ENetAddress *address, char *name, ulong nameLength) {
        struct in6_addr in;
        struct hostent *hostEntry = null;
        in = address.host;
        hostEntry = gethostbyaddr((char *)&in, sizeof(struct in6_addr), AF_INET6);
        if (hostEntry == null) {
            return enet_address_get_host_ip(address, name, nameLength);
        } else {
            ulong hostLen = strlen(hostEntry.h_name);
            if (hostLen >= nameLength) {
                return -1;
            }
            memcpy(name, hostEntry.h_name, hostLen + 1);
        }
        return 0;
    }

    public static int enet_socket_bind(ENetSocket socket, const ENetAddress *address) {
        struct sockaddr_in6 sin = {0};
        sin.sin6_family = AF_INET6;

        if (address != null) {
            sin.sin6_port       = ENET_HOST_TO_NET_16 (address.port);
            sin.sin6_addr       = address.host;
            sin.sin6_scope_id   = address.sin6_scope_id;
        } else   {
            sin.sin6_port       = 0;
            sin.sin6_addr       = in6addr_any;
            sin.sin6_scope_id   = 0;
        }

        return bind(socket, (struct sockaddr *) &sin, sizeof(struct sockaddr_in6)) == SOCKET_ERROR ? -1 : 0;
    }

    public static int enet_socket_get_address(ENetSocket socket, ENetAddress *address) {
        struct sockaddr_in6 sin = {0};
        int sinLength = sizeof(struct sockaddr_in6);

        if (getsockname(socket, (struct sockaddr *) &sin, &sinLength) == -1) {
            return -1;
        }

        address.host           = sin.sin6_addr;
        address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
        address.sin6_scope_id  = sin.sin6_scope_id;

        return 0;
    }

    public static int enet_socket_listen(ENetSocket socket, int backlog) {
        return listen(socket, backlog < 0 ? SOMAXCONN : backlog) == SOCKET_ERROR ? -1 : 0;
    }

    ENetSocket enet_socket_create(ENetSocketType type) {
        return socket(PF_INET6, type == ENET_SOCKET_TYPE_DATAGRAM ? SOCK_DGRAM : SOCK_STREAM, 0);
    }

    public static int enet_socket_set_option(ENetSocket socket, ENetSocketOption option, int value) {
        int result = SOCKET_ERROR;

        switch (option) {
            case ENET_SOCKOPT_NONBLOCK: {
                u_long nonBlocking = (u_long) value;
                result = ioctlsocket(socket, FIONBIO, &nonBlocking);
                break;
            }

            case ENET_SOCKOPT_BROADCAST:
                result = setsockopt(socket, SOL_SOCKET, SO_BROADCAST, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_REUSEADDR:
                result = setsockopt(socket, SOL_SOCKET, SO_REUSEADDR, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_RCVBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVBUF, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_SNDBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDBUF, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_RCVTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVTIMEO, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_SNDTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDTIMEO, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_NODELAY:
                result = setsockopt(socket, IPPROTO_TCP, TCP_NODELAY, (char *)&value, sizeof(int));
                break;

            case ENET_SOCKOPT_IPV6_V6ONLY:
                result = setsockopt(socket, IPPROTO_IPV6, IPV6_V6ONLY, (char *)&value, sizeof(int));
                break;
            
            case ENET_SOCKOPT_TTL:
                result = setsockopt(socket, IPPROTO_IP, IP_TTL, (char *)&value, sizeof(int));
                break;

            default:
                break;
        }
        return result == SOCKET_ERROR ? -1 : 0;
    } /* enet_socket_set_option */

    public static int enet_socket_get_option(ENetSocket socket, ENetSocketOption option, int *value) {
        int result = SOCKET_ERROR, len;

        switch (option) {
            case ENET_SOCKOPT_ERROR:
                len    = sizeof(int);
                result = getsockopt(socket, SOL_SOCKET, SO_ERROR, (char *)value, &len);
                break;

            case ENET_SOCKOPT_TTL:
                len = sizeof(int);
                result = getsockopt(socket, IPPROTO_IP, IP_TTL, (char *)value, &len);
                break;

            default:
                break;
        }
        return result == SOCKET_ERROR ? -1 : 0;
    }

    public static int enet_socket_connect(ENetSocket socket, const ENetAddress *address) {
        struct sockaddr_in6 sin = {0};
        int result;

        sin.sin6_family     = AF_INET6;
        sin.sin6_port       = ENET_HOST_TO_NET_16(address.port);
        sin.sin6_addr       = address.host;
        sin.sin6_scope_id   = address.sin6_scope_id;

        result = connect(socket, (struct sockaddr *) &sin, sizeof(struct sockaddr_in6));
        if (result == SOCKET_ERROR && WSAGetLastError() != WSAEWOULDBLOCK) {
            return -1;
        }

        return 0;
    }

    ENetSocket enet_socket_accept(ENetSocket socket, ENetAddress *address) {
        SOCKET result;
        struct sockaddr_in6 sin = {0};
        int sinLength = sizeof(struct sockaddr_in6);

        result = accept(socket, address != null ? (struct sockaddr *)&sin : null, address != null ? &sinLength : null);

        if (result == INVALID_SOCKET) {
            return ENET_SOCKET_NULL;
        }

        if (address != null) {
            address.host           = sin.sin6_addr;
            address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id  = sin.sin6_scope_id;
        }

        return result;
    }

    public static int enet_socket_shutdown(ENetSocket socket, ENetSocketShutdown how) {
        return shutdown(socket, (int) how) == SOCKET_ERROR ? -1 : 0;
    }

    public static void enet_socket_destroy(ENetSocket socket) {
        if (socket != INVALID_SOCKET) {
            closesocket(socket);
        }
    }

    public static int enet_socket_send(ENetSocket socket, const ENetAddress *address, const ENetBuffer *buffers, ulong bufferCount) {
        struct sockaddr_in6 sin = {0};
        DWORD sentLength = 0;

        if (address != null) {
            sin.sin6_family     = AF_INET6;
            sin.sin6_port       = ENET_HOST_TO_NET_16(address.port);
            sin.sin6_addr       = address.host;
            sin.sin6_scope_id   = address.sin6_scope_id;
        }

        if (WSASendTo(socket,
            (LPWSABUF) buffers,
            (DWORD) bufferCount,
            &sentLength,
            0,
            address != null ? (struct sockaddr *) &sin : null,
            address != null ? sizeof(struct sockaddr_in6) : 0,
            null,
            null) == SOCKET_ERROR
        ) {
            return (WSAGetLastError() == WSAEWOULDBLOCK) ? 0 : -1;
        }

        return (int) sentLength;
    }

    public static int enet_socket_receive(ENetSocket socket, ENetAddress *address, ENetBuffer *buffers, ulong bufferCount) {
        INT sinLength = sizeof(struct sockaddr_in6);
        DWORD flags   = 0, recvLength = 0;
        struct sockaddr_in6 sin = {0};

        if (WSARecvFrom(socket,
            (LPWSABUF) buffers,
            (DWORD) bufferCount,
            &recvLength,
            &flags,
            address != null ? (struct sockaddr *) &sin : null,
            address != null ? &sinLength : null,
            null,
            null) == SOCKET_ERROR
        ) {
            switch (WSAGetLastError()) {
                case WSAEWOULDBLOCK:
                case WSAECONNRESET:
                    return 0;
                case WSAEINTR:
                case WSAEMSGSIZE:
                    return -2;
                default:
                    return -1;
            }
        }

        if (flags & MSG_PARTIAL) {
            return -2;
        }

        if (address != null) {
            address.host           = sin.sin6_addr;
            address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id  = sin.sin6_scope_id;
        }

        return (int) recvLength;
    } /* enet_socket_receive */

    public static int enet_socketset_select(ENetSocket maxSocket, ENetSocketSet *readSet, ENetSocketSet *writeSet, uint timeout) {
        struct timeval timeVal;

        timeVal.tv_sec  = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        return select(maxSocket + 1, readSet, writeSet, null, &timeVal);
    }

    public static int enet_socket_wait(ENetSocket socket, uint *condition, ulong timeout) {
        fd_set readSet, writeSet;
        struct timeval timeVal;
        int selectCount;

        timeVal.tv_sec  = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        FD_ZERO(&readSet);
        FD_ZERO(&writeSet);

        if (*condition & ENET_SOCKET_WAIT_SEND) {
            FD_SET(socket, &writeSet);
        }

        if (*condition & ENET_SOCKET_WAIT_RECEIVE) {
            FD_SET(socket, &readSet);
        }

        selectCount = select(socket + 1, &readSet, &writeSet, null, &timeVal);

        if (selectCount < 0) {
            return -1;
        }

        *condition = ENET_SOCKET_WAIT_NONE;

        if (selectCount == 0) {
            return 0;
        }

        if (FD_ISSET(socket, &writeSet)) {
            *condition |= ENET_SOCKET_WAIT_SEND;
        }

        if (FD_ISSET(socket, &readSet)) {
            *condition |= ENET_SOCKET_WAIT_RECEIVE;
        }

        return 0;
    } /* enet_socket_wait */

}