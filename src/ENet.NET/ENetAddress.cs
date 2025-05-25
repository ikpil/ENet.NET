using System.Net;

namespace ENet.NET
{
    /**
     * Portable internet address structure.
     *
     * The host must be specified in network byte-order, and the port must be in host
     * byte-order. The constant ENET_HOST_ANY may be used to specify the default
     * server host. The constant ENET_HOST_BROADCAST may be used to specify the
     * broadcast address (255.255.255.255).  This makes sense for enet_host_connect,
     * but not for enet_host_create.  Once a server responds to a broadcast, the
     * address is updated from ENET_HOST_BROADCAST to the server's actual IP address.
     */
    public struct ENetAddress
    {
        public readonly IPAddress host;
        public readonly ushort port;
        public readonly long sin6_scope_id;

        public ENetAddress(IPAddress host, ushort port, long sin6_scope_id)
        {
            this.host = host;
            this.port = port;
            this.sin6_scope_id = sin6_scope_id;
        }

        public ENetAddress Clone()
        {
            ENetFixedArray16<byte> bytes = new ENetFixedArray16<byte>();
            host.TryWriteBytes(bytes.AsSpan(), out var bytesWritten);
            var ipaddr = new IPAddress(bytes.AsSpan().Slice(0, bytesWritten));
            
            return new ENetAddress(ipaddr, port, sin6_scope_id);
        }
    }
}