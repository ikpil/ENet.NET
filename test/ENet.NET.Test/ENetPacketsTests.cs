using System;
using NUnit.Framework;
using static ENet.NET.ENetPackets;
using static ENet.NET.ENetPacketFlag;

namespace ENet.NET.Test;

public class ENetPacketsTests
{
    [Test]
    public void Test_enet_packet_create_and_destroy()
    {
        var foo = "foo"u8.ToArray();
        ENetPacket packet = enet_packet_create(foo, foo.Length, ENET_PACKET_FLAG_RELIABLE);

        Assert.That(packet.referenceCount, Is.EqualTo(0));
        Assert.That(packet.flags, Is.EqualTo(ENET_PACKET_FLAG_RELIABLE));
        Assert.That(packet.data.Array, Is.Not.Null);
        Assert.That(packet.dataLength, Is.EqualTo(3));
        Assert.That(packet.userData, Is.Null);

        enet_packet_destroy(packet);
    }

    [Test]
    public void Test_enet_packet_copy()
    {
        var userData = new int[1];
        var bar = "bar"u8.ToArray();
        ENetPacket p1 = enet_packet_create(bar, bar.Length, ENET_PACKET_FLAG_RELIABLE);
        p1.userData = userData;

        ENetPacket p2 = enet_packet_copy(p1);

        //
        Assert.That(p2, Is.Not.Null);
        Assert.That(p2.data.Array, Is.Not.Null);

        //
        Assert.That(p2.dataLength, Is.EqualTo(p1.dataLength));
        Assert.That(p2.data, Is.EqualTo(p1.data));
        Assert.That(p2.flags, Is.EqualTo(p1.flags));
        Assert.That(p2.referenceCount, Is.EqualTo(p1.referenceCount));
        Assert.That(p2.userData, Is.Null);

        enet_packet_destroy(p1);
        enet_packet_destroy(p2);
    }

    [Test]
    public void Test_enet_packet_resize()
    {
        var buf6 = "packet"u8.ToArray();
        var buf9 = "packetfoo"u8.ToArray();
        var buf12 = "packetfoobar"u8.ToArray();
        ENetPacket packet = enet_packet_create(buf6, buf6.Length, ENET_PACKET_FLAG_RELIABLE);
        Assert.That(packet.dataLength, Is.EqualTo(6));

        packet = enet_packet_resize(packet, buf9.Length);
        buf9.CopyTo(packet.data.AsSpan());
        Assert.That(packet.dataLength, Is.EqualTo(9));

        packet = enet_packet_resize(packet, buf6.Length);
        Assert.That(packet.dataLength, Is.EqualTo(6));

        packet = enet_packet_resize(packet, buf12.Length);
        buf12.CopyTo(packet.data.AsSpan());
        Assert.That(packet.dataLength, Is.EqualTo(12));

        enet_packet_destroy(packet);
    }
}