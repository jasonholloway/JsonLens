using System;
using System.Xml;
using JsonLens.Test;
using Shouldly;
using Xunit;

namespace JsonLens.Test3 {
    public class CircularBufferTests {

        [Fact]
        public void CanWriteAndRead() {
            Span<int> data = stackalloc int[8];
            var buffer = new Buffer<int>(data, 7);

            buffer.Write(1);

            buffer.Read(out int result).ShouldBeTrue();
            result.ShouldBe(1);
        }

        [Fact]
        public void MultiWritesAndReads() {
            Span<int> data = stackalloc int[8];
            var buffer = new Buffer<int>(data, 7);

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
            var buffer = new Buffer<int>(data, 1);

            buffer.Write(1);
            
            buffer.Read(out int _).ShouldBeTrue();
            buffer.Read(out int __).ShouldBeFalse();
        }

        [Fact]
        public void CantWriteAfterFillingBuffer() {
            Span<int> data = stackalloc int[2];
            var buffer = new Buffer<int>(data, 1);

            buffer.Write(1).ShouldBeTrue();
            buffer.Write(2).ShouldBeTrue();
            buffer.Write(3).ShouldBeFalse();
        }

        [Fact]
        public void ReadingAndWritingLoop() {
            Span<int> data = stackalloc int[2];
            var buffer = new Buffer<int>(data, 1);

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
        
        [Fact]
        public void BlahBlah1() {
            Span<int> data = stackalloc int[4];
            var buffer = new Buffer<int>(data, 3);

            buffer.Write(1);
            
            data[0].ShouldBe(1);
            
            buffer.Read(out var i);
            i.ShouldBe(1);
        }

        
        //unfortunately appears to be the case that
        //just making something refstruct doesn't make it perform magically:
        //it just shuts down what the compiler will allow
        //Span itself is magic, as it includes the magical, managed pointer containing ByReference
        
        [Fact]
        public void BlahBlah2() {
            Span<int> data = stackalloc int[4];
            var cont = new Container(data, 3);

            cont.Write(1);
            
            data[0].ShouldBe(1);
            cont.Read().ShouldBe(1);
        }

        ref struct Container {
            Buffer<int> _buffer;

            public Container(Span<int> data, int mask) {
                _buffer = new Buffer<int>(data, mask);
            }

            public void Write(int i)
                => _buffer.Write(i);

            public int Read() {
                _buffer.Read(out int i);
                return i;
            }
        }

        [Fact]
        public void Gah() {
            var a = new AStruct(1);
            a.Inc();
            a.Val.ShouldBe(2);
        }
        
        [Fact]
        public void Gah2() {
            var c = new Container2(1);
            c.Inc();
            c.Val.ShouldBe(2);
        }

        ref struct AStruct {
            int _i;

            public AStruct(int i) {
                _i = i;
            }

            public void Inc() => _i++;

            public int Val => _i;
        }

        ref struct Container2 {
            AStruct _s;

            public Container2(int i) {
                _s = new AStruct(i);
            }

            public void Inc() => _s.Inc();

            public int Val => _s.Val;
        }
    }
}