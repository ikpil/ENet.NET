using System.Net.Sockets;

namespace ENet.NET;

public static class ENetSockets
{
    public static int enet_socket_bind(Socket socket,  const ENetAddress* address) {
        struct sockaddr_in6 sin = { 0 };
        sin.sin6_family = AF_INET6;

        if (address != null)
        {
            sin.sin6_port = ENET_HOST_TO_NET_16(address.port);
            sin.sin6_addr = address.host;
            sin.sin6_scope_id = address.sin6_scope_id;
        }
        else
        {
            sin.sin6_port = 0;
            sin.sin6_addr = in6addr_any;
            sin.sin6_scope_id = 0;
        }

        return bind(socket, (struct sockaddr *) &sin, sizeof(struct sockaddr_in6)) == SOCKET_ERROR ? -1 : 0;
    }

    public static int enet_socket_get_address(Socket socket, ref ENetAddress address)
    {
        struct sockaddr_in6 sin = { 0 };
        int sinLength = sizeof(struct sockaddr_in6);

        if (getsockname(socket, (struct sockaddr *) &sin, &sinLength) == -1) {
            return -1;
        }

        address.host = sin.sin6_addr;
        address.port = ENET_NET_TO_HOST_16(sin.sin6_port);
        address.sin6_scope_id = sin.sin6_scope_id;

        return 0;
    }

    public static int enet_socket_listen(Socket socket, int backlog)
    {
        return listen(socket, backlog < 0 ? SOMAXCONN : backlog) == SOCKET_ERROR ? -1 : 0;
    }

    public static Socket enet_socket_create(ENetSocketType type)
    {
        return socket(PF_INET6, type == ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM ? SOCK_DGRAM : SOCK_STREAM, 0);
    }

    public static int enet_socket_set_option(Socket socket, ENetSocketOption option, int value)
    {
        int result = SOCKET_ERROR;

        switch (option)
        {
            case ENetSocketOption.ENET_SOCKOPT_NONBLOCK:
            {
                u_long nonBlocking = (u_long)value;
                result = ioctlsocket(socket, FIONBIO, &nonBlocking);
                break;
            }

            case ENetSocketOption.ENET_SOCKOPT_BROADCAST:
                result = setsockopt(socket, SOL_SOCKET, SO_BROADCAST, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_REUSEADDR:
                result = setsockopt(socket, SOL_SOCKET, SO_REUSEADDR, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_RCVBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVBUF, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_SNDBUF:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDBUF, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_RCVTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_RCVTIMEO, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_SNDTIMEO:
                result = setsockopt(socket, SOL_SOCKET, SO_SNDTIMEO, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_NODELAY:
                result = setsockopt(socket, IPPROTO_TCP, TCP_NODELAY, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY:
                result = setsockopt(socket, IPPROTO_IPV6, IPV6_V6ONLY, (char*)&value, sizeof(int));
                break;

            case ENetSocketOption.ENET_SOCKOPT_TTL:
                result = setsockopt(socket, IPPROTO_IP, IP_TTL, (char*)&value, sizeof(int));
                break;

            default:
                break;
        }

        return result == SOCKET_ERROR ? -1 : 0;
    } /* enet_socket_set_option */

    public static int enet_socket_get_option(Socket socket, ENetSocketOption option, int* value)
    {
        int result = SOCKET_ERROR, len;

        switch (option)
        {
            case ENetSocketOption.ENET_SOCKOPT_ERROR:
                len = sizeof(int);
                result = getsockopt(socket, SOL_SOCKET, SO_ERROR, (char*)value, &len);
                break;

            case ENetSocketOption.ENET_SOCKOPT_TTL:
                len = sizeof(int);
                result = getsockopt(socket, IPPROTO_IP, IP_TTL, (char*)value, &len);
                break;

            default:
                break;
        }

        return result == SOCKET_ERROR ? -1 : 0;
    }

    public static int enet_socket_connect(Socket socket,  const ENetAddress* address) {
        struct sockaddr_in6 sin = { 0 };
        int result;

        sin.sin6_family = AF_INET6;
        sin.sin6_port = ENET_HOST_TO_NET_16(address.port);
        sin.sin6_addr = address.host;
        sin.sin6_scope_id = address.sin6_scope_id;

        result = connect(socket, (struct sockaddr *) &sin, sizeof(struct sockaddr_in6));
        if (result == SOCKET_ERROR && WSAGetLastError() != WSAEWOULDBLOCK)
        {
            return -1;
        }

        return 0;
    }

    public static Socket enet_socket_accept(Socket socket, ENetAddress* address)
    {
        SOCKET result;
        struct sockaddr_in6 sin = { 0 };
        int sinLength = sizeof(struct sockaddr_in6);

        result = accept(socket, address != null ? (struct sockaddr *)&sin : null, address != null ? &sinLength : null);

        if (result == INVALID_SOCKET)
        {
            return null;
        }

        if (address != null)
        {
            address.host = sin.sin6_addr;
            address.port = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id = sin.sin6_scope_id;
        }

        return result;
    }

    public static int enet_socket_shutdown(Socket socket, ENetSocketShutdown how)
    {
        return shutdown(socket, (int)how) == SOCKET_ERROR ? -1 : 0;
    }

    public static void enet_socket_destroy(Socket socket)
    {
        if (socket != INVALID_SOCKET)
        {
            closesocket(socket);
        }
    }

    public static int enet_socket_send(Socket socket, ref ENetAddress address, ref ENetBuffer buffers, ulong bufferCount)
    {
        struct sockaddr_in6 sin = { 0 };
        DWORD sentLength = 0;

        if (address != null)
        {
            sin.sin6_family = AF_INET6;
            sin.sin6_port = ENET_HOST_TO_NET_16(address.port);
            sin.sin6_addr = address.host;
            sin.sin6_scope_id = address.sin6_scope_id;
        }

        if (WSASendTo(socket,
                (LPWSABUF)buffers,
                (DWORD)bufferCount,
                &sentLength,
                0,
                address != null ? (struct sockaddr *) &sin : null,
        address != null ? sizeof(struct sockaddr_in6) : 0,
        null,
        null) == SOCKET_ERROR
            ) {
            return (WSAGetLastError() == WSAEWOULDBLOCK) ? 0 : -1;
        }

        return (int)sentLength;
    }

    public static int enet_socket_receive(Socket socket, ENetAddress* address, ENetBuffer* buffers, ulong bufferCount)
    {
        INT sinLength = sizeof(struct sockaddr_in6);
        DWORD flags = 0, recvLength = 0;
        struct sockaddr_in6 sin = { 0 };

        if (WSARecvFrom(socket,
                (LPWSABUF)buffers,
                (DWORD)bufferCount,
                &recvLength,
                &flags,
                address != null ? (struct sockaddr *) &sin : null,
        address != null ? &sinLength : null,
        null,
        null) == SOCKET_ERROR
            ) {
            switch (WSAGetLastError())
            {
                case WSAEWOULDBLOCK:
                case WSAECONNRESET:
                    return 0;
                case WSAEINTR:
                case WSAEMSGSIZE:
                    return -2;
                default:
                    return -1;
            }
        }

        if (flags & MSG_PARTIAL)
        {
            return -2;
        }

        if (address != null)
        {
            address.host = sin.sin6_addr;
            address.port = ENET_NET_TO_HOST_16(sin.sin6_port);
            address.sin6_scope_id = sin.sin6_scope_id;
        }

        return (int)recvLength;
    } /* enet_socket_receive */

    public static int enet_socketset_select(Socket maxSocket, ENetSocketSet* readSet, ENetSocketSet* writeSet, uint timeout)
    {
        struct timeval timeVal;

        timeVal.tv_sec = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        return select(maxSocket + 1, readSet, writeSet, null, &timeVal);
    }

    public static int enet_socket_wait(Socket socket, ref uint condition, ulong timeout)
    {
        fd_set readSet, writeSet;
        struct timeval timeVal;
        int selectCount;

        timeVal.tv_sec = timeout / 1000;
        timeVal.tv_usec = (timeout % 1000) * 1000;

        FD_ZERO(&readSet);
        FD_ZERO(&writeSet);

        if (*condition & ENetSocketWait.ENET_SOCKET_WAIT_SEND)
        {
            FD_SET(socket, &writeSet);
        }

        if (*condition & ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE)
        {
            FD_SET(socket, &readSet);
        }

        selectCount = select(socket + 1, &readSet, &writeSet, null, &timeVal);

        if (selectCount < 0)
        {
            return -1;
        }

        *condition = ENetSocketWait.ENET_SOCKET_WAIT_NONE;

        if (selectCount == 0)
        {
            return 0;
        }

        if (FD_ISSET(socket, &writeSet))
        {
            *condition |= ENetSocketWait.ENET_SOCKET_WAIT_SEND;
        }

        if (FD_ISSET(socket, &readSet))
        {
            *condition |= ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE;
        }

        return 0;
    } /* enet_socket_wait */
}