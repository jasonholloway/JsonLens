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

            if(x.Mode != Mode.String && IsWhitespace(x.Span[0])) {
                return SkipWhitespace(ref x);
            }

            //stupid edge case
            //of line feed without the carriage return
            //requires look-ahead

            switch(x.Mode)
            {
                case Mode.Line:
                    if(x.Span[0] == 0)
                        return Ok(Mode.End);
                    else {
                        x.Emit(Token.Line);
                        x.Push(Mode.LineEnd);
                        return Ok(Mode.Value);
                    }

                case Mode.LineEnd:
                    if (x.Span[0] == 0)
                        return Ok(Mode.End);

                    throw new NotImplementedException("Handle line break?");

                case Mode.End:
                    x.Emit(Token.End);
                    return End;

                case Mode.Value:
                    switch(x.Span[0]) {
                        case '"':
                            x.Emit(Token.String);
                            return Ok(1, Mode.String);

                        case char c when IsNumeric(c):
                            return ReadNumber(ref x);

                        case '{':
                            x.Emit(Token.Object);
                            return Ok(1, Mode.Object);

                        case '[':
                            x.Emit(Token.Array);
                            return Ok(1, Mode.Array);
                    }
                    break;

                case Mode.Object:
                    switch (x.Span[0]) {
                        case '}':
                            x.Emit(Token.ObjectEnd);
                            return Ok(1, x.Pop());

                        case '"':
                            x.Emit(Token.String);
                            x.Push(Mode.ObjectSeparator);
                            return Ok(1, Mode.String);
                    }
                    break;
                
                case Mode.ObjectSeparator:
                    switch(x.Span[0]) {
                        case ':':
                            x.Push(Mode.Object);
                            return Ok(1, Mode.Value);
                    }
                    break;

                case Mode.Array:
                    switch(x.Span[0]) {
                        case ']':
                            x.Emit(Token.ArrayEnd);
                            return Ok(1, x.Pop());

                        default:
                            x.Push(Mode.ArrayTail);
                            return Ok(Mode.Value);
                    }

                case Mode.ArrayTail:
                    switch (x.Span[0]) {
                        case ']':
                            x.Emit(Token.ArrayEnd);
                            return Ok(1, x.Pop());

                        case ',':
                            return Ok(1, Mode.Array);
                    }
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
                    x.Emit(Token.Number, 0, i);
                    return Ok(i, x.Pop());
                }
            }

            return Underrun;
        }

        Result ReadString(ref Context x)
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
                        x.Emit(Token.StringEnd, 0, i);
                        return Ok(i + 1, x.Pop());
                }
            }

            x.Emit(Token.StringPart, 0, i);  //but only if i > 1...!
            return Underrun;
        }

        Result SkipWhitespace(ref Context x)
        {
            int i = 0;

            for (; i < x.Span.Length; i++)
            {
                if (!IsWhitespace(x.Span[i]))
                    break;
            }

            return Ok(i, x.Mode);
        }

        
        bool IsNumeric(char c)
            => c >= 48 && c < 58;

        bool IsWhitespace(char c)
            => c == ' '; //more to add!


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
        public List<(Token, (int, int))> Output;
        public Mode Mode;

        public Context(ReadOnlySpan<char> span, int index, Mode mode)
        {
            Span = span;
            Index = index;
            ModeStack = new Stack<Mode>();
            Output = new List<(Token, (int, int))>();
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
            => Output.Add((token, (Index, 0)));

        public void Emit(Token token, int from, int len)
            => Output.Add((token, (Index + from, len)));

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
        Line,
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
