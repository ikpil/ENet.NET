using System.Diagnostics;
using System.Threading;

namespace ENet.NET
{
    // =======================================================================//
    // !
    // ! Time
    // !
    // =======================================================================//
    public static class ENetTimes
    {
        private static long startTimeNs = 0;

        public const int CLOCK_MONOTONIC = 0;
        public const int ENET_TIME_OVERFLOW = 86400000;

        public static bool ENET_TIME_LESS(long a, long b)
        {
            return ((uint)(a) - (uint)(b) >= ENET_TIME_OVERFLOW);
        }

        public static bool ENET_TIME_GREATER(long a, long b)
        {
            return ((uint)(b) - (uint)(a) >= ENET_TIME_OVERFLOW);
        }

        public static bool ENET_TIME_LESS_EQUAL(long a, long b)
        {
            return (!ENET_TIME_GREATER(a, b));
        }

        public static bool ENET_TIME_GREATER_EQUAL(long a, long b)
        {
            return (!ENET_TIME_LESS(a, b));
        }

        public static long ENET_TIME_DIFFERENCE(long a, long b)
        {
            return ((uint)(a) - (uint)(b) >= ENET_TIME_OVERFLOW ? (uint)(b) - (uint)(a) : (uint)(a) - (uint)(b));
        }

        public static long enet_time_get()
        {
            const long nsInS = 1_000_000_000;
            const long nsInMs = 1_000_000;

            long timestamp = Stopwatch.GetTimestamp();
            long currentTimeNs = timestamp * nsInS / Stopwatch.Frequency;

            long offsetNs = Interlocked.Read(ref startTimeNs);
            if (offsetNs == 0)
            {
                long wantValue = currentTimeNs - nsInMs;
                long oldValue = Interlocked.CompareExchange(ref startTimeNs, wantValue, 0);
                offsetNs = oldValue == 0 ? wantValue : oldValue;
            }

            long resultInNs = currentTimeNs - offsetNs;
            return resultInNs / nsInMs;
        }
    }
}