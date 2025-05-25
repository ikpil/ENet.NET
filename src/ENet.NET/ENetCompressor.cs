using static ENet.NET.ENetDelegates;

namespace ENet.NET
{
    /** An ENet packet compressor for compressing UDP packets before socket sends or receives. */
    public struct ENetCompressor
    {
        /** Context data for the compressor. Must be non-NULL. */
        public object context;

        public CompressorCompressDelegate compress;
        public CompressorDecompressDelegate decompress;
        public CompressorDestroyDelegate destroy;
    }
}