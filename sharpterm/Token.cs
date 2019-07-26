using Veldrid;

namespace SharpTerm
{
    public class Token
    {
        
    }

    public class CharToken : Token
    {
        public char Char { get; }

        public CharToken(char c) => Char = c;
    }

    public class EraseScreenToken : Token
    {
        public enum EraseBounds
        {
            BeginningToCursor,
            CursorToEnd,
            EntireScreen,
            EntireScreenAndScrollback
        }
        
        public EraseBounds Bounds { get; }

        public EraseScreenToken(EraseBounds bounds)
        {
            Bounds = bounds;
        }
    }

    public class EraseLineToken : Token
    {
        public enum EraseBounds
        {
            BeginningToCursor,
            CursorToEnd,
            BeginningToEnd
        }
        
        public EraseBounds Bounds { get; }

        public EraseLineToken(EraseBounds bounds)
        {
            Bounds = bounds;
        }
    }

    public class SgrResetToken : Token
    {
    }

    public class BoldToken : Token
    {
        public BoldToken(bool isSet)
        {
            IsSet = isSet;
        }

        public bool IsSet { get; }
    }

    public class Set8ColorToken : Token
    {
        public enum ColorName
        {
            Black,
            Red,
            Green,
            Yellow,
            Blue,
            Magenta,
            Cyan,
            White
        }
        
        public Set8ColorToken(ColorName color, bool isForeground)
        {
            Color = color;
            IsForeground = isForeground;
        }

        public ColorName Color { get; }

        public bool IsForeground { get; }
    }
}