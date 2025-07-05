using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using static ENet.NET.ENets;


namespace ENet.NET
{
    public static class ENetSockets
    {
        public static int enet_socket_bind(Socket socket, ENetAddress address)
        {
            try
            {
                IPEndPoint endPoint;

                if (null != address.host)
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
            catch (SocketException e)
            {
                print(e.ToString());
                return -1; // 실패
            }
        }

        public static int enet_socket_get_address(Socket socket, ref ENetAddress address)
        {
            try
            {
                if (socket == null || address.host == null)
                    return -1;

                var endPoint = socket.LocalEndPoint as IPEndPoint;
                if (endPoint == null)
                    return -1;

                var host = endPoint.Address;
                var port = (ushort)endPoint.Port;
                var scopeId = address.sin6_scope_id;

                if (endPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    scopeId = (uint)endPoint.Address.ScopeId;
                }

                address = new ENetAddress(host, port, scopeId);

                return 0;
            }
            catch (SocketException e)
            {
                print(e.ToString());
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
            catch (SocketException e)
            {
                print(e.ToString());
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
            catch (SocketException e)
            {
                print(e.ToString());
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
            catch (SocketException e)
            {
                value = 0;
                print(e.ToString());
                return -1;
            }
        }

        public static int enet_socket_connect(Socket socket, ref ENetAddress address)
        {
            try
            {
                if (socket == null || address.host == null)
                    return -1;

                socket.Connect(address.host, address.port);
                return socket.Connected ? 0 : -1;
            }
            catch (SocketException e)
            {
                print(e.ToString());
                return -1;
            }
        }

        public static Socket enet_socket_accept(Socket socket, ref ENetAddress address)
        {
            try
            {
                if (socket == null)
                    return null;

                Socket acceptedSocket = socket.Accept();

                if (address.host != null)
                {
                    var remoteEndPoint = acceptedSocket.RemoteEndPoint as IPEndPoint;
                    if (remoteEndPoint != null)
                    {
                        var host = remoteEndPoint.Address;
                        var port = (ushort)remoteEndPoint.Port;
                        var scopeId = address.sin6_scope_id;

                        if (remoteEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            scopeId = (uint)remoteEndPoint.Address.ScopeId;
                        }

                        address = new ENetAddress(host, port, scopeId);
                    }
                }

                return acceptedSocket;
            }
            catch (SocketException e)
            {
                print(e.ToString());
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
            catch (SocketException e)
            {
                print(e.ToString());
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
            catch (SocketException e)
            {
                print(e.ToString());
            }
        }

        public static int enet_socket_send(Socket socket, ref ENetAddress address, ENetBuffer buffers)
        {
            enet_assert(false);
            return -1;
        }

        public static int enet_socket_send(Socket socket, ref ENetAddress address, Span<ENetBuffer> buffers, long bufferCount)
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

                    bufferList.Add(buffer.ToArraySegment());
                }

                if (bufferList.Count == 0)
                    return 0;

                // todo @ikpil test code
                int sent = 0;
                var endpoint = new IPEndPoint(address.host, address.port);
                for (int i = 0; i < bufferList.Count; i++)
                {
                    var buffer = bufferList[i];
                    sent += socket.SendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endpoint);
                }
                return sent;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock)
                    return 0;

                print(e.ToString());
                return -1;
            }
        }

        public static int enet_socket_receive(Socket socket, ref ENetAddress address, ref ENetBuffer buffer)
        {
            try
            {
                if (socket == null)
                    return -1;

                EndPoint remoteEP = null;
                if (address.host != null)
                {
                    remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                }
                //int received = socket.Receive(buffer.data.Slice(0, buffer.dataLength));
                //int received = socket.ReceiveFrom(buffer.data, SocketFlags.None, ref remoteEP);
                int received = socket.ReceiveFrom(buffer.data, buffer.offset, buffer.dataLength, SocketFlags.None, ref remoteEP);


                if (address.host != null && remoteEP is IPEndPoint ipEndPoint)
                {
                    var host = ipEndPoint.Address;
                    var port = (ushort)ipEndPoint.Port;
                    var scopeId = address.sin6_scope_id;

                    if (ipEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        scopeId = (uint)ipEndPoint.Address.ScopeId;
                    }

                    address = new ENetAddress(host, port, scopeId);
                }

                return received;
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.WouldBlock:
                    case SocketError.ConnectionReset:
                        return 0;
                    case SocketError.Interrupted:
                    case SocketError.MessageSize:
                        return -2;
                    default:
                        print(e.ToString());
                        return -1;
                }
            }
        }

        public static int enet_socket_receive(Socket socket, ref ENetAddress address, Span<ENetBuffer> buffers, long bufferCount)
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

                    bufferList.Add(buffer.ToArraySegment());
                }

                if (bufferList.Count == 0)
                    return 0;

                EndPoint remoteEP = null;
                if (address.host != null)
                {
                    remoteEP = new IPEndPoint(IPAddress.IPv6Any, 0);
                }

                int received = socket.Receive(bufferList);

                if (address.host != null && remoteEP is IPEndPoint ipEndPoint)
                {
                    var host = ipEndPoint.Address;
                    var port = (ushort)ipEndPoint.Port;
                    var scopeId = address.sin6_scope_id;

                    if (ipEndPoint.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        scopeId = (uint)ipEndPoint.Address.ScopeId;
                    }

                    address = new ENetAddress(host, port, scopeId);
                }

                return received;
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.WouldBlock:
                    case SocketError.ConnectionReset:
                        return 0;
                    case SocketError.Interrupted:
                    case SocketError.MessageSize:
                        return -2;
                    default:
                        print(e.ToString());
                        return -1;
                }
            }
        }

        public static int enet_socketset_select(Socket maxSocket, List<Socket> readSet, List<Socket> writeSet, int timeout)
        {
            try
            {
                if (maxSocket == null)
                    return -1;

                // 타임아웃 설정
                int timeoutMs = 0 >= timeout ? -1 : timeout * 1000;

                // Select 호출
                Socket.Select(readSet, writeSet, null, timeoutMs);

                return readSet.Count + writeSet.Count;
            }
            catch (SocketException e)
            {
                print(e.ToString());
                return -1;
            }
        }

        public static int enet_socket_wait(Socket socket, ref uint condition, long timeout)
        {
            try
            {
                if (socket == null)
                    return -1;

                var readList = new List<Socket>();
                var writeList = new List<Socket>();

                // 조건에 따라 소켓 추가
                if ((condition & ENetSocketWait.ENET_SOCKET_WAIT_SEND) != 0)
                {
                    writeList.Add(socket);
                }

                if ((condition & ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE) != 0)
                {
                    readList.Add(socket);
                }

                // 타임아웃 설정 (밀리초 단위)
                int timeoutMs = timeout <= 0 ? -1 : (int)timeout;

                // Select 호출
                Socket.Select(readList, writeList, null, timeoutMs);

                // 결과 업데이트
                condition = ENetSocketWait.ENET_SOCKET_WAIT_NONE;

                if (readList.Contains(socket))
                {
                    condition |= ENetSocketWait.ENET_SOCKET_WAIT_RECEIVE;
                }

                if (writeList.Contains(socket))
                {
                    condition |= ENetSocketWait.ENET_SOCKET_WAIT_SEND;
                }

                return 0;
            }
            catch (SocketException e)
            {
                print(e.ToString());
                return -1;
            }
        }
    }
}