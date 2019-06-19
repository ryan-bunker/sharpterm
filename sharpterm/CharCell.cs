using Veldrid;

namespace SharpTerm
{
    public class CharCell
    {
        public CharCell(char c, RgbaFloat foreColor)
        {
            Char = c;
            ForeColor = foreColor;
        }

        public char Char { get; }
        
        public RgbaFloat ForeColor { get; }
    }
}