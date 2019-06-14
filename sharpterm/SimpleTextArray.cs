using System;

namespace SharpTerm
{
    public class SimpleTextArray : ITextArray
    {
        private readonly char?[,] _chars;
        private string _text;
        
        public SimpleTextArray(string text)
        {
//            Width = 0;
//            Height = 1;
//            uint lineWidth = 0;
//            for (int i = 0; i < text.Length; ++i)
//            {
//                if (text[i] == '\n')
//                {
//                    Width = Math.Max(lineWidth, Width);
//                    ++Height;
//                    lineWidth = 0;
//                    continue;
//                }
//
//                ++lineWidth;
//            }
//
//            Width = Math.Max(lineWidth, Width);
            Width = 35;
            Height = 15;
            
            _chars = new char?[Width, Height];
            Text = text;
//            for (int y=0; y<Height; ++y)
//                for (int x = 0; x < Width; ++x)
//                    _chars[x, y] = '.';
        }
        
        public uint Width { get; }
        public uint Height { get; }

        public char? this[uint col, uint row] => _chars[col, row];

        public string Text
        {
            get { return _text; }
            set
            {
                Clear();
                int x = 0, y = 0;
                for (int i = 0; i < value.Length; ++i)
                {
                    if (value[i] == '\n')
                    {
                        x = 0;
                        ++y;
                        continue;
                    }

                    _chars[x++, y] = value[i];
                }

                _text = value;
            }
        }

        private void Clear()
        {
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    _chars[x, y] = null;
        }
    }
}