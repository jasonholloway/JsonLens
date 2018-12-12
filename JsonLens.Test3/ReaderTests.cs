using System;
using Xunit;
using Shouldly;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JsonLens.Test3;

namespace JsonLens.Test
{
    public class ReaderTests
    {
        public class SelectAll
        {
            [Fact]
            public void SimpleString_SelectAll()
                => Read("\"Hello!!!\"", null)
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "Hello!!!"),
                        (Token.End, "")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Read("{\"wibble\":\"blah\"}", null)
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
        }

        static (Token, string)[] Read(string json, object selector)
        {
            var x = new Reader.Context(
                        new Tokenizer.Context(json.AsZeroTerminatedSpan(), Mode.Line), 
                        selector);
            
            while(true)
            {
                var (status, chars) = Reader.Read(ref x);
             
                switch(status)
                {
                    case Status.Ok:
                        throw new NotImplementedException();

                    case Status.Underrun:
                        throw new NotImplementedException("UNDERRUN");

                    case Status.BadInput:
                        throw new NotImplementedException("BADINPUT");

                    case Status.End:
                        throw new NotImplementedException();
                }
            }
        }

    }

}
