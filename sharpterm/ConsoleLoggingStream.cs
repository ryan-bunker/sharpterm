using System;
using System.IO;
using System.Text;

namespace SharpTerm
{
    public class ConsoleLoggingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly string _label;

        public ConsoleLoggingStream(Stream innerStream, string label)
        {
            _innerStream = innerStream;
            _label = label;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);
            Console.WriteLine($"{_label}: Read {read} bytes [{ToLiteral(Encoding.ASCII.GetString(buffer, offset, read))}]");
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            Console.WriteLine($"{_label}: Wrote {count} bytes [{ToLiteral(Encoding.ASCII.GetString(buffer, offset, count))}]");
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
        
        private static string ToLiteral(string input)
        {
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                if (c == '\0')
                    sb.Append("\\0");
                else if (c == '\a')
                    sb.Append("\\a");
                else if (c == '\b')
                    sb.Append("\\b");
                else if (c == '\f')
                    sb.Append("\\f");
                else if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\r')
                    sb.Append("\\r");
                else if (c == '\t')
                    sb.Append("\\t");
                else if (c == '\v')
                    sb.Append("\\v");
                else if (c < ' ')
                    sb.Append($"\\x{(int)c:x}");
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}