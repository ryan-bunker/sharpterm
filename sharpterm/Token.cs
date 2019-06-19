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
}