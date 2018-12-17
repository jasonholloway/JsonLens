using JsonLens.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonLens.Test3
{
    using Result = ValueTuple<Status, int, (Token, int, int)?>;
    
    public static class Reader
    {
        public enum Mode
        {
            Seek,
            Read,
            Skip
        }
               
        public static Result Next(ref Context x)
        {
            switch(x.Mode)
            {
                case Mode.Seek:
                    switch (x.Select.Strategy)
                    {
                        case Match.None:
                            x.Mode = Mode.Skip;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.All:
                            x.Mode = Mode.Read;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.Object:
                            var (status, chars, emitted) = ReadNext(ref x);
                            //and now our depth has changed, even without us having committed to reading anything! BEWARE!
                            
                            if (status == Status.Ok && emitted.HasValue) 
                            {
                                var (token, _, _) = emitted.Value;

                                if (token == Token.Object)
                                {
                                    x.Mode = Mode.Seek;

                                    //having matched on Object opener
                                    //now we go back to seeking
                                    //but we, as readers, are in 'Object' mode, in as much as we are now looking at prop names
                                    //maybe this is in fact a distinct reading mode...
                                    //we've detected an object, and now we're looking at a PropName
                                    //if we like the PropName, then we can maybe seek on the PropValue
                                    //...

                                    return (status, chars, emitted);
                                }
                                else
                                {
                                    //if it's not an object, skip it!
                                    //but should we return Nothing?
                                    //should have a precise test for this
                                }

                                throw new NotImplementedException();
                            }

                            return (status, chars, emitted);
                            
                        case Match.Prop:
                            throw new NotImplementedException();
                    }
                    throw new NotImplementedException();

                case Mode.Skip:
                    if(x.Depth != x.MoveTill) {
                        return SuppressNext(ref x);    //drive locally: no data to accumulate, just cursor to increment
                    }
                    else {
                        x.Mode = Mode.Seek;
                        return Ok();
                    }

                case Mode.Read:
                    if(x.Depth != x.MoveTill) {
                        return ReadNext(ref x);
                    }
                    else {
                        x.Mode = Mode.Seek;
                        return Ok();
                    }

                default:
                    throw new Exception("Strange Reader.Mode encountered!");
            }
            
            //BEWARE state changing in the event of an underrun - that'll need to change
            //******************************
        }

        static Result SuppressNext(ref Context x)
        {
            var (status, chars, _) = ReadNext(ref x);
            return (status, chars, null);
        }

        static Result ReadNext(ref Context x)
        {
            var (status, chars, emitted) = Tokenizer.Next(ref x.TokenizerContext);

            if(status == Status.Ok && emitted.HasValue) { 
                var (token, _, _) = emitted.Value;
                x.Depth += GetDepthChange(token);
            }

            return (status, chars, emitted);
        }

        static int GetDepthChange(Token token)
        {
            switch(token)
            {
                case Token.Object:
                    return 1;
                case Token.ObjectEnd:
                    return -1;

                case Token.Array:
                    return 1;
                case Token.ArrayEnd:
                    return -1;

                default:
                    return 0;
            }
        }

        static Result Ok()
            => (Status.Ok, 0, null);



        public ref struct Context
        {
            public Tokenizer.Context TokenizerContext;                       
            public Mode Mode;
            public SelectNode Select;
            public int Depth;
            public int MoveTill;
            
            public Context(Tokenizer.Context tokenizerContext, SelectNode select)
            {
                TokenizerContext = tokenizerContext;
                Select = select;
                Mode = Mode.Seek;
                Depth = 0;
                MoveTill = 0;
            }

        }

    }
}
