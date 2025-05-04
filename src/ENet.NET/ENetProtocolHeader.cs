using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolHeader
    {
        public ushort peerID;
        public ushort sentTime;
    }
}