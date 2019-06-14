namespace SharpTerm
{
    public interface ITextArray
    {
        uint Width { get; }
        uint Height { get; }
        
        char? this[uint col, uint row] { get; }
    }
}