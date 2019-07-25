#nullable enable
namespace SharpTerm
{
    public interface ITextArray
    {
        uint Width { get; }
        uint Height { get; }
        
        CharCell? this[uint col, uint row] { get; }
    }
}