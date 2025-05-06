using System;
using System.Net;
using System.Threading;
using static ENet.NET.ENets;
using static ENet.NET.ENetHosts;
using static ENet.NET.ENetPackets;
using static ENet.NET.ENetAddresses;
using static ENet.NET.ENetPeers;

namespace ENet.NET.Demo;

public struct Client
{
    public ENetHost host;
    public ENetPeer peer;
}

public static class Program
{
    private const int MAX_CLIENTS = 5000;
    private static ulong g_counter = 0;
    private static ulong g_disconnected = 0;
    
    // program will make N iterations, and then exit
    static int counter = 1000;

    private static void host_server(ENetHost server)
    {
        ENetEvent @event = new ENetEvent();
        while (enet_host_service(server, @event, 2) > 0)
        {
            switch (@event.type)
            {
                case ENetEventType.ENET_EVENT_TYPE_CONNECT:
                    printf("A new peer with ID %u connected from ::1:%u.\n", @event.peer.incomingPeerID, @event.peer.address.port);
                    /* Store any relevant client information here. */
                    @event.peer.data = g_counter++;
                    break;
                case ENetEventType.ENET_EVENT_TYPE_RECEIVE:
                    //printf("A packet of length %zu containing %s was received from %s on channel %u.\n", @event.packet.dataLength, @event.packet.data, (char*)@event.peer.data, @event.channelID);

                    /* Clean up the packet now that we're done using it. */
                    enet_packet_destroy(@event.packet);
                    break;

                case ENetEventType.ENET_EVENT_TYPE_DISCONNECT:
                    printf("Peer with ID %u disconnected.\n", @event.peer.incomingPeerID);
                    g_disconnected++;
                    /* Reset the peer's client information. */
                    @event.peer.data = null;
                    break;

                case ENetEventType.ENET_EVENT_TYPE_DISCONNECT_TIMEOUT:
                    printf("Client %u timeout.\n", @event.peer.incomingPeerID);
                    g_disconnected++;
                    /* Reset the peer's client information. */
                    @event.peer.data = null;
                    break;

                case ENetEventType.ENET_EVENT_TYPE_NONE:
                    break;
            }
        }
    }

    public static int Main(string[] args)
    {
        if (enet_initialize() != 0)
        {
            printf("An error occurred while initializing ENet.\n");
            return 1;
        }

        int i = 0;
        Client[] clients = new Client[MAX_CLIENTS];
        ENetAddress address = new ENetAddress();

        address.host = IPAddress.Any; /* Bind the server to the default localhost. */
        address.port = 7777; /* Bind the server to port 7777. */


        /* create a server */
        printf("starting server...\n");
        ENetHost server = enet_host_create(address, MAX_CLIENTS, 2, 0, 0);
        if (server == null)
        {
            printf("An error occurred while trying to create an ENet server host.\n");
            return 1;
        }

        printf("starting clients...\n");
        for (i = 0; i < MAX_CLIENTS; ++i)
        {
            enet_address_set_host(out address, "127.0.0.1");
            clients[i].host = enet_host_create(null, 1, 2, 0, 0);
            clients[i].peer = enet_host_connect(clients[i].host, address, 2, 0);
            if (clients[i].peer == null)
            {
                printf("coundlnt connect\n");
                return 1;
            }
        }

        printf("running server...\n");


        do
        {
            host_server(server);

            ENetEvent @event = new ENetEvent();
            for (i = 0; i < MAX_CLIENTS; ++i)
            {
                enet_host_service(clients[i].host, @event, 0);
            }

            counter--;
        } while (counter > 0);

        printf("stopping clients...\n");

        for (i = 0; i < MAX_CLIENTS; ++i)
        {
            enet_peer_disconnect_now(clients[i].peer, 0);
            enet_host_destroy(clients[i].host);
        }

        counter = 1000;

        do
        {
            host_server(server);
            Thread.Sleep(1);
            counter--;
        } while (g_disconnected < g_counter);

        enet_host_destroy(server);
        enet_deinitialize();
        return 0;
    }
}