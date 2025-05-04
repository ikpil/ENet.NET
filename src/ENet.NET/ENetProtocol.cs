namespace ENet.NET
{

    public struct ENetProtocol
    {
        // @ikpil check union
        public ENetProtocolCommandHeader header;
        public ENetProtocolAcknowledge acknowledge;
        public ENetProtocolConnect connect;
        public ENetProtocolVerifyConnect verifyConnect;
        public ENetProtocolDisconnect disconnect;
        public ENetProtocolPing ping;
        public ENetProtocolSendReliable sendReliable;
        public ENetProtocolSendUnreliable sendUnreliable;
        public ENetProtocolSendUnsequenced sendUnsequenced;
        public ENetProtocolSendFragment sendFragment;
        public ENetProtocolBandwidthLimit bandwidthLimit;
        public ENetProtocolThrottleConfigure throttleConfigure;
    }
}