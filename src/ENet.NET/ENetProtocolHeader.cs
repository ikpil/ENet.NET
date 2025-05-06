using System;
using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolHeader
    {
        public ushort peerID;
        public ushort sentTime;

        public ENetProtocolHeader(ReadOnlySpan<byte> bytes)
        {
            peerID = BitConverter.ToUInt16(bytes);
            sentTime = BitConverter.ToUInt16(bytes.Slice(2));
        }
    }
}