﻿using System;
using System.Collections.Generic;
using Xunit;
using Shouldly;
using System.Runtime.InteropServices;
using System.Linq;

namespace JsonLens.Test
{
    public class TokenizerTests
    {
        List<(int, Token)> Results;
        JsonTokenizer Tokenizer;

        public TokenizerTests()
        {
            Results = new List<(int, Token)>();
            Tokenizer = new JsonTokenizer();
        }
        
        [Fact]
        public void OpeningBrace()
        {
            var result = Tokenizer.Read("{}".AsSpan()).Value;                        
            result.CharsRead.ShouldBe(1);
            result.Token.ShouldBe(Token.OpenBrace);
        }

        [Fact]
        public void OpenClosedBraces()
        {
            int i = 0;
            var span = "{}".AsZeroTerminatedSpan();

            var result1 = Tokenizer.Read(span.Slice(i)).Value;
            result1.CharsRead.ShouldBe(1);
            result1.Token.ShouldBe(Token.OpenBrace);

            var result2 = Tokenizer.Read(span.Slice(i += result1.CharsRead)).Value;
            result2.CharsRead.ShouldBe(1);
            result2.Token.ShouldBe(Token.CloseBrace);

            var result3 = Tokenizer.Read(span.Slice(i += result2.CharsRead)).Value;
            result3.CharsRead.ShouldBe(1);
            result3.Token.ShouldBe(Token.End);
        }
     
        public class WithSimpleDriver
        {
            [Fact]
            public void Empty()
                => Tokenize("")
                    .ShouldBe(new[] {
                        (Token.End, "")
                    });
            
            [Fact]
            public void OpenCloseEnd()
                => Tokenize("{}")
                    .ShouldBe(new[] {
                        (Token.OpenBrace, "{"),
                        (Token.CloseBrace, "}"),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString()
                => Tokenize("\"Hello!!!\"")
                    .ShouldBe(new[] {
                        (Token.String, "\"Hello!!!\""),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleString_WithEscapedQuote()
                => Tokenize("\"Bl\\\"ah\"")
                    .ShouldBe(new[] {
                        (Token.String, "\"Bl\\\"ah\""),
                        (Token.End, "")
                    });

            [Fact]
            public void StringColonString()
                => Tokenize("\"wibble\":\"blah\"")
                    .ShouldBe(new[] {
                        (Token.String, "\"wibble\""),
                        (Token.Colon, ":"),
                        (Token.String, "\"blah\""),
                        (Token.End, "")
                    });

            [Fact]
            public void SimpleNumber()
                => Tokenize("1234")
                    .ShouldBe(new[] {
                        (Token.Number, "1234"),
                        (Token.End, "")
                    });
        }

        static (Token, string)[] Tokenize(string input)
        {
            var tokenizer = new JsonTokenizer();

            var span = input.AsZeroTerminatedSpan();

            var results = new List<(Token, string)>();
            bool go = true;
            int i = 0;
            
            while(go)
            {
                var result = tokenizer.Read(span.Slice(i));
                if (!result.HasValue) break; //should throw error???

                switch(result.Value.Type)
                {
                    case ResultType.Underrun:
                        go = false;
                        break;

                    case ResultType.Token:
                        var token = result.Value.Token.Value;
                        var charsRead = result.Value.CharsRead;
                        results.Add((token, new string(span.Slice(i, charsRead).ToArray())));
                        i += charsRead;
                        break;
                }
            }

            return results.ToArray();
        }
    }

    public class JsonTokenizer
    {
        public Result? Read(ReadOnlySpan<char> inp)
        {
            if (inp.Length == 0)
                return ResultType.Underrun;
            else 
                return ReadEnd(inp)
                    ?? ReadSyntax(inp)
                    ?? ReadString(inp)
                    ?? ReadNumber(inp);
        }

        Result? ReadEnd(ReadOnlySpan<char> inp)
            => inp[0] == 0
                ? (Token.End, 1)
                : (Result?)null;
        
        Result? ReadSyntax(ReadOnlySpan<char> inp)
        {
            switch(inp[0])
            {
                case '{':
                    return (Token.OpenBrace, 1);

                case '}':
                    return (Token.CloseBrace, 1);

                case ':':
                    return (Token.Colon, 1);

                default:
                    return null;
            }
        }

        Result? ReadString(ReadOnlySpan<char> inp)
        {
            if (inp[0] != '"') return null;

            for (int i = 1; i < inp.Length; i++)
            {
                switch (inp[i])
                {
                    case '\\':
                        i++;
                        break;

                    case '"':
                        return (Token.String, i + 1);
                }
            }

            return ResultType.Underrun;
        }

        Result? ReadNumber(ReadOnlySpan<char> inp)
        {
            if (!IsNumeric(inp[0])) return null;

            for(int i = 0; i < inp.Length; i++) {
                if (!IsNumeric(inp[i])) {
                    return (Token.Number, i);
                }
            }

            return ResultType.Underrun;

            //how can we distinguish between a real end
            //and a simple underrun? Some kind of special character from above - a '0' maybe
            //otherwise we just need more input to decide...
            //and if we can't decide, summat should go 'pop'

            //so we'd have a 'ReadEnd' strat 
            //
            //
            //
        }

        bool IsNumeric(char c)
            => c >= 48 && c < 58;


        Result? Skip()
            => null;
                
        public struct Result
        {
            public readonly ResultType Type;
            public readonly Token? Token;
            public readonly int CharsRead;

            public Result(ResultType type, Token? token = null, int charsRead = 0)
            {
                Type = type;
                Token = token;
                CharsRead = charsRead;
            }

            public static implicit operator Result((Token, int) token)
                => new Result(ResultType.Token, token.Item1, token.Item2);

            public static implicit operator Result(ResultType result)
                => new Result(result);
        }
    }

    public enum ResultType : byte
    {
        Underrun,
        Token
    }

    public enum Token : byte
    {
        End,
        Empty,
        NewLine,
        Comment,
        OpenBrace,
        CloseBrace,
        Colon,
        String,
        Number
    }
    
}