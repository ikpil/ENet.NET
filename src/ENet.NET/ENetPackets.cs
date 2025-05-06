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
        public static ENetPacket enet_packet_create(byte[] data, long dataLength, uint flags)
        {
            ENetPacket packet;
            if (0 != (flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet = enet_malloc_packet(0);
                if (packet == null)
                {
                    return null;
                }

                packet.data = data;
            }
            else
            {
                packet = enet_malloc_packet(dataLength);
                if (packet == null)
                {
                    return null;
                }

                // todo : @ikpil check
                //packet.data = (byte*)packet + Marshal.SizeOf<ENetPacket>();

                if (data != null)
                {
                    // todo : check
                    //memcpy(packet.data, data, dataLength);
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
        public static ENetPacket enet_packet_resize(ENetPacket packet, long dataLength)
        {
            if (dataLength <= packet.dataLength || 0 != (packet.flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet.dataLength = dataLength;

                return packet;
            }

            ENetPacket newPacket = enet_malloc_packet(dataLength);
            if (newPacket == null)
                return null;

            // todo : ikpil
            //memcpy(newPacket, packet, Marshal.SizeOf<ENetPacket>() + packet.dataLength);
            newPacket.CopyFrom(packet);
            //newPacket.data = (byte*)newPacket + Marshal.SizeOf<ENetPacket>();
            newPacket.dataLength = dataLength;
            enet_free(packet);

            return newPacket;
        }

        public static ENetPacket enet_packet_create_offset(byte[] data, long dataLength, long dataOffset, uint flags)
        {
            ENetPacket packet;
            if (0 != (flags & ENetPacketFlag.ENET_PACKET_FLAG_NO_ALLOCATE))
            {
                packet = enet_malloc_packet(0);
                if (packet == null)
                {
                    return null;
                }

                packet.data = data;
            }
            else
            {
                packet = enet_malloc_packet(dataLength + dataOffset);
                if (packet == null)
                {
                    return null;
                }

                // todo : check
                //packet.data = (byte*)packet + Marshal.SizeOf<ENetPacket>();

                if (data != null)
                {
                    // todo : check
                    //memcpy(packet.data + dataOffset, data, dataLength);
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