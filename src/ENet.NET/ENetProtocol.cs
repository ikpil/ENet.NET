using System;
using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ENetProtocol
    {
        [FieldOffset(0)]
        public ENetFixedArray48<byte> dummyBytes;

        [FieldOffset(0)]
        public ENetProtocolCommandHeader header;

        [FieldOffset(0)]
        public ENetProtocolAcknowledge acknowledge;

        [FieldOffset(0)]
        public ENetProtocolConnect connect;

        [FieldOffset(0)]
        public ENetProtocolVerifyConnect verifyConnect;

        [FieldOffset(0)]
        public ENetProtocolDisconnect disconnect;

        [FieldOffset(0)]
        public ENetProtocolPing ping;

        [FieldOffset(0)]
        public ENetProtocolSendReliable sendReliable;

        [FieldOffset(0)]
        public ENetProtocolSendUnreliable sendUnreliable;

        [FieldOffset(0)]
        public ENetProtocolSendUnsequenced sendUnsequenced;

        [FieldOffset(0)]
        public ENetProtocolSendFragment sendFragment;

        [FieldOffset(0)]
        public ENetProtocolBandwidthLimit bandwidthLimit;

        [FieldOffset(0)]
        public ENetProtocolThrottleConfigure throttleConfigure;

        public void MergeForm(ArraySegment<byte> bytes)
        {
            bytes.AsSpan().Slice(0, dummyBytes.Length).CopyTo(dummyBytes.AsSpan());
        }
    }
}