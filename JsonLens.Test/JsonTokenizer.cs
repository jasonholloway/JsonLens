using System;
using System.Collections.Generic;

namespace JsonLens.Test
{
    using Result = ValueTuple<Status, int, Mode?>;


    public class JsonTokenizer
    {
        public Result Tokenize(ref Context x)
        {
            if(x.Span.Length == 0) { //should just try reading, surely...
                return Underrun;
            }

            if(x.Span[0] == 0) {    //but file-end makes no sense in the middle of most yieldings...
                x.Emit(Token.End);  //it only makes sense at the end of a line
                return End;
            }

            switch(x.Mode)
            {
                case Mode.Line:
                    x.Emit(Token.NewLine);
                    x.Push(Mode.LineEnd);
                    return Ok(Mode.Value);

                case Mode.LineEnd:
                    throw new NotImplementedException("HELLO!!!!!");


                case Mode.Value:
                    switch(x.Span[0]) {
                        case '"':
                            x.Emit(Token.String);
                            return Ok(1, Mode.String);

                        case '{':
                            x.Emit(Token.Object);
                            return Ok(1, Mode.Object);

                        case char c when c > 0:
                            return ReadNumber(ref x);
                    }
                    break;

                case Mode.Object:
                    switch(x.Span[0]) {
                        case '"':
                            x.Emit(Token.Prop);
                            return Ok(Mode.Prop);

                        case '}':
                            x.Emit(Token.ObjectEnd);
                            return Ok(1, x.Pop());
                    }
                    break;
                
                case Mode.Prop:
                    return Ok(Mode.String);

                case Mode.ObjectValue:
                    return Ok(Mode.Value);

                case Mode.Array:
                    break;

                case Mode.String:
                    return ReadString(ref x);
            }

            return BadInput;
        }


        Result ReadNumber(ref Context x)
        {
            for(int i = 1; i < x.Span.Length; i++)
            {
                if (!IsNumeric(x.Span[i]))
                {
                    x.Emit(Token.Number);
                    return Ok(i, x.Pop());
                }
            }

            return Underrun;
        }

        Result ReadString(ref Context x)
        {
            int i = 0;

            for (i = 1; i < x.Span.Length; i++)
            {
                switch (x.Span[i])
                {
                    case '\\':      //BUT! what about "\\", eh???
                        continue;

                    case '"':
                        x.Emit(Token.StringChunk);
                        return Ok(i, x.Pop());
                }
            }

            x.Emit(Token.StringChunk);  //but only if i > 1...!
            return Underrun;
        }

        
        bool IsNumeric(char c)
            => c >= 48 && c < 58;



        Result Ok(Mode next)
            => (Status.Ok, 0, next);

        Result Ok(int charsRead, Mode next)
            => (Status.Ok, charsRead, next);

        Result Underrun
            => (Status.Underrun, 0, null);

        Result End
            => (Status.End, 0, null);

        Result BadInput
            => (Status.BadInput, 0, null);
    }


    public ref struct Context
    {
        public ReadOnlySpan<char> Span;
        public int Index;
        public Stack<Mode> ModeStack;
        public List<(Token, (int, int)?)> Output;
        public Mode Mode;

        public Context(ReadOnlySpan<char> span, int index, Mode mode)
        {
            Span = span;
            Index = index;
            ModeStack = new Stack<Mode>();
            Output = new List<(Token, (int, int)?)>();
            Mode = mode;
        }

        public Status? Check(char c)
        {
            return null;
        }

        public Status? CheckForUnderrun()
            => null;

        public Status? NextIsNumeric()
        {
            return null;
        }

        public void Skip(int count)
        {
            Index += count;
        }

        public void Push(Mode mode)
            => ModeStack.Push(mode);

        public Mode Pop()
            => ModeStack.Pop();


        public void Emit(Token token)
            => Output.Add((token, null));

        public void Emit(Token token, int from, int to)
            => Output.Add((token, (from, to)));

    }


    public enum Status : byte
    {
        Ok,
        Underrun,
        End,
        BadInput
    }

    public enum Token : byte
    {
        None,
        End,
        Empty,
        NewLine,
        Comment,
        Object,
        ObjectEnd,
        Colon,
        String,
        StringChunk,
        StringEnd,
        Number,
        Prop,
        Key
    }

    public enum Mode : byte
    {
        None,
        Line,
        Object,
        Array,
        Value,
        String,
        Prop,
        ObjectValue,
        Number,
        LineEnd
    }

    public enum ModeAction : byte
    {
        None,
        Switch,
        Push,
        Pop
    }




    //can't just put together different sub-parsings here
    //we have to have different modes then
    //which feels - complicated

    //but if we're going to have everything inline, then we can't nicely yield...
    //so we need different modes
    //KeyValue mode
    //String mode

    //
    //line
    //  object
    //    key
    //      string
    //        chunk
    //    value
    //      number

    //but we can't 'orchestrate' two modes two follow one another
    //as we have to plop back to the same context
    //we wanna set the mode separately from the stack
    //so... when we've parsed a key, then the handler will set the mode to 'value'
    //and after 'value', we want to pop back to where we were previously
    //so like a little subsystem of states

    //the advantage in all this is the yielding which allows us to resume midstream

    //otherwise we could just compose expectations within functions
    //but then the AOP-style concerns have to be managed within app code
    //at the call places, dispersed
    //or - as we know - we'd need a more fancy way of gluing computations together

    //but we don't have that... not easily, and not without lambdas and suchlike.
    //if we wanted that kind of niceness, it'd be most snazzy to have some interpretable structure
    //which would allow us to piece shit together
    //static structures that can be piced togther...

    //and so we have another potential output from the handler
    //PushMode
    //PopMode
    //SetMode
    //None

    //PushMode would set the mode and push the mode
    //so at the point of entering the mode 
    //we will be expected to know the semantics of return
    //an object isn't just a transient mode, but a lingering context

    //so when we find an object,
    //we need to say we're now in object mode
    //though really now we're in key mode
    //and then we'll be in value mode
    //and then key mode or popped back out

    //when we are in value mode (and only value mode?)
    //then we need to push to the stack
    //otherwise there's a clear 
    //so with every granule, we can emit a token, switch a mode, push a mode, or pop a mode
    //and we also want to state the number of characters read
    //and whether we've had an underrun or not

    //---------------------------------------------------

    //so we're recreating the stack, and a kind of dispatch, instead of just using the facilities of the language
    //and that's all so we can yield
    //
    //
    //


}
