namespace ENet.NET
{
    public static class ENetVersions
    {
        public const int ENET_VERSION_MAJOR = 2;
        public const int ENET_VERSION_MINOR = 6;
        public const int ENET_VERSION_PATCH = 2;

        public static readonly uint ENET_VERSION = ENET_VERSION_CREATE(ENET_VERSION_MAJOR, ENET_VERSION_MINOR, ENET_VERSION_PATCH);

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
    }
}