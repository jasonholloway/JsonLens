﻿using System;
using Xunit;
using Shouldly;
using JsonLens.Test3;
using System.Collections.Generic;
using System.Linq;

namespace JsonLens.Test
{
    public class ReaderTests
    {
        public class SelectAll
        {
            [Fact]
            public void SimpleString()
                => Read("\"Hello!!!\"", Select.Any)
                    .ShouldBe(new[] {
                        (Token.String, ""),
                        (Token.StringEnd, "Hello!!!")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Read("{\"wibble\":\"blah\"}", Select.Any)
                    .ShouldBe(new[] {
                        (Token.Object, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "wibble"),
                        (Token.String, ""),
                        (Token.StringEnd, "blah"),
                        (Token.ObjectEnd, "")
                    });
        }

        public class SelectNone
        {
            [Fact]
            public void SimpleString()
                => Read("\"wibble\"", Select.None)
                    .ShouldBeEmpty();
        }
               
        public class Objects
        {
            [Fact]
            public void MatchesProp()
                => Read("{\"hello\":123}", Select.Object.Prop("hello").All)
                    .ShouldBe(new[] {
                        (Token.Object, ""),
                        (Token.String, "hello"),
                        (Token.StringEnd, ""),
                        (Token.Number, "123"),
                        (Token.ObjectEnd, "")
                    });

            [Fact]
            public void DoesntMatchProp()
                => Read("{\"hello\":123}", Select.Object.Prop("nope").All)
                    .ShouldBe(new[] {
                        (Token.Object, ""),
                        (Token.ObjectEnd, "")
                    });

            [Fact]
            public void MatchesPropButNotValue()
                => Read("{\"hello\":123}", Select.Object.Prop("hello").None)
                    .ShouldBe(new[] {
                        (Token.Object, ""),
                        (Token.String, "hello"),
                        (Token.StringEnd, ""),
                        (Token.Nothing, ""),
                        (Token.ObjectEnd, "")
                    });

        }



        static (Token, string)[] Read(string json, Selector selector)
        {
            var x = new Reader.Context(
                        new Tokenizer.Context(json.AsZeroTerminatedSpan()), 
                        selector.GetSelectTree());

            var output = new List<(Token, string)>();
            int index = 0;
                                   
            while(true)
            {
                var (status, chars, emitted) = Reader.Next(ref x);
             
                switch(status)
                {
                    case Status.Ok:
                        if(emitted.HasValue) {
                            var (token, offset, length) = emitted.Value;
                            output.Add((token, json.Substring(index + offset, length)));
                        }

                        x.TokenizerContext.Span = x.TokenizerContext.Span.Slice(chars);
                        index += chars;
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
