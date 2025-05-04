using System;
using static ENet.NET.ENetAddresses;

namespace ENet.NET
{
    public static class ENetCrc32
    {
        private static int initializedCRC32 = 0;
        private static uint[] crcTable = new uint[256];

        public static uint reflect_crc(int val, int bits)
        {
            int result = 0, bit;

            for (bit = 0; bit < bits; bit++)
            {
                if (0 != (val & 1))
                {
                    result |= 1 << (bits - 1 - bit);
                }

                val >>= 1;
            }

            return (uint)result;
        }

        private static void initialize_crc32()
        {
            int @byte;

            for (@byte = 0; @byte < 256; ++@byte)
            {
                uint crc = reflect_crc(@byte, 8) << 24;
                int offset;

                for (offset = 0; offset < 8; ++offset)
                {
                    if (0 != (crc & 0x80000000))
                    {
                        crc = (crc << 1) ^ 0x04c11db7;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }

                crcTable[@byte] = reflect_crc((int)crc, 32);
            }

            initializedCRC32 = 1;
        }

        public static uint enet_crc32(Span<ENetBuffer> buffers, long bufferCount)
        {
            uint crc = 0xFFFFFFFF;

            if (0 == initializedCRC32)
            {
                initialize_crc32();
            }

            int n = 0;

            while (bufferCount-- > 0)
            {
                ArraySegment<byte> data = buffers[n].data;

                int begin = 0;
                int end = (int)buffers[n].dataLength;

                while (begin < end)
                {
                    crc = (crc >> 8) ^ crcTable[(crc & 0xFF) ^ data[begin++]];
                }

                ++n;
            }

            return ENET_HOST_TO_NET_32(~crc);
        }
    }
}