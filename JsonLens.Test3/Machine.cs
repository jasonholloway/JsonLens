using System;
using System.Collections.Generic;
using JsonLens.Test;
using Xunit;

namespace JsonLens.Test3 {
    
    
    public enum Mode: byte {
        Start,
        ReadObject,
        BindLine,
        ReadLine,
        MatchProps,
        ReadAll
    }
    
    public struct Frame {
        public readonly Mode Type;

        public Frame(Mode type) {
            Type = type;
        }
    }


    public class Machine
    {
        Stack<Frame> _frames = new Stack<Frame>();

        public int? Next(ref CircularBuffer<Tokenizer.Emitted> @in, ref CircularBuffer<Op> ops) { //no need for circular buffer; just need read-only list thing
            var frame = _frames.Peek();

            switch (frame.Type) {
                case Mode.Start:
                    if (ops.Read(out var op)) {
                        _frames.Push(new Frame(op.Type));
                        return null;
                    }
                    return null;

                case Mode.ReadObject:
                    //we're in the mood for taking an object
                    //if what we have isn't an object (say, a number or a string) then skip it
                    throw new NotImplementedException();
                
                case Mode.MatchProps:
                    //props should be matched via a trie
                    //each prop encountered should be tested for a match
                    //problem is, how to structure program to fork
                    //easy - we always match multiple props
                    //the prefix trie yields an index, like a jump statement
                    throw new NotImplementedException();
            }
            
            throw new NotImplementedException();
        }
        
    }
    
    public struct Op {
        public readonly Mode Type;

        public Op(Mode type) {
            Type = type;
        }

        public static Op ReadAll()
            => new Op();

        public static Op ReadObject()
            => new Op();

        public static Op MatchProps(params (string, int)[] matches)
            => new Op();
    }

    
    public class MachineTests {
        
        [Fact]
        public void BlahBlah() {
            var @in = "{\"Wibble\":123}".AsZeroTerminatedSpan();
            var inBuffer = new CircularBuffer<char>(@in, 15);
            
            var machine = new Machine();
            var ops = new[] {
                Op.ReadObject(),                                  //assert '{', delegate, assert '}'
                Op.MatchProps(("Wibble", 0), ("Krrrumpt", 1)),    //read each prop, match to trie, delegate via jump
                Op.ReadObject(),
                Op.MatchProps(("Meep!", 0)),
                Op.ReadAll()
            };
            
            //what would we output here?
            //it'd be up to the machines...
            //ops shouldn't themselves have references
            //but should point to somewhere, say an id of an output buffer, or an id of a binder
            //most simply, an Op.ReadAll should copy raw string out to a contextual buffer
            //this would be our starting point
            //(we could test to make sure the right bits were being extracted)
            
            //but even in the output buffer, there is the possibility (or, the necessity)
            //of returning signals informing our host to flush the buffer before
            //we can continue
            
            //the entire program needs to be run with signals in mind
            //so, every machine, as it runs, is given an opportunity to return (nothing runs in parallel, so each and every machine can return directly to the runner)
            //the signal returned must relate to whichever buffers are being read from/into
            //ReadAll will output into a particular buffer, but maybe other ops (especially those contextually binding into an object graph)
            
            //i mean, maybe there should be a definable language between ops and runner...
            //but then the runner wouldn't be generic
            //well, it would be... kinda
            //the idea of keeping the ops non-referrin gis nice however
            //resources should be managed by the runner
            //(contextual nested binding? the binding machines would output a language to the runner, which would run constructors etc as bidden)

            machine.Next(ref @in, default);
        }
    }
    
    
    
    
    
}