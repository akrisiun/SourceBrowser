using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
   public abstract partial class DocumentationProvider
    {
       public static DocumentationProvider Default { get; private set; }
       static DocumentationProvider() { Default = new NullDocumentationProvider(); }

       private class NullDocumentationProvider : DocumentationProvider
       {
           protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
           {
               return "";
           }

           public override bool Equals(object obj)
           {
               // Only one instance is expected to exist, so reference equality is fine.
               return ReferenceEquals(this, obj);
           }

           public override int GetHashCode()
           {
               return 1; // RuntimeHelpers.GetHashCode(this);
           }
       }

       protected DocumentationProvider()
       {
       }

       protected internal abstract string GetDocumentationForSymbol(
            string documentationMemberID,
            CultureInfo preferredCulture,
            CancellationToken cancellationToken = default(CancellationToken));
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }

    public class XmlDocumentationProvider : DocumentationProvider
    {
        private readonly Dictionary<string, string> members = new Dictionary<string, string>();

        public XmlDocumentationProvider(string filePath)
        {
            var xmlDocFile = XDocument.Load(filePath);

            foreach (var member in xmlDocFile.Descendants("member"))
            {
                var id = member.Attribute("name").Value;
                var value = member.ToString();

                // there might be multiple entries with same id, just pick one at random
                members[id] = value;
            }
        }

        //protected internal abstract string GetDocumentationForSymbol(
        //    string documentationMemberID,
        //    CultureInfo preferredCulture,
        //    CancellationToken cancellationToken = default(CancellationToken));
 
        protected internal override 
            string GetDocumentationForSymbol(
                string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
        {
            string result = null;
            members.TryGetValue(documentationMemberID, out result);
            return result;
        }

        public override int GetHashCode()
        {
            return members.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == obj;
        }
    }
}