using System;
using System.Collections.Generic;

namespace JsonLens.Test
{
    using Result = ValueTuple<Status, int, Tokenizer.Emit?>;
    
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
                        return Ok;
                    }
                    else {
                        x.Push(Mode.LineEnd);
                        x.Switch(Mode.Value);
                        return Ok;
                    }

                case Mode.LineEnd:
                    if (x.Current == 0) {
                        x.Switch(Mode.End);
                        return Ok;
                    }

                    throw new NotImplementedException("Handle line break?");

                case Mode.End:
                    return End;

                case Mode.Value:
                    switch(x.Current) {
                        case '"':
                            x.Switch(Mode.String);
                            return x.Emit(1, Token.String);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref x);

                        case '{':
                            x.Switch(Mode.Object1);
                            return x.Emit(1, Token.Object);

                        case '[':
                            x.Switch(Mode.Array);
                            return x.Emit(1, Token.Array);
                    }
                    break;

                case Mode.Object1:
                    switch (x.Current) {
                        case '}':
                            x.Pop();
                            return x.Emit(1, Token.ObjectEnd);

                        case '"':
                            x.Push(Mode.Object2);
                            x.Switch(Mode.String);
                            return x.Emit(1, Token.String);
                    }
                    break;
                
                case Mode.Object2:
                    switch(x.Current) {
                        case ':':
                            x.Push(Mode.Object3);
                            x.Switch(Mode.Value);
                            return x.Emit(1);
                    }
                    break;
                
                case Mode.Object3:
                    switch (x.Current) {
                        case ',':
                            x.Switch(Mode.Object1);
                            return x.Emit(1);
                        case '}':
                            x.Pop();
                            return x.Emit(1, Token.ObjectEnd);
                    }
                    break;
                
                case Mode.Array:
                    switch(x.Current) {
                        case ']':
                            x.Pop();
                            return x.Emit(1, Token.ArrayEnd);

                        default:
                            x.Push(Mode.ArrayTail);
                            x.Switch(Mode.Value);
                            return Ok;
                    }

                case Mode.ArrayTail:
                    switch (x.Current) {
                        case ']':
                            x.Pop();
                            return x.Emit(1, Token.ArrayEnd);

                        case ',':
                            x.Switch(Mode.Array);
                            return x.Emit(1);
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
                    return x.Emit(i, new Emit(0, Token.Number, 0, i));
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
                        return x.Emit(i + 1, new Emit(0, Token.StringEnd, 0, i));
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

            return x.Emit(i);
        }

        
        static bool IsNumeric(char c)
            => c >= 48 && c < 58;

        static bool IsWhitespace(char c)
            => c == ' '; //more to add!

        static Result Ok
            => (Status.Ok, 0, null);

        static Result Underrun
            => (Status.Underrun, 0, null);

        static Result End
            => (Status.End, 0, null);

        static Result BadInput
            => (Status.BadInput, 0, null);

        public struct Emit
        {
            public readonly int Depth;
            public readonly Token Token;
            public readonly int Offset;
            public readonly int Length;

            public Emit(int depth, Token token, int offset, int length)
            {
                Depth = depth;
                Token = token;
                Offset = offset;
                Length = length;
            }
        }

        public ref struct Context
        {
            public ReadOnlySpan<char> Span;
            public Stack<Mode> ModeStack;
            public Mode Mode;
            public int Depth;

            public Context(ReadOnlySpan<char> span)
            {
                Span = span;
                ModeStack = new Stack<Mode>();
                Mode = Mode.Line;
                Depth = 0;
            }

            public char Current => Span[0];

            public void Switch(Mode mode)
                => Mode = mode;

            public void Push(Mode mode)
                => ModeStack.Push(mode);

            public void Pop()
                => Switch(ModeStack.Pop());

            public Result Emit(Token token)
                => Emit(new Emit(0, token, 0, 0));

            public Result Emit(Emit emit)
                => Emit(0, emit);
            
            public Result Emit(int chars = 0, Emit? emit = null)
                => (Status.Ok, chars, emit);

            public Result Emit(int chars, Token token)
                => Emit(chars, new Emit(0, token, 0, 0));
        }

        public enum Mode : byte
        {
            Line,
            Object1,
            Array,
            Value,
            String,
            LineEnd,
            End,
            Object2,
            Object3,
            ArrayTail,
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
        Null,
        Nothing
    }

}
