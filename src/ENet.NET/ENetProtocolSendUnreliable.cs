﻿using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolSendUnreliable
    {
        public ENetProtocolCommandHeader header;
        public ushort unreliableSequenceNumber;
        public ushort dataLength;
    }
}