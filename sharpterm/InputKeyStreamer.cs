using System.IO;
using Veldrid;

namespace SharpTerm
{
    public class InputKeyStreamer
    {
        public Stream? OutStream { get; set; }
        
        public void ProcessToStream(InputSnapshot input)
        {
            foreach (var c in input.KeyCharPresses)
                OutStream?.WriteByte((byte) c);

            foreach (var key in input.KeyEvents)
            {
                switch (key.Key)
                {
                    case Key.Enter:
                        if (!key.Down) OutStream?.WriteByte((byte) '\n');
                        break;
                    case Key.BackSpace:
                        if (key.Down) OutStream?.WriteByte(127);
                        break;
                }
            }
            
            OutStream?.Flush();
        }
    }
}