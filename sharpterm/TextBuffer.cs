namespace SharpTerm
{
    public class TextBuffer : ITextArray
    {
        private readonly CharCell[,] _chars;
        
        public TextBuffer(uint width, uint height)
        {
            Width = width;
            Height = height;
            _chars = new CharCell[Width, Height];
        }
        
        public uint Width { get; }
        public uint Height { get; }

        public CharCell this[uint col, uint row]
        {
            get => _chars[col, row];
            set => _chars[col, row] = value;
        }
    }
}