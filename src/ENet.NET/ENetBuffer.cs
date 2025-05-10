using System;

namespace ENet.NET
{
    public struct ENetBuffer
    {
        public ArraySegment<byte> data;
        public int dataLength;
    }
}