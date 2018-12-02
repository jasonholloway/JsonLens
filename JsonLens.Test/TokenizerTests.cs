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
                        (Token.Object, "{"),
                        (Token.ObjectEnd, "}"),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString()
                => Tokenize("\"Hello!!!\"")
                    .ShouldBe(new[] {
                        (Token.String, "Hello!!!"),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString_WithEscapedQuote()
                => Tokenize("\"Bl\\\"ah\"")
                    .ShouldBe(new[] {
                        (Token.String, "Bl\\\"ah"),
                        (Token.End, "")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Tokenize("{\"wibble\":\"blah\"}")
                    .ShouldBe(new[] {
                        (Token.Object, "{"),
                        (Token.Prop, ""),
                        (Token.String, "wibble"),
                        (Token.String, "blah"),
                        (Token.ObjectEnd, "}"),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleNumber()
                => Tokenize("1234")
                    .ShouldBe(new[] {
                        (Token.Number, "1234"),
                        (Token.End, "")
                    });
        }

        static (Token, string)[] Tokenize(string input)
        {
            var tokenizer = new JsonTokenizer();
            var x = new Context(input.AsZeroTerminatedSpan(), 0, Mode.Line);
            
            while (true)
            {
                var (status, charsRead, nextMode) = tokenizer.Tokenize(ref x);       
                
                switch(status)
                {
                    case Status.Ok:
                        x.Span = x.Span.Slice(charsRead);
                        x.Mode = nextMode.Value;
                        break;

                    case Status.End:
                        return x.Output
                                .Select(o => (o.Item1, ""))
                                .ToArray();

                    case Status.Underrun:
                        throw new NotImplementedException("UNDERRUN to do!");

                    case Status.BadInput:
                        throw new NotImplementedException("BADINPUT to do!");
                }
            }

        }
    }
    

    public class ContextTests
    {
        //[Fact]
        //public void StateMutates()
        //{
        //    var context = new Context("hello".AsSpan());
        //    Blah(ref context);
        //    Blah(ref context);
        //    context.Index.ShouldBe(4);

        //    void Blah(ref Context x)
        //        => x.Emit(Token.Colon, 2);
        //}

        [Fact]
        public void NullFallback_WorksAsExpected()
        {
            var r = Ok() ?? OhDear();
            r.ShouldBe(Status.Ok);
            
            Status? Ok()
                => Status.Ok;

            Status? OhDear()
                => throw new Exception("bugger");
        }

        [Fact]
        public void BoolFallback_SurprisinglyAlsoWorks()
        {
            var r = Ok() || OhDear();
            r.ShouldBe(true);

            bool Ok()
                => true;

            bool OhDear()
                => throw new Exception("bugger");
        }
    }

}
