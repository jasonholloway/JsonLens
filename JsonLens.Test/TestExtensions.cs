using System;
using System.Linq;

namespace JsonLens.Test
{
    public static class TestExtensions
    {
        public static ReadOnlySpan<char> AsSpan(this string str)
            => str.AsMemory().Slice(0).Span;

        public static ReadOnlySpan<char> AsZeroTerminatedSpan(this string str)
            => Enumerable.Concat(str, new[] { (char)0 })
                .ToArray().AsMemory().Span;
    }
}
