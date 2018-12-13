using System;
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
            public void SimpleString_SelectAll()
                => Read("\"Hello!!!\"", Select.All)
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "Hello!!!")
                    });

            [Fact]
            public void ObjectWithProperty()
                => Read("{\"wibble\":\"blah\"}", Select.All)
                    .ShouldBe(new[] {
                        (Token.Line, ""),
                        (Token.Object, ""),
                        (Token.String, ""),
                        (Token.StringEnd, "wibble"),
                        (Token.String, ""),
                        (Token.StringEnd, "blah"),
                        (Token.ObjectEnd, "")
                    });
        }



        public class SelectProp
        {
            [Fact]
            public void ObjectProp()
                => Read("{\"hello\":123}", Select.Value.Object.Prop("hello").All)
                    .ShouldBe(new[] {
                        (Token.Number, "123")
                    });
        }



        static (Token, string)[] Read(string json, Selector selector)
        {
            var x = new Reader.Context(
                        new Tokenizer.Context(json.AsZeroTerminatedSpan(), Mode.Line), 
                        selector.GetRoot());

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



        public static class Select
        {
            public static AllSelector All
                => new AllSelector(null);

            public static ValueSelector Value
                => new ValueSelector(null);
        }

        
        public abstract class Selector
        {
            Selector _parent;
            List<Selector> _children = new List<Selector>();

            public Selector(Selector parent)
            {
                _parent = parent;
            }

            internal IEnumerable<Selector> Children => _children;

            protected S Add<S>(S child) where S : Selector
            {
                _children.Add(child);
                return child;
            }

            internal Selector GetRoot()
                => _parent != null
                    ? _parent.GetRoot()
                    : this;

            public AllSelector All
                => Add(new AllSelector(this));
        }



        public class ValueSelector : Selector
        {
            public ValueSelector(Selector parent) : base(parent)
            { }

            public ObjectSelector Object
                => Add(new ObjectSelector(this));
        }

        public class ObjectSelector : Selector
        {
            public ObjectSelector(Selector parent) : base(parent)
            { }

            public PropSelector Prop(string name)
                => Add(new PropSelector(this, name));
        }

        public class PropSelector : Selector
        {
            public readonly string Name;

            public PropSelector(Selector parent, string name) : base(parent)
            {
                Name = name;
            }
        }

        public class AllSelector : Selector
        {
            public AllSelector(Selector parent) : base(parent)
            { }
        }


    }

}
