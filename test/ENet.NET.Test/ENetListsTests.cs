using System.Collections.Generic;
using NUnit.Framework;

namespace ENet.NET.Test;

public class ENetListsTests
{
    [Test]
    public void Test_ASDf()
    {
        var asdf = new LinkedList<int>();
        asdf.AddLast(1);

        var first = asdf.First;
        var last = asdf.Last;
    }
}