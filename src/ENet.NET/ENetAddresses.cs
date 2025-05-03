namespace ENet.NET;

public static class ENetAddresses
{
        public static void enet_inaddr_map4to6(struct in_addr in, struct in6_addr *out)
    {
        if (in.s_addr == 0x00000000) { /* 0.0.0.0 */
            *out = enet_v6_anyaddr;
        } else if (in.s_addr == 0xFFFFFFFF) { /* 255.255.255.255 */
            *out = enet_v6_noaddr;
        } else {
            *out = enet_v4_anyaddr;
            out.s6_addr[10] = 0xFF;
            out.s6_addr[11] = 0xFF;
            out.s6_addr[12] = ((uint8_t *)&in.s_addr)[0];
            out.s6_addr[13] = ((uint8_t *)&in.s_addr)[1];
            out.s6_addr[14] = ((uint8_t *)&in.s_addr)[2];
            out.s6_addr[15] = ((uint8_t *)&in.s_addr)[3];
        }
    }
    public static void enet_inaddr_map6to4(const struct in6_addr *in, struct in_addr *out)
    {
        memset(out, 0, sizeof(struct in_addr));
        ((uint8_t *)&out.s_addr)[0] = in.s6_addr[12];
        ((uint8_t *)&out.s_addr)[1] = in.s6_addr[13];
        ((uint8_t *)&out.s_addr)[2] = in.s6_addr[14];
        ((uint8_t *)&out.s_addr)[3] = in.s6_addr[15];
    }

    public static int enet_in6addr_lookup_host(const char *name, bool nodns, ENetAddress *out) {
        struct addrinfo hints, *resultList = null, *result = null;

        memset(&hints, 0, sizeof(hints));
        hints.ai_family = AF_UNSPEC;

        if (nodns)
        {
            hints.ai_flags = AI_NUMERICHOST; /* prevent actual DNS lookups! */
        }

        if (getaddrinfo(name, null, &hints, &resultList) != 0) {
            freeaddrinfo(resultList);
            return -1;
        }

        for (result = resultList; result != null; result = result.ai_next) {
            if (result.ai_addr != null) {
                if (result.ai_family == AF_INET || (result.ai_family == AF_UNSPEC && result.ai_addrlen == sizeof(struct sockaddr_in))) {
                    enet_inaddr_map4to6(((struct sockaddr_in*)result.ai_addr).sin_addr, &out.host);
                    out.sin6_scope_id = 0;
                    freeaddrinfo(resultList);
                    return 0;

                } else if (result.ai_family == AF_INET6 || (result.ai_family == AF_UNSPEC && result.ai_addrlen == sizeof(struct sockaddr_in6))) {
                    memcpy(&out.host, &((struct sockaddr_in6*)result.ai_addr).sin6_addr, sizeof(struct ENetAddress.in6_addr));
                    out.sin6_scope_id = (ushort) ((struct sockaddr_in6*)result.ai_addr).sin6_scope_id;
                    freeaddrinfo(resultList);
                    return 0;
                }
            }
        }
        freeaddrinfo(resultList);
        return -1;
    }

    public static int enet_address_set_host_ip_new(ENetAddress *address, const char *name) {
        return enet_in6addr_lookup_host(name, true, address);
    }

    public static int enet_address_set_host_new(ENetAddress *address, const char *name) {
        return enet_in6addr_lookup_host(name, false, address);
    }

    public static int enet_address_get_host_ip_new(const ENetAddress *address, char *name, ulong nameLength) {
        if (IN6_IS_ADDR_V4MAPPED(&address.host)) {
            struct in_addr buf;
            enet_inaddr_map6to4(&address.host, &buf);

            if (inet_ntop(AF_INET, &buf, name, nameLength) == null) {
                return -1;
            }
        }
        else {
            if (inet_ntop(AF_INET6, (void*)&address.host, name, nameLength) == null) {
                return -1;
            }
        }

        return 0;
    } /* enet_address_get_host_ip_new */

    public static int enet_address_get_host_new(const ENetAddress *address, char *name, ulong nameLength) {
        struct sockaddr_in6 sin;
        memset(&sin, 0, sizeof(struct sockaddr_in6));

        int err;


        sin.sin6_family = AF_INET6;
        sin.sin6_port = ENET_HOST_TO_NET_16 (address.port);
        sin.sin6_addr = address.host;
        sin.sin6_scope_id = address.sin6_scope_id;

        err = getnameinfo((struct sockaddr *) &sin, sizeof(sin), name, nameLength, null, 0, NI_NAMEREQD);
        if (!err) {
            if (name != null && nameLength > 0 && !memchr(name, '\0', nameLength)) {
                return -1;
            }
            return 0;
        }
        if (err != EAI_NONAME) {
            return -1;
        }

        return enet_address_get_host_ip_new(address, name, nameLength);
    } /* enet_address_get_host_new */

    
       public static int enet_address_set_host_ip_old(ENetAddress *address, const char *name) {
        byte vals[4] = { 0, 0, 0, 0 };
        int i;

        for (i = 0; i < 4; ++i) {
            const char *next = name + 1;
            if (*name != '0') {
                long val = strtol(name, (char **) &next, 10);
                if (val < 0 || val > 255 || next == name || next - name > 3) {
                    return -1;
                }
                vals[i] = (byte) val;
            }

            if (*next != (i < 3 ? '.' : '\0')) {
                return -1;
            }
            name = next + 1;
        }

        memcpy(&address.host, vals, sizeof(uint));
        return 0;
    }

    public static int enet_address_set_host_old(ENetAddress *address, const char *name) {
        struct hostent *hostEntry = null;
        hostEntry = gethostbyname(name);

        if (hostEntry == null || hostEntry.h_addrtype != AF_INET) {
            if (!inet_pton(AF_INET6, name, &address.host)) {
                return -1;
            }

            return 0;
        }

        ((uint *)&address.host.s6_addr)[0] = 0;
        ((uint *)&address.host.s6_addr)[1] = 0;
        ((uint *)&address.host.s6_addr)[2] = htonl(0xffff);
        ((uint *)&address.host.s6_addr)[3] = *(uint *)hostEntry.h_addr_list[0];

        return 0;
    }

    public static int enet_address_get_host_ip_old(const ENetAddress *address, char *name, ulong nameLength) {
        if (inet_ntop(AF_INET6, (PVOID)&address.host, name, nameLength) == null) {
            return -1;
        }

        return 0;
    }

    public static int enet_address_get_host_old(const ENetAddress *address, char *name, ulong nameLength) {
        struct in6_addr in;
        struct hostent *hostEntry = null;
        in = address.host;
        hostEntry = gethostbyaddr((char *)&in, sizeof(struct in6_addr), AF_INET6);
        if (hostEntry == null) {
            return enet_address_get_host_ip(address, name, nameLength);
        } else {
            ulong hostLen = strlen(hostEntry.h_name);
            if (hostLen >= nameLength) {
                return -1;
            }
            memcpy(name, hostEntry.h_name, hostLen + 1);
        }
        return 0;
    }
 
}