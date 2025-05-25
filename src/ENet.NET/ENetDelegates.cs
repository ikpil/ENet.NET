using System;

namespace ENet.NET
{
    public class ENetDelegates
    {
        public delegate void ENetPacketFreeCallback(object _);

        /** Callback that computes the checksum of the data held in buffers[0:bufferCount-1] */
        public delegate uint ENetChecksumCallback(Span<ENetBuffer> buffers, long bufferCount);

        /** Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error. */
        public delegate int ENetInterceptCallback(ENetHost host, object @event);

        public delegate T[] MallocDelegate<out T>(long size);

        public delegate void FreeDelegate<in T>(T[] size);

        public delegate void NoMemoryDelegate();

        public delegate ENetPacket PacketCreateDelegate(ArraySegment<byte> data, int dataLength, uint flags);

        public delegate void PacketDestroyDelegate(ENetPacket packet);

        /** Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
        public delegate int CompressorCompressDelegate(object context, ref ENetBuffer inBuffers, long inBufferCount, long inLimit, ArraySegment<byte> outData, long outLimit);

        /** Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
        public delegate int CompressorDecompressDelegate(object context, ArraySegment<byte> inData, long inLimit, ArraySegment<byte> outData, long outLimit);

        /** Destroys the context when compression is disabled or the host is destroyed. May be NULL. */
        public delegate void CompressorDestroyDelegate(object context);
    }
}