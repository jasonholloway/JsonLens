using System;

namespace JsonLens.Test {
    
    public ref struct CircularBuffer<T> {
        int _writeHead;
        int _readHead;
        Span<T> _data;
        
        //should take mask for loopin...

        public CircularBuffer(Span<T> data) {
            _writeHead = 0;
            _readHead = 0;
            _data = data;
        }

        public bool Write(T v) {
            if (_writeHead >= _data.Length) {
                return false;
            }
            else {
                _data[_writeHead++] = v;
                return true;
            }
        }

        public bool Read(out T v) {
            if (_readHead >= _writeHead) {
                v = default(T);
                return false;
            }
            else {
                v = _data[_readHead++];
                return true;
            }
        }
    }
    
}