#nullable enable
namespace SharpTerm
{
    public class BufferWindow : ITextArray
    {
        private readonly ITextArray _sourceBuffer;

        public BufferWindow(ITextArray sourceBuffer, uint windowWidth, uint windowHeight)
        {
            _sourceBuffer = sourceBuffer;
            Width = windowWidth;
            Height = windowHeight;
        }
        
        public uint Width { get; }
        public uint Height { get; }
        
        public uint OffsetX { get; set; }
        public uint OffsetY { get; set; }
        
        public CharCell? this[uint col, uint row] => _sourceBuffer[col + OffsetX, row + OffsetY];
    }
}