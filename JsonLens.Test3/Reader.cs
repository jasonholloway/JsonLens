using JsonLens.Test;
using System;
using System.Collections.Generic;
using System.Text;
using static JsonLens.Test.ReaderTests;

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
                            return (Status.Ok, 0, null);

                        case Match.All:
                            //like skip, we have to put place a condition for popping out
                            x.Mode = Mode.Read;
                            return (Status.Ok, 0, null);

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
                    throw new NotImplementedException();

                case Mode.Read:
                    throw new NotImplementedException();
            }



            //if we're told to let all through, we should do, up till we escape the current json scope
            //so, we need the measure of depth: then we know to skip till our depth pops us back out - a v. fast manoevre.
            //
            //if we're looking for an object, we should either take an object if it is here, or we should skip as above
            //
            //the act of taking involves changing our context and trampolining out and back in again
            //(or does it?) we have to return bad statuses if we find them
            //BEWARE state changing in the event of an underrun - that'll need to change
            //

            return Tokenizer.Next(ref x.TokenizerContext);

            //var (status, chars, emitted) = Tokenizer.Next(ref x.TokenizerContext);

            //switch(status)
            //{
            //    case Status.Ok:
            //        return (status, chars, emitted);

            //    default:
            //        throw new NotImplementedException();
            //}
        }
        

        public ref struct Context
        {
            public Tokenizer.Context TokenizerContext;                       
            public Mode Mode;
            public SelectNode Select;
            
            public Context(Tokenizer.Context tokenizerContext, SelectNode select)
            {
                TokenizerContext = tokenizerContext;
                Select = select;
                Mode = Mode.Seek;
            }

        }

    }
}
