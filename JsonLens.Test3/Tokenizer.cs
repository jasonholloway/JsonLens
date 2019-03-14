using System;
using System.Collections.Generic;
using JsonLens.Test3;

namespace JsonLens.Test
{
    using Outp = Buffer<Tokenized>;
    using Inp = ReadOnlySpan<char>;
    
    public struct Tokenizer
    {        
        Mode _mode;
        Stack<Mode> _modes;

        public Tokenizer(Mode mode) {
            _mode = mode;
            _modes = new Stack<Mode>();
        }

        void Switch(Mode mode)
            => _mode = mode;

        void Push(Mode mode)
            => _modes.Push(mode);

        void Pop()
            => Switch(_modes.Pop());


        public Signal Next(ref Readable<char> @in, out Tokenized @out)
        {
            start:
            
            if(@in.IsEmpty) { //should just try reading, surely...
                return Underrun(out @out);
            }
            
            char current = @in.Peek;

            if(_mode != Mode.String && IsWhitespace(current)) {
                SkipWhitespace(ref @in);
                goto start;
            }

            //stupid edge case
            //of line feed without the carriage return
            //requires look-ahead

            switch(_mode)
            {
                case Mode.Line:
                    if (current == 0) {    //shouldn't we just, you know, return the signal here... 
                        @in.Move();
                        return End(out @out);
                    }
                    else {
                        Push(Mode.LineEnd);
                        Switch(Mode.Value);
                        goto start;
                    }

                case Mode.LineEnd:
                    if (current == 0) {
                        @in.Move();
                        return End(out @out);
                    }

                    throw new NotImplementedException("Handle line break?");

                case Mode.Value:
                    switch(current) {
                        case '"':
                            @in.Move();
                            Switch(Mode.String);
                            goto start;
                            
                        case '{':
                            @in.Move();
                            Switch(Mode.Object1);
                            return Emit(out @out, 1, 0, Token.Object);

                        case '[':
                            @in.Move();
                            Switch(Mode.Array1);
                            return Emit(out @out, 1, 0, Token.Array);
                            
                        case char c when IsNumeric(c):
                            return ReadNumber(ref @in, out @out);
                    }
                    break;

                case Mode.Object1:
                    switch (current) {
                        case '}':
                            @in.Move();
                            Pop();
                            return Emit(out @out, 1, 0, Token.ObjectEnd);

                        case '"':
                            @in.Move();
                            Push(Mode.Object2);
                            Switch(Mode.String);
                            goto start;
                    }
                    break;
                
                case Mode.Object2:
                    switch(current) {
                        case ':':
                            @in.Move();
                            Push(Mode.Object3);
                            Switch(Mode.Value);
                            goto start;
                    }
                    break;
                
                case Mode.Object3:
                    switch (current) {
                        case ',':
                            @in.Move();
                            Switch(Mode.Object1);
                            goto start;
                            
                        case '}':
                            @in.Move();
                            Pop();
                            return Emit(out @out, 1, 0, Token.ObjectEnd);
                    }
                    break;
                
                case Mode.Array1:
                    switch(current) {
                        case ']':
                            @in.Move();
                            Pop();
                            return Emit(out @out, 1, 0, Token.ArrayEnd);

                        default:
                            Push(Mode.Array2);
                            Switch(Mode.Value);
                            goto start;
                    }

                case Mode.Array2:
                    switch (current) {
                        case ']':
                            @in.Move();
                            Pop();
                            return Emit(out @out, 1, 0, Token.ArrayEnd);

                        case ',':
                            @in.Move();
                            Switch(Mode.Array1);
                            goto start;
                    }
                    break;

                case Mode.String:
                    return ReadString(ref @in, out @out);
            }
            
            return BadInput(out @out);
        }


        Signal ReadNumber(ref Readable<char> @in, out Tokenized @out) {
            int start = @in.Offset;
            @in.Move();

            for(; !@in.AtEnd; @in.Move())
            {
                if (!IsNumeric(@in.Peek)) {
                    Pop();
                    int len = @in.Offset - start;
                    return Emit(out @out, start, len, Token.Number);
                }
            }

            return Underrun(out @out);
        }

        Signal ReadString(ref Readable<char> @in, out Tokenized @out) {
            int start = @in.Offset;

            for (; !@in.AtEnd; @in.Move()) {
                switch (@in.Peek)
                {
                    //below should do another move(), but checking to see if empty...
                    case '\\':      //BUT! what about "\\", eh??? need to lookahead to know what to do
                        @in.Move(); //which we can't do, as we might be at end of buffer
                        break;      //if we're at end, then, what? it's like we want to look enter a special mode when we get here
                                    //or - maybe we have to emit a StringPart and start again
                    case '"':
                        int length = @in.Offset - start;
                        @in.Move();
                        Pop();
                        return Emit(out @out, start, length, Token.String);
                }
            }
            
            //x.Emit(Token.StringPart, 0, i);  //but only if i > 1...!
            return Underrun(out @out);
        }

        void SkipWhitespace(ref Readable<char> @in) {
            int i = 0;
            var data = @in.Data;

            for (; i < data.Length; i++) {
                if (!IsWhitespace(data[i]))
                    break;
            }

            @in.Move(i);
        }

        
        static bool IsNumeric(char c)
            => c >= 48 && c < 58;

        static bool IsWhitespace(char c)
            => c == ' '; //more to add!

//        Result Ok(out Tokenized @out, int chars = 0) {
//            _offset += chars;
//            @out = default; //but there needs to be some way of signalling that there's no token returned... BUT why are we even yielding, if there's no token, and no signal?
//            return (Status.Ok, chars);
//        }

        Signal Emit(out Tokenized @out, int before, int length, Token token) {
            @out = new Tokenized(before, length, token);
            return Signal.Ok;
        }

        static Signal Underrun(out Tokenized @out)
            => Return(out @out, Signal.Underrun);

        static Signal End(out Tokenized @out)
            => Return(out @out, Signal.End);

        static Signal BadInput(out Tokenized @out)
            => Return(out @out, Signal.BadInput);

        static Signal Return(out Tokenized @out, Signal status) {
            @out = default;
            return status;
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
            Object2,
            Object3,
            Array2,
        }
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
    
    public struct Tokenized
    {
        public readonly int Offset;
        public readonly Token Token;
        public readonly int Length;

        public Tokenized(int offset, int length, Token token)
        {
            Offset = offset;
            Length = length;
            Token = token;
        }
    }

}
