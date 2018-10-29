using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace JsonLens.Test
{
    public class ObjectSelector : Selector
    {
        public readonly Selector Inner;

        public ObjectSelector(Selector inner)
        {
            Inner = inner;
        }
    }

    public class MemberSelector : Selector
    {
        public readonly string MemberName;
        public readonly Selector Inner;

        public MemberSelector(string memberName, Selector inner = null)
        {
            MemberName = memberName;
            Inner = inner;
        }
    }
    
    public class MultiSelector : Selector
    {
        public readonly Selector[] Selectors;

        public MultiSelector(Selector[] selectors)
        {
            Selectors = selectors;
        }
    }
    
    public abstract class Selector { }


    public static class SelectorExtensions
    {
        public static JsonLens<T> Compile<T>(this Selector selector)
            => new JsonLensCompiler().Compile<T>(selector);
    }
    

    public class JsonLensCompiler
    {
        public JsonLens<T> Compile<T>(Selector selector)
            => new JsonLens<T>(inp => {
                var interpretor = new JsonLensInterpretor();
                return interpretor.Interpret<T>(selector, inp);
            });
    }

    public class JsonLensInterpretor
    {
        //in the first place, we can interpret into a tree of dictionaries
        //though the goal is of course to fill up an object graph

        //using Json.NET more completely, we'd use its own resources analyse objects
        //in fact - that's exactly what we want - we want to recognise the attributes etcof Json.NET
        //so that the models are infact the same
        //so, in interpreting, the type must itself be broken down into a plan
        //both the type and the selector are trees of value
        //then we must map between them

        //the lens takes from one place
        //and commits to another

        //in interpreting, we read through a stream of JsonTokens, presumably
        //the first opening brace, each subsequent leaf of the tree is to be interpreted
        //we have to read each bit to understand each subsequent part

        //but we don't want to body each one up into an object
        //we only care about what we want:
        //at one point, if we know the current branch is unneeded by our selectors,
        //all we care about is the end of the branch: nothing needs committing - we are in a special parsing mode

        //each time we encounter a member, we need to look up in our table of members
        //if the member exists, we proceed in one mode, if its doesn't, we proceed cheaply in another
        //either way, the current value needs to be passed around, even if we don't wish to box it for permanence
        //we need an emit() function,beyond which our code does not care

        //in emitting, our new token is put in a buffer - or, even better, it is passed to a subsequent stage of interpretation
        //the bits of memory summoned are all the small state of the layered parsers
        
        //for initial cheapness, we can use JsonReader from Json.NET - this tokenises for us, and lets us read till we find what we
        //want. The program of parsing is determined by the tree of selectors, as well as by the input string.

        //but - in interpreting, we don't take a single string: instead we delegate to the underneath layer to read more
        //what if the underneath layer might take some time to read? then the read becomes asynchronous - but we don't want to
        //use wasteful tasks in all cases

        //the state of a parser unit should then be stored, not just on the stack, but within an object
        //instead of the simple call to the underlying parsing unit, we want to save our current state, and expect further
        //punctual impetuses - an actor system in fact.

        //the parser part then always receives a token, passed into it by some mediating mechanism
        //emitting too goes via the mediator

        //and so, if we want a stream of the most basic tokens, we can co-opt the JsonReader.


        public struct JsonToken { }

        public class Binder
        {
            Action<string> _emit;

            public Binder(Action<string> emit)
            {
                _emit = emit;
            }

            public void Receive(JsonToken token)
            {
                _emit("blah");
            }
        }

        //but binding isn't an act of interpretation...
        //first we have a stream of characters
        //which become syntactical tokens
        //which become objects and members and values
        //
        //but this last stage isn't so much of a stream
        //as it is an expanding tree of branches
        //this articulated structure is necessary to do *any* matching against a tree of selectors
        //but we don't need to store this structure on the heap - it should exist trasniently

        //so much for gradual parsing - but what of compilation? Given a selection tree, we always have certain
        //possibilities of parsing. Parsing units are going to effectively be state machines: but we can code these directly without dynamic recompilation.
        //what will be dynamic is the matching with the selection tree. 
        //compilation would allow a simple sequential processing with minimal heap usage

        //so, after the token stage, the thought is that we can no longer just emit tokens more rarified
        //we wanna send out a single tree. But nesting is not needed: the same can be expressed streamlike...
        //instead of returning a fully-nested tree, we want to instead filter the input stream of tokens
        //so that the output stream is only that required to bind given our selectors

        //once we have the filtered stream,
        //then we can use it fill our boots
        //as a final binding step

        //we want batch processing also
        //the mediator can go through one by one
        //or - it can also be the case that each processing stage, instead of receiving single tokens,
        //can receive and process a number of them before yielding
        
        //but if one can't fully process the tokens it has received before more come through, that is fine too - as the current
        //state will be saved, and work will restart once more inputs have been fed in

        //the output stream of tokens, very much like selectors, will be gradually read and applied
        //firstly to a free tree of hashes and values, then to typed objects
        
        //a single selector could perhaps yield a single value, contextualised. 

        //lenses can also write, of course - not just one-way traffic. How can we write to JSON? By reading and re-emitting, presumably.
        //the JSON tokens can be nicely re-rendered, no problem at all. But the selectors are then no longer filters of tokens: they can become
        //modifiers. Filtering and emplacement are two modes of selectors, then. The selector is combined with an action - either a filter or 
        //an emplacer. Emplacers could also modify based on various plucked values, delegating to a supplied callback.
 
        //But if so, how would the scope of such variations be determined. Presumably, given ndjson, we would have many values
        //winging through. We would want to transduce over a certain span of elements.
        //The situation would however be identical given an intended repeated operation over an array of values, sat within a single
        //line of json: the boundary needn't be on the line. It's also possible to imagine coursing through all properties of an object graph.

        //we therefore want to expose various yield points: if we want to apply an action per line, then fine - per property encountered, fine also.
        //after the stream of tokens, we get to the articulation. Bodying into objects is one articulation. First we would articulate into lines, however.
        
        //each line would be a grouping of tokens, each of which being interpretable as whatever you like.
        //but - this scheme would happily separate the binding/articulation from the token processing.

        //token filtering, modification, could be done at the token stage.
        
        public T Interpret<T>(Selector selector, string inp)
        {
            switch (selector)
            {
                case ObjectSelector obj:
                    throw new NotImplementedException();

                case MemberSelector member:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }
    }



    public class JsonLens<T> : IJsonLens
    {
        Func<string, T> _fn;

        internal JsonLens(Func<string, T> fn)
        {
            _fn = fn;
        }

        public T Read(string raw)
        {
            return _fn(raw);
        }
    }
       

    public interface IJsonLens {
        
    }


    public class Tests
    {
        static string _rawJson = @"{ ""name"": ""Humphrey"", ""species"": ""fish"" }";
        
        [Fact]
        public void CanSelectByMember()
        {
            var lens = new ObjectSelector(
                                new MemberSelector("species")
                            ).Compile<string>();
            
            lens.Read(_rawJson).ShouldBe("fish");
        }
        
    }
}
