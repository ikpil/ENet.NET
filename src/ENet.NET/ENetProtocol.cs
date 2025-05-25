using System;
using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ENetProtocol
    {
        [FieldOffset(0)]
        private ENetFixedArray48<byte> _internal;

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

        public void CopyFrom(ArraySegment<byte> source)
        {
            var sourceSpan = source.AsSpan().Slice(0, _internal.Length);
            sourceSpan.CopyTo(_internal.AsSpan());
        }

        public void CopyTo(Span<byte> bytes, int size)
        {
            _internal.AsSpan().Slice(0, size).CopyTo(bytes);
        }

        public byte[] ToArray(int size)
        {
            var dummy = new byte[size];
            CopyTo(dummy, size);
            return dummy;
        }
    }
}