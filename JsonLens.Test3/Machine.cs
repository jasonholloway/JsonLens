using System;
using System.Collections.Generic;
using System.Linq;
using JsonLens.Test;
using Xunit;

namespace JsonLens.Test3 
{
    
    public enum Mode: byte {
        Dump,
        ReadObject,
        MatchProps,
        ReadAll
    }
    
    public enum Signal {
        End,
        Underrun
    }

    public enum Stream {
        Ops,
        In,
        Tokens,
        Out1,
        Out2
    }

    public class Machine 
    {
        Stack<Op> _ops = new Stack<Op>();
        Tokenizer _tokenizer = new Tokenizer(Tokenizer.Mode.Line);

        public (Signal, Stream) Next(ref Readable<Op> ops, ref Readable<char> @in, ref Buffer<Tokenized> tokens, ref Buffer<char> @out)
        {
            if (!ops.Read(out var op)) return (Signal.Underrun, Stream.Ops);

            switch (op.Type) {
                case Mode.Dump:
                    //dumping just reads out entirely to the output
                    //but, for this, we need to know the scope of the input to read from
                    //ie, ReadObject will be handled, and then we may want to read everything in it till the end
                    //well, we've been here already: we read ahead till the depth comes back down to our level
                    
                    //question again of how to take tokens from tokenizer;
                    //the problem of where to buffer the bloody things...
                    //instead of having a fixed buffer everywhere, it makes more sense to have one-by-one yielding, each one handled here
                    //but, if we want to skip, then the tokenizer should be able to skip to the proper part
                    //in this case, the tokenizer itself doesn't have to yield depths...
                    //it can just track thm itself
                    //but it does yield them
                    
                    //so, how can we progress with the buffer in place?
                    //we'd have a Readable of tokens, not managed as part of the machine here, but separately
                    //there'd then be an ulterior machine, that fielded signals from the Reader and Binder
                    //the Reader would tokenize into a buffer; the Binder would do whatever it bloody well liked
                    //but at the cost of copying into intermediate memory repeatedly
                    
                    //otherwise, the tokenizer lives here and yields each token upwards to be immediately handled
                    //so, for each token read there'd be a check, and the Tokenizer would move forwards, always updating its state
                    //and the token would be immediately returned to update the state of our thingy here
                    
                    //in the case of skipping, the tokenizer can go into a more efficient mode that just skims till it finds the requisite depth returned
                    //which sounds nice doesn't it
                    //a big stateful structure of machines that proceeds as it reads
                    
                    
                    throw new NotImplementedException();

                case Mode.ReadObject:
                    _tokenizer.Next(@in, default);
                    
                    //so the machine itself has its buffer of tokens
                    //which are written into here
                    //ReadObject reads tokens into its buffer,forever...
                    
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
    
    public struct Op
    {
        public readonly Mode Type;
        public readonly int SinkId;

        public Op(Mode type, int sinkId) {
            Type = type;
            SinkId = sinkId;
        }

        public static Op DumpTo(int sinkId)
            => new Op(Mode.Dump, sinkId);

        public static Op ReadAll()
            => new Op(Mode.ReadAll, 0);

        public static Op ReadObject()
            => new Op(Mode.ReadObject, 0);

        public static Op MatchProps(params (string, int)[] matches)
            => new Op(Mode.MatchProps, 0);
    }
    
    
    public class MachineTests
    {
        [Fact]
        public void BlahBlah() 
        {
            var @in = new Readable<char>(
                "{\"Wibble\":123}".AsZeroTerminatedSpan()
            );
            
            var ops = new Readable<Op>(new[] {
                Op.ReadObject(),
                Op.DumpTo(0)
            });
            
            var tokenData = new Tokenized[16];
            var tokens = new Buffer<Tokenized>(tokenData, 15);

            var charData = new char[16];
            var @out = new Buffer<char>(charData, 15);
            
            var machine = new Machine();
            
            Write(ref tokens,
                (0,  0, 0, Token.Object),
                (1,  2, 6, Token.String),
                (1, 10, 3, Token.Number),
                (0, 13, 1, Token.ObjectEnd),
                (0,  0, 0, Token.End));

            while (true) {
                var (signal, bufferTag) 
                    = machine.Next(ref ops, ref @in, ref tokens, ref @out);
                
                switch (signal) {
                    case Signal.End:
                        return;
                    
                    case Signal.Underrun:
                        switch (bufferTag) {
                            case Stream.Ops:
                                //load more ops here (should just be simple readable interface)
                                throw new NotImplementedException();
                            
                            case Stream.In:
                                //read more chars into buffer and carry on
                                //and if we have to wait, then... we can wait at this top level
                                throw new NotImplementedException();
                            
                            case Stream.Tokens:
                                //token underrun shouldn't be handled here, like
                                throw new NotImplementedException();
                            
                            case Stream.Out1:
                                //this is the first output buffer
                                //how would this level cope with this? higher level contexts should bind to this
                                throw new NotImplementedException();
                            
                            case Stream.Out2:
                                //second output buffer
                                throw new NotImplementedException();
                           
                            default:
                                throw new NotImplementedException();
                        }
                }
            }
        }
        
        static void Write<T>(ref Buffer<T> buff, params T[] vals) {
            foreach (var v in vals) {
                Assert.True(buff.Write(v));
            }
        }

        static void Write(ref Buffer<Tokenized> buff, params (int depth, int offset, int length, Token token)[] tokens)
            => Write(ref buff, tokens.Select(t => 
                                            new Tokenized(t.depth, t.offset, t.length, t.token)
                                        ).ToArray());
        
        
    }
    
}
