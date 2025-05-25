using System.Runtime.InteropServices;

namespace ENet.NET
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ENetProtocolSendOutgoingCommand
    {
        [FieldOffset(0)]
        public ENetFixedArray9<byte> bytes;
            
        [FieldOffset(0)]
        public ENetProtocolHeader header;
            
        [FieldOffset(4)]
        public byte extraPeerIDByte; // additional peer id byte
            
        [FieldOffset(5)]
        public uint b;

        public byte[] ToByteArray()
        {
            var dummy = new byte[9];
            bytes.AsSpan().CopyTo(dummy);
            return dummy;
        }
    }
}