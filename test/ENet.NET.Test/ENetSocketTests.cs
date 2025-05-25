using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using static ENet.NET.ENets;
using static ENet.NET.ENetHosts;
using static ENet.NET.ENetPackets;

namespace ENet.NET.Test;

public class ENetSocketTests
{
    private static int g_counter;
    private static int g_disconnected;

    [Test]
    public async Task Test_UdpEchoServer_Client_With_ReceiveFromAsync()
    {
        const int ServerPort = 12345;
        const int MessageCount = 10;
        var serverEndpoint = new IPEndPoint(IPAddress.Loopback, ServerPort);

        var server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        server.Bind(serverEndpoint);

        // 서버 수신 및 에코 응답 루프
        _ = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                SocketReceiveFromResult result = await server.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, remote);

                // 받은 메시지를 그대로 다시 보냄 (에코)
                await server.SendToAsync(new ArraySegment<byte>(buffer, 0, result.ReceivedBytes), SocketFlags.None, result.RemoteEndPoint);
            }
        });

        var client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        for (int i = 0; i < MessageCount; i++)
        {
            string msg = $"hello-{i}";
            byte[] sendBuf = Encoding.UTF8.GetBytes(msg);

            // 서버에 전송
            await client.SendToAsync(new ArraySegment<byte>(sendBuf), SocketFlags.None, serverEndpoint);

            // 응답 수신
            byte[] recvBuf = new byte[1024];
            EndPoint any = new IPEndPoint(IPAddress.Any, 0);
            SocketReceiveFromResult result = await client.ReceiveFromAsync(new ArraySegment<byte>(recvBuf), SocketFlags.None, any);

            string echo = Encoding.UTF8.GetString(recvBuf, 0, result.ReceivedBytes);
            Assert.That(echo, Is.EqualTo(msg), $"에코 메시지가 일치하지 않음: {echo} != {msg}");
        }

        client.Close();
        server.Close(); // 수신 Task는 살아있지만 테스트 목적상 종료
    }

    [Test]
    public async Task Test_ENetSocket()
    {
        enet_initialize();

        const int ServerPort = 12345;
        var address = new ENetAddress(IPAddress.Loopback, ServerPort, 0);
        ENetHost server = enet_host_create(address, 500, 2, 0, 0);


        var dummyAddress = new ENetAddress();
        ENetHost clientHost = enet_host_create(dummyAddress, 1, 2, 0, 0);
        ENetPeer clientPeer = enet_host_connect(clientHost, address, 2, 0);

        // program will make N iterations, and then exit
        int counter = 1000;

        do
        {
            host_server(server);

            ENetEvent @event = new ENetEvent();
            enet_host_service(clientHost, @event, 0);

            Thread.Sleep(100);
            counter--;
        } while (counter > 0);

        Assert.That(g_counter, Is.EqualTo(1));
    }


    private static void host_server(ENetHost server)
    {
        ENetEvent @event = new ENetEvent();
        while (enet_host_service(server, @event, 2) > 0)
        {
            switch (@event.type)
            {
                case ENetEventType.ENET_EVENT_TYPE_CONNECT:
                    print($"A new peer with ID {@event.peer.incomingPeerID} connected from ::1:{@event.peer.address.port}.");
                    /* Store any relevant client information here. */
                    @event.peer.data = g_counter++;
                    break;
                case ENetEventType.ENET_EVENT_TYPE_RECEIVE:
                    print($"A packet of length {@event.packet.dataLength} containing {@event.packet.data} was received from {@event.peer.data} on channel {@event.channelID}.");

                    /* Clean up the packet now that we're done using it. */
                    enet_packet_destroy(@event.packet);
                    break;

                case ENetEventType.ENET_EVENT_TYPE_DISCONNECT:
                    print($"Peer with ID {@event.peer.incomingPeerID} disconnected.");
                    g_disconnected++;
                    /* Reset the peer's client information. */
                    @event.peer.data = null;
                    break;

                case ENetEventType.ENET_EVENT_TYPE_DISCONNECT_TIMEOUT:
                    print($"Client {@event.peer.incomingPeerID} timeout.");
                    g_disconnected++;
                    /* Reset the peer's client information. */
                    @event.peer.data = null;
                    break;

                case ENetEventType.ENET_EVENT_TYPE_NONE:
                    break;
            }
        }
    }
}