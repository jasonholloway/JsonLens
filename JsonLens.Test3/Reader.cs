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
        public static Result Next(ref Context x)
        {
            switch(x.Selector)
            {
                case AllSelector s:
                    throw new NotImplementedException();

                case ObjectSelector s:
                    throw new NotImplementedException();

                case PropSelector s:
                    throw new NotImplementedException();

                case ValueSelector s:
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
            public Selector Selector;
            
            public Context(Tokenizer.Context tokenizerContext, Selector selector)
            {
                TokenizerContext = tokenizerContext;
                Selector = selector;
            }

        }

    }
}
