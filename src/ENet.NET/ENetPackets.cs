using System;
using System.Runtime.InteropServices;
using static ENet.NET.ENets;

namespace ENet.NET
{
    public static class ENetPackets
    {
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
        public static ENetPacket enet_packet_create(ArraySegment<byte> data, int dataLength, uint flags)
        {
            ENetPacket packet = new ENetPacket();
            if (0 != (flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet.data = data;
            }
            else
            {
                packet = new ENetPacket();
                packet.data = enet_malloc_bytes(dataLength);
                if (data != null)
                {
                    Span<byte> src = data.AsSpan().Slice(0, dataLength);
                    Span<byte> dest = packet.data.AsSpan();
                    src.CopyTo(dest);
                }
            }

            packet.referenceCount = 0;
            packet.flags = flags;
            packet.dataLength = dataLength;
            packet.freeCallback = null;
            packet.userData = null;

            return packet;
        }

        /** Attempts to resize the data in the packet to length specified in the
            dataLength parameter
            @param packet packet to resize
            @param dataLength new size for the packet data
            @returns new packet pointer on success, null on failure
        */
        public static ENetPacket enet_packet_resize(ENetPacket packet, int dataLength)
        {
            if (dataLength <= packet.dataLength || 0 != (packet.flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet.dataLength = dataLength;

                return packet;
            }

            byte[] dest = enet_malloc_bytes(dataLength);
            Span<byte> src = packet.data.AsSpan(0, packet.dataLength);
            src.CopyTo(dest);

            ENetPacket newPacket = packet.Clone();
            newPacket.data = dest;
            newPacket.dataLength = dataLength;
            
            enet_free(packet);

            return newPacket;
        }

        public static ENetPacket enet_packet_create_offset(byte[] data, int dataLength, int dataOffset, uint flags)
        {
            ENetPacket packet = new ENetPacket();
            if (0 != (flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet.data = data;
            }
            else
            {
                packet.data = enet_malloc_bytes(dataOffset + dataLength);
                if (data != null)
                {
                    Span<byte> src = data.AsSpan(dataLength);
                    Span<byte> dest = packet.data.AsSpan(dataOffset);
                    src.CopyTo(dest);
                }
            }

            packet.referenceCount = 0;
            packet.flags = flags;
            packet.dataLength = dataLength + dataOffset;
            packet.freeCallback = null;
            packet.userData = null;

            return packet;
        }

        public static ENetPacket enet_packet_copy(ENetPacket packet)
        {
            return enet_packet_create(packet.data, packet.dataLength, packet.flags);
        }

        /**
         * Destroys the packet and deallocates its data.
         * @param packet packet to be destroyed
         */
        public static void enet_packet_destroy(ENetPacket packet)
        {
            if (packet == null)
            {
                return;
            }

            if (packet.freeCallback != null)
            {
                packet.freeCallback(packet);
            }

            enet_free(packet);
        }
    }
}