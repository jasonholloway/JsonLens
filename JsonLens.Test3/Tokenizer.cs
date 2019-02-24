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
                            return x.Emit(1, Token.String);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref x);

                        case '{':
                            x.Switch(Mode.Object1);
                            return x.Emit(1, Token.Object);

                        case '[':
                            x.Switch(Mode.Array1);
                            return x.Emit(1, Token.Array);
                    }
                    break;

                case Mode.Object1:
                    x.IncreaseDepth();
                    switch (x.Current) {
                        case '}':
                            x.Pop();
                            x.DecreaseDepth();
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
                            return Ok(1);
                    }
                    break;
                
                case Mode.Object3:
                    switch (x.Current) {
                        case ',':
                            x.Switch(Mode.Object1);
                            return Ok(1);
                        case '}':
                            x.Pop();
                            x.DecreaseDepth();
                            return x.Emit(1, Token.ObjectEnd);
                    }
                    break;
                
                case Mode.Array1:
                    x.IncreaseDepth();
                    switch(x.Current) {
                        case ']':
                            x.Pop();
                            x.DecreaseDepth();
                            return x.Emit(1, Token.ArrayEnd);

                        default:
                            x.Push(Mode.Array2);
                            x.Switch(Mode.Value);
                            return Ok();
                    }

                case Mode.Array2:
                    switch (x.Current) {
                        case ']':
                            x.Pop();
                            x.DecreaseDepth();
                            return x.Emit(1, Token.ArrayEnd);

                        case ',':
                            x.Switch(Mode.Array1);
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
                    return x.Emit(i, Token.Number, 0, i);
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
                        return x.Emit(i + 1, Token.StringEnd, 0, i);
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

        static Result Ok(int chars = 0)
            => (Status.Ok, chars, null);

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
            public Mode Mode;
            
            Stack<Mode> _modes;
            int _depth;

            public Context(ReadOnlySpan<char> span)
            {
                Span = span;
                _modes = new Stack<Mode>();
                Mode = Mode.Line;
                _depth = 0;
            }

            public char Current => Span[0];

            public void Switch(Mode mode)
                => Mode = mode;

            public void Push(Mode mode)
                => _modes.Push(mode);

            public void Pop()
                => Switch(_modes.Pop());

            public void IncreaseDepth()
                => _depth++;

            public void DecreaseDepth()
                => _depth--;
            
            public Result Emit(int chars, Token token, int offset = 0, int length = 0)
                => (Status.Ok, chars, new Emit(_depth, token, offset, length));
        }

        public enum Mode : byte
        {
            Line,
            Object1,
            Array1,
            Value,
            String,
            LineEnd,
            End,
            Object2,
            Object3,
            Array2,
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
