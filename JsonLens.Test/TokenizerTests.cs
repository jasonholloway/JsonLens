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

        static (Token, string)[] Tokenize(string input)
        {
            var x = new TokenizerContext(input.AsZeroTerminatedSpan(), 0, Mode.Line);

            while (true)
            {
                var (status, charsRead, nextMode) = JsonTokenizer.Tokenize(ref x);

                switch (status)
                {
                    case Status.Ok:
                        x.Span = x.Span.Slice(charsRead);
                        x.Index += charsRead;
                        x.Mode = nextMode.Value;
                        break;

                    case Status.End:
                        return x.Output
                                .Select(emitted =>
                                {
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

    public static class TestCursor
    {        
        public static object Run(string input)
        {
            var x = new TokenizerContext(input.AsZeroTerminatedSpan(), 0, Mode.Line);

            while (true)
            {
                var (status, charsRead, nextMode) = JsonTokenizer.Tokenize(ref x);

                switch (status)
                {
                    case Status.Ok:
                        x.Span = x.Span.Slice(charsRead);
                        x.Index += charsRead;
                        x.Mode = nextMode.Value;
                        break;

                    case Status.End:
                        return x.Output
                                .Select(emitted =>
                                {
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


    public class CursorTests
    {
        [Fact]
        public void Blah()
        {

        }


    }




    //The Selector is itself a tree, effectively of strings
    //having its nodes as objects opens the possibility 
    //of subclassing behaviours - can you imagine, for instance
    //a wildcard? either we subclass here or we interpret instructions
    //hmm you know... we don't want to check each one individually (or do we?)
    //some kind of HashSet thing seems best here - or even better, nested Dictionaries
    //
    //but we can't just check a dict, as we may have different matching rules in place...
    //at this juncture maybe it'd be best just to do naive matching
    //in fact this is exactly (exactly!) where we wanted our fancy trie... but save that for later, eh

    public enum Selected
    {
        Select,
        Skip,
        TakeAll
    }

    public interface ISelector
    {
        (Selected, ISelector) TrySelect(string propName);

        //false above means 'skip it'
        //whereas a true means 'take it'
        //but in taking, the selector also needs to yield back a new frame
        //
        //eventually, once we get to the leaf node of a selector, TrySelect should say 'take all of this'
        //then the fsm should skip through till all is done
        //though the selector effectively the fsm here - all we do at this layer is selection
        //
        //but the skipping is a shared, stable thing
        //the deciding of where we are in the selection tree, less so
        //
        //but, based on the selector, the fsm will be pushed into one of three modes:
        //  - normal mode: asking constantly whether we should take or not,
        //  - skipping mode: don't worry till we're back at the same level as before
        //  - take mode: course through everything without querying what it is - just spew through

        //if we're doing the lens thing tho, where we wanna re-inject into a stream, the detritus isn't just to be chucked away...
        //in re-emitting, we need to write our tokens to a new buffer, tho re-emitting will be blocked until we have the replacement values to put in place
        //but if the input stream all has to be read before we can re-emit, then the input stream is going to build up
        //or rather, we can't release the buffer holding the input tokens, as we need them to re-emit em once the injection goes through

        //in the driver, input spans are only ours while we have control of the stack, how would a backlog of spans work?
        //how would we get our spans in the first place? From a buffer, evidently
        //I think the buffer would actually be ours to manage: we'd receive a stream, which we'd readasync into our own buffer
        //or at least, the buffer would be under the management of the driver.

        //and so, a kind of backlog would be acceptable, as we'd naturally be reading into our own resizable buffer.
        //we'd keep on reading the token stream, until we had no need 
        //
        //
        //



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


        public TokenizerContext TokenizerContext;
        
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

                              
    public static class JsonCursor
    {
        public static object Run(ref CursorContext x)
        {
            while (true)
            {
                var (status, charsRead, nextMode) = JsonTokenizer.Tokenize(ref x.TokenizerContext);

                switch (status)
                {
                    case Status.Ok:
                        x.Span = x.Span.Slice(charsRead);
                        x.Index += charsRead;
                        x.Mode = nextMode.Value;
                        break;

                    case Status.End:
                        return x.Output
                                .Select(emitted =>
                                {
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
