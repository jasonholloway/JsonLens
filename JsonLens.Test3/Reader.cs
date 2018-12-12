using JsonLens.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonLens.Test3
{
    using Result = ValueTuple<Status, int>;
    
    public static class Reader
    {
        public static Result Read(ref Context x)
        {
            var (status, chars, token) = Tokenizer.Read(ref x.TokenizerContext);

            switch(status)
            {
                case Status.Ok:
                    throw new NotImplementedException();

                default:
                    return (status, chars);
            }
        }
        

        public ref struct Context
        {
            public Tokenizer.Context TokenizerContext;
            public readonly object Selector;
            
            public Context(Tokenizer.Context tokenizerContext, object selector)
            {
                TokenizerContext = tokenizerContext;
                Selector = selector;
            }

        }

    }
}
