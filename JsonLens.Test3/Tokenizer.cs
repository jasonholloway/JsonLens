using System;
using System.Collections.Generic;
using JsonLens.Test3;

namespace JsonLens.Test
{
    using Result = ValueTuple<Status, int>;
    using Outp = CircularBuffer<Tokenizer.Emitted>;
    using Inp = ReadOnlySpan<char>;
    
    public struct Tokenizer
    {        
        Mode _mode;
        Stack<Mode> _modes;
        int _depth;
        int _offset;

        public Tokenizer(Mode mode) {
            _mode = mode;
            _modes = new Stack<Mode>();
            _depth = 0;
            _offset = 0;
        }

        void Switch(Mode mode)
            => _mode = mode;

        void Push(Mode mode)
            => _modes.Push(mode);

        void Pop()
            => Switch(_modes.Pop());

        void IncreaseDepth()
            => _depth++;

        void DecreaseDepth()
            => _depth--;

        Result Emit(ref Outp output, int before, int length, Token token, int after = 0)
            => output.Write(new Emitted(_depth, before, length, token))
                ? Ok(before + length + after)
                : Underrun;
        
        
        public Result Next(ref Inp @in, ref Outp @out)
        {
            if(@in.Length == 0) { //should just try reading, surely...
                return Underrun;
            }

            if(_mode != Mode.String && IsWhitespace(@in[0])) {
                return SkipWhitespace(ref @in);
            }

            //stupid edge case
            //of line feed without the carriage return
            //requires look-ahead

            char current = @in[0];

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
                            return Ok(1);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref @in, ref @out);

                        case '{':
                            Switch(Mode.Object1);
                            return Emit(ref @out, 1, 0, Token.Object);

                        case '[':
                            Switch(Mode.Array1);
                            return Emit(ref @out, 1, 0, Token.Array);
                    }
                    break;

                case Mode.Object1:
                    IncreaseDepth();
                    switch (current) {
                        case '}':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref @out, 1, 0, Token.ObjectEnd);

                        case '"':
                            Push(Mode.Object2);
                            Switch(Mode.String);
                            return Ok(1);
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
                            return Emit(ref @out, 1, 0, Token.ObjectEnd);
                    }
                    break;
                
                case Mode.Array1:
                    IncreaseDepth();
                    switch(current) {
                        case ']':
                            Pop();
                            DecreaseDepth();
                            return Emit(ref @out, 1, 0, Token.ArrayEnd);

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
                            return Emit(ref @out, 1, 0, Token.ArrayEnd);

                        case ',':
                            Switch(Mode.Array1);
                            return Ok(1);
                    }
                    break;

                case Mode.String:
                    return ReadString(ref @in, ref @out);
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
                    return Emit(ref outp, 0, i, Token.Number);
                }
            }

            return Underrun;
        }

        Result ReadString(ref Inp inp, ref Outp outp)
        {
            int i = 0;

            for (; i < inp.Length; i++)
            {
                switch (inp[i])
                {
                    case '\\':      //BUT! what about "\\", eh??? need to lookahead to know what to do
                        i++;        //which we can't do, as we might be at end of buffer
                        break;      //if we're at end, then, what? it's like we want to look enter a special mode when we get here
                                    //or - maybe we have to emit a StringPart and start again
                    case '"':
                        Pop();
                        return Emit(ref outp, 0, i, Token.String, 1);
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

        Result Ok(int chars = 0) {
            _offset += chars;
            return (Status.Ok, chars);
        }

        static Result Underrun
            => (Status.Underrun, 0);

        static Result End
            => (Status.End, 0);

        static Result BadInput
            => (Status.BadInput, 0);

        
        public struct Emitted
        {
            public readonly int Depth;
            public readonly int Offset;
            public readonly Token Token;
            public readonly int Length;

            public Emitted(int depth, int offset, int length, Token token)
            {
                Depth = depth;
                Offset = offset;
                Length = length;
                Token = token;
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
