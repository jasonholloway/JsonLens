using System;
using JsonLens.Test;
using Shouldly;
using Xunit;

namespace JsonLens.Test3 {
    public class CircularBufferTests {

        [Fact]
        public void CanWriteAndRead() {
            Span<int> data = stackalloc int[8];
            var buffer = new CircularBuffer<int>(data, 7);

            buffer.Write(1);

            buffer.Read(out int result).ShouldBeTrue();
            result.ShouldBe(1);
        }

        [Fact]
        public void MultiWritesAndReads() {
            Span<int> data = stackalloc int[8];
            var buffer = new CircularBuffer<int>(data, 7);

            buffer.Write(1).ShouldBeTrue();
            buffer.Write(2).ShouldBeTrue();
            buffer.Write(3).ShouldBeTrue();

            buffer.Read(out int result1).ShouldBeTrue();
            result1.ShouldBe(1);
            
            buffer.Read(out int result2).ShouldBeTrue();
            result2.ShouldBe(2);
            
            buffer.Read(out int result3).ShouldBeTrue();
            result3.ShouldBe(3);
        }

        [Fact]
        public void ReadingPastWrite_ReturnsFalse() {
            Span<int> data = stackalloc int[2];
            var buffer = new CircularBuffer<int>(data, 1);

            buffer.Write(1);
            
            buffer.Read(out int _).ShouldBeTrue();
            buffer.Read(out int __).ShouldBeFalse();
        }

        [Fact]
        public void CantWriteAfterFillingBuffer() {
            Span<int> data = stackalloc int[2];
            var buffer = new CircularBuffer<int>(data, 1);

            buffer.Write(1).ShouldBeTrue();
            buffer.Write(2).ShouldBeTrue();
            buffer.Write(3).ShouldBeFalse();
        }

        [Fact]
        public void ReadingAndWritingLoop() {
            Span<int> data = stackalloc int[2];
            var buffer = new CircularBuffer<int>(data, 1);

            buffer.Write(1);
            buffer.Write(2);
            
            buffer.Read(out int a).ShouldBeTrue();
            a.ShouldBe(1);

            buffer.Write(3);
            
            buffer.Read(out int b).ShouldBeTrue();
            buffer.Read(out int c).ShouldBeTrue();
            b.ShouldBe(2);
            c.ShouldBe(3);

            buffer.Write(4).ShouldBeTrue();
            buffer.Write(5).ShouldBeTrue();
            
            buffer.Read(out int d).ShouldBeTrue();
            buffer.Read(out int e).ShouldBeTrue();
            d.ShouldBe(4);
            e.ShouldBe(5);
        }
        
        //then, the circularity of the buffer needs testing too...
        
    }
}