using System;
using Xunit;
using Shouldly;
using System.Collections.Generic;

namespace JsonLens.Test
{
    public class TokenizerDepthTests
    {
        [Fact]
        public void Empty()
            => Tokenize("")
                .ShouldBeEmpty();

        [Fact]
        public void OpenCloseEnd()
            => Tokenize("{}")
                .ShouldBe(new[] {
                    (0, Token.Object),
                    (0, Token.ObjectEnd)
                });

        [Fact]
        public void SimpleString()
            => Tokenize("\"Hello!!!\"")
                .ShouldBe(new[] {
                    (0, Token.String),
                    (0, Token.StringEnd)
                });

        [Fact]
        public void ObjectWithProperty()
            => Tokenize("{\"wibble\":\"blah\"}")
                .ShouldBe(new[] {
                    (0, Token.Object),
                    (1, Token.String),
                    (1, Token.StringEnd),
                    (1, Token.String),
                    (1, Token.StringEnd),
                    (0, Token.ObjectEnd)
                });

        [Fact]
        public void NestedArrays()
            => Tokenize("[1,[2,3],[]]")
                .ShouldBe(new[] {
                    (0, Token.Array),
                    (1, Token.Number),
                    (1, Token.Array),
                    (2, Token.Number),
                    (2, Token.Number),
                    (1, Token.ArrayEnd),
                    (1, Token.Array),
                    (1, Token.ArrayEnd),
                    (0, Token.ArrayEnd)
                });

        [Fact]
        public void NestedObjects()
            => Tokenize("{\"a\":1,\"b\":{\"c\":{}}}")
                .ShouldBe(new[] {
                    (0, Token.Object),
                    (1, Token.String),
                    (1, Token.StringEnd),
                    (1, Token.Number),
                    (1, Token.String),
                    (1, Token.StringEnd),
                    (1, Token.Object),
                    (2, Token.String),
                    (2, Token.StringEnd),
                    (2, Token.Object),
                    (2, Token.ObjectEnd),
                    (1, Token.ObjectEnd),
                    (0, Token.ObjectEnd)
                });

        
        static (int, Token)[] Tokenize(string input)
        {
            var output = new List<(int, Token)>();

            Span<Tokenizer.Emitted> bufferData = new Tokenizer.Emitted[16];
            var buffer = new CircularBuffer<Tokenizer.Emitted>(bufferData, 15);            //passing bufferData here breaks line 88 below

            var s = input.AsZeroTerminatedSpan();
            var tokenizer = new Tokenizer();

            while (true)
            {
                var (status, chars) = tokenizer.Next(ref s, ref buffer);

                switch (status)
                {
                    case Status.Ok:
                        while (buffer.Read(out var e)) {
                            output.Add((e.Depth, e.Token));
                        }
                        s = s.Slice(chars);
                        break;

                    case Status.End:
                        return output.ToArray();

                    case Status.Underrun:
                        throw new NotImplementedException("UNDERRUN");

                    case Status.BadInput:
                        throw new NotImplementedException("BADINPUT");
                }
            }

        }
    }
    
}
