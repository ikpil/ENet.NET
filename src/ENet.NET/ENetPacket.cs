using static ENet.NET.ENets;
using System.Diagnostics;

namespace ENet.NET
{
    /**
     * ENet packet structure.
     *
     * An ENet data packet that may be sent to or received from a peer. The shown
     * fields should only be read and never modified. The data field contains the
     * allocated data for the packet. The dataLength fields specifies the length
     * of the allocated data.  The flags field is either 0 (specifying no flags),
     * or a bitwise-or of any combination of the following flags:
     *
     *    ENET_PACKET_FLAG_RELIABLE - packet must be received by the target peer and resend attempts should be made until the packet is delivered
     *    ENET_PACKET_FLAG_UNSEQUENCED - packet will not be sequenced with other packets (not supported for reliable packets)
     *    ENET_PACKET_FLAG_NO_ALLOCATE - packet will not allocate data, and user must supply it instead
     *    ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT - packet will be fragmented using unreliable (instead of reliable) sends if it exceeds the MTU
     *    ENET_PACKET_FLAG_SENT - whether the packet has been sent from all queues it has been entered into
     * @sa ENetPacketFlag
     */
    public class ENetPacket
    {
        public long referenceCount; /* internal use only */
        public uint flags; /* bitwise-or of ENetPacketFlag constants */
        public byte[] data; /* allocated data for packet */
        public long dataLength; /* length of data */
        public ENetPacketFreeCallback freeCallback; /* function to be called when the packet is no longer in use */
        public object userData; /* application private data, may be freely modified */

        public void CopyFrom(ENetPacket packet)
        {
            // ..
            Check(false);
        }
    }
}