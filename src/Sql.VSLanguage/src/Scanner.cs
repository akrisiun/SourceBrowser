using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Language.Xml
{
    public class Scanner
    {
        //private ScannerToken _prevToken;
        //protected ScannerToken _currentToken;
        protected int _lineBufferOffset; // marks the next character to read from _LineBuffer
        private int _bufferLen;
        //private readonly List<ScannerToken> _tokens = new List<ScannerToken>();
        private Buffer buffer;
        private int _endOfTerminatorTrivia;
        private StringTable _stringTable = StringTable.GetInstance();
        //private SyntaxListPool triviaListPool = new SyntaxListPool();
        private readonly PooledStringBuilder _sbPooled;
        private readonly StringBuilder _sb;
        private readonly char[] _internBuffer = new char[256];
        //private ConditionalWeakTable<string, SyntaxNode> _triviaCache = new ConditionalWeakTable<string, SyntaxNode>();

        public Scanner(Buffer buffer)
        {
            this.buffer = buffer;
            this._bufferLen = buffer.Length;
            _sbPooled = PooledStringBuilder.GetInstance();
            _sb = _sbPooled.Builder;
        }

        //internal SyntaxToken ScanXmlStringUnQuoted()

    }
}
