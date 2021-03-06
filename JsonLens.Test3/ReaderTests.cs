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
                        (Token.String, "Hello!!!")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Read("{\"wibble\":\"blah\"}", Select.Any)
                    .ShouldBe(new[] {
                        (Token.Object, ""),
                        (Token.String, "wibble"),
                        (Token.String, "blah"),
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
                        (Token.Nothing, ""),
                        (Token.ObjectEnd, "")
                    });

        }



        static (Token, string)[] Read(string json, Selector selector) {
            
            Span<object> inputData = new object[16];
            var @in = new Buffer<object>(inputData, 15);
            
            Span<object> outputData = new object[16];
            var @out = new Buffer<object>(outputData, 15);
            
            var reader = new Reader(selector.GetSelectTree());

            var output = new List<(Token, string)>();
            int index = 0;
                                   
            while(true)
            {
                var (status, chars, emit) = reader.Next(ref @in, ref @out);
             
                switch(status)
                {
                    case Signal.Ok:
                        if(emit.HasValue) {
                            var e = emit.Value;
                            output.Add((e.Token, json.Substring(index + e.Offset, e.Length)));
                        }
                        
                        //@in = @in.Slice(chars);
                        index += chars;
                        break;

                    case Signal.End:
                        return output.ToArray();

                    case Signal.Underrun:
                        throw new NotImplementedException("UNDERRUN");

                    case Signal.BadInput:
                        throw new NotImplementedException("BADINPUT");

                }
            }
        }

    }

}
