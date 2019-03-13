using System;

namespace JsonLens.Test 
{
    public ref struct Readable<T> 
    {
        ReadOnlySpan<T> _data;
        int _offset;

        public Readable(ReadOnlySpan<T> data, int start = 0) {
            _data = data;
            _offset = start;
        }

        public bool IsEmpty 
            => _data.IsEmpty;

        public int Offset
            => _offset;

        public ReadOnlySpan<T> Data
            => _data;
        
        public bool Read(out T v) {
            if (IsEmpty) {
                v = default(T);
                return false;
            }

            v = _data[0];
            _data = _data.Slice(1);
            
            return true;
        }

        public void Move(int i = 1) {
            _offset += i;
        }

        public T Peek => _data[_offset];
        public bool AtEnd => _offset >= _data.Length;
    }
}