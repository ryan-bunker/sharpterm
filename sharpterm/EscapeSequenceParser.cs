using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpTerm
{
    public class EscapeSequenceParser
    {
        private Parser _parser;
        private readonly List<char> _chars = new List<char>();
        
        public bool? Process(char c)
        {
            _chars.Add(c);
            
            if (_parser == null)
            {
                switch (c)
                {
                    case '[':
                        _parser = new CsiParser();
                        return null;
                    
                    default:
                        return false;
                }
            }

            return _parser.Process(c);
        }

        public Token GetToken() => _parser.GetToken();

        public char[] GetChars() => _chars.ToArray();

        private abstract class Parser
        {
            public abstract bool? Process(in char c);
            public abstract Token GetToken();
        }

        private class CsiParser : Parser
        {
            private enum State
            {
                ParsingParams,
                ParsingIntermediate
            }
            
            private readonly List<char> _params = new List<char>();
            private readonly List<char> _intermediate = new List<char>();
            private char _finalByte;
            private State _state = State.ParsingParams;
            
            public override bool? Process(in char c)
            {
                if (0x30 <= c && c <= 0x3f)
                {
                    // parameter byte
                    if (_state == State.ParsingParams)
                    {
                        // if we're in the Param parsing state then add the character
                        // to our list of parameters and continue parsing
                        _params.Add(c);
                        return null;
                    }

                    // in any other state, a parameter byte is illegal, so stop
                    // parsing, indicating failure
                    return false;
                }

                if (0x20 <= c && c <= 0x2f)
                {
                    // intermediate byte
                    // if we're in the Param parsing state, then move to the intermediate
                    // parsing state, and if we're already in the intermediate parsing
                    // state, then we continue in that state, in either case, record
                    // the byte and continue parsing
                    _state = State.ParsingIntermediate;
                    _intermediate.Add(c);
                    return null;
                }

                if (0x40 <= c && c <= 0x7e)
                {
                    // final byte
                    // once we have the final byte, we know what it all means and can
                    // return a token
                    _finalByte = c;
                    return true;
                }

                // any other character is illegal, so stop parsing
                return false;
            }

            public override Token GetToken()
            {
                switch (_finalByte)
                {
                    case 'K':
                        EraseLineToken.EraseBounds bounds;
                        if (_params.Count > 1)
                            return null;
                        else if (_params.Count == 0 || _params[0] == '0')
                            bounds = EraseLineToken.EraseBounds.CursorToEnd;
                        else if (_params[0] == '1')
                            bounds = EraseLineToken.EraseBounds.BeginningToCursor;
                        else if (_params[0] == '2')
                            bounds = EraseLineToken.EraseBounds.CursorToEnd;
                        else
                            return null;
                        return new EraseLineToken(bounds);
                    
                    default:
                        return null;
                }
            }
        }
    }
}