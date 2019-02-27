using JsonLens.Test;
using System;

namespace JsonLens.Test3
{
    using Result = ValueTuple<Status, int, Tokenizer.Emitted?>;
    
    public static class Reader
    {
        public enum Mode
        {
            Seek,
            SeekProps,
            Read,
            Skip
        }
               
        public static Result Next(ref Context x)
        {
            switch(x.Mode)
            {
                case Mode.SeekProps:
                    //so now I need to delegate to something to be able to match prop names to follow-on strategies
                    //I need a bag of properties!




                    throw new NotImplementedException();

                case Mode.Seek:
                    switch (x.Select.Match)
                    {
                        case Match.None:
                            x.Mode = Mode.Skip;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.Any:
                            x.Mode = Mode.Read;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.Object:
                            var (status, chars, emit) = ReadNext(ref x);
                            //and now our depth has changed, even without us having committed to reading anything! BEWARE!
                            
                            if (status == Status.Ok && emit.HasValue) 
                            {
                                var token = emit.Value.Token;

                                if (token == Token.Object)
                                {
                                    x.Mode = Mode.Seek;
                                    
                                    //so, now we look for Props...
                                    //this is indeed a separate reading mode
                                    //SeekProps
                                    //and SeekProps requires a compiled map of possible properties, with sub-strategies hanging off em
                                    //so then such seeking is also a kind of strategy - strategies are shared between modes and matchers, then

                                    return (status, chars, emit);
                                }
                                else
                                {
                                    //if it's not an object, skip it!
                                    //but should we return Nothing?
                                    //should have a precise test for this
                                }

                                throw new NotImplementedException();
                            }

                            return (status, chars, emit);
                            
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
            throw new NotImplementedException();
            
            
//            var (status, chars) = Tokenizer.Next(ref x.TokenizerContext);
//
//            if(status == Status.Ok) { 
////                var token = emit.Value.Token;
////                x.Depth += GetDepthChange(token);
//            }
//
//            return (status, chars, default);
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
