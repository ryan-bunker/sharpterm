namespace SharpTerm
{
    public class TextBuffer : ITextArray
    {
        private readonly char?[,] _chars;
        
        public TextBuffer(uint width, uint height)
        {
            Width = width;
            Height = height;
            _chars = new char?[Width, Height];
        }
        
        public uint Width { get; }
        public uint Height { get; }

        public char? this[uint col, uint row]
        {
            get => _chars[col, row];
            set => _chars[col, row] = value;
        }
    }
}