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
        Assert.That((0), Is.EqualTo(buffer.RealIndexFromVirtualIndex(0)));
        Assert.That((1), Is.EqualTo(buffer.RealIndexFromVirtualIndex(1)));
        Assert.That((2), Is.EqualTo(buffer.RealIndexFromVirtualIndex(2)));

        buffer.Push(0);

        // | 0 | _ | _ |
        //   ^    
        Assert.That((0), Is.EqualTo(buffer.RealIndexFromVirtualIndex(0)));
        Assert.That((1), Is.EqualTo(buffer.RealIndexFromVirtualIndex(1)));
        Assert.That((2), Is.EqualTo(buffer.RealIndexFromVirtualIndex(2)));

        // This will cause the buffer to loop twice.
        for (var i = 1; i < 7; i++)
            buffer.Push(i);

        // | 6 | 4 | 5 |
        //       ^    
        Assert.That((1), Is.EqualTo(buffer.RealIndexFromVirtualIndex(0)));
        Assert.That((2), Is.EqualTo(buffer.RealIndexFromVirtualIndex(1)));
        Assert.That((0), Is.EqualTo(buffer.RealIndexFromVirtualIndex(2)));

        // Cause the buffer to loop back once
        for (var i = 0; i < 2; i++)
            buffer.Pop();

        // | _ | 4 | _ |
        //       ^    
        Assert.That((1), Is.EqualTo(buffer.RealIndexFromVirtualIndex(0)));
        Assert.That((2), Is.EqualTo(buffer.RealIndexFromVirtualIndex(1)));
        Assert.That((0), Is.EqualTo(buffer.RealIndexFromVirtualIndex(2)));
    }

    [Test]
    public void RingBuffer_GeneralFunctionality()
    {
        var buffer = new RingBuffer<int>(3);

        Assert.That((0), Is.EqualTo(buffer.Count));
        Assert.Throws<InvalidOperationException>(() => buffer.Pop());

        buffer.Push(0);

        // | 0 | _ | _ |
        //   ^    
        Assert.That((1), Is.EqualTo(buffer.Count));
        Assert.That((0), Is.EqualTo(buffer[0]));
        Assert.That((0), Is.EqualTo(buffer.Peek()));
        Assert.Throws<ArgumentException>(() => { var x = buffer[1]; });
        Assert.Throws<ArgumentException>(() => { var x = buffer[2]; });

        for (var i = 1; i < 7; i++)
            buffer.Push(i);

        // | 6 | 4 | 5 |
        //       ^    
        Assert.That((3), Is.EqualTo(buffer.Count));
        Assert.That((4), Is.EqualTo(buffer[0]));
        Assert.That((5), Is.EqualTo(buffer[1]));
        Assert.That((6), Is.EqualTo(buffer[2]));
        Assert.That((6), Is.EqualTo(buffer.Peek()));

        // Cause the buffer to loop back once
        for (var i = 0; i < 2; i++)
            buffer.Pop();

        Assert.That((1), Is.EqualTo(buffer.Count));
        Assert.That((4), Is.EqualTo(buffer[0]));
        Assert.Throws<ArgumentException>(() => { var x = buffer[1]; });
        Assert.Throws<ArgumentException>(() => { var x = buffer[2]; });
        Assert.That((4), Is.EqualTo(buffer.Peek()));
    }
}
