using System;
using System.Net;
using System.Net.Sockets;

namespace ENet.NET
{
    public static class ENetAddresses
    {
        public static ushort ENET_HOST_TO_NET_16(ushort value)
        {
            return (ushort)IPAddress.NetworkToHostOrder((short)value); /* macro that converts host to net byte-order of a 16-bit value */
        }

        public static uint ENET_HOST_TO_NET_32(uint value)
        {
            return (uint)IPAddress.HostToNetworkOrder((int)value); /* macro that converts host to net byte-order of a 32-bit value */
        }

        public static ushort ENET_NET_TO_HOST_16(ushort value)
        {
            return (ushort)IPAddress.NetworkToHostOrder((short)value); /* macro that converts net to host byte-order of a 16-bit value */
        }

        public static uint ENET_NET_TO_HOST_32(uint value)
        {
            return (uint)IPAddress.NetworkToHostOrder((int)value); /* macro that converts net to host byte-order of a 32-bit value */
        }

        public static bool in6_equal(IPAddress a, IPAddress b)
        {
            if (a == null || b == null)
                return false;

            if (a.AddressFamily != AddressFamily.InterNetworkV6 ||
                b.AddressFamily != AddressFamily.InterNetworkV6)
                return false;

            Span<byte> bytesA = stackalloc byte[16];
            Span<byte> bytesB = stackalloc byte[16];

            if (!a.TryWriteBytes(bytesA, out int writtenA) || writtenA != 16)
                return false;

            if (!b.TryWriteBytes(bytesB, out int writtenB) || writtenB != 16)
                return false;

            return bytesA.SequenceEqual(bytesB);
        }

        public static void enet_inaddr_map4to6(IPAddress ipv4Addr, out IPAddress ipv6Addr)
        {
            byte[] ipv4Bytes = ipv4Addr.GetAddressBytes();
            uint ipv4Value = BitConverter.ToUInt32(ipv4Bytes, 0);

            // Special cases from original ENet code
            if (ipv4Value == 0x00000000) // 0.0.0.0
            {
                ipv6Addr = IPAddress.IPv6Any;
            }
            else if (ipv4Value == 0xFFFFFFFF) // 255.255.255.255
            {
                ipv6Addr = IPAddress.IPv6None;
            }
            else
            {
                // Create IPv4-mapped IPv6 address (::ffff:0:0/96)
                ipv6Addr = ipv4Addr.MapToIPv6();
            }
        }

        public static void enet_inaddr_map6to4(IPAddress ipv6Addr, out IPAddress ipv4Addr)
        {
            ipv4Addr = ipv6Addr.MapToIPv4();
        }


        public static int enet_in6addr_lookup_host(string name, bool noDns, out ENetAddress outAddr)
        {
            outAddr = new ENetAddress();

            try
            {
                // If noDns is true, we only accept numeric IP addresses
                if (noDns)
                {
                    if (!IPAddress.TryParse(name, out IPAddress ipAddress))
                    {
                        return -1;
                    }

                    outAddr = new ENetAddress(ipAddress, 0, ipAddress.ScopeId);
                    return 0;
                }

                // Try to resolve the host name
                IPAddress[] addresses = Dns.GetHostAddresses(name);
                if (addresses.Length == 0)
                {
                    return -1;
                }

                // Prefer IPv6 addresses if available
                IPAddress selectedAddress = null;
                foreach (IPAddress addr in addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        selectedAddress = addr;
                        break;
                    }
                    else if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        selectedAddress = addr.MapToIPv4();
                        break;
                    }
                }

                if (selectedAddress == null)
                {
                    return -1;
                }

                outAddr = new ENetAddress(selectedAddress, 0, selectedAddress.ScopeId);
                return 0;
            }
            catch
            {
                return -1;
            }
        }


        public static int enet_address_set_host_ip(out ENetAddress address, string name)
        {
            return enet_in6addr_lookup_host(name, true, out address);
        }

        public static int enet_address_set_host(out ENetAddress address, string name)
        {
            return enet_in6addr_lookup_host(name, false, out address);
        }

        public static int enet_address_get_host_ip(ref ENetAddress address, out string name)
        {
            if (address.host.IsIPv4MappedToIPv6)
            {
                name = address.host.MapToIPv4().ToString();
                return 0;
            }
            else
            {
                // Use IPv6 address as is
                name = address.host.ToString();
                return 0;
            }

            return 0;
        } /* enet_address_get_host_ip_new */

        public static int enet_address_get_host(ref ENetAddress address, out string name)
        {
            name = null;

            try
            {
                // IPv6 설정
                var endpoint = new IPEndPoint(address.host, address.port);

                if (address.host.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // ScopeId 적용
                    var addrBytes = address.host.GetAddressBytes();
                    var ipv6 = new IPAddress(addrBytes, address.host.ScopeId);

                    var hostEntry = Dns.GetHostEntry(ipv6);
                    name = hostEntry.HostName;

                    return 0;
                }
                else
                {
                    var hostEntry = Dns.GetHostEntry(address.host);
                    name = hostEntry.HostName;
                    return 0;
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
            {
                return enet_address_get_host_ip(ref address, out name);
            }
            catch
            {
                return -1;
            }
        } /* enet_address_get_host_new */
    }
}