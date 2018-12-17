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
                            //Enter skip mode till... when? Till we've popped out of our current depth; then we go back to seeking
                            x.Mode = Mode.Skip;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.All:
                            //like skip, we have to put place a condition for popping out
                            x.Mode = Mode.Read;
                            x.MoveTill = x.Depth - 1;
                            return Ok();

                        case Match.Object:
                            //we're after an object mate: read forwards till we find it OR we know we've failed in our search
                            throw new NotImplementedException();

                        case Match.Prop:
                            throw new NotImplementedException();

                        case Match.Value:
                            throw new NotImplementedException();
                    }
                    throw new NotImplementedException();

                case Mode.Skip:
                    if(x.Depth != x.MoveTill) {
                        return SuppressNextToken(ref x);    //drive locally: no data to accumulate, just cursor to increment
                    }
                    else {
                        x.Mode = Mode.Seek;
                        return Ok();
                    }

                case Mode.Read:
                    //pass through the results of the tokenizer to the driver, please
                    //but only if we're still in-depth
                    throw new NotImplementedException();

                default:
                    throw new Exception("Strange Reader.Mode encountered!");
            }
            
            //BEWARE state changing in the event of an underrun - that'll need to change
            //******************************
        }

        static Result SuppressNextToken(ref Context x)
        {
            var (status, chars, _) = NextToken(ref x);
            return (status, chars, null);
        }

        static Result NextToken(ref Context x)
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
