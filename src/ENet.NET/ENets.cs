namespace ENet.NET;

public delegate void ENetPacketFreeCallback(object _);

/** Callback that computes the checksum of the data held in buffers[0:bufferCount-1] */
public delegate uint ENetChecksumCallback(ref ENetBuffer buffers, ulong bufferCount);

/** Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error. */
public delegate int ENetInterceptCallback(ENetHost host, object @event);

public static class ENets
{
    public const int ENET_BUFFER_MAXIMUM = (1 + 2 * ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS);

    // protocols
    public const uint ENET_PROTOCOL_MINIMUM_MTU = 576;
    public const uint ENET_PROTOCOL_MAXIMUM_MTU = 4096;
    public const int ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS = 32;
    public const uint ENET_PROTOCOL_MINIMUM_WINDOW_SIZE = 4096;
    public const uint ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE = 65536;
    public const int ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT = 1;
    public const int ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT = 255;
    public const ushort ENET_PROTOCOL_MAXIMUM_PEER_ID = 0xFFFF;
    public const uint ENET_PROTOCOL_MAXIMUM_FRAGMENT_COUNT = 1024 * 1024;

    // peers
    public const int ENET_PEER_DEFAULT_ROUND_TRIP_TIME = 500;
    public const int ENET_PEER_DEFAULT_PACKET_THROTTLE = 32;
    public const int ENET_PEER_PACKET_THROTTLE_SCALE = 32;
    public const int ENET_PEER_PACKET_THROTTLE_COUNTER = 7;
    public const int ENET_PEER_PACKET_THROTTLE_ACCELERATION = 2;
    public const int ENET_PEER_PACKET_THROTTLE_DECELERATION = 2;
    public const int ENET_PEER_PACKET_THROTTLE_INTERVAL = 5000;
    public const int ENET_PEER_PACKET_LOSS_SCALE = (1 << 16);
    public const int ENET_PEER_PACKET_LOSS_INTERVAL = 10000;
    public const int ENET_PEER_WINDOW_SIZE_SCALE = 64 * 1024;
    public const int ENET_PEER_TIMEOUT_LIMIT = 32;
    public const int ENET_PEER_TIMEOUT_MINIMUM = 5000;
    public const int ENET_PEER_TIMEOUT_MAXIMUM = 30000;
    public const int ENET_PEER_PING_INTERVAL = 500;
    public const int ENET_PEER_UNSEQUENCED_WINDOWS = 64;
    public const int ENET_PEER_UNSEQUENCED_WINDOW_SIZE = 1024;
    public const int ENET_PEER_FREE_UNSEQUENCED_WINDOWS = 32;
    public const int ENET_PEER_RELIABLE_WINDOWS = 16;
    public const ushort ENET_PEER_RELIABLE_WINDOW_SIZE = 0x1000;
    public const int ENET_PEER_FREE_RELIABLE_WINDOWS = 8;

    // hosts
    public const int ENET_HOST_RECEIVE_BUFFER_SIZE = 256 * 1024;
    public const int ENET_HOST_SEND_BUFFER_SIZE = 256 * 1024;
    public const int ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL = 1000;
    public const int ENET_HOST_DEFAULT_MTU = 1392;
    public const int ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE = 32 * 1024 * 1024;
    public const int ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA = 32 * 1024 * 1024;

    public static void perror(string message)
    {
        Console.Error.WriteLine(message);
    }

    public const int CLOCK_MONOTONIC = 0;
    public const int ENET_TIME_OVERFLOW = 86400000;

    public static bool ENET_TIME_LESS(uint a, uint b)
    {
        return ((a) - (b) >= ENET_TIME_OVERFLOW);
    }

    public static bool ENET_TIME_GREATER(int a, int b)
    {
        return ((b) - (a) >= ENET_TIME_OVERFLOW);
    }

    public static bool ENET_TIME_LESS_EQUAL(int a, int b)
    {
        return (!ENET_TIME_GREATER(a, b));
    }

    public static bool ENET_TIME_GREATER_EQUAL(uint a, uint b)
    {
        return (!ENET_TIME_LESS(a, b));
    }

    public static uint ENET_TIME_DIFFERENCE(uint a, uint b)
    {
        return ((a) - (b) >= ENET_TIME_OVERFLOW ? (b) - (a) : (a) - (b));
    }

    public static ushort ENET_HOST_TO_NET_16(ushort value)
    {
        return (htons(value)); /* macro that converts host to net byte-order of a 16-bit value */
    }

    public static uint ENET_HOST_TO_NET_32(uint value)
    {
        return (htonl(value)); /* macro that converts host to net byte-order of a 32-bit value */
    }

    public static ushort ENET_NET_TO_HOST_16(int value)
    {
        return (ntohs(value)); /* macro that converts net to host byte-order of a 16-bit value */
    }

    public static uint ENET_NET_TO_HOST_32(uint value)
    {
        return (ntohl(value)); /* macro that converts net to host byte-order of a 32-bit value */
    }

    public static int ENET_SOCKETSET_EMPTY(int sockset)
    {
        return FD_ZERO(&(sockset));
    }

    public static int ENET_SOCKETSET_ADD(int sockset, int socket)
    {
        return FD_SET(socket, &(sockset));
    }

    public static int ENET_SOCKETSET_REMOVE(int sockset, int socket)
    {
        return FD_CLR(socket, &(sockset));
    }

    public static int ENET_SOCKETSET_CHECK(int sockset, int socket)
    {
        return FD_ISSET(socket, &(sockset));
    }

    public static void ENET_UNUSED<T>(T x)
    {
        //(void)x;
    }

    // #define ENET_DIFFERENCE(x, y) ((x) < (y) ? (y) - (x) : (x) - (y))


// =======================================================================//
// !
// ! Callbacks
// !
// =======================================================================//

    public static ENetCallbacks callbacks = { malloc, free, abort, enet_packet_create, enet_packet_destroy };

    public const int ENET_VERSION_MAJOR = 2;
    public const int ENET_VERSION_MINOR = 6;
    public const int ENET_VERSION_PATCH = 2;

    public static uint ENET_VERSION_CREATE(uint major, uint minor, uint patch)
    {
        return (((major) << 16) | ((minor) << 8) | (patch));
    }

    public static uint ENET_VERSION_GET_MAJOR(uint version)
    {
        return (((version) >> 16) & 0xFF);
    }

    public static uint ENET_VERSION_GET_MINOR(uint version)
    {
        return (((version) >> 8) & 0xFF);
    }

    public static uint ENET_VERSION_GET_PATCH(uint version)
    {
        return ((version) & 0xFF);
    }

    public static readonly uint ENET_VERSION = ENET_VERSION_CREATE(ENET_VERSION_MAJOR, ENET_VERSION_MINOR, ENET_VERSION_PATCH);

    /**
     * Initializes ENet globally and supplies user-overridden callbacks. Must be called prior to using any functions in ENet. Do not use enet_initialize() if you use this variant. Make sure the ENetCallbacks structure is zeroed out so that any additional callbacks added in future versions will be properly ignored.
     *
     * @param version the constant ENET_VERSION should be supplied so ENet knows which version of ENetCallbacks struct to use
     * @param inits user-overridden callbacks where any null callbacks will use ENet's defaults
     * @returns 0 on success, < 0 on failure
     */
    public static int enet_initialize_with_callbacks(uint version, ENetCallbacks inits)
    {
        if (version < ENET_VERSION_CREATE(1, 3, 0))
        {
            return -1;
        }

        if (inits.malloc != null || inits.free != null)
        {
            if (inits.malloc == null || inits.free == null)
            {
                return -1;
            }

            callbacks.malloc = inits.malloc;
            callbacks.free = inits.free;
        }

        if (inits.no_memory != null)
        {
            callbacks.no_memory = inits.no_memory;
        }

        if (inits.packet_create != null || inits.packet_destroy != null)
        {
            if (inits.packet_create == null || inits.packet_destroy == null)
            {
                return -1;
            }

            callbacks.packet_create = inits.packet_create;
            callbacks.packet_destroy = inits.packet_destroy;
        }

        return enet_initialize();
    }

    public static uint enet_linked_version()
    {
        return ENET_VERSION;
    }

    public static ENetPacket enet_malloc_packet(ulong bufferSize)
    {
        // TODO : check ikpil
        object memory = callbacks.malloc(bufferSize);

        if (memory == null)
        {
            callbacks.no_memory();
        }

        return (ENetPacket)memory;
    }

    public static T enet_malloc<T>()
    {
        return enet_malloc<T>(1)[0];
    }

    public static T[] enet_malloc<T>(long size)
    {
        object memory = callbacks.malloc(size);

        if (memory == null)
        {
            callbacks.no_memory();
        }

        return (T[])memory;
    }

    public static void enet_free(object memory)
    {
        callbacks.free(memory);
    }


    public static int enet_initialize()
    {
        WORD versionRequested = MAKEWORD(1, 1);
        WSADATA wsaData = { 0 };

        if (WSAStartup(versionRequested, &wsaData))
        {
            return -1;
        }

        if (LOBYTE(wsaData.wVersion) != 1 || HIBYTE(wsaData.wVersion) != 1)
        {
            WSACleanup();
            return -1;
        }

        timeBeginPeriod(1);
        return 0;
    }

    public static void enet_deinitialize()
    {
        timeEndPeriod(1);
        WSACleanup();
    }
}