﻿using System;
using Xunit;
using Shouldly;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
                    (Token.String, ""),
                    (Token.StringEnd, "Hello!!!")
                });

        [Fact]
        public void SimpleString_WithEscapedQuote()
            => Tokenize(@"""Bl\""ah""")
                .ShouldBe(new[] {
                    (Token.String, ""),
                    (Token.StringEnd, "Bl\\\"ah"), //BUT!!! the escape needs decoding in the reading...
                });

        [Fact]
        public void SimpleString_WithEscapedEscape()
            => Tokenize("\"Oi\\\"")
                .ShouldBe(new[] {
                    (Token.String, ""),
                    (Token.StringEnd, "Oi\\")
                });

        [Fact]
        public void SimpleString_WithSpaceAtStart()
            => Tokenize("\"  Boo!\"")
                .ShouldBe(new[] {
                    (Token.String, ""),
                    (Token.StringEnd, "  Boo!")
                });

        [Fact]
        public void ObjectWithProperty()
            => Tokenize("{\"wibble\":\"blah\"}")
                .ShouldBe(new[] {
                    (Token.Object, ""),
                    (Token.String, ""),
                    (Token.StringEnd, "wibble"),
                    (Token.String, ""),
                    (Token.StringEnd, "blah"),
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
                    (Token.String, ""),
                    (Token.StringEnd, "hello"),
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

            var x = new Tokenizer.Context(input.AsZeroTerminatedSpan());

            while (true)
            {
                var (status, chars, emitted) = Tokenizer.Next(ref x);

                switch (status)
                {
                    case Status.Ok:
                        if (emitted.HasValue)
                        {
                            var (token, from, length) = emitted.Value;
                            output.Add((token, input.Substring(offset + from, length)));
                        }
                        
                        x.Span = x.Span.Slice(chars);
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
