using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

namespace JsonLens.Test
{
    using Result = ValueTuple<Status, int>;
    using Outp = CircularBuffer<Tokenizer.Emitted>;
    using Inp = Span<char>;
    
    public struct Tokenizer
    {        
        Mode _mode;
        Stack<Mode> _modes;
        int _depth;

        public Tokenizer(Mode mode = Mode.Line) {
            _mode = mode;
            _modes = new Stack<Mode>();
            _depth = 0;
        }

        public void Switch(Mode mode)
            => _mode = mode;

        public void Push(Mode mode)
            => _modes.Push(mode);

        public void Pop()
            => Switch(_modes.Pop());

        public void IncreaseDepth()
            => _depth++;

        public void DecreaseDepth()
            => _depth--;
        
        public Result Emit(ref Outp output, int chars, Token token, int offset = 0, int length = 0)
            => output.Write(new Emitted(_depth, token, offset, length))
                ? (Status.Ok, chars)
                : (Status.Underrun, 0);
        
        
        public Result Next(ref Inp inp, ref Outp outp)
        {
            if(inp.Length == 0) { //should just try reading, surely...
                return Underrun;
            }

            if(_mode != Mode.String && IsWhitespace(inp[0])) {
                return SkipWhitespace(ref inp);
            }

            //stupid edge case
            //of line feed without the carriage return
            //requires look-ahead

            char current = inp[0];

            switch(_mode)
            {
                case Mode.Line:
                    if (current == 0) {
                        Switch(Mode.End);
                        return Ok();
                    }
                    else {
                        Push(Mode.LineEnd);
                        Switch(Mode.Value);
                        return Ok();
                    }

                case Mode.LineEnd:
                    if (current == 0) {
                        Switch(Mode.End);
                        return Ok();
                    }

                    throw new NotImplementedException("Handle line break?");

                case Mode.End:
                    return End;

                case Mode.Value:
                    switch(current) {
                        case '"':
                            Switch(Mode.String);
                            return Emit(ref outp, 1, Token.String);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref inp, ref outp);

                        case '{':
                            Switch(Mode.Object1);
                            return Emit(ref o, 1, Token.Object);

                        case '[':
                            Switch(Mode.Array1);
                            return Emit(ref o, 1, Token.Array);
                    }
                    break;

                case Mode.Object1:
                    IncreaseDepth();
                    switch (current) {
                        case '}':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref o, 1, Token.ObjectEnd);

                        case '"':
                            Push(Mode.Object2);
                            Switch(Mode.String);
                            return Emit(ref o, 1, Token.String);
                    }
                    break;
                
                case Mode.Object2:
                    switch(current) {
                        case ':':
                            Push(Mode.Object3);
                            Switch(Mode.Value);
                            return Ok(1);
                    }
                    break;
                
                case Mode.Object3:
                    switch (current) {
                        case ',':
                            Switch(Mode.Object1);
                            return Ok(1);
                        case '}':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref o, 1, Token.ObjectEnd);
                    }
                    break;
                
                case Mode.Array1:
                    IncreaseDepth();
                    switch(current) {
                        case ']':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref o, 1, Token.ArrayEnd);

                        default:
                            Push(Mode.Array2);
                            Switch(Mode.Value);
                            return Ok();
                    }

                case Mode.Array2:
                    switch (current) {
                        case ']':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref o, 1, Token.ArrayEnd);

                        case ',':
                            Switch(Mode.Array1);
                            return Ok(1);
                    }
                    break;

                case Mode.String:
                    return ReadString(ref x, ref o);
            }
            
            return BadInput;
        }


        Result ReadNumber(ref Inp inp, ref Outp outp)
        {
            for(int i = 1; i < inp.Length; i++)
            {
                if (!IsNumeric(inp[i]))
                {
                    Pop();
                    return Emit(ref o, i, Token.Number);
                }
            }

            return Underrun;
        }

        Result ReadString(ref Inp inp, ref Outp o)
        {
            int i = 0;

            for (; i < inp.Length; i++)
            {
                switch (inp[i])
                {
                    case '\\':      //BUT! what about "\\", eh???
                        i++;
                        break;

                    case '"':
                        Pop();
                        return Emit(ref o, i + 1, Token.StringEnd);
                }
            }

            //x.Emit(Token.StringPart, 0, i);  //but only if i > 1...!
            return Underrun;
        }

        Result SkipWhitespace(ref Inp inp) {
            int i = 0;
                
            for (; i < inp.Length; i++)
            {
                if (!IsWhitespace(inp[i]))
                    break;
            }

            return Ok(i);
        }

        
        static bool IsNumeric(char c)
            => c >= 48 && c < 58;

        static bool IsWhitespace(char c)
            => c == ' '; //more to add!

        static Result Ok(int chars = 0)
            => (Status.Ok, chars);

        static Result Underrun
            => (Status.Underrun, 0);

        static Result End
            => (Status.End, 0);

        static Result BadInput
            => (Status.BadInput, 0);

        
        public struct Emitted
        {
            public readonly int Depth;
            public readonly Token Token;
            public readonly int Offset;
            public readonly int Length;

            public Emitted(int depth, Token token, int offset, int length)
            {
                Depth = depth;
                Token = token;
                Offset = offset;
                Length = length;
            }
        }

        public ref struct Context
        {
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
