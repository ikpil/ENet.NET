using System.Runtime.InteropServices;

namespace ENet.NET
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetProtocolAcknowledge
    {
        public ENetProtocolCommandHeader header;
        public ushort receivedReliableSequenceNumber;
        public ushort receivedSentTime;
    }
}