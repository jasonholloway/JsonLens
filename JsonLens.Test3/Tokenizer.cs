using System;
using System.Collections.Generic;

namespace JsonLens.Test
{
    using Result = ValueTuple<Status, int, (Token, int, int)?>;
    
    public static class Tokenizer
    {        
        public static Result Next(ref Context x)
        {
            if(x.Span.Length == 0) { //should just try reading, surely...
                return Underrun;
            }

            if(x.Mode != Mode.String && IsWhitespace(x.Span[0])) {
                return SkipWhitespace(ref x);
            }

            //stupid edge case
            //of line feed without the carriage return
            //requires look-ahead

            switch(x.Mode)
            {
                case Mode.Line:
                    if (x.Current == 0) {
                        x.Switch(Mode.End);
                        return Ok();
                    }
                    else {
                        x.Push(Mode.LineEnd);
                        x.Switch(Mode.Value);
                        return Ok();
                    }

                case Mode.LineEnd:
                    if (x.Current == 0) {
                        x.Switch(Mode.End);
                        return Ok();
                    }

                    throw new NotImplementedException("Handle line break?");

                case Mode.End:
                    return End;

                case Mode.Value:
                    switch(x.Current) {
                        case '"':
                            x.Switch(Mode.String);
                            return Ok(1, Token.String);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref x);

                        case '{':
                            x.Switch(Mode.Object);
                            return Ok(1, Token.Object);

                        case '[':
                            x.Switch(Mode.Array);
                            return Ok(1, Token.Array);
                    }
                    break;

                case Mode.Object:
                    switch (x.Current) {
                        case '}':
                            x.Pop();
                            return Ok(1, Token.ObjectEnd);

                        case '"':
                            x.Push(Mode.ObjectSeparator);
                            x.Switch(Mode.String);
                            return Ok(1, Token.String);
                    }
                    break;
                
                case Mode.ObjectSeparator:
                    switch(x.Current) {
                        case ':':
                            x.Push(Mode.Object);
                            x.Switch(Mode.Value);
                            return Ok(1);
                    }
                    break;

                case Mode.Array:
                    switch(x.Current) {
                        case ']':
                            x.Pop();
                            return Ok(1, Token.ArrayEnd);

                        default:
                            x.Push(Mode.ArrayTail);
                            x.Switch(Mode.Value);
                            return Ok();
                    }

                case Mode.ArrayTail:
                    switch (x.Current) {
                        case ']':
                            x.Pop();
                            return Ok(1, Token.ArrayEnd);

                        case ',':
                            x.Switch(Mode.Array);
                            return Ok(1);
                    }
                    break;

                case Mode.String:
                    return ReadString(ref x);
            }
            
            return BadInput;
        }


        static Result ReadNumber(ref Context x)
        {
            for(int i = 1; i < x.Span.Length; i++)
            {
                if (!IsNumeric(x.Span[i]))
                {
                    x.Pop();
                    return Ok(i, (Token.Number, 0, i));
                }
            }

            return Underrun;
        }

        static Result ReadString(ref Context x)
        {
            int i = 0;

            for (; i < x.Span.Length; i++)
            {
                switch (x.Span[i])
                {
                    case '\\':      //BUT! what about "\\", eh???
                        i++;
                        break;

                    case '"':
                        x.Pop();
                        return Ok(i + 1, (Token.StringEnd, 0, i));
                }
            }

            //x.Emit(Token.StringPart, 0, i);  //but only if i > 1...!
            return Underrun;
        }

        static Result SkipWhitespace(ref Context x)
        {
            int i = 0;

            for (; i < x.Span.Length; i++)
            {
                if (!IsWhitespace(x.Span[i]))
                    break;
            }

            return Ok(i);
        }

        
        static bool IsNumeric(char c)
            => c >= 48 && c < 58;

        static bool IsWhitespace(char c)
            => c == ' '; //more to add!

        
        static Result Ok(Token token)
            => Ok((token, 0, 0));

        static Result Ok((Token, int, int) token)
            => Ok(0, token);
        
        static Result Ok(int chars = 0, (Token, int, int)? token = null)
            => (Status.Ok, chars, token);

        static Result Ok(int chars, Token token)
            => Ok(chars, (token, 0, 0));
        

        static Result Underrun
            => (Status.Underrun, 0, null);

        static Result End
            => (Status.End, 0, null);

        static Result BadInput
            => (Status.BadInput, 0, null);


        public ref struct Context
        {
            public ReadOnlySpan<char> Span;
            public Stack<Mode> ModeStack;
            public List<(Token, (int, int))> Output;
            public Mode Mode;

            public Context(ReadOnlySpan<char> span)
            {
                Span = span;
                ModeStack = new Stack<Mode>();
                Output = new List<(Token, (int, int))>();
                Mode = Mode.Line;
            }

            public char Current => Span[0];


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

            public void Switch(Mode mode)
                => Mode = mode;

            public void Push(Mode mode)
                => ModeStack.Push(mode);

            public void Pop()
                => Switch(ModeStack.Pop());

        }

        public enum Mode : byte
        {
            Line,
            Object,
            Array,
            Value,
            String,
            ObjectKey,
            ObjectValue,
            Number,
            LineEnd,
            End,
            ObjectSeparator,
            ArrayTail
        }
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
        End,
        Comment,
        Object,
        ObjectEnd,
        String,
        StringPart,
        StringEnd,
        Number,
        Array,
        ArrayEnd,
        True,
        False,
        Undefined,
        Null
    }

}
