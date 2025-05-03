#define WIN32

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ENet.NET;

public delegate void ENetPacketFreeCallback(object _);

/** Callback that computes the checksum of the data held in buffers[0:bufferCount-1] */
public delegate uint ENetChecksumCallback(ref ENetBuffer buffers, ulong bufferCount);

/** Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error. */
public delegate int ENetInterceptCallback(ENetHost host, object @event);


public static class ENet
{
    public static void perror(string message)
    {
        Console.Error.WriteLine(message);
    }
    
    public const int CLOCK_MONOTONIC = 0;
    public const int ENET_TIME_OVERFLOW = 86400000;
    public static bool ENET_TIME_LESS(uint a, uint b) 
    {
        return ((a) - (b) >= ENET_TIME_OVERFLOW);
    }
    public static bool ENET_TIME_GREATER(int a, int b) 
    {
        return ((b) - (a) >= ENET_TIME_OVERFLOW);
    }
    public static int ENET_TIME_LESS_EQUAL(int a, int b) 
    {
        return (! ENET_TIME_GREATER (a, b));
    }
    public static int ENET_TIME_GREATER_EQUAL(int a, int b) 
    {
        return (! ENET_TIME_LESS (a, b));
    }
    public static uint ENET_TIME_DIFFERENCE(uint a, uint b) 
    {
        return ((a) - (b) >= ENET_TIME_OVERFLOW ? (b) - (a) : (a) - (b));
    }

    public static ushort ENET_HOST_TO_NET_16(ushort value)
    {
        return (htons(value)); /* macro that converts host to net byte-order of a 16-bit value */
    }
    public static uint ENET_HOST_TO_NET_32(uint value)
    {
        return (htonl(value)); /* macro that converts host to net byte-order of a 32-bit value */
    }
    public static ushort ENET_NET_TO_HOST_16(int value)
    {
        return (ntohs(value)); /* macro that converts net to host byte-order of a 16-bit value */
    }
    public static uint ENET_NET_TO_HOST_32(uint value)
    {
        return (ntohl(value)); /* macro that converts net to host byte-order of a 32-bit value */
    }

    public static int ENET_SOCKETSET_EMPTY(int sockset)
    {
        return FD_ZERO(&(sockset));
    }

    public static int ENET_SOCKETSET_ADD(int sockset, int socket)   
    {
        return FD_SET(socket, &(sockset));
    }
    public static int ENET_SOCKETSET_REMOVE(int sockset, int socket)
    {
        return FD_CLR(socket, &(sockset));
    }
    public static int ENET_SOCKETSET_CHECK(int sockset, int socket) 
    {
        return FD_ISSET(socket, &(sockset));
    }

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
    public static ENetListNode<T> enet_list_begin<T>(ref ENetList<T> list)
    {
        return list.sentinel.next;
    }
    
    public static ENetListNode<T> enet_list_end<T>(ref ENetList<T> list)
    {
        return list.sentinel;
    }
    
    public static bool enet_list_empty<T>(ref ENetList<T> list)
    {
        return enet_list_begin(ref list) == enet_list_end(ref list);
    }
    
    public static ENetListNode<T> enet_list_next<T>(ENetListNode<T> iterator)
    {
        return iterator.next;
    }
    
    public static ENetListNode<T> enet_list_previous<T>(ENetListNode<T> iterator)
    {
        return iterator.previous;
    }
    
    public static ref T enet_list_front<T>(ref ENetList<T> list)
    {
        return ref list.sentinel.next.value;
    }
    
    public static ref T enet_list_back<T>(ref ENetList<T> list)
    {
        return ref list.sentinel.previous.value;
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

    public static T enet_malloc<T>()
    {
        return enet_malloc<T>(1)[0];
    }
    public static T[] enet_malloc<T>(int size) 
    {
        object memory = callbacks.malloc(size);

        if (memory == null) {
            callbacks.no_memory();
        }

        return (T[])memory;
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

    public static void enet_list_clear<T>(ref ENetList<T> list) {
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

    public static int enet_list_size<T>(ref ENetList<T> list) 
    {
        int size = 0;
        ENetListNode<T> position;

        for (position = enet_list_begin(ref list); position != enet_list_end(ref list); position = enet_list_next(position)) {
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

            //packet.data = (byte *)packet + sizeof(ENetPacket);

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
        newPacket.data = (byte *)newPacket + sizeof(ENetPacket);
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

            packet.data = (byte *)packet + sizeof(ENetPacket);

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

    public static void enet_protocol_remove_sent_unreliable_commands(ENetPeer peer, ENetList<ENetOutgoingCommand> sentUnreliableCommands) 
    {

        if (enet_list_empty (sentUnreliableCommands))
            return;

        do
        {
            ref ENetOutgoingCommand outgoingCommand = ref enet_list_front(sentUnreliableCommands);
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

        outgoingCommand = enet_list_front(peer.sentReliableCommands);
        peer.nextTimeout = outgoingCommand.sentTime + outgoingCommand.roundTripTimeout;

        return commandNumber;
    } /* enet_protocol_remove_sent_reliable_command */

    public static ENetPeer enet_protocol_handle_connect(ENetHost host, ref ENetProtocolHeader header, ref ENetProtocol command)
    {
        ENET_UNUSED(header);

        byte incomingSessionID, outgoingSessionID;
        uint mtu, windowSize;
        ENetChannel channel = null;
        uint channelCount, duplicatePeers = 0;
        ENetPeer currentPeer, peer = null;
        ENetProtocol verifyCommand = new ENetProtocol();

        channelCount = ENET_NET_TO_HOST_32(command.connect.channelCount);

        if (channelCount < ENets.ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT || channelCount > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            return null;
        }

        for (ulong peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
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

        for (channel = peer.channels; channel < &peer.channels[channelCount]; ++channel) {
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

        channel = &peer.channels[command.header.channelID];
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

                if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) !=
                    ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT ||
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
        ulong headerSize;
        ushort peerID, flags;
        byte sessionID;

        if (host.receivedDataLength < sizeof(ENetProtocolHeaderMinimal)) {
            return 0;
        }

        ref ENetProtocolHeader header = ref host.receivedData;

        peerID    = ENET_NET_TO_HOST_16(header.peerID);
        sessionID = (peerID & ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK) >> ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_SHIFT;
        flags     = peerID & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK;
        peerID   &= ~(ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_MASK | ENetProtocolFlag.ENET_PROTOCOL_HEADER_SESSION_MASK);

        headerSize = (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_SENT_TIME ? sizeof(ENetProtocolHeader) : sizeof(ENetProtocolHeaderMinimal));

        if (flags & ENetProtocolFlag.ENET_PROTOCOL_HEADER_FLAG_PEER_EXTRA) {
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
            peer = &host.peers[peerID];

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
            if (host.compressor.context == null || host.compressor.CompressorDecompress == null) {
                return 0;
            }

            originalSize = host.compressor.CompressorDecompress(host.compressor.context,
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

            commandNumber = command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK;
            if (commandNumber >= ENetProtocolCommand.ENET_PROTOCOL_COMMAND_COUNT) {
                break;
            }

            commandSize = commandSizes[commandNumber];
            if (commandSize == 0 || currentData + commandSize > &host.receivedData[host.receivedDataLength]) {
                break;
            }

            currentData += commandSize;

            if (peer == null && (commandNumber != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT || currentData < &host.receivedData[host.receivedDataLength])) {
                break;
            }

            command.header.reliableSequenceNumber = ENET_NET_TO_HOST_16(command.header.reliableSequenceNumber);

            switch ((ENetProtocolCommand)commandNumber) {
                case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_ACKNOWLEDGE:
                    if (enet_protocol_handle_acknowledge(host, @event, peer, ref command)) {
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
                    if (enet_protocol_handle_send_reliable(host, peer, ref command, &currentData)) {
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

#ifdef ENET_DEBUG
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
                        #ifdef ENET_DEBUG_COMPRESS
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

    /** Checks for any queued events on the host and dispatches one if available.
     *
     *  @param host    host to check for events
     *  @param event   an event structure where event details will be placed if available
     *  @retval > 0 if an event was dispatched
     *  @retval 0 if no events are available
     *  @retval < 0 on failure
     *  @ingroup host
     */
    public static int enet_host_check_events(ENetHost *host, ENetEvent @event) {
        if (event == null) { return -1; }

        event.type   = ENetEventType.ENET_EVENT_TYPE_NONE;
        event.peer   = null;
        event.packet = null;

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
    public static int enet_host_service(ENetHost host, ENetEvent @event, uint timeout) {
        uint waitCondition;

        if (@event != null) {
            @event.type   = ENetEventType.ENET_EVENT_TYPE_NONE;
            @event.peer   = null;
            @event.packet = null;

            switch (enet_protocol_dispatch_incoming_commands(host, @event)) {
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
            if (ENET_TIME_DIFFERENCE(host.serviceTime, host.bandwidthThrottleEpoch) >= ENets.ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL) {
                enet_host_bandwidth_throttle(host);
            }

            switch (enet_protocol_send_outgoing_commands(host, @event, 1)) {
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

            switch (enet_protocol_receive_incoming_commands(host, @event)) {
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

            switch (enet_protocol_send_outgoing_commands(host, @event, 1)) {
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
                switch (enet_protocol_dispatch_incoming_commands(host, @event)) {
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
    public static void enet_peer_throttle_configure(ENetPeer peer, uint interval, uint acceleration, uint deceleration)
    {
        ENetProtocol command = new ENetProtocol();

        peer.packetThrottleInterval     = interval;
        peer.packetThrottleAcceleration = acceleration;
        peer.packetThrottleDeceleration = deceleration;

        command.header.command   = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_THROTTLE_CONFIGURE | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        command.header.channelID = 0xFF;

        command.throttleConfigure.packetThrottleInterval     = ENET_HOST_TO_NET_32(interval);
        command.throttleConfigure.packetThrottleAcceleration = ENET_HOST_TO_NET_32(acceleration);
        command.throttleConfigure.packetThrottleDeceleration = ENET_HOST_TO_NET_32(deceleration);

        enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
    }

    public static int enet_peer_throttle(ENetPeer peer, uint rtt) {
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
    public static ulong enet_host_get_peers_count(ENetHost host) {
        return host.connectedPeers;
    }

    public static uint enet_host_get_packets_sent(ENetHost host) {
        return host.totalSentPackets;
    }

    public static uint enet_host_get_packets_received(ENetHost host) {
        return host.totalReceivedPackets;
    }

    public static uint enet_host_get_bytes_sent(ENetHost host) {
        return host.totalSentData;
    }

    public static uint enet_host_get_bytes_received(ENetHost host) {
        return host.totalReceivedData;
    }

    /** Gets received data buffer. Returns buffer length.
     *  @param host host to access recevie buffer
     *  @param data ouput parameter for recevied data
     *  @retval buffer length
     */
    public static uint enet_host_get_received_data(ENetHost host, /*out*/ byte** data) {
        *data = host.receivedData;
        return host.receivedDataLength;
    }

    public static uint enet_host_get_mtu(ENetHost host) {
        return host.mtu;
    }

    public static uint enet_peer_get_id(ENetPeer peer) {
        return peer.connectID;
    }

    public static uint enet_peer_get_ip(ENetPeer peer, string ip, ulong ipLength) {
        return enet_address_get_host_ip(peer.address, ip, ipLength);
    }

    public static ushort enet_peer_get_port(ENetPeer peer) {
        return peer.address.port;
    }

    public static ENetPeerState enet_peer_get_state(ENetPeer peer) {
        return peer.state;
    }

    public static uint enet_peer_get_rtt(ENetPeer peer) {
        return peer.roundTripTime;
    }

    public static ulong enet_peer_get_packets_sent(ENetPeer peer) {
        return peer.totalPacketsSent;
    }

    public static uint enet_peer_get_packets_lost(ENetPeer peer) {
        return peer.totalPacketsLost;
    }

    public static ulong enet_peer_get_bytes_sent(ENetPeer peer) {
        return peer.totalDataSent;
    }

    public static ulong enet_peer_get_bytes_received(ENetPeer peer) {
        return peer.totalDataReceived;
    }

    public static object enet_peer_get_data(ENetPeer peer) {
        return peer.data;
    }

    public static void enet_peer_set_data(ENetPeer peer, object data) {
        peer.data = data;
    }

    public static object enet_packet_get_data(ENetPacket packet) {
        return packet.data;
    }

    public static ulong enet_packet_get_length(ENetPacket packet) {
        return packet.dataLength;
    }

    public static void enet_packet_set_free_callback(ENetPacket packet, ENetPacketFreeCallback callback) {
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
    public static int enet_peer_send(ENetPeer peer, byte channelID, ENetPacket packet) {
        ENetChannel channel = peer.channels[channelID];
        ulong fragmentLength;

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED || channelID >= peer.channelCount || packet.dataLength > peer.host.maximumPacketSize) {
            return -1;
        }

        fragmentLength = peer.mtu - (uint)Marshal.SizeOf<ENetProtocolHeader>() - (uint)Marshal.SizeOf<ENetProtocolSendFragment>();
        if (peer.host.checksum != null) {
            fragmentLength -= sizeof(uint);
        }

        if (packet.dataLength > fragmentLength) {
            uint fragmentCount = (packet.dataLength + fragmentLength - 1) / fragmentLength, fragmentNumber, fragmentOffset;
            byte commandNumber;
            ushort startSequenceNumber;
            ENetList<ENetOutgoingCommand> fragments = new ENetList<ENetOutgoingCommand>();
            ENetOutgoingCommand fragment;

            if (fragmentCount > ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT) {
                return -1;
            }

            if ((packet.flags & (ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE | ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT)) ==
                ENetPacketFlag.ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT &&
                channel.outgoingUnreliableSequenceNumber < 0xFFFF)
            {
                commandNumber       = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT;
                startSequenceNumber = ENET_HOST_TO_NET_16(channel.outgoingUnreliableSequenceNumber + 1);
            } else {
                commandNumber       = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT | ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                startSequenceNumber = ENET_HOST_TO_NET_16(channel.outgoingReliableSequenceNumber + 1);
            }

            enet_list_clear(fragments);

            for (fragmentNumber = 0, fragmentOffset = 0; fragmentOffset < packet.dataLength; ++fragmentNumber, fragmentOffset += fragmentLength) {
                if (packet.dataLength - fragmentOffset < fragmentLength) {
                    fragmentLength = packet.dataLength - fragmentOffset;
                }

                fragment = (ENetOutgoingCommand *) enet_malloc(sizeof(ENetOutgoingCommand));

                if (fragment == null) {
                    while (!enet_list_empty(ref fragments)) {
                        fragment = enet_list_remove(enet_list_begin(ref fragments));

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

                fragment.command.sendFragment.dataLength     = ENET_HOST_TO_NET_16((ushort)fragmentLength);
                fragment.command.sendFragment.fragmentCount  = ENET_HOST_TO_NET_32(fragmentCount);
                fragment.command.sendFragment.fragmentNumber = ENET_HOST_TO_NET_32(fragmentNumber);
                fragment.command.sendFragment.totalLength    = ENET_HOST_TO_NET_32((uint)packet.dataLength);
                fragment.command.sendFragment.fragmentOffset = ENET_NET_TO_HOST_32(fragmentOffset);

                enet_list_insert(enet_list_end(ref fragments), fragment);
            }

            packet.referenceCount += fragmentNumber;

            while (!enet_list_empty(ref fragments)) {
                fragment = enet_list_remove(enet_list_begin(ref fragments));
                enet_peer_setup_outgoing_command(peer, fragment);
            }

            return 0;
        }

        ENetProtocol command = new ENetProtocol();
        command.header.channelID = channelID;

        if ((packet.flags & (ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE | ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED)) == ENetPacketFlag.ENET_PACKET_FLAG_UNSEQUENCED) {
            command.header.command = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
            command.sendUnsequenced.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
        }
        else if (packet.flags & ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE || channel.outgoingUnreliableSequenceNumber >= 0xFFFF) {
            command.header.command = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
            command.sendReliable.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
        }
        else {
            command.header.command = ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE;
            command.sendUnreliable.dataLength = ENET_HOST_TO_NET_16((ushort)packet.dataLength);
        }

        if (enet_peer_queue_outgoing_command(peer, ref command, packet, 0, packet.dataLength) == null) {
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

        if (enet_list_empty(ref peer.dispatchedCommands)) {
            return null;
        }

        incomingCommand = enet_list_remove(enet_list_begin(ref peer.dispatchedCommands));

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

        while (!enet_list_empty(ref queue)) {
            outgoingCommand = enet_list_remove(enet_list_begin(ref queue));

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
        enet_peer_remove_incoming_commands(peer, queue, enet_list_begin(ref queue), enet_list_end(ref queue), null);
    }

    public static void enet_peer_reset_queues(ENetPeer peer) {
        ulong channel;

        if (0 != (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH) )
        {
            enet_list_remove(peer.dispatchList);
            peer.flags = (ushort)(peer.flags & ~(ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH);
        }

        while (!enet_list_empty(ref peer.acknowledgements)) {
            enet_free(enet_list_remove(enet_list_begin(ref peer.acknowledgements)));
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
        peer.outgoingPeerID                = ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID;
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
        peer.packetThrottle                = ENets.ENET_PEER_DEFAULT_PACKET_THROTTLE;
        peer.packetThrottleLimit           = ENets.ENET_PEER_PACKET_THROTTLE_SCALE;
        peer.packetThrottleCounter         = 0;
        peer.packetThrottleEpoch           = 0;
        peer.packetThrottleAcceleration    = ENets.ENET_PEER_PACKET_THROTTLE_ACCELERATION;
        peer.packetThrottleDeceleration    = ENets.ENET_PEER_PACKET_THROTTLE_DECELERATION;
        peer.packetThrottleInterval        = ENets.ENET_PEER_PACKET_THROTTLE_INTERVAL;
        peer.pingInterval                  = ENets.ENET_PEER_PING_INTERVAL;
        peer.timeoutLimit                  = ENets.ENET_PEER_TIMEOUT_LIMIT;
        peer.timeoutMinimum                = ENets.ENET_PEER_TIMEOUT_MINIMUM;
        peer.timeoutMaximum                = ENets.ENET_PEER_TIMEOUT_MAXIMUM;
        peer.lastRoundTripTime             = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.lowestRoundTripTime           = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.lastRoundTripTimeVariance     = 0;
        peer.highestRoundTripTimeVariance  = 0;
        peer.roundTripTime                 = ENets.ENET_PEER_DEFAULT_ROUND_TRIP_TIME;
        peer.roundTripTimeVariance         = 0;
        peer.mtu                           = peer.host.mtu;
        peer.reliableDataInTransit         = 0;
        peer.outgoingReliableSequenceNumber = 0;
        peer.windowSize                    = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
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
    public static void enet_peer_ping(ENetPeer peer) {

        if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED) {
            return;
        }

        ENetProtocol command = new ENetProtocol();
        command.header.command   = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_PING | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
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
public static void enet_peer_ping_interval(ENetPeer peer, uint pingInterval) {
        peer.pingInterval = pingInterval ? pingInterval : (uint)ENets.ENET_PEER_PING_INTERVAL;
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

    public static void enet_peer_timeout(ENetPeer peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum) {
        peer.timeoutLimit   = 0 < timeoutLimit ? timeoutLimit : (uint)ENets.ENET_PEER_TIMEOUT_LIMIT;
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
    public static void enet_peer_disconnect_now(ENetPeer peer, uint data) {

        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED) {
            return;
        }

        if (peer.state != ENetPeerState.ENET_PEER_STATE_ZOMBIE && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECTING) {
            enet_peer_reset_queues(peer);

            ENetProtocol command = new ENetProtocol();
            command.header.command   = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
            command.header.channelID = 0xFF;
            command.disconnect.data  = ENET_HOST_TO_NET_32(data);

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
        ) {
            return;
        }

        enet_peer_reset_queues(peer);

        ENetProtocol command = new ENetProtocol();
        command.header.command   = (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_DISCONNECT;
        command.header.channelID = 0xFF;
        command.disconnect.data  = ENET_HOST_TO_NET_32(data);

        if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            command.header.command |= (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        } else {
            command.header.command |= (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED;
        }

        enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);

        if (peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            enet_peer_on_disconnect(peer);

            peer.state = ENetPeerState.ENET_PEER_STATE_DISCONNECTING;
        } else {
            enet_host_flush(peer.host);
            enet_peer_reset(peer);
        }
    }

    public static bool enet_peer_has_outgoing_commands (ENetPeer  peer)
    {
        if (enet_list_empty (ref peer.outgoingCommands) &&
            enet_list_empty (ref peer.outgoingSendReliableCommands) &&
            enet_list_empty (ref peer.sentReliableCommands))
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
    public static void enet_peer_disconnect_later(ENetPeer peer, uint data) {
        if ((peer.state == ENetPeerState.ENET_PEER_STATE_CONNECTED || peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) &&
            enet_peer_has_outgoing_commands(peer)
        ) {
            peer.state     = ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER;
            peer.eventData = data;
        } else {
            enet_peer_disconnect(peer, data);
        }
    }

    public static ENetAcknowledgement enet_peer_queue_acknowledgement(ENetPeer peer, ref ENetProtocol command, ushort sentTime) {

        if (command.header.channelID < peer.channelCount) {
            ENetChannel channel       = peer.channels[command.header.channelID];
            ushort reliableWindow = (ushort)(command.header.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);
            ushort currentWindow  = (ushort)(channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);

            if (command.header.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
            }

            if (reliableWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1 && reliableWindow <= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS) {
                return null;
            }
        }

        ENetAcknowledgement acknowledgement = enet_malloc<ENetAcknowledgement>();
        if (acknowledgement == null) {
            return null;
        }

        peer.outgoingDataTotal += (uint)Marshal.SizeOf<ENetProtocolAcknowledge>();

        acknowledgement.sentTime = sentTime;
        acknowledgement.command  = command;

        enet_list_insert(enet_list_end(ref peer.acknowledgements), acknowledgement);
        return acknowledgement;
    }

    public static void enet_peer_setup_outgoing_command(ENetPeer peer, ENetOutgoingCommand outgoingCommand) {
        ENetChannel channel = peer.channels[outgoingCommand.command.header.channelID];
        peer.outgoingDataTotal += enet_protocol_command_size(outgoingCommand.command.header.command) + outgoingCommand.fragmentLength;

        if (outgoingCommand.command.header.channelID == 0xFF) {
            ++peer.outgoingReliableSequenceNumber;

            outgoingCommand.reliableSequenceNumber   = peer.outgoingReliableSequenceNumber;
            outgoingCommand.unreliableSequenceNumber = 0;
        }
        else if (0 != (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE)) {
            ++channel.outgoingReliableSequenceNumber;
            channel.outgoingUnreliableSequenceNumber = 0;

            outgoingCommand.reliableSequenceNumber   = channel.outgoingReliableSequenceNumber;
            outgoingCommand.unreliableSequenceNumber = 0;
        }
        else if (0 != (outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_UNSEQUENCED)) {
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

        switch ((ENetProtocolCommand)(outgoingCommand.command.header.command & (byte)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK)) {
            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
                outgoingCommand.command.sendUnreliable.unreliableSequenceNumber = ENET_HOST_TO_NET_16(outgoingCommand.unreliableSequenceNumber);
                break;

            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                outgoingCommand.command.sendUnsequenced.unsequencedGroup = ENET_HOST_TO_NET_16(peer.outgoingUnsequencedGroup);
                break;

            default:
                break;
        }

        if ((outgoingCommand.command.header.command & (byte)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE) != 0 && outgoingCommand.packet != null) {
            enet_list_insert(enet_list_end(ref peer.outgoingSendReliableCommands), outgoingCommand);
        } else {
            enet_list_insert(enet_list_end(ref peer.outgoingCommands), outgoingCommand);
        }
    }

    public static ENetOutgoingCommand enet_peer_queue_outgoing_command(ENetPeer peer, ref ENetProtocol command, ENetPacket packet, uint offset, ushort length) {
        ENetOutgoingCommand outgoingCommand = enet_malloc<ENetOutgoingCommand>();

        if (outgoingCommand == null) {
            return null;
        }

        outgoingCommand.command        = command;
        outgoingCommand.fragmentOffset = offset;
        outgoingCommand.fragmentLength = length;
        outgoingCommand.packet         = packet;
        if (packet != null) {
            ++packet.referenceCount;
        }

        enet_peer_setup_outgoing_command(peer, outgoingCommand);
        return outgoingCommand;
    }

    public static void enet_peer_dispatch_incoming_unreliable_commands(ENetPeer peer, ENetChannel channel, ENetIncomingCommand queuedCommand) {
        ENetListNode<ENetIncomingCommand> droppedCommand, startCommand, currentCommand = null;

        for (droppedCommand = startCommand = currentCommand = enet_list_begin(ref channel.incomingUnreliableCommands);
            currentCommand != enet_list_end(ref channel.incomingUnreliableCommands);
            currentCommand = enet_list_next(currentCommand)
        ) {
            ENetIncomingCommand incomingCommand =  currentCommand.value;

            if ((incomingCommand.command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
                continue;
            }

            if (incomingCommand.reliableSequenceNumber == channel.incomingReliableSequenceNumber) {
                if (incomingCommand.fragmentsRemaining <= 0) {
                    channel.incomingUnreliableSequenceNumber = incomingCommand.unreliableSequenceNumber;
                    continue;
                }

                if (startCommand != currentCommand) {
                    enet_list_move(enet_list_end(ref peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

                    if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                        enet_list_insert(enet_list_end(ref peer.host.dispatchQueue), &peer.dispatchList);
                        peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                    }

                    droppedCommand = currentCommand;
                } else if (droppedCommand != currentCommand) {
                    droppedCommand = enet_list_previous(currentCommand);
                }
            } else {
                ushort reliableWindow = (ushort)(incomingCommand.reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);
                ushort currentWindow  = (ushort)(channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE);

                if (incomingCommand.reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                    reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
                }

                if (reliableWindow >= currentWindow && reliableWindow < currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
                    break;
                }

                droppedCommand = enet_list_next(currentCommand);

                if (startCommand != currentCommand) {
                    enet_list_move(enet_list_end(ref peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

                    if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                        enet_list_insert(enet_list_end(ref peer.host.dispatchQueue), peer.dispatchList);
                        peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
                    }
                }
            }

            startCommand = enet_list_next(currentCommand);
        }

        if (startCommand != currentCommand) {
            enet_list_move(enet_list_end(ref peer.dispatchedCommands), startCommand, enet_list_previous(currentCommand));

            if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH)) {
                enet_list_insert(enet_list_end(ref peer.host.dispatchQueue), peer.dispatchList);
                peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
            }

            droppedCommand = currentCommand;
        }

        enet_peer_remove_incoming_commands(peer, &channel.incomingUnreliableCommands, enet_list_begin(ref channel.incomingUnreliableCommands), droppedCommand, queuedCommand);
    }

    public static void enet_peer_dispatch_incoming_reliable_commands(ENetPeer peer, ENetChannel channel, ENetIncomingCommand queuedCommand) {
        ENetListNode<ENetIncomingCommand> currentCommand;

        for (currentCommand = enet_list_begin(ref channel.incomingReliableCommands);
            currentCommand != enet_list_end(ref channel.incomingReliableCommands);
            currentCommand = enet_list_next(currentCommand)
        ) {
            ENetIncomingCommand incomingCommand = currentCommand.value;

            if (incomingCommand.fragmentsRemaining > 0 || incomingCommand.reliableSequenceNumber != (ushort) (channel.incomingReliableSequenceNumber + 1)) {
                break;
            }

            channel.incomingReliableSequenceNumber = incomingCommand.reliableSequenceNumber;

            if (incomingCommand.fragmentCount > 0)
            {
                channel.incomingReliableSequenceNumber += (ushort)(incomingCommand.fragmentCount - 1);
            }
        }

        if (currentCommand == enet_list_begin(ref channel.incomingReliableCommands)) {
            return;
        }

        channel.incomingUnreliableSequenceNumber = 0;
        enet_list_move(enet_list_end(ref peer.dispatchedCommands), enet_list_begin(ref channel.incomingReliableCommands), enet_list_previous(currentCommand));

        if (0 == (peer.flags & (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH)) {
            enet_list_insert(enet_list_end(ref peer.host.dispatchQueue), &peer.dispatchList);
            peer.flags |= (ushort)ENetPeerFlag.ENET_PEER_FLAG_NEEDS_DISPATCH;
        }

        if (!enet_list_empty(ref channel.incomingUnreliableCommands)) {
            enet_peer_dispatch_incoming_unreliable_commands(peer, channel, queuedCommand);
        }
    }
    
    private static ENetIncomingCommand dummyCommand;

    public static ENetIncomingCommand enet_peer_queue_incoming_command(ENetPeer peer, ref ENetProtocol command, byte[] data, ulong dataLength, uint flags, uint fragmentCount) {

        ENetChannel channel = peer.channels[command.header.channelID];
        uint unreliableSequenceNumber = 0, reliableSequenceNumber = 0;
        ushort reliableWindow, currentWindow;
        ENetIncomingCommand incomingCommand = null;
        ENetListNode<ENetIncomingCommand> currentCommand;
        ENetPacket packet = null;

        if (peer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
            goto discardCommand;
        }

        if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) != ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
            reliableSequenceNumber = command.header.reliableSequenceNumber;
            reliableWindow         = reliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;
            currentWindow = channel.incomingReliableSequenceNumber / ENets.ENET_PEER_RELIABLE_WINDOW_SIZE;

            if (reliableSequenceNumber < channel.incomingReliableSequenceNumber) {
                reliableWindow += ENets.ENET_PEER_RELIABLE_WINDOWS;
            }

            if (reliableWindow < currentWindow || reliableWindow >= currentWindow + ENets.ENET_PEER_FREE_RELIABLE_WINDOWS - 1) {
                goto discardCommand;
            }
        }

        switch (command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) {
            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_FRAGMENT:
            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_RELIABLE:
                if (reliableSequenceNumber == channel.incomingReliableSequenceNumber) {
                    goto discardCommand;
                }

                for (currentCommand = enet_list_previous(enet_list_end(ref channel.incomingReliableCommands));
                    currentCommand != enet_list_end(ref channel.incomingReliableCommands);
                    currentCommand = enet_list_previous(currentCommand)
                )
                {
                    incomingCommand = currentCommand.value;

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

            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE:
            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNRELIABLE_FRAGMENT:
                unreliableSequenceNumber = ENET_NET_TO_HOST_16(command.sendUnreliable.unreliableSequenceNumber);

                if (reliableSequenceNumber == channel.incomingReliableSequenceNumber && unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber) {
                    goto discardCommand;
                }

                for (currentCommand = enet_list_previous(enet_list_end(ref channel.incomingUnreliableCommands));
                    currentCommand != enet_list_end(ref channel.incomingUnreliableCommands);
                    currentCommand = enet_list_previous(currentCommand)
                )
                {
                    incomingCommand = currentCommand.value;

                    if ((command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) == ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED) {
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

            case ENetProtocolCommand.ENET_PROTOCOL_COMMAND_SEND_UNSEQUENCED:
                currentCommand = enet_list_end(ref channel.incomingUnreliableCommands);
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

        incomingCommand = enet_malloc<ENetIncomingCommand>();
        if (incomingCommand == null) {
            goto notifyError;
        }

        incomingCommand.reliableSequenceNumber     = command.header.reliableSequenceNumber;
        incomingCommand.unreliableSequenceNumber = (ushort)(unreliableSequenceNumber & 0xFFFF);
        incomingCommand.command                    = command;
        incomingCommand.fragmentCount              = fragmentCount;
        incomingCommand.fragmentsRemaining         = fragmentCount;
        incomingCommand.packet                     = packet;
        incomingCommand.fragments                  = null;

        if (fragmentCount > 0) 
        {
            if (fragmentCount <= ENets.ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT)
            {
                incomingCommand.fragments = enet_malloc<uint>((fragmentCount + 31) / 32);
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

        switch (command.header.command & ENetProtocolCommand.ENET_PROTOCOL_COMMAND_MASK) {
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
        if (fragmentCount > 0) {
            goto notifyError;
        }

        if (packet != null  && packet.referenceCount == 0) {
            callbacks.packet_destroy(packet);
        }

        return dummyCommand;

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
    public static ENetHost enet_host_create(ref ENetAddress address, ulong peerCount, ulong channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
    {
        ENetHost host = null;
        ENetPeer currentPeer = null;

        if (peerCount > ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID) {
            return null;
        }

        host = enet_malloc<ENetHost>();
        if (host == null)
        {
            return null;
        }
        // todo : @ikpil check
        //memset(host, 0, sizeof(ENetHost));

        host.peers = enet_malloc<ENetPeer>(peerCount);
        if (host.peers == null) {
            enet_free(host);
            return null;
        }

        // todo : @ikpil check
        //memset(host.peers, 0, peerCount * sizeof(ENetPeer));

        host.socket = enet_socket_create(ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM);
        if (host.socket != null) {
            enet_socket_set_option (host.socket, ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY, 0);
        }

        if (host.socket == null || (address != null && enet_socket_bind(host.socket, address) < 0)) {
            if (host.socket != null) {
                enet_socket_destroy(host.socket);
            }

            enet_free(host.peers);
            enet_free(host);

            return null;
        }

        enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_NONBLOCK, 1);
        enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_BROADCAST, 1);
        enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_RCVBUF, ENets.ENET_HOST_RECEIVE_BUFFER_SIZE);
        enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_SNDBUF, ENets.ENET_HOST_SEND_BUFFER_SIZE);
        enet_socket_set_option(host.socket, ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY, 0);

        if (address != null && enet_socket_get_address(host.socket, ref host.address) < 0) {
            host.address = *address;
        }

        if (!channelLimit || channelLimit > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            channelLimit = ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
        }

        host.randomSeed                    = (uint) ((uintptr_t) host % UINT32_MAX);
        host.randomSeed                    += enet_host_random_seed();
        host.randomSeed                    = (host.randomSeed << 16) | (host.randomSeed >> 16);
        host.channelLimit                  = channelLimit;
        host.incomingBandwidth             = incomingBandwidth;
        host.outgoingBandwidth             = outgoingBandwidth;
        host.bandwidthThrottleEpoch        = 0;
        host.recalculateBandwidthLimits    = 0;
        host.mtu                           = ENets.ENET_HOST_DEFAULT_MTU;
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
        host.duplicatePeers                = ENets.ENET_PROTOCOL_MAXIMUM_PEER_ID;
        host.maximumPacketSize             = ENets.ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE;
        host.maximumWaitingData            = ENets.ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA;
        host.compressor.context            = null;
        host.compressor.compress           = null;
        host.compressor.decompress         = null;
        host.compressor.destroy            = null;
        host.intercept                     = null;

        enet_list_clear(ref host.dispatchQueue);

        for (currentPeer = host.peers; currentPeer < &host.peers[host.peerCount]; ++currentPeer) {
            currentPeer.host = host;
            currentPeer.incomingPeerID    = currentPeer - host.peers;
            currentPeer.outgoingSessionID = currentPeer.incomingSessionID = 0xFF;
            currentPeer.data = null;

            enet_list_clear(ref currentPeer.acknowledgements);
            enet_list_clear(ref currentPeer.sentReliableCommands);
            enet_list_clear(ref currentPeer.outgoingCommands);
            enet_list_clear(ref currentPeer.outgoingSendReliableCommands);
            enet_list_clear(ref currentPeer.dispatchedCommands);

            enet_peer_reset(currentPeer);
        }

        return host;
    } /* enet_host_create */

    /** Destroys the host and all resources associated with it.
     *  @param host pointer to the host to destroy
     */
    public static void enet_host_destroy(ENetHost host) {

        if (host == null) {
            return;
        }

        enet_socket_destroy(host.socket);

        for (int currentPeer = 0; currentPeer < host.peerCount; ++currentPeer) {
            ENetPeer curPeer = host.peers[currentPeer];
            enet_peer_reset(curPeer);
        }

        if (host.compressor.context != null && host.compressor.CompressorDestroy) {
            host.compressor.CompressorDestroy(host.compressor.context);
        }

        enet_free(host.peers);
        enet_free(host);
    }

    public static uint enet_host_random(ENetHost host) {
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
    public static ENetPeer enet_host_connect(ENetHost host, ref ENetAddress address, int channelCount, uint data) {
        ENetPeer currentPeer = null;
        ENetChannel channel = null;

        if (channelCount < ENets.ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT) {
            channelCount = ENets.ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;
        } else if (channelCount > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            channelCount = ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
        }

        int currentPeerIdx = 0;
        for (currentPeerIdx = 0; currentPeerIdx < host.peerCount; ++currentPeerIdx)
        {
            currentPeer = host.peers[currentPeerIdx];
            if (currentPeer.state == ENetPeerState.ENET_PEER_STATE_DISCONNECTED) {
                break;
            }
        }

        if (currentPeerIdx >= host.peerCount) {
            return null;
        }

        currentPeer.channels = enet_malloc<ENetChannel>(channelCount);
        if (currentPeer.channels == null) {
            return null;
        }

        currentPeer.channelCount = channelCount;
        currentPeer.state        = ENetPeerState.ENET_PEER_STATE_CONNECTING;
        currentPeer.address      = address;
        currentPeer.connectID    = enet_host_random(host);
        currentPeer.mtu          = host.mtu;

        if (host.outgoingBandwidth == 0) {
            currentPeer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        } else {
            currentPeer.windowSize = (host.outgoingBandwidth / ENets.ENET_PEER_WINDOW_SIZE_SCALE) * ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        }

        if (currentPeer.windowSize < ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE) {
            currentPeer.windowSize = ENets.ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
        } else if (currentPeer.windowSize > ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE) {
            currentPeer.windowSize = ENets.ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
        }

        int channelIdx = 0;
        for (channelIdx = 0; channelIdx < channelCount; ++channelIdx)
        {
            channel = currentPeer.channels[channelIdx];
            channel.outgoingReliableSequenceNumber   = 0;
            channel.outgoingUnreliableSequenceNumber = 0;
            channel.incomingReliableSequenceNumber   = 0;
            channel.incomingUnreliableSequenceNumber = 0;

            enet_list_clear(ref channel.incomingReliableCommands);
            enet_list_clear(ref channel.incomingUnreliableCommands);

            channel.usedReliableWindows = 0;
            memset(channel.reliableWindows, 0, sizeof(channel.reliableWindows));
        }

        ENetProtocol command = new ENetProtocol();
        command.header.command                     = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_CONNECT | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
        command.header.channelID                   = 0xFF;
        command.connect.outgoingPeerID             = ENET_HOST_TO_NET_16(currentPeer.incomingPeerID);
        command.connect.incomingSessionID          = currentPeer.incomingSessionID;
        command.connect.outgoingSessionID          = currentPeer.outgoingSessionID;
        command.connect.mtu                        = ENET_HOST_TO_NET_32(currentPeer.mtu);
        command.connect.windowSize                 = ENET_HOST_TO_NET_32(currentPeer.windowSize);
        command.connect.channelCount               = ENET_HOST_TO_NET_32((uint)channelCount);
        command.connect.incomingBandwidth          = ENET_HOST_TO_NET_32(host.incomingBandwidth);
        command.connect.outgoingBandwidth          = ENET_HOST_TO_NET_32(host.outgoingBandwidth);
        command.connect.packetThrottleInterval     = ENET_HOST_TO_NET_32(currentPeer.packetThrottleInterval);
        command.connect.packetThrottleAcceleration = ENET_HOST_TO_NET_32(currentPeer.packetThrottleAcceleration);
        command.connect.packetThrottleDeceleration = ENET_HOST_TO_NET_32(currentPeer.packetThrottleDeceleration);
        command.connect.connectID                  = currentPeer.connectID;
        command.connect.data                       = ENET_HOST_TO_NET_32(data);

        enet_peer_queue_outgoing_command(currentPeer, ref command, null, 0, 0);

        return currentPeer;
    } /* enet_host_connect */

    /** Queues a packet to be sent to all peers associated with the host.
     *  @param host host on which to broadcast the packet
     *  @param channelID channel on which to broadcast
     *  @param packet packet to broadcast
     */
    public static void enet_host_broadcast(ENetHost host, byte channelID, ENetPacket packet) {
        ENetPeer currentPeer = null;

        for (int currentPeerIdx = 0; currentPeerIdx < host.peerCount; ++currentPeerIdx)
        {
            currentPeer = host.peers[currentPeerIdx];
            if (currentPeer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED) {
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
    public static int enet_host_send_raw(ENetHost host, ref ENetAddress address, byte[] data, ulong dataLength) {
        ENetBuffer buffer = new ENetBuffer();
        buffer.data = data;
        buffer.dataLength = dataLength;
        return enet_socket_send(host.socket, ref address, ref buffer, 1);
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
    public static int enet_host_send_raw_ex(ENetHost host, ref ENetAddress address, ArraySegment<byte> data, int skipBytes, ulong bytesToSend) {
        ENetBuffer buffer;
        buffer.data = data.Slice(skipBytes);
        buffer.dataLength = bytesToSend;
        return enet_socket_send(host.socket, ref address, ref buffer, 1);
    }

    /** Sets intercept callback for the host.
     *  @param host host to set a callback
     *  @param callback intercept callback
     */
    public static void enet_host_set_intercept(ENetHost host, ENetInterceptCallback callback) {
        host.intercept = callback;
    }

    /** Sets the packet compressor the host should use to compress and decompress packets.
     *  @param host host to enable or disable compression for
     *  @param compressor callbacks for for the packet compressor; if null, then compression is disabled
     */
    public static void enet_host_compress(ENetHost host, ENetCompressor compressor) {
        if (host.compressor.context != null && null != host.compressor.destroy) {
            host.compressor.destroy(host.compressor.context);
        }

        if (null != compressor) {
            host.compressor = compressor;
        } else {
            host.compressor.context = null;
        }
    }

    /** Limits the maximum allowed channels of future incoming connections.
     *  @param host host to limit
     *  @param channelLimit the maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT
     */
    public static void enet_host_channel_limit(ENetHost host, ulong channelLimit) {
        if (0 >= channelLimit || channelLimit > ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT) {
            channelLimit = ENets.ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
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
    public static void enet_host_bandwidth_limit(ENetHost host, uint incomingBandwidth, uint outgoingBandwidth) {
        host.incomingBandwidth = incomingBandwidth;
        host.outgoingBandwidth = outgoingBandwidth;
        host.recalculateBandwidthLimits = 1;
    }

    public static void enet_host_bandwidth_throttle(ENetHost host) {
        uint timeCurrent       = enet_time_get();
        uint elapsedTime       = timeCurrent - host.bandwidthThrottleEpoch;
        uint peersRemaining    = (uint) host.connectedPeers;
        uint dataTotal         = uint.MaxValue;
        uint bandwidth         = uint.MaxValue;
        uint throttle          = 0;
        uint bandwidthLimit    = 0;

        int needsAdjustment = host.bandwidthLimitedPeers > 0 ? 1 : 0;
        ENetPeer peer;

        if (elapsedTime < ENets.ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL) {
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

            for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
            {
                peer = host.peers[peerIdx];
                if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
                    continue;
                }

                dataTotal += peer.outgoingDataTotal;
            }
        }

        while (peersRemaining > 0 && needsAdjustment != 0) {
            needsAdjustment = 0;

            if (dataTotal <= bandwidth) {
                throttle = ENets.ENET_PEER_PACKET_THROTTLE_SCALE;
            } else {
                throttle = (bandwidth * ENets.ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
            }

            for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
            {
                peer = host.peers[peerIdx];
                uint peerBandwidth;

                if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) ||
                    peer.incomingBandwidth == 0 ||
                    peer.outgoingBandwidthThrottleEpoch == timeCurrent
                ) {
                    continue;
                }

                peerBandwidth = (peer.incomingBandwidth * elapsedTime) / 1000;
                if ((throttle * peer.outgoingDataTotal) / ENets.ENET_PEER_PACKET_THROTTLE_SCALE <= peerBandwidth) {
                    continue;
                }

                peer.packetThrottleLimit = (peerBandwidth * ENets.ENET_PEER_PACKET_THROTTLE_SCALE) / peer.outgoingDataTotal;

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
                throttle = ENets.ENET_PEER_PACKET_THROTTLE_SCALE;
            } else {
                throttle = (bandwidth * ENets.ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;
            }

            for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
            {
                peer = host.peers[peerIdx];
                if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) || peer.outgoingBandwidthThrottleEpoch == timeCurrent) {
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

        if (0 != host.recalculateBandwidthLimits) {
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

                    for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
                    {
                        peer = host.peers[peerIdx];
                        if ((peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) ||
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

            for (int peerIdx = 0; peerIdx < host.peerCount; ++peerIdx)
            {
                peer = host.peers[peerIdx];
                if (peer.state != ENetPeerState.ENET_PEER_STATE_CONNECTED && peer.state != ENetPeerState.ENET_PEER_STATE_DISCONNECT_LATER) {
                    continue;
                }

                ENetProtocol command = new ENetProtocol();
                command.header.command   = (int)ENetProtocolCommand.ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT | (int)ENetProtocolFlag.ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
                command.header.channelID = 0xFF;
                command.bandwidthLimit.outgoingBandwidth = ENET_HOST_TO_NET_32(host.outgoingBandwidth);

                if (peer.incomingBandwidthThrottleEpoch == timeCurrent) {
                    command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32(peer.outgoingBandwidth);
                } else {
                    command.bandwidthLimit.incomingBandwidth = ENET_HOST_TO_NET_32(bandwidthLimit);
                }

                enet_peer_queue_outgoing_command(peer, ref command, null, 0, 0);
            }
        }
    } /* enet_host_bandwidth_throttle */

    // =======================================================================//
    // !
    // ! Time
    // !
    // =======================================================================//
    public static uint enet_time_get() 
    {
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
                    out.sin6_scope_id = (ushort) ((struct sockaddr_in6*)result.ai_addr).sin6_scope_id;
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


    public static ulong enet_host_random_seed() {
        return (ulong) time(null);
    }


// =======================================================================//
// !
// ! Platform Specific (Win)
// !
// =======================================================================//

    public static int enet_initialize() {
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

    public static void enet_deinitialize() {
        timeEndPeriod(1);
        WSACleanup();
    }

    ulong enet_host_random_seed(void) {
        return (ulong) timeGetTime();
    }

    public static int enet_address_set_host_ip_old(ENetAddress *address, const char *name) {
        byte vals[4] = { 0, 0, 0, 0 };
        int i;

        for (i = 0; i < 4; ++i) {
            const char *next = name + 1;
            if (*name != '0') {
                long val = strtol(name, (char **) &next, 10);
                if (val < 0 || val > 255 || next == name || next - name > 3) {
                    return -1;
                }
                vals[i] = (byte) val;
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

    public static int enet_socket_bind(Socket socket, const ENetAddress *address) {
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

    public static int enet_socket_get_address(Socket socket, ref ENetAddress address) {
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

    public static int enet_socket_listen(Socket socket, int backlog) {
        return listen(socket, backlog < 0 ? SOMAXCONN : backlog) == SOCKET_ERROR ? -1 : 0;
    }

    public static Socket enet_socket_create(ENetSocketType type) {
        return socket(PF_INET6, type == ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM ? SOCK_DGRAM : SOCK_STREAM, 0);
    }

    public static int enet_socket_set_option(Socket socket, ENetSocketOption option, int value) {
        int result = SOCKET_ERROR;

        switch (option) {
            case ENetSocketOption.ENET_SOCKOPT_NONBLOCK: {
                u_long nonBlocking = (u_long) value;
                result = ioctlsocket(socket, FIONBIO, &nonBlocking);
                break;
            }

            case ENetSocketOption.ENET_SOCKOPT_BROADCAST:
                result = setsockopt(socket, SOL_SOCKET, SO_BROADCAST, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_REUSEADDR:
                result = setsockopt(socket, SOL_SOCKET, SO_REUSEADDR, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_RCVBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVBUF, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_SNDBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDBUF, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_RCVTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVTIMEO, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_SNDTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDTIMEO, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_NODELAY:
                result = setsockopt(socket, IPPROTO_TCP, TCP_NODELAY, (char *)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY:
                result = setsockopt(socket, IPPROTO_IPV6, IPV6_V6ONLY, (char *)&value, sizeof(int));
                break;
            
            case ENetSocketOption.ENET_SOCKOPT_TTL:
                result = setsockopt(socket, IPPROTO_IP, IP_TTL, (char *)&value, sizeof(int));
                break;

            default:
                break;
        }
        return result == SOCKET_ERROR ? -1 : 0;
    } /* enet_socket_set_option */

    public static int enet_socket_get_option(Socket socket, ENetSocketOption option, int *value) {
        int result = SOCKET_ERROR, len;

        switch (option) {
            case ENetSocketOption.ENET_SOCKOPT_ERROR:
                len    = sizeof(int);
                result = getsockopt(socket, SOL_SOCKET, SO_ERROR, (char *)value, &len);
                break;

            case ENetSocketOption.ENET_SOCKOPT_TTL:
                len = sizeof(int);
                result = getsockopt(socket, IPPROTO_IP, IP_TTL, (char *)value, &len);
                break;

            default:
                break;
        }
        return result == SOCKET_ERROR ? -1 : 0;
    }

    public static int enet_socket_connect(Socket socket, const ENetAddress *address) {
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

    public static Socket enet_socket_accept(Socket socket, ENetAddress *address) {
        SOCKET result;
        struct sockaddr_in6 sin = {0};
        int sinLength = sizeof(struct sockaddr_in6);

        result = accept(socket, address != null ? (struct sockaddr *)&sin : null, address != null ? &sinLength : null);

        if (result == INVALID_SOCKET) {
            return null;
        }

        if (address != null) {
            address.host           = sin.sin6_addr;
            address.port           = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id  = sin.sin6_scope_id;
        }

        return result;
    }

    public static int enet_socket_shutdown(Socket socket, ENetSocketShutdown how) {
        return shutdown(socket, (int) how) == SOCKET_ERROR ? -1 : 0;
    }

    public static void enet_socket_destroy(Socket socket) {
        if (socket != INVALID_SOCKET) {
            closesocket(socket);
        }
    }

    public static int enet_socket_send(Socket socket, ref ENetAddress address, ref ENetBuffer buffers, ulong bufferCount) {
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

    public static int enet_socket_receive(Socket socket, ENetAddress *address, ENetBuffer *buffers, ulong bufferCount) {
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

    public static int enet_socketset_select(Socket maxSocket, ENetSocketSet *readSet, ENetSocketSet *writeSet, uint timeout) {
        struct timeval timeVal;

        timeVal.tv_sec  = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        return select(maxSocket + 1, readSet, writeSet, null, &timeVal);
    }

    public static int enet_socket_wait(Socket socket, uint *condition, ulong timeout) {
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