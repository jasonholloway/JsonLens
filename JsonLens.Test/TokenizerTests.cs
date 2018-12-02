using System;
using Xunit;
using Shouldly;
using System.Linq;

namespace JsonLens.Test
{
    public class TokenizerTests
    {
        JsonTokenizer Tokenizer;

        public TokenizerTests()
        {
            Tokenizer = new JsonTokenizer();
        }

        public class WithSimpleDriver
        {
            [Fact]
            public void Empty()
                => Tokenize("")
                    .ShouldBe(new[] {
                        (Token.End, "")
                    });

            [Fact]
            public void OpenCloseEnd()
                => Tokenize("{}")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Object, ""),
                        (Token.ObjectEnd, ""),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString()
                => Tokenize("\"Hello!!!\"")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "Hello!!!"),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString_WithEscapedQuote()
                => Tokenize(@"""Bl\""ah""")
                    .ShouldBe(new[] {
                        (Token.Line, ""), 
                        (Token.String, ""),
                        (Token.StringEnd, "Bl\\\"ah"), //BUT!!! the escape needs decoding in the reading...
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString_WithEscapedEscape()
                => Tokenize("\"Oi\\\"")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "Oi\\"),
                        (Token.End, "")
                    });



            [Fact]
            public void SimpleString_WithSpaceAtStart()
                => Tokenize("\"  Boo!\"")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "  Boo!"),
                        (Token.End, "")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Tokenize("{\"wibble\":\"blah\"}")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Object, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "wibble"),
                        (Token.String, ""),
                        (Token.StringEnd, "blah"),
                        (Token.ObjectEnd, ""),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleNumber()
                => Tokenize("1234")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Number, "1234"),
                        (Token.End, "")
                    });

            [Fact]
            public void ArrayWithValues()
                => Tokenize("[1,2,3,\"hello\"]")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Array, ""),
                        (Token.Number, "1"),
                        (Token.Number, "2"),
                        (Token.Number, "3"),
                        (Token.String, ""),
                        (Token.StringEnd, "hello"),
                        (Token.ArrayEnd, ""),
                        (Token.End, "")
                    });

            [Fact]
            public void NestedArrays()
                => Tokenize("[1,[2,3],[]]")
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Array, ""),
                        (Token.Number, "1"),
                        (Token.Array, ""),
                        (Token.Number, "2"),
                        (Token.Number, "3"),
                        (Token.ArrayEnd, ""),
                        (Token.Array, ""),
                        (Token.ArrayEnd, ""),
                        (Token.ArrayEnd, ""),
                        (Token.End, "")
                    });



            public class IgnoresWhitespace
            {
                [Fact]
                public void AroundNumber()
                    => Tokenize(" 123   ")
                        .ShouldBe(new[] {
                            (Token.Line, ""),
                            (Token.Number, "123"),
                            (Token.End, "")
                        });

            }

        }


        static (Token, string)[] Tokenize(string input)
        {
            var tokenizer = new JsonTokenizer();
            var x = new Context(input.AsZeroTerminatedSpan(), 0, Mode.Start);
            
            while (true)
            {
                var (status, charsRead, nextMode) = tokenizer.Tokenize(ref x);       
                
                switch(status)
                {
                    case Status.Ok:
                        x.Span = x.Span.Slice(charsRead);
                        x.Index += charsRead;
                        x.Mode = nextMode.Value;
                        break;

                    case Status.End:
                        return x.Output
                                .Select(emitted => {
                                    var (token, (start, len)) = emitted;
                                    return (token, input.Substring(start, len));
                                })
                                .ToArray();

                    case Status.Underrun:
                        throw new NotImplementedException("UNDERRUN");

                    case Status.BadInput:
                        throw new NotImplementedException("BADINPUT");
                }
            }

        }
    }
    
}
