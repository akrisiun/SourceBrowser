
using System.Collections.Generic;
using System.IO;

namespace Roslyn.NotInternal
{
    // let's make Internal to public 

    public sealed class ProjectFileInfoX
    {
        /// </summary>
        public IReadOnlyList<DocumentFileInfoX> Documents { get; set; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfoX> AdditionalDocuments { get; set; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public IReadOnlyList<ProjectFileReference> ProjectReferences { get; }
    }

    public sealed class DocumentFileInfoX
    {
        public string FilePath { get; set; }
        public string LogicalPath { get; set; }
        public bool IsLinked { get; set; }
        public bool IsGenerated { get; set; }

        // where ShouldShow(child) 
        // where CanAccess(child)
        public static bool CanAccess(string child) { return !string.IsNullOrWhiteSpace(child) && File.Exists(child); }
    }

    public class ProjectFileReference
    {
        public string Path { get; set; }

        public IReadOnlyList<string>  // ImmutableArray<string> 
               Aliases { get; set; }
    }

}
