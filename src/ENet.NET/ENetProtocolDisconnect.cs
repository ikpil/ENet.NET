﻿using System.Runtime.InteropServices;

namespace ENet.NET
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolDisconnect
    {
        public ENetProtocolCommandHeader header;
        public uint data;
    }
}