using System;
using System.Text;
using NUnit.Framework;
using static ENet.NET.ENetPackets;

namespace ENet.NET.Test;

public class ENetPacketsTests
{
    [Test]
    public void Test_ENetPackets_enet_packet_resize()
    {
        byte[] packetBytes = Encoding.UTF8.GetBytes("packet");
        byte[] packetFooBytes = Encoding.UTF8.GetBytes("packetfoo");
        byte[] fooBytes = Encoding.UTF8.GetBytes("foo");
        byte[] fooBarBytes = Encoding.UTF8.GetBytes("foobar");
        byte[] packetFooBarBytes = Encoding.UTF8.GetBytes("packetfoobar");

        ENetPacket packet = enet_packet_create(packetBytes, packetBytes.Length, ENetPacketFlag.ENET_PACKET_FLAG_RELIABLE);
        Console.WriteLine($"length: {packet.dataLength}, data: {packet.dataLength}.{Encoding.UTF8.GetString(packet.data)}");

        packet = enet_packet_resize(packet, packetFooBytes.Length);
        fooBytes.AsSpan().CopyTo(packet.data.AsSpan(packetBytes.Length));
        Console.WriteLine($"length: {packet.dataLength}, data: {packet.dataLength}.{Encoding.UTF8.GetString(packet.data)}");

        packet = enet_packet_resize(packet, packetBytes.Length);
        Console.WriteLine($"length: {packet.dataLength}, data: {packet.dataLength}.{Encoding.UTF8.GetString(packet.data)}");

        packet = enet_packet_resize(packet, packetFooBarBytes.Length);
        fooBarBytes.AsSpan().CopyTo(packet.data.AsSpan(packetBytes.Length));
        Console.WriteLine($"length: {packet.dataLength}, data: {packet.dataLength}.{Encoding.UTF8.GetString(packet.data)}");
    }
}