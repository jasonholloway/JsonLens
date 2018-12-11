using JsonLens.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonLens.Test3
{
    using Result = ValueTuple<Status>;
    
    public static class FilteredReader
    {

        public static Result Read(ref Context x)
        {
            var (status, chars) = Tokenizer.Read(ref x.TokenizerContext);
            
            throw new NotImplementedException();
        }





        public ref struct Context
        {
            public Tokenizer.Context TokenizerContext;
        }

    }
}
