using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Language.Xml
{
    public class XmlNodeSyntax
    {

    }

    public class Parser
    {
        private readonly Scanner _scanner;
        //private SyntaxToken currentToken;
        //private SyntaxListPool _pool = new SyntaxListPool();
        private Buffer buffer;
        private CancellationToken cancellationToken;

        public Parser(Buffer buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.buffer = buffer;
            this._scanner = new Scanner(buffer);
            this.cancellationToken = cancellationToken;
        }

        public static XmlNodeSyntax ParseText(string xml)
        {
            var buffer = new StringBuffer(xml);
            var parser = new Parser(buffer);
            var root = parser.Parse();
            return root;
        }

        public XmlNodeSyntax Parse()
        {
            //Debug.Assert(
            //    CurrentToken.Kind == SyntaxKind.LessThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanGreaterThanToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanSlashToken ||
            //    CurrentToken.Kind == SyntaxKind.BeginCDataToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanExclamationMinusMinusToken ||
            //    CurrentToken.Kind == SyntaxKind.LessThanQuestionToken,
            //    "Invalid XML");

            XmlNodeSyntax result = null;
            //if (CurrentToken.Kind == SyntaxKind.LessThanQuestionToken)
            //{
            //    result = ParseXmlDocument();

            return result;
        }

    }
}
