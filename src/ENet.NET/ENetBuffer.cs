using System;

namespace ENet.NET
{
    public struct ENetBuffer
    {
        public byte[] data;
        public int offset;
        public int dataLength;

        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(data, offset, dataLength);
        }
    }
}