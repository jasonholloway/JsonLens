using JsonLens.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JsonLens.Test3
{
    //a reader just tokenizes, filters and proffers its residue
    
    //but - what will the reader fill the receiving buffer with? the actual textual data lives of course in the source buffer
    //if we can't return spans...
    //yes, its like we don't want data at rest here - we want a callback to be called for blocks of tokens - but if we're dealing in blocks,
    //we already have the problem of referring to the hot source: if we drive from below, then we have to be calling something to handle our data,
    //
    //the only alternative to this is for the consumer to be in control
    //well, that's what the reader interface does of course - it lets the consumer yield to the producer long enough to fill up its brandished bucket
    //to avoid the world of buffers (to remain as lean as possible!) we expect there to be more chattiness, but less memory usage
    //this means we can't supply the traditional reader interface (though it'd be nice to...)
    //in fact, no, again its actually impossible, as writing to a buffer means we lose control of our spans, unless they were stack allocated
    //
    //the whole span thing means the entire stack has to proceed by state machines directly
    //there's a question whether this gives better performance, as the constant calling incurs overhead
    //
    //the reader, too, has to proceed by state machine
    //we can offer a generic reader, but this means we have to call a callback, with some presumable access to state... though this is no great problem -
    //the stack will be ours
    //
    //a generic interface would have to be an IEnumerable, albeit an asynchronous one
    //the language doesn't have it... a callback offering a value then has no explicit guarantees of sequential execution, but it amounts to the same thing in practice
    //for our own uses, it'd be better though to have a bespoke driver in such cases, with composition of less generic functions (and therefore more fitted interfaces) 

    //so... why are we even making this class??? just to fitin with the common reader pattern...


    public class JsonReader
    {
        readonly Stream _input;

        public JsonReader(Stream input)
        {
            _input = input;
        }

        public int ReadAsync(Action<Token> callback)
        {
            


            var r = new StringReader("");

            return 6;
        }


    }
}
