using System;
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


        public static object Run(ref CursorContext x)
        {
            var y = x.TokenizerContext;
            var input = "BlahBlahBlah";

            var output = new List<(Token, string)>();

            while (true)
            {
                var (status, chars, emitted) = Tokenizer.Next(ref x.TokenizerContext);

                switch (status)
                {
                    case Status.Ok:
                        if (emitted.HasValue)
                        {
                            var (token, from, to) = emitted.Value;
                            output.Add((token, input.Substring(from, to)));
                        }

                        y.Span = y.Span.Slice(chars);
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



        static (Token, string)[] Tokenize(string input)
        {
            var output = new List<(Token, string)>();
            int offset = 0;

            var x = new Tokenizer.Context(
                            input.AsZeroTerminatedSpan(),
                            Tokenizer.Mode.Line);

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


    
    public enum Selected
    {
        Select,
        Skip,
        TakeAll
    }

    public interface ISelector
    {
        (Selected, ISelector) TrySelect(string propName);

    }




    public class Driver
    {        
        public async Task Drive(Stream input, Stream output)
        {
            var buffer = new MemoryStream();
            await input.CopyToAsync(buffer);
            


            var reader = new StreamReader(input);

            void boo()
            {
                Span<char> c;                
            }

            var writer = new StreamWriter(output);
     
            //we need our own buffer if we're doing look-ahead kinda stuff
            //well, if we're doing any kind of injection...
            //in our actual use case, we want to slurp up everything before we re-emit
            //so the buffer will be filled up, and then splunked out only when the overall handler has finished

            //the (token) buffer will be splunked out when we re-serialize
            //BUT IT IS A TOKEN BUFFER!
            
            //but tokens refer to the underlying input stream...
            //
            //
            //
            


        }

    }



    public ref struct CursorContext
    {
        public object Selector;


        public Tokenizer.Context TokenizerContext;
        
    }

    //so this down here is a FSM on top of a FSM
    //could obvs be flattened into one... but at the cost of complication yup
    //and so, instead...
    
    //if the tokenizer returns underrun, we too need to return underrun
    //the driver is always above...
    //so this means there'll be many sites where exactly this return value must be checked for REMORSELESSLY
    
    //the cursor has a filter tree, and based on the make-up of this tree, it proceeds through its modes
    //at anyone time, it isn't just seeking a single value, but always a set of them
    //some kind of trie would be the fastest thing, but we can more simply just do complete scans 
    //te trie could be compiled once on construction
    //or rather, we'd have nested matcher tries
    //for each property encountered, the trie would have to be consulted
    
    //a trie could be encoded in an array of nodes linked by reference, which'd be, like, very nice, obvs
    //or we could even use a pre-made one, or something. But inside the trie, we'd have to have nested dictionaries, wouldn't we?
    //more daft ideas: a bloom filter for each character
    //or, with no hashing, an 8-byte bitmap at each trie node - though in branching out, this will get unwieldy very quickly indeed
    //and such a bitmap won't tell us anything more than 'yes' and 'no' - we need to also link to another map elsewhere; each bit needs to have
    //its own link...
    
    //how about... a flat list of characters, coupled with offsets to jump, to find the next node
    //but not always simply characters... could also be lengths of strings, especially if there were no contention...
    //but again, it'd be simplest FOR NOW just to have a Dictionary of all strings, and compare all prop names as they come in (involving, of course mucho copying)
    
    //so the cursor would course through properties, testing each one in turn
    //there'd be a mode to skip an entire value...
    //when there were no matches in the tree for the prop encountered, we'd go into Skip mode: in this mode we'd scan through tokens, yielding at each point to the
    //upper driver; and we'd keep track of depth in our context

    //when we find a match, we step into the tree for the duration of this branch
    //when we get to a leaf of the match tree, do we take the entire protruding branch of the JSON? I'd say yes - we should yield it up above ourselves,
    //for we are only a filter. Some reifier thing should then deal with it.
    
    //it's like there shouldn't just be exclusive matchers, but entire positive Takers, for lopping off and returning entire branches.
    //all these intermediate filters are really just Takers on long necks. We can also think of each Taker as being a JsonLens component,
    //one of many combined into one.
    
    //but if they're all merged into one structure in the trie, then they are no longer separate components! maybe this is what combining JsonLens's amounts to - 
    //the promiscuous compiling of all of theminto a single shared trie
    //
    //but the trie just says 'yes' and excludes by implication
    //if there isn't an entry for an encountered prop, then we don't want it; unless at the end of the trie there is a definite YES inscribed
    //a prop part in the trie is like a partial YES - or rather, a YES, BUT...
    //at the end of each, its true, we need a positive YES to actually do the take

    //and in taking, what we are actually doing is yielding up - there's no boxing or reifying of the units we choose; instead we return them one by one
    //to the downstream reifier, which is actually above us - it's then up to the reifier to actually do something with the values it is given
    
    //a good first step at this point would be to form the selectors - they're absolutely needed to progress here; there's nothing for us to do otherwise
    //
    //so with these in place, we'd then be either seeking or purposely skipping - these are the two modes; though in moving about, we'll be traversing multiple tokens
    //at once, in single handlings. yield points will be at the boundaries of returned contextualized values. Though! we'll also need to yield in the middle of skippings.
    //we don't need to emit of course; we only need to return a Status...
    //



    //but the filter shouldn't just remove, and let through what we know: 
    //the underlying stream of tokens needs to be preserved, injected into
    //
    //but at the same time, the filter *should* just let through what the binder needs
    //so the raw tokens should be decanted before the filter; but the binder doesn't just read, it re-inserts
    //like every bound site needs a handle to be the locus of injection, if needed. 
    //
    //but then such handles need to be managed so they stick around. If we wanted this to be fast and super efficient,
    //then we would get a map, a list of coordinates, of where we needed to exchange bits. But we do have precisely this!
    //This is exactly what spans are... though they also refer to the underlying memory, and as such have peculiar semantics
    //
    //We'd buffer, and re-emit, till we came across a Span<Token> that hadn't yet been approved, either as pass-through,
    //or as exchanged. The tokens need to be temporarily stored in a buffer, as we need arbitrary look ahead in the replacing of them - therefore
    //we can't just rely on the easy efficiency of percolation. Each token emitted should be stored in a buffer of tokens - but could that buffer also store
    //spans? 

    //The initial source stream can serve us back a nice Span, that will give us in-scope access to its innards, to its interior buffer
    //but as the actual handling of the deserialized, partially-bound mass will be done in asynchronous continuations and allsorts like it,
    //the spans can't be passed around. If we had a handle on the underlying memory... but there's nothing to stop the input... STOP! The memory is entirely OURS.
    //
    //Streams don't expose their own memory: they're just coroutiney things for shovelling back data via the stack. We need a buffer into which to shovel...
    //of course, if we are actually served a string or ready-made buffer, a shortcut could be taken at that point.
    //

    //
    //
    //
    //


    public static class JsonFilter
    {

    }

                              
    public static class JsonCursor
    {
        public static object Run(ref CursorContext x)
        {
            var y = x.TokenizerContext;
            var input = "BlahBlahBlah";

            var output = new List<(Token, string)>();

            while (true)
            {
                var (status, chars, emitted) = Tokenizer.Next(ref x.TokenizerContext);

                switch (status)
                {
                    case Status.Ok:
                        if (emitted.HasValue)
                        {
                            var (token, from, to) = emitted.Value;
                            output.Add((token, input.Substring(from, to)));
                        }

                        y.Span = y.Span.Slice(chars);
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
