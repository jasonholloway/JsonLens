using System;

namespace JsonLens.Test 
{
    public ref struct Readable<T> 
    {
        ReadOnlySpan<T> _data;
        int _index;

        public Readable(ReadOnlySpan<T> data) {
            _data = data;
            _index = 0;
        }
        
        public bool Read(out T v) {
            if (_index > _data.Length) {
                v = default(T);
                return false;
            }
            
            v = _data[_index++];
            return true;
        }
    }
}