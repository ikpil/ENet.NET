namespace ENet.NET
{
    /** Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
    public delegate long CompressorCompressDelegate(object context, ref ENetBuffer inBuffers, long inBufferCount, long inLimit, byte[] outData, long outLimit);

    /** Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure. */
    public delegate long CompressorDecompressDelegate(object context, byte[] inData, long inLimit, byte[] outData, long outLimit);

    /** Destroys the context when compression is disabled or the host is destroyed. May be NULL. */
    public delegate void CompressorDestroyDelegate(object context);

    /** An ENet packet compressor for compressing UDP packets before socket sends or receives. */
    public class ENetCompressor
    {
        /** Context data for the compressor. Must be non-NULL. */
        public object context;

        public CompressorCompressDelegate compress;
        public CompressorDecompressDelegate decompress;
        public CompressorDestroyDelegate destroy;
    }
}