using System;

namespace JsonLens.Test {
    
    public ref struct CircularBuffer<T> 
    {
        readonly Span<T> _data;
        readonly int _mask;
        
        int _cursor;
        int _charge;
        
        public CircularBuffer(Span<T> data, int mask) {
            _data = data;
            _mask = mask;
            _cursor = 0;
            _charge = 0;
        }
        
        public bool Write(T v) {
            if (_charge >= _data.Length) {
                return false;
            }
            
            _data[(_cursor + _charge) & _mask] = v;
            _charge++;
            return true;
        }

        public bool Read(out T v) {
            if (_charge <= 0) {
                v = default(T);
                return false;
            }
            
            v = _data[_cursor];
            _cursor = (_cursor + 1) & _mask;
            _charge--;
            return true;
        }
    }
    
}