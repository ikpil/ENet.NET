namespace ENet.NET;

public static class ENetTimes
{
    // =======================================================================//
    // !
    // ! Time
    // !
    // =======================================================================//
    public static uint enet_time_get()
    {
        // TODO enet uses 32 bit timestamps. We should modify it to use
        // 64 bit timestamps, but this is not trivial since we'd end up
        // changing half the structs in enet. For now, retain 32 bits, but
        // use an offset so we don't run out of bits. Basically, the first
        // call of enet_time_get() will always return 1, and follow-up calls
        // indicate elapsed time since the first call.
        //
        // Note that we don't want to return 0 from the first call, in case
        // some part of enet uses 0 as a special value (meaning time not set
        // for example).
        static uint64_t start_time_ns = 0;

        struct timespec ts;
#if defined(CLOCK_MONOTONIC_RAW)
        clock_gettime(CLOCK_MONOTONIC_RAW, &ts);
#else
        clock_gettime(CLOCK_MONOTONIC, &ts);
#endif

        static
        const uint64_t ns_in_s = 1000 * 1000 * 1000;
        static
        const uint64_t ns_in_ms = 1000 * 1000;
        uint64_t current_time_ns = ts.tv_nsec + (uint64_t)ts.tv_sec * ns_in_s;

        // Most of the time we just want to atomically read the start time. We
        // could just use a single CAS instruction instead of this if, but it
        // would be slower in the average case.
        //
        // Note that statics are auto-initialized to zero, and starting a thread
        // implies a memory barrier. So we know that whatever thread calls this,
        // it correctly sees the start_time_ns as 0 initially.
        uint64_t offset_ns = ENET_ATOMIC_READ(&start_time_ns);
        if (offset_ns == 0)
        {
            // We still need to CAS, since two different threads can get here
            // at the same time.
            //
            // We assume that current_time_ns is > 1ms.
            //
            // Set the value of the start_time_ns, such that the first timestamp
            // is at 1ms. This ensures 0 remains a special value.
            uint64_t want_value = current_time_ns - 1 * ns_in_ms;
#if defined(__GNUC__) // Ignore warning.
            #pragma GCC diagnostic push
            #pragma GCC diagnostic ignored "-Wpedantic"
#endif
            uint64_t old_value = ENET_ATOMIC_CAS(&start_time_ns, 0, want_value);
#if defined(__GNUC__)
            #pragma GCC diagnostic pop
#endif
            offset_ns = old_value == 0 ? want_value : old_value;
        }

        uint64_t result_in_ns = current_time_ns - offset_ns;
        return (uint)(result_in_ns / ns_in_ms);
    }
}