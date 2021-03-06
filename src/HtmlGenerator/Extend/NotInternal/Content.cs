﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// http://source.roslyn.io/#Microsoft.CodeAnalysis.EditorFeatures/Implementation/Intellisense/Completion/FileSystem/FileSystemCompletionHelper.cs,f628e4f4d92dba36

namespace Content
{
    using Microsoft.CodeAnalysis.Shared.Utilities;
    using Roslyn.NotInternal;
    using Roslyn.Utilities;
    // using static Roslyn.Utilities.SpecializedCollections;
    // using Roslyn.Utilities.SpecializedCollections;

    public class FileSystemCompletionHelper
    {
        public IEnumerable<DocumentFileInfoX> GetFilesAndDirectoriesInDirectory(string fullDirectoryPath)
        {
            Debug.Assert(PathUtilitiesX.IsAbsolute(fullDirectoryPath));

            if (IOUtilitiesX.PerformIO(() => Directory.Exists(fullDirectoryPath)))
            {
                var directoryInfo = IOUtilitiesX.PerformIO(() => new DirectoryInfo(fullDirectoryPath));
                if (directoryInfo != null)
                {
                    return from child in IOUtilitiesX.GetFileSystemInfos(directoryInfo)
                               // where ShouldShow(child) where CanAccess(child)
                           select new DocumentFileInfoX { FilePath = child.FullName };
                    // this.CreateCompletion(child);
                }
            }

            return SpecializedCollections.EmptyEnumerable<DocumentFileInfoX>();
        }

        //private CompletionItem CreateCompletion(FileSystemInfo child)
        //{
        //    return CommonCompletionItem.Create(
        //        child.Name,
        //        glyph: child is DirectoryInfo ? _folderGlyph : _fileGlyph,
        //        description: child.FullName.ToSymbolDisplayParts(),
        //        rules: _itemRules);
        //}


        //protected virtual IEnumerable<ITaskItem> GetDocumentsFromModel(ProjectInstance executedProject)
        //{
        //    return executedProject.GetItems("Compile");
        //}

        //protected virtual IEnumerable<ITaskItem> GetMetadataReferencesFromModel(ProjectInstance executedProject)
        //{
        //    return executedProject.GetItems("ReferencePath");
        //}

        //protected virtual IEnumerable<ITaskItem> GetAnalyzerReferencesFromModel(ProjectInstance executedProject)
        //{
        //    return executedProject.GetItems("Analyzer");
        //}

        //protected virtual IEnumerable<ITaskItem> GetAdditionalFilesFromModel(ProjectInstance executedProject)
        //{
        //    return executedProject.GetItems("AdditionalFiles");
        //}

    }

}


namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    public static class IOUtilitiesX
    {
        public static FileSystemInfo[] GetFileSystemInfos(DirectoryInfo directoryInfo)
        {
            return PerformIO(directoryInfo.GetFileSystemInfos, Array.Empty<FileSystemInfo>());
        }


        public static void PerformIO(Action action)
        {
            PerformIO<object>(() =>
            {
                action();
                return null;
            });
        }

        public static T PerformIO<T>(Func<T> function, T defaultValue = default(T))
        {
            try
            {
                return function();
            }
            catch (Exception e) when (IsNormalIOException(e))
            {
            }

            return defaultValue;
        }

        public static async Task<T> PerformIOAsync<T>(Func<Task<T>> function, T defaultValue = default(T))
        {
            try
            {
                return await function().ConfigureAwait(false);
            }
            catch (Exception e) when (IsNormalIOException(e))
            {
            }

            return defaultValue;
        }

        public static bool IsNormalIOException(Exception e)
        {
            return e is IOException ||
                   // e is SecurityException ||
                   e is ArgumentException ||
                   e is UnauthorizedAccessException ||
                   e is NotSupportedException ||
                   e is InvalidOperationException;
        }
    }
}


namespace Roslyn.Utilities
{

    internal static partial class SpecializedCollections
    {
        public static IEnumerator<T> EmptyEnumerator<T>()
        {
            return Empty.Enumerator<T>.Instance;
        }

        public static IEnumerable<T> EmptyEnumerable<T>()
        {
            return Empty.List<T>.Instance;
        }

        private partial class Empty
        {
            internal class Enumerator<T> : Enumerator, IEnumerator<T>
            {
                public static new readonly IEnumerator<T> Instance = new Enumerator<T>();

                protected Enumerator() { }
                public new T Current => throw new InvalidOperationException();

                public void Dispose() { }

            }

            internal class Enumerator : IEnumerator
            {
                public static readonly IEnumerator Instance = new Enumerator();
                protected Enumerator() { }
                public object Current => throw new InvalidOperationException();

                public bool MoveNext() { return false; }
                public void Reset() { throw new InvalidOperationException(); }
            }


            internal class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
            {
                public static readonly List<T> Instance = new List<T>();

                protected List() { }

                //public int IndexOf(T item)
                //{
                //    return -1;
                //}

                //public void Insert(int index, T item)
                //{
                //    throw new NotSupportedException();
                //}

                //public void RemoveAt(int index)
                //{
                //    throw new NotSupportedException();
                //}

                //public T this[int index] {
                //    get {
                //        throw new ArgumentOutOfRangeException(nameof(index));
                //    }

                //    set {
                //        throw new NotSupportedException();
                //    }
                //}
            }

        }

    }

    public static class PlatformInformation
    {
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static bool IsUnix => Path.DirectorySeparatorChar == '/';
    }

    // Contains path parsing utilities.
    // We need our own because System.IO.Path is insufficient for our purposes
    // For example we need to be able to work with invalid paths or paths containing wildcards
    public static class PathUtilitiesX
    {
        // We consider '/' a directory separator on Unix like systems. 
        // On Windows both / and \ are equally accepted.
        internal static readonly char DirectorySeparatorChar = PlatformInformation.IsUnix ? '/' : '\\';
        internal static readonly char AltDirectorySeparatorChar = '/';
        internal static readonly string ParentRelativeDirectory = "..";
        internal static readonly string ThisDirectory = ".";
        internal static readonly string DirectorySeparatorStr = new string(DirectorySeparatorChar, 1);
        internal const char VolumeSeparatorChar = ':';
        internal static bool IsUnixLikePlatform => PlatformInformation.IsUnix;

        /// <summary>
        /// True if the character is a directory separator character.
        /// </summary>
        public static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
        }

        /// <summary>
        /// Removes trailing directory separator characters
        /// </summary>
        public static string TrimTrailingSeparators(string s)
        {
            int lastSeparator = s.Length;
            while (lastSeparator > 0 && IsDirectorySeparator(s[lastSeparator - 1]))
            {
                lastSeparator = lastSeparator - 1;
            }

            if (lastSeparator != s.Length)
            {
                s = s.Substring(0, lastSeparator);
            }

            return s;
        }

        public static string GetExtension(string path)
        {
            // FileNameUtilities
            return Path.GetExtension(path); // FileNameUtilities.GetExtension(path);
        }

        public static string ChangeExtension(string path, string extension)
        {
            return Path.ChangeExtension(path, extension);
            // FileNameUtilities.ChangeExtension(path, extension);
        }

        public static string RemoveExtension(string path)
        {
            return Path.ChangeExtension(path, extension: null);
            // FileNameUtilities.ChangeExtension(path, extension: null);
        }

        public static string GetFileName(string path, bool includeExtension = true)
        {
            return includeExtension ? Path.GetFileName(path)
                   : Path.GetFileNameWithoutExtension(path);
            // FileNameUtilities.GetFileName(path, includeExtension);
        }

        public static string GetOnlyFileName(string path, bool includeExtension = false)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Get directory name from path.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="System.IO.Path.GetDirectoryName"/> it doesn't check for invalid path characters
        /// </remarks>
        /// <returns>Prefix of path that represents a directory</returns>
        public static string GetDirectoryName(string path)
        {
            return GetDirectoryName(path, IsUnixLikePlatform);
        }

        // Exposed for testing purposes only.
        internal static string GetDirectoryName(string path, bool isUnixLike)
        {
            if (path != null)
            {
                var rootLength = GetPathRoot(path, isUnixLike).Length;
                if (path.Length > rootLength)
                {
                    var i = path.Length;
                    while (i > rootLength)
                    {
                        i--;
                        if (IsDirectorySeparator(path[i]))
                        {
                            if (i > 0 && IsDirectorySeparator(path[i - 1]))
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    return path.Substring(0, i);
                }
            }

            return null;
        }

        internal static bool IsSameDirectoryOrChildOf(string child, string parent)
        {
            parent = RemoveTrailingDirectorySeparator(parent);

            while (child != null)
            {
                child = RemoveTrailingDirectorySeparator(child);

                if (child.Equals(parent, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                child = GetDirectoryName(child);
            }

            return false;
        }

        /// <summary>
        /// Gets the root part of the path.
        /// </summary>
        public static string GetPathRoot(string path)
        {
            return GetPathRoot(path, IsUnixLikePlatform);
        }

        private static string GetPathRoot(string path, bool isUnixLike)
        {
            if (path == null)
            {
                return null;
            }

            if (isUnixLike)
            {
                return GetUnixRoot(path);
            }
            else
            {
                return GetWindowsRoot(path);
            }
        }

        private static string GetWindowsRoot(string path)
        {
            // Windows
            int length = path.Length;
            if (length >= 1 && IsDirectorySeparator(path[0]))
            {
                if (length < 2 || !IsDirectorySeparator(path[1]))
                {
                    //  It was of the form:
                    //          \     
                    //          \f
                    // in this case, just return \ as the root.
                    return path.Substring(0, 1);
                }

                // First consume all directory separators.
                int i = 2;
                i = ConsumeDirectorySeparators(path, length, i);

                // We've got \\ so far.  If we have a path of the form \\x\y\z
                // then we want to return "\\x\y" as the root portion.
                bool hitSeparator = false;
                while (true)
                {
                    if (i == length)
                    {
                        // We reached the end of the path. The entire path is
                        // considered the root.
                        return path;
                    }

                    if (!IsDirectorySeparator(path[i]))
                    {
                        // We got a non separator character.  Just keep consuming.
                        i++;
                        continue;
                    }

                    if (!hitSeparator)
                    {
                        // This is the first separator group we've hit after some server path.  
                        // Consume them and keep going.
                        hitSeparator = true;
                        i = ConsumeDirectorySeparators(path, length, i);
                        continue;
                    }

                    // We hit the second separator.  The root is the path up to this point.
                    return path.Substring(0, i);
                }
            }
            else if (length >= 2 && path[1] == VolumeSeparatorChar)
            {
                // handles c: and c:\
                return length >= 3 && IsDirectorySeparator(path[2])
                    ? path.Substring(0, 3)
                    : path.Substring(0, 2);
            }
            else
            {
                // No path root.
                return "";
            }
        }

        private static int ConsumeDirectorySeparators(string path, int length, int i)
        {
            while (i < length && IsDirectorySeparator(path[i]))
            {
                i++;
            }

            return i;
        }

        private static string GetUnixRoot(string path)
        {
            // either it starts with "/" and thus has "/" as the root.  Or it has no root.
            return path.Length > 0 && IsDirectorySeparator(path[0])
                ? path.Substring(0, 1)
                : "";
        }

        /// <summary>
        /// Gets the specific kind of relative or absolute path.
        /// </summary>
        public static PathKind GetPathKind(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return PathKind.Empty;
            }

            // "C:\"
            // "\\machine" (UNC)
            // "/etc"      (Unix)
            if (IsAbsolute(path))
            {
                return PathKind.Absolute;
            }

            // "."
            // ".."
            // ".\"
            // "..\"
            if (path.Length > 0 && path[0] == '.')
            {
                if (path.Length == 1 || IsDirectorySeparator(path[1]))
                {
                    return PathKind.RelativeToCurrentDirectory;
                }

                if (path[1] == '.')
                {
                    if (path.Length == 2 || IsDirectorySeparator(path[2]))
                    {
                        return PathKind.RelativeToCurrentParent;
                    }
                }
            }

            if (!IsUnixLikePlatform)
            {
                // "\"
                // "\foo"
                if (path.Length >= 1 && IsDirectorySeparator(path[0]))
                {
                    return PathKind.RelativeToCurrentRoot;
                }

                // "C:foo"

                if (path.Length >= 2 && path[1] == VolumeSeparatorChar && (path.Length <= 2 || !IsDirectorySeparator(path[2])))
                {
                    return PathKind.RelativeToDriveDirectory;
                }
            }

            // "foo.dll"
            return PathKind.Relative;
        }

        /// <summary>
        /// True if the path is an absolute path (rooted to drive or network share)
        /// </summary>
        public static bool IsAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (IsUnixLikePlatform)
            {
                return path[0] == DirectorySeparatorChar;
            }

            // "C:\"
            if (IsDriveRootedAbsolutePath(path))
            {
                // Including invalid paths (e.g. "*:\")
                return true;
            }

            // "\\machine\share"
            // Including invalid/incomplete UNC paths (e.g. "\\foo")
            return path.Length >= 2 &&
                IsDirectorySeparator(path[0]) &&
                IsDirectorySeparator(path[1]);
        }

        /// <summary>
        /// Returns true if given path is absolute and starts with a drive specification ("C:\").
        /// </summary>
        private static bool IsDriveRootedAbsolutePath(string path)
        {
            Debug.Assert(!IsUnixLikePlatform);
            return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
        }

        /// <summary>
        /// Combines an absolute path with a relative.
        /// </summary>
        /// <param name="root">Absolute root path.</param>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>
        /// An absolute combined path, or null if <paramref name="relativePath"/> is 
        /// absolute (e.g. "C:\abc", "\\machine\share\abc"), 
        /// relative to the current root (e.g. "\abc"), 
        /// or relative to a drive directory (e.g. "C:abc\def").
        /// </returns>
        /// <seealso cref="CombinePossiblyRelativeAndRelativePaths"/>
        public static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
        {
            Debug.Assert(IsAbsolute(root));

            return CombinePossiblyRelativeAndRelativePaths(root, relativePath);
        }

        /// <summary>
        /// Combine two paths, the first of which may be absolute.
        /// </summary>
        /// <param name="rootOpt">First path: absolute, relative, or null.</param>
        /// <param name="relativePath">Second path: relative and non-null.</param>
        /// <returns>null, if <paramref name="rootOpt"/> is null; a combined path, otherwise.</returns>
        /// <seealso cref="CombineAbsoluteAndRelativePaths"/>
        public static string CombinePossiblyRelativeAndRelativePaths(string rootOpt, string relativePath)
        {
            if (string.IsNullOrEmpty(rootOpt))
            {
                return null;
            }

            switch (GetPathKind(relativePath))
            {
                case PathKind.Empty:
                    return rootOpt;

                case PathKind.Absolute:
                case PathKind.RelativeToCurrentRoot:
                case PathKind.RelativeToDriveDirectory:
                    return null;
            }

            return CombinePathsUnchecked(rootOpt, relativePath);
        }

        public static string CombinePathsUnchecked(string root, string relativePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(root));

            char c = root[root.Length - 1];
            if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
            {
                return root + DirectorySeparatorStr + relativePath;
            }

            return root + relativePath;
        }

        private static string RemoveTrailingDirectorySeparator(string path)
        {
            if (path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]))
            {
                return path.Substring(0, path.Length - 1);
            }
            else
            {
                return path;
            }
        }

        /// <summary>
        /// Determines whether an assembly reference is considered an assembly file path or an assembly name.
        /// used, for example, on values of /r and #r.
        /// </summary>
        public static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            Debug.Assert(assemblyDisplayNameOrPath != null);

            string extension = Path.GetExtension(assemblyDisplayNameOrPath);
            // FileNameUtilities.

            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }

        /// <summary>
        /// Determines if "path" contains 'component' within itself.
        /// i.e. asking if the path "c:\foo\bar\baz" has component "bar" would return 'true'.
        /// On the other hand, if you had "c:\foo\bar1\baz" then it would not have "bar" as a
        /// component.
        /// 
        /// A path contains a component if any file name or directory name in the path
        /// matches 'component'.  As such, if you had something like "\\foo" then that would
        /// not have "foo" as a component. That's because here "foo" is the server name portion
        /// of the UNC path, and not an actual directory or file name.
        /// </summary>
        public static bool ContainsPathComponent(string path, string component, bool ignoreCase)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (path?.IndexOf(component, comparison) >= 0)
            {
                var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

                int count = 0;
                var currentPath = path;
                while (currentPath != null)
                {
                    var currentName = GetFileName(currentPath);
                    if (comparer.Equals(currentName, component))
                    {
                        return true;
                    }

                    currentPath = GetDirectoryName(currentPath);
                    count++;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a path relative to a directory.
        /// </summary>
        public static string GetRelativePath(string directory, string fullPath)
        {
            string relativePath = string.Empty;

            if (IsChildPath(directory, fullPath))
            {
                return GetRelativeChildPath(directory, fullPath);
            }

            var directoryPathParts = GetPathParts(directory);
            var fullPathParts = GetPathParts(fullPath);

            if (directoryPathParts.Length == 0 || fullPathParts.Length == 0)
            {
                return fullPath;
            }

            int index = 0;

            // find index where full path diverges from base path
            for (; index < directoryPathParts.Length; index++)
            {
                if (!PathsEqual(directoryPathParts[index], fullPathParts[index]))
                {
                    break;
                }
            }

            // if the first part doesn't match, they don't even have the same volume
            // so there can be no relative path.
            if (index == 0)
            {
                return fullPath;
            }

            // add backup notation for remaining base path levels beyond the index
            var remainingParts = directoryPathParts.Length - index;
            if (remainingParts > 0)
            {
                for (int i = 0; i < remainingParts; i++)
                {
                    relativePath = relativePath + ParentRelativeDirectory + DirectorySeparatorStr;
                }
            }

            // add the rest of the full path parts
            for (int i = index; i < fullPathParts.Length; i++)
            {
                relativePath = CombinePathsUnchecked(relativePath, fullPathParts[i]);
            }

            return relativePath;
        }

        /// <summary>
        /// True if the child path is a child of the parent path.
        /// </summary>
        public static bool IsChildPath(string parentPath, string childPath)
        {
            return parentPath.Length > 0
                && childPath.Length > parentPath.Length
                && PathsEqual(childPath, parentPath, parentPath.Length)
                && (IsDirectorySeparator(parentPath[parentPath.Length - 1]) || IsDirectorySeparator(childPath[parentPath.Length]));
        }

        private static string GetRelativeChildPath(string parentPath, string childPath)
        {
            var relativePath = childPath.Substring(parentPath.Length);

            // trim any leading separators left over after removing leading directory
            int start = ConsumeDirectorySeparators(relativePath, relativePath.Length, 0);
            if (start > 0)
            {
                relativePath = relativePath.Substring(start);
            }

            return relativePath;
        }

        private static readonly char[] s_pathChars = new char[] { VolumeSeparatorChar, DirectorySeparatorChar, AltDirectorySeparatorChar };

        private static string[] GetPathParts(string path)
        {
            var pathParts = path.Split(s_pathChars);

            // remove references to self directories ('.')
            if (Contains(pathParts, ThisDirectory))
            {
                pathParts = pathParts.Where(s => s != ThisDirectory).ToArray();
            }

            return pathParts;
        }

        public static bool Contains(IEnumerable<string> sequence, string s)
        {
            foreach (var item in sequence)
            {
                if (item == s)
                {
                    return true;
                }
            }

            return false;
        }

        public static IComparer<T> ToComparer<T>(Comparison<T> comparison)
        {
            return Comparer<T>.Create(comparison);
        }


        /// <summary>
        /// True if the two paths are the same.
        /// </summary>
        public static bool PathsEqual(string path1, string path2)
        {
            return PathsEqual(path1, path2, Math.Max(path1.Length, path2.Length));
        }

        /// <summary>
        /// True if the two paths are the same.  (but only up to the specified length)
        /// </summary>
        private static bool PathsEqual(string path1, string path2, int length)
        {
            if (path1.Length < length || path2.Length < length)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (!PathCharEqual(path1[i], path2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PathCharEqual(char x, char y)
        {
            if (IsDirectorySeparator(x) && IsDirectorySeparator(y))
            {
                return true;
            }

            return IsUnixLikePlatform
                ? x == y
                : char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
        }

        //private static int PathHashCode(string path)
        //{
        //    int hc = 0;

        //    if (path != null)
        //    {
        //        foreach (var ch in path)
        //        {
        //            if (!IsDirectorySeparator(ch))
        //            {
        //                hc = Hash.Combine((int)char.ToUpperInvariant(ch), hc);
        //            }
        //        }
        //    }

        //    return hc;
        //}

        public static readonly IEqualityComparer<string> Comparer = new PathComparer();

        private class PathComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return PathsEqual(x, y);
            }

            public int GetHashCode(string s)
            {
                return PathHashCode(s);
            }

            static int PathHashCode(string path)
            {
                int hc = 0;

                if (path != null)
                {
                    foreach (var ch in path)
                    {
                        if (!IsDirectorySeparator(ch))
                        {
                            hc = Combine((int)char.ToUpperInvariant(ch), hc);
                        }
                    }
                }

                return hc;
            }

            static int Combine(int newKey, int currentKey)
            {
                return unchecked((currentKey * (int)0xA5555529) + newKey);
            }


        }
    }

    // internal 
    public enum PathKindX
    {
        //
        // Summary:
        //     Null or empty.
        Empty = 0,
        //
        // Summary:
        //     "file"
        Relative = 1,
        //
        // Summary:
        //     ".\file"
        RelativeToCurrentDirectory = 2,
        //
        // Summary:
        //     "..\file"
        RelativeToCurrentParent = 3,
        //
        // Summary:
        //     "\dir\file"
        RelativeToCurrentRoot = 4,
        //
        // Summary:
        //     "C:dir\file"
        RelativeToDriveDirectory = 5,
        //
        // Summary:
        //     "C:\file" or "\\machine" (UNC).
        Absolute = 6
    }

}


