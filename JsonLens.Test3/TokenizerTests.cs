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

            var s = input.AsZeroTerminatedSpan();
            var @in = new Readable<char>(s);
            
            var tokenizer = new Tokenizer(Tokenizer.Mode.Line);
            Tokenized @out;

            while (true)
            {
                var status = tokenizer.Next(ref @in, out @out);
                switch (status)
                {
                    case Status.Ok:
                        output.Add((@out.Token, input.Substring(offset + @out.Offset, @out.Length)));
                        //offset here should be taken directly from the readable...
                        //then it's up to the supplier of the readable (ie the context) to
                        //marry both up
                        
                        //so... the readable needs to tell us its offset
                        
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
