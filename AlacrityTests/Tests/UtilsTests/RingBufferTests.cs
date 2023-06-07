using AlacrityCore.Utils;

namespace AlacrityTests.Tests.UtilsTests;
internal class RingBufferTests
{
    [Test]
    public void RealIndexFromVirtualIndex_CalculatesCorrectly()
    {
        var buffer = new RingBuffer<int>(3);

        // | _ | _ | _ |
        //   ^
        Assert.AreEqual(0, buffer.RealIndexFromVirtualIndex(0));
        Assert.AreEqual(1, buffer.RealIndexFromVirtualIndex(1));
        Assert.AreEqual(2, buffer.RealIndexFromVirtualIndex(2));

        buffer.Push(0);

        // | 0 | _ | _ |
        //   ^    
        Assert.AreEqual(0, buffer.RealIndexFromVirtualIndex(0));
        Assert.AreEqual(1, buffer.RealIndexFromVirtualIndex(1));
        Assert.AreEqual(2, buffer.RealIndexFromVirtualIndex(2));

        // This will cause the buffer to loop twice.
        for (var i = 1; i < 7; i++)
            buffer.Push(i);

        // | 6 | 4 | 5 |
        //       ^    
        Assert.AreEqual(1, buffer.RealIndexFromVirtualIndex(0));
        Assert.AreEqual(2, buffer.RealIndexFromVirtualIndex(1));
        Assert.AreEqual(0, buffer.RealIndexFromVirtualIndex(2));

        // Cause the buffer to loop back once
        for (var i = 0; i < 2; i++)
            buffer.Pop();

        // | _ | 4 | _ |
        //       ^    
        Assert.AreEqual(1, buffer.RealIndexFromVirtualIndex(0));
        Assert.AreEqual(2, buffer.RealIndexFromVirtualIndex(1));
        Assert.AreEqual(0, buffer.RealIndexFromVirtualIndex(2));
    }

    [Test]
    public void RingBuffer_GeneralFunctionality()
    {
        var buffer = new RingBuffer<int>(3);

        Assert.AreEqual(0, buffer.Count);
        Assert.Throws<InvalidOperationException>(() => buffer.Pop());

        buffer.Push(0);

        // | 0 | _ | _ |
        //   ^    
        Assert.AreEqual(1, buffer.Count);
        Assert.AreEqual(0, buffer[0]);
        Assert.AreEqual(0, buffer.Peek());
        Assert.Throws<ArgumentException>(() => { var x = buffer[1]; });
        Assert.Throws<ArgumentException>(() => { var x = buffer[2]; });

        for (var i = 1; i < 7; i++)
            buffer.Push(i);

        // | 6 | 4 | 5 |
        //       ^    
        Assert.AreEqual(3, buffer.Count);
        Assert.AreEqual(4, buffer[0]);
        Assert.AreEqual(5, buffer[1]);
        Assert.AreEqual(6, buffer[2]);
        Assert.AreEqual(6, buffer.Peek());

        // Cause the buffer to loop back once
        for (var i = 0; i < 2; i++)
            buffer.Pop();

        Assert.AreEqual(1, buffer.Count);
        Assert.AreEqual(4, buffer[0]);
        Assert.Throws<ArgumentException>(() => { var x = buffer[1]; });
        Assert.Throws<ArgumentException>(() => { var x = buffer[2]; });
        Assert.AreEqual(4, buffer.Peek());
    }
}
