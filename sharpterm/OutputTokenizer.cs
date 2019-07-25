#nullable enable
namespace SharpTerm
{
    public class OutputTokenizer
    {
        private readonly TextLayout _output;
        private EscapeSequenceParser? _escapeParser;

        public OutputTokenizer(TextLayout output)
        {
            _output = output;
            _escapeParser = null;
        }

        public void Process(char c)
        {
            switch (c)
            {
                case '\u001b': // ESC
                    if (_escapeParser != null) FlushEscapeParser();
                    _escapeParser = new EscapeSequenceParser();
                    break;

                default:
                    if (_escapeParser != null)
                    {
                        var progress = _escapeParser.Process(c);
                        if (progress == false)
                            FlushEscapeParser();
                        else if (progress == true)
                        {
                            var tokens = _escapeParser.GetToken();
                            if (tokens == null)
                                FlushEscapeParser();
                            else
                                foreach (var token in tokens)
                                    _output.Write(token);
                            _escapeParser = null;
                        }
                    }
                    else
                        WriteCharToOutput(c);

                    break;
            }
        }

        private void WriteCharToOutput(char c) => _output.Write(new CharToken(c));
        

        private void FlushEscapeParser()
        {
            WriteCharToOutput('\u001b');
            var chars = _escapeParser?.GetChars();
            if (chars != null)
                foreach (var c in chars)
                    WriteCharToOutput(c);
            _escapeParser = null;
        }
    }
}