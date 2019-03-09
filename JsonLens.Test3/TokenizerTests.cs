using System;
using Xunit;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using JsonLens.Test3;

namespace JsonLens.Test
{
    public class TokenizerTests
    {
        [Fact]
        public void Empty()
            => Tokenize("")
                .ShouldBeEmpty();

        [Fact]
        public void OpenCloseEnd()
            => Tokenize("{}")
                .ShouldBe(new[] {
                    (Token.Object, ""),
                    (Token.ObjectEnd, "")
                });

        [Fact]
        public void SimpleString()
            => Tokenize("\"Hello!!!\"")
                .ShouldBe(new[] {
                    (Token.String, "Hello!!!")
                });

        [Fact]
        public void SimpleString_WithEscapedQuote()
            => Tokenize(@"""Bl\""ah""")
                .ShouldBe(new[] {
                    (Token.String, "Bl\\\"ah"), //BUT!!! the escape needs decoding in the reading...
                });

        [Fact]
        public void SimpleString_WithEscapedEscape()
            => Tokenize("\"Oi\\\"")
                .ShouldBe(new[] {
                    (Token.String, "Oi\\")
                });

        [Fact]
        public void SimpleString_WithSpaceAtStart()
            => Tokenize("\"  Boo!\"")
                .ShouldBe(new[] {
                    (Token.String, "  Boo!")
                });
        
        //and need to test StringParts...
        //but need buffer underruns for this
        //...

        [Fact]
        public void ObjectWithProperty()
            => Tokenize("{\"wibble\":\"blah\"}")
                .ShouldBe(new[] {
                    (Token.Object, ""),
                    (Token.String, "wibble"),
                    (Token.String, "blah"),
                    (Token.ObjectEnd, "")
                });

        [Fact]
        public void ObjectWithProperties()
            => Tokenize("{\"wibble\":\"blah\",\"plop\":3}")
                .ShouldBe(new[] {
                    (Token.Object, ""),
                    (Token.String, "wibble"),
                    (Token.String, "blah"),
                    (Token.String, "plop"),
                    (Token.Number, "3"),
                    (Token.ObjectEnd, "")
                });


        [Fact]
        public void SimpleNumber()
            => Tokenize("1234")
                .ShouldBe(new[] {
                    (Token.Number, "1234")
                });

        [Fact]
        public void ArrayWithValues()
            => Tokenize("[1,2,3,\"hello\"]")
                .ShouldBe(new[] {
                    (Token.Array, ""),
                    (Token.Number, "1"),
                    (Token.Number, "2"),
                    (Token.Number, "3"),
                    (Token.String, "hello"),
                    (Token.ArrayEnd, "")
                });

        [Fact]
        public void NestedArrays()
            => Tokenize("[1,[2,3],[]]")
                .ShouldBe(new[] {
                    (Token.Array, ""),
                    (Token.Number, "1"),
                    (Token.Array, ""),
                    (Token.Number, "2"),
                    (Token.Number, "3"),
                    (Token.ArrayEnd, ""),
                    (Token.Array, ""),
                    (Token.ArrayEnd, ""),
                    (Token.ArrayEnd, "")
                });



        public class IgnoresWhitespace
        {
            [Fact]
            public void AroundNumber()
                => Tokenize(" 123   ")
                    .ShouldBe(new[] {
                        (Token.Number, "123")
                    });
        }
        

        static (Token, string)[] Tokenize(string input)
        {
            var output = new List<(Token, string)>();
            int offset = 0;

            Span<Tokenized> bufferData = new Tokenized[16]; //wish stackalloc would work here...
            var buffer = new Buffer<Tokenized>(bufferData, 15);

            var s = input.AsZeroTerminatedSpan();
            var tokenizer = new Tokenizer(Tokenizer.Mode.Line);

            while (true)
            {
                var (status, chars) = tokenizer.Next(ref s, ref buffer);

                switch (status)
                {
                    case Status.Ok:
                        while (buffer.Read(out var e)) {
                            output.Add((e.Token, input.Substring(offset + e.Offset, e.Length)));
                        }
                        
                        s = s.Slice(chars);
                        offset += chars;
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
