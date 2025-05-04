using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using static ENet.NET.ENetAddresses;
using System.Collections.Generic;

namespace ENet.NET
{
    public static class ENetSockets
    {
        public static int enet_socket_bind(Socket socket, ENetAddress address)
        {
            try
            {
                IPEndPoint endPoint;

                if (null != address && null != address.host)
                {
                    IPAddress ip = address.host;
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ip = new IPAddress(ip.GetAddressBytes(), address.sin6_scope_id);
                    }

                    endPoint = new IPEndPoint(ip, address.port);
                }
                else
                {
                    IPAddress any = IPAddress.IPv6Any;
                    endPoint = new IPEndPoint(any, 0);
                }

                socket.Bind(endPoint);
                return 0; // 성공
            }
            catch (SocketException)
            {
                return -1; // 실패
            }
        }

        public static int enet_socket_get_address(Socket socket, ENetAddress address)
        {
            try
            {
                if (socket == null || address == null)
                    return -1;

                var endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint == null)
                    return -1;

                address.host = endPoint.Address;
                address.port = (ushort)endPoint.Port;
                
                if (endPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    address.sin6_scope_id = (uint)endPoint.Address.ScopeId;
                }

                return 0;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static int enet_socket_listen(Socket socket, int backlog)
        {
            try
            {
                if (socket == null)
                    return -1;

                socket.Listen(backlog < 0 ? (int)SocketOptionName.MaxConnections : backlog);
                return 0;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static Socket enet_socket_create(ENetSocketType type)
        {
            Socket socket = new Socket(
                AddressFamily.InterNetworkV6,
                type == ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM ? SocketType.Dgram : SocketType.Stream,
                type == ENetSocketType.ENET_SOCKET_TYPE_DATAGRAM ? ProtocolType.Udp : ProtocolType.Tcp
            );

            socket.DualMode = true;
            return socket;
        }

        public static int enet_socket_set_option(Socket socket, ENetSocketOption option, int value)
        {
            try
            {
                if (socket == null)
                    return -1;

                switch (option)
                {
                    case ENetSocketOption.ENET_SOCKOPT_NONBLOCK:
                        socket.Blocking = value == 0;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_BROADCAST:
                        socket.EnableBroadcast = value != 0;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_REUSEADDR:
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value);
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_RCVBUF:
                        socket.ReceiveBufferSize = value;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_SNDBUF:
                        socket.SendBufferSize = value;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_RCVTIMEO:
                        socket.ReceiveTimeout = value;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_SNDTIMEO:
                        socket.SendTimeout = value;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_NODELAY:
                        socket.NoDelay = value != 0;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_IPV6_V6ONLY:
                        socket.DualMode = value == 0;
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_TTL:
                        socket.Ttl = (short)value;
                        break;

                    default:
                        return -1;
                }

                return 0;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static int enet_socket_get_option(Socket socket, ENetSocketOption option, out int value)
        {
            try
            {
                if (socket == null)
                {
                    value = 0;
                    return -1;
                }

                switch (option)
                {
                    case ENetSocketOption.ENET_SOCKOPT_ERROR:
                        value = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                        break;

                    case ENetSocketOption.ENET_SOCKOPT_TTL:
                        value = socket.Ttl;
                        break;

                    default:
                        value = 0;
                        return -1;
                }

                return 0;
            }
            catch (SocketException)
            {
                value = 0;
                return -1;
            }
        }

        public static int enet_socket_connect(Socket socket, ENetAddress address)
        {
            try
            {
                if (socket == null || address == null)
                    return -1;

                socket.Connect(address.host, address.port);
                return socket.Connected ? 0 : -1;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static Socket enet_socket_accept(Socket socket, ENetAddress address)
        {
            try
            {
                if (socket == null)
                    return null;

                Socket acceptedSocket = socket.Accept();
                
                if (address != null)
                {
                    var remoteEndPoint = acceptedSocket.RemoteEndPoint as IPEndPoint;
                    if (remoteEndPoint != null)
                    {
                        address.host = remoteEndPoint.Address;
                        address.port = (ushort)remoteEndPoint.Port;
                        
                        if (remoteEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            address.sin6_scope_id = (uint)remoteEndPoint.Address.ScopeId;
                        }
                    }
                }

                return acceptedSocket;
            }
            catch (SocketException)
            {
                return null;
            }
        }

        public static int enet_socket_shutdown(Socket socket, ENetSocketShutdown how)
        {
            try
            {
                if (socket == null)
                    return -1;

                SocketShutdown shutdownMode;
                switch (how)
                {
                    case ENetSocketShutdown.ENET_SOCKET_SHUTDOWN_READ:
                        shutdownMode = SocketShutdown.Receive;
                        break;
                    case ENetSocketShutdown.ENET_SOCKET_SHUTDOWN_WRITE:
                        shutdownMode = SocketShutdown.Send;
                        break;
                    case ENetSocketShutdown.ENET_SOCKET_SHUTDOWN_READ_WRITE:
                        shutdownMode = SocketShutdown.Both;
                        break;
                    default:
                        return -1;
                }

                socket.Shutdown(shutdownMode);
                return 0;
            }
            catch (SocketException)
            {
                return -1;
            }
        }

        public static void enet_socket_destroy(Socket socket)
        {
            try
            {
                if (socket != null)
                {
                    socket.Close();
                    socket.Dispose();
                }
            }
            catch (SocketException)
            {
                // 소켓 닫기 실패는 무시
            }
        }

        public static int enet_socket_send(Socket socket, ENetAddress address, Span<ENetBuffer> buffers, long bufferCount)
        {
            try
            {
                if (socket == null)
                    return -1;

                var bufferList = new List<ArraySegment<byte>>();
                for (int i = 0; i < bufferCount; i++)
                {
                    var buffer = buffers[i];
                    if (buffer.data == null || buffer.dataLength <= 0)
                        continue;

                    bufferList.Add(new ArraySegment<byte>(buffer.data, 0, (int)buffer.dataLength));
                }

                if (bufferList.Count == 0)
                    return 0;

                IPEndPoint endPoint = null;
                if (address != null)
                {
                    endPoint = new IPEndPoint(address.host, address.port);
                }

                int sent = socket.Send(bufferList, bufferList.Count);
                return sent;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                    return 0;
                return -1;
            }
        }

        public static int enet_socket_receive(Socket socket, ENetAddress address, ref ENetBuffer buffers, long bufferCount)
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

        public static int enet_socket_wait(Socket socket, ref uint condition, long timeout)
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
}