using JsonLens.Test;
using System;

namespace JsonLens.Test3
{
    using Result = ValueTuple<Signal, int, Tokenized?>;
    using Input = Buffer<object>;
    using Output = Buffer<object>;
    
    public struct Reader
    {
        public enum Mode
        {
            Seek,
            SeekProps,
            Read,
            Skip
        }
        
        public SelectNode Select;
        
        Mode _mode;
        int _depth;
        int _moveTill;
        
        public Reader(SelectNode select)
        {
            _mode = Mode.Seek;
            _depth = 0;
            _moveTill = 0;
            Select = select;
        }
               
        public Result Next(ref Input @in, ref Output @out)
        {
            switch(_mode)
            {
                case Mode.SeekProps:
                    //so now I need to delegate to something to be able to match prop names to follow-on strategies
                    //I need a bag of properties!
                    throw new NotImplementedException();

                case Mode.Seek:
                    switch (Select.Match)
                    {
                        case Match.None:
                            _mode = Mode.Skip;
                            _moveTill = _depth - 1;
                            return Ok();

                        case Match.Any:
                            _mode = Mode.Read;
                            _moveTill = _depth - 1;
                            return Ok();

                        case Match.Object:
                            var (status, chars, emit) = ReadNext(ref @in, ref @out);
                            //and now our depth has changed, even without us having committed to reading anything! BEWARE!
                            
                            if (status == Signal.Ok && emit.HasValue) 
                            {
                                var token = emit.Value.Token;

                                if (token == Token.Object)
                                {
                                    _mode = Mode.Seek;
                                    
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
                    if(_depth != _moveTill) {
                        return SuppressNext(ref @in, ref @out);    //drive locally: no data to accumulate, just cursor to increment
                    }
                    else {
                        _mode = Mode.Seek;
                        return Ok();
                    }

                case Mode.Read:
                    if(_depth != _moveTill) {
                        return ReadNext(ref @in, ref @out);
                    }
                    else {
                        _mode = Mode.Seek;
                        return Ok();
                    }

                default:
                    throw new Exception("Strange Reader.Mode encountered!");
            }
            
            //BEWARE state changing in the event of an underrun - that'll need to change
            //******************************
        }

        Result SuppressNext(ref Input @in, ref Output @out)
        {
            var (status, chars, _) = ReadNext(ref @in, ref @out);
            return (status, chars, null);
        }

        Result ReadNext(ref Input @in, ref Output @out)
        {
            throw new NotImplementedException();
            
            
//            var (status, chars) = Tokenizer.Next(ref x.TokenizerContext);
//
//            if(status == Status.Ok) { 
////                var token = emit.Value.Token;
////                _depth += GetDepthChange(token);
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
            => (Signal.Ok, 0, null);



        public ref struct Context
        {

        }

    }
}
