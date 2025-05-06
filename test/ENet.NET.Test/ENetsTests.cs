using NUnit.Framework;
using static ENet.NET.ENets;

namespace ENet.NET.Test;

public class ENetsTests
{
    [Test]
    public void Test_ENets_ent_Initialize_destroy()
    {
        Assert.That(enet_initialize(), Is.EqualTo(0)) ;
        Assert.That(enet_deinitialize(), Is.EqualTo(0));
    }
}