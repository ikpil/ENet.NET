namespace ENet.NET
{
    /**
     * Packet flag bit constants.
     *
     * The host must be specified in network byte-order, and the port must be in
     * host byte-order. The constant ENET_HOST_ANY may be used to specify the
     * default server host.
     *
     * @sa ENetPacket
     */
    public static class ENetPacketFlag
    {
        public const uint ENET_PACKET_FLAG_RELIABLE = (1 << 0); /* packet must be received by the target peer and resend attempts should be made until the packet is delivered */
        public const uint ENET_PACKET_FLAG_UNSEQUENCED = (1 << 1); /* packet will not be sequenced with other packets */
        public const uint ENET_PACKET_FLAG_NO_ALLOCATE = (1 << 2); /* packet will not allocate data, and user must supply it instead */
        public const uint ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT = (1 << 3); /* packet will be fragmented using unreliable (instead of reliable) sends if it exceeds the MTU */
        public const uint ENET_PACKET_FLAG_UNTHROTTLED = (1 << 4); /* packet that was enqueued for sending unreliably should not be dropped due to throttling and sent if possible */
        public const uint ENET_PACKET_FLAG_SENT = (1 << 8); /* whether the packet has been sent from all queues it has been entered into */
    }
}