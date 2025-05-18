using System;
using System.Threading;
using NUnit.Framework;
using static ENet.NET.ENetTimes;

namespace ENet.NET.Test;

public class ENetTimesTests
{
    [Test]
    public void Test_ENET_TIME_LESS()
    {
        // Normal cases
        Assert.That(ENET_TIME_LESS(1000, 2000), Is.True, "ENET_TIME_LESS should return true for a < b");
        Assert.That(ENET_TIME_LESS(1000, 1000), Is.False, "ENET_TIME_LESS should return false for a == b");
        Assert.That(ENET_TIME_LESS(2000, 1000), Is.False, "ENET_TIME_LESS should return false for a > b");

        // Cases involving ENET_TIME_OVERFLOW constant
        Assert.That(ENET_TIME_LESS(1000, ENET_TIME_OVERFLOW), Is.True, "ENET_TIME_LESS with a < ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be true");
        Assert.That(ENET_TIME_LESS(ENET_TIME_OVERFLOW, int.MaxValue), Is.False, "ENET_TIME_LESS with a == ENET_TIME_OVERFLOW and b < ENET_TIME_OVERFLOW should be false");
        Assert.That(ENET_TIME_LESS(ENET_TIME_OVERFLOW, ENET_TIME_OVERFLOW), Is.False, "ENET_TIME_LESS with a == ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be false");
    }

    [Test]
    public void Test_ENET_TIME_GREATER()
    {
        // Normal cases
        Assert.That(ENET_TIME_GREATER(2000, 1000), Is.True, "ENET_TIME_GREATER should return true for a > b");
        Assert.That(ENET_TIME_GREATER(1000, 1000), Is.False, "ENET_TIME_GREATER should return false for a == b");
        Assert.That(ENET_TIME_GREATER(1000, 2000), Is.False, "ENET_TIME_GREATER should return false for a < b");

        // Cases involving ENET_TIME_OVERFLOW constant
        Assert.That(ENET_TIME_GREATER(1000, ENET_TIME_OVERFLOW), Is.False, "ENET_TIME_GREATER with a < ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be false");
        Assert.That(ENET_TIME_GREATER(ENET_TIME_OVERFLOW, 1000), Is.True, "ENET_TIME_GREATER with a == ENET_TIME_OVERFLOW and b < ENET_TIME_OVERFLOW should be true"); // ENET_TIME_GREATER is !ENET_TIME_LESS_EQUAL
        Assert.That(ENET_TIME_GREATER(ENET_TIME_OVERFLOW, ENET_TIME_OVERFLOW), Is.False, "ENET_TIME_GREATER with a == ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be false");
    }

    [Test]
    public void Test_ENET_TIME_LESS_EQUAL()
    {
        // Normal cases
        Assert.That(ENET_TIME_LESS_EQUAL(1000, 2000), Is.True, "ENET_TIME_LESS_EQUAL should return true for a <= b");
        Assert.That(ENET_TIME_LESS_EQUAL(1000, 1000), Is.True, "ENET_TIME_LESS_EQUAL should return true for a <= b (equal)");
        Assert.That(ENET_TIME_LESS_EQUAL(2000, 1000), Is.False, "ENET_TIME_LESS_EQUAL should return false for a <= b (greater)");

        // Cases involving ENET_TIME_OVERFLOW constant
        Assert.That(ENET_TIME_LESS_EQUAL(1000, ENET_TIME_OVERFLOW), Is.True, "ENET_TIME_LESS_EQUAL with a < ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be true");
        Assert.That(ENET_TIME_LESS_EQUAL(ENET_TIME_OVERFLOW, 1000), Is.False, "ENET_TIME_LESS_EQUAL with a == ENET_TIME_OVERFLOW and b < ENET_TIME_OVERFLOW should be false");
        Assert.That(ENET_TIME_LESS_EQUAL(ENET_TIME_OVERFLOW, ENET_TIME_OVERFLOW), Is.True, "ENET_TIME_LESS_EQUAL with a == ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be true");
    }

    [Test]
    public void Test_ENET_TIME_GREATER_EQUAL()
    {
        // Normal cases
        Assert.That(ENET_TIME_GREATER_EQUAL(2000, 1000), Is.True, "ENET_TIME_GREATER_EQUAL should return true for a >= b");
        Assert.That(ENET_TIME_GREATER_EQUAL(1000, 1000), Is.True, "ENET_TIME_GREATER_EQUAL should return true for a >= b (equal)");
        Assert.That(ENET_TIME_GREATER_EQUAL(1000, 2000), Is.False, "ENET_TIME_GREATER_EQUAL should return false for a >= b (less)");

        // Cases involving ENET_TIME_OVERFLOW constant (based on ENET_TIME_GREATER logic)
        Assert.That(ENET_TIME_GREATER_EQUAL(1000, ENET_TIME_OVERFLOW), Is.False, "ENET_TIME_GREATER_EQUAL with a < ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be false");
        Assert.That(ENET_TIME_GREATER_EQUAL(ENET_TIME_OVERFLOW, 1000), Is.True, "ENET_TIME_GREATER_EQUAL with a == ENET_TIME_OVERFLOW and b < ENET_TIME_OVERFLOW should be true");
        Assert.That(ENET_TIME_GREATER_EQUAL(ENET_TIME_OVERFLOW, ENET_TIME_OVERFLOW), Is.True, "ENET_TIME_GREATER_EQUAL with a == ENET_TIME_OVERFLOW and b == ENET_TIME_OVERFLOW should be true");
    }

    [Test]
    public void Test_ENET_TIME_DIFFERENCE()
    {
        // Normal cases
        Assert.That(ENET_TIME_DIFFERENCE(2000, 1000), Is.EqualTo(1000), "ENET_TIME_DIFFERENCE should return the absolute difference for a > b");
        Assert.That(ENET_TIME_DIFFERENCE(1000, 2000), Is.EqualTo(1000), "ENET_TIME_DIFFERENCE should return the absolute difference for a < b");
        Assert.That(ENET_TIME_DIFFERENCE(1000, 1000), Is.EqualTo(0), "ENET_TIME_DIFFERENCE should return 0 for a == b");

        // Case involving ENET_TIME_OVERFLOW constant as per user's example
        Assert.That(ENET_TIME_DIFFERENCE(1000, ENET_TIME_OVERFLOW), Is.EqualTo(ENET_TIME_OVERFLOW - 1000), "ENET_TIME_DIFFERENCE should return the difference involving ENET_TIME_OVERFLOW");
    }


    [Test]
    public void Test_enet_time_get()
    {
        long firstTime = enet_time_get();
        Thread.Sleep(100);
        long secondTime = enet_time_get();

        Assert.That(secondTime, Is.GreaterThan(firstTime), "enet_time_get should return increasing time");
        Assert.That(secondTime - firstTime, Is.GreaterThanOrEqualTo(90), "Time difference should be at least ~100ms");
    }
}