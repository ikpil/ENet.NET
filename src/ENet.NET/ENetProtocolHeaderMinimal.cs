using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolHeaderMinimal
    {
        public ushort peerID;
    }
}