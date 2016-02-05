// Decompiled with JetBrains decompiler
// Type: Roslyn.Utilities.ReferencePathUtilities
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace Roslyn.Utilities
{
    // internal 
    public static class ReferencePathUtilities
    {
        public static bool TryGetReferenceFilePath(string filePath, out string referenceFilePath)
        {
            string str = System.IO.Path.GetFileName(filePath);
            string extension = System.IO.Path.GetExtension(str);
            if (!string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
                str = str + ".dll";

            foreach (string path1 in ReferencePathUtilities.GetReferencePaths())
            {
                string path = System.IO.Path.Combine(path1, str);
                if (System.IO.File.Exists(path))
                {
                    referenceFilePath = path;
                    return true;
                }
            }
            referenceFilePath = (string)null;
            return false;
        }

        public static bool TryFindXmlDocumentationFile(string assemblyFilePath, out string xmlDocumentationFilePath)
        {
            string path1 = string.Empty;
            string path3 = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(assemblyFilePath), ".xml");
            string directoryName = System.IO.Path.GetDirectoryName(assemblyFilePath);
            for (System.Globalization.CultureInfo cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
                 cultureInfo != System.Globalization.CultureInfo.InvariantCulture; cultureInfo = cultureInfo.Parent)
            {
                path1 = System.IO.Path.Combine(directoryName, cultureInfo.Name, path3);
                if (System.IO.File.Exists(path1))
                    break;
            }
            if (System.IO.File.Exists(path1))
            {
                xmlDocumentationFilePath = path1;
                return true;
            }
            else
            {
                string extension = System.IO.Path.GetExtension(assemblyFilePath);
                string path2 = string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                    ? System.IO.Path.ChangeExtension(assemblyFilePath, ".xml") : assemblyFilePath + ".xml";
                if (System.IO.File.Exists(path2))
                {
                    xmlDocumentationFilePath = path2;
                    return true;
                }
                else
                {
                    string referenceFilePath;
                    if (!ReferencePathUtilities.TryGetReferenceFilePath(assemblyFilePath, out referenceFilePath))
                    {
                        xmlDocumentationFilePath = (string)null;
                        return false;
                    }
                    else
                    {
                        string path4 = System.IO.Path.ChangeExtension(referenceFilePath, ".xml");
                        if (System.IO.File.Exists(path4))
                        {
                            xmlDocumentationFilePath = path4;
                            return true;
                        }
                        else
                        {
                            xmlDocumentationFilePath = (string)null;
                            return false;
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetFrameworkPaths()
        {
            //return EnumerableExtensions.Concat<string>((IEnumerable<string>)
            //    GlobalAssemblyCache.RootLocations, RuntimeEnvironment.GetRuntimeDirectory());
            yield break;
        }

        // [IteratorStateMachine(typeof (ReferencePathUtilities.\u003CGetReferencePaths\u003Ed__3))]
        public static IEnumerable<string> GetReferencePaths()
        {
            yield return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
                "Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.5");
            yield return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
                "Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.0");
        }

        public static bool PartOfFrameworkOrReferencePaths(string filePath)
        {
            if (!PathUtilities.IsAbsolute(filePath))
                return false;
            string directory = System.IO.Path.GetDirectoryName(filePath);
            return Enumerable.Any<string>(Enumerable.Select<string, string>(Enumerable.Concat<string>(ReferencePathUtilities.GetReferencePaths(),
                ReferencePathUtilities.GetFrameworkPaths()),
                new Func<string, string>(FileUtilities.NormalizeDirectoryPath)),
                (Func<string, bool>)(dir => directory.StartsWith(dir, StringComparison.OrdinalIgnoreCase)));
        }
    }

    internal enum PathKind
    {
        /// <summary>
        /// Null or empty.
        /// </summary>
        Empty,

        /// <summary>
        /// "file"
        /// </summary>
        Relative,

        /// <summary>
        /// ".\file"
        /// </summary>
        RelativeToCurrentDirectory,

        /// <summary>
        /// "..\file"
        /// </summary>
        RelativeToCurrentParent,

        /// <summary>
        /// "\dir\file"
        /// </summary>
        RelativeToCurrentRoot,

        /// <summary>
        /// "C:dir\file"
        /// </summary>
        RelativeToDriveDirectory,

        /// <summary>
        /// "C:\file" or "\\machine" (UNC).
        /// </summary>
        Absolute,
    }

    internal static class FileUtilities
    {
        /// <summary>
        /// Resolves relative path and returns absolute path.
        /// The method depends only on values of its parameters and their implementation (for fileExists).
        /// It doesn't itself depend on the state of the current process (namely on the current drive directories) or 
        /// the state of file system.
        /// </summary>
        /// <param name="path">
        /// Path to resolve.
        /// </param>
        /// <param name="basePath">
        /// Base file path to resolve CWD-relative paths against. Null if not available.
        /// </param>
        /// <param name="baseDirectory">
        /// Base directory to resolve CWD-relative paths against if <paramref name="basePath"/> isn't specified. 
        /// Must be absolute path.
        /// Null if not available.
        /// </param>
        /// <param name="searchPaths">
        /// Sequence of paths used to search for unqualified relative paths.
        /// </param>
        /// <param name="fileExists">
        /// Method that tests existence of a file.
        /// </param>
        /// <returns>
        /// The resolved path or null if the path can't be resolved or does not exist.
        /// </returns>
        internal static string ResolveRelativePath(
            string path,
            string basePath,
            string baseDirectory,
            IEnumerable<string> searchPaths,
            Func<string, bool> fileExists)
        {
            Debug.Assert(baseDirectory == null || searchPaths != null || PathUtilities.IsAbsolute(baseDirectory));
            Debug.Assert(searchPaths != null);
            Debug.Assert(fileExists != null);

            string combinedPath;
            var kind = PathUtilities.GetPathKind(path);
            if (kind == PathKind.Relative)
            {
                // first, look in the base directory:
                baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                if (baseDirectory != null)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                // try search paths:
                foreach (var searchPath in searchPaths)
                {
                    combinedPath = PathUtilities.CombinePathsUnchecked(searchPath, path);
                    Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                    if (fileExists(combinedPath))
                    {
                        return combinedPath;
                    }
                }

                return null;
            }

            combinedPath = ResolveRelativePath(kind, path, basePath, baseDirectory);
            if (combinedPath != null)
            {
                Debug.Assert(PathUtilities.IsAbsolute(combinedPath));
                if (fileExists(combinedPath))
                {
                    return combinedPath;
                }
            }

            return null;
        }

        internal static string ResolveRelativePath(string path, string baseDirectory)
        {
            return ResolveRelativePath(path, null, baseDirectory);
        }

        internal static string ResolveRelativePath(string path, string basePath, string baseDirectory)
        {
            Debug.Assert(baseDirectory == null || PathUtilities.IsAbsolute(baseDirectory));
            return ResolveRelativePath(PathUtilities.GetPathKind(path), path, basePath, baseDirectory);
        }

        private static string ResolveRelativePath(PathKind kind, string path, string basePath, string baseDirectory)
        {
            switch (kind)
            {
                case PathKind.Empty:
                    return null;

                case PathKind.Relative:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    // with no search paths relative paths are relative to the base directory:
                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);

                case PathKind.RelativeToCurrentDirectory:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    if (path.Length == 1)
                    {
                        // "."
                        return baseDirectory;
                    }
                    else
                    {
                        // ".\path"
                        return PathUtilities.CombinePathsUnchecked(baseDirectory, path);
                    }

                case PathKind.RelativeToCurrentParent:
                    baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                    if (baseDirectory == null)
                    {
                        return null;
                    }

                    // ".."
                    return PathUtilities.CombinePathsUnchecked(baseDirectory, path);

                case PathKind.RelativeToCurrentRoot:
                    string baseRoot;
                    if (basePath != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(basePath);
                    }
                    else if (baseDirectory != null)
                    {
                        baseRoot = PathUtilities.GetPathRoot(baseDirectory);
                    }
                    else
                    {
                        return null;
                    }

                    if (baseRoot == null)
                    {
                        return null;
                    }

                    Debug.Assert(PathUtilities.IsDirectorySeparator(path[0]));
                    Debug.Assert(path.Length == 1 || !PathUtilities.IsDirectorySeparator(path[1]));
                    Debug.Assert(baseRoot.Length >= 3);
                    return PathUtilities.CombinePathsUnchecked(baseRoot, path.Substring(1));

                case PathKind.RelativeToDriveDirectory:
                    // drive relative paths not supported, can't resolve:
                    return null;

                case PathKind.Absolute:
                    return path;

                default:
                    // EDMAURER this is not using ExceptionUtilities.UnexpectedValue() because this file
                    // is shared via linking with other code that doesn't have the ExceptionUtilities.
                    throw new InvalidOperationException(string.Format("Unexpected PathKind {0}.", kind));
            }
        }

        private static string GetBaseDirectory(string basePath, string baseDirectory)
        {
            // relative base paths are relative to the base directory:
            string resolvedBasePath = ResolveRelativePath(basePath, baseDirectory);
            if (resolvedBasePath == null)
            {
                return baseDirectory;
            }

            // Note: Path.GetDirectoryName doesn't normalize the path and so it doesn't depend on the process state.
            Debug.Assert(PathUtilities.IsAbsolute(resolvedBasePath));
            try
            {
                return Path.GetDirectoryName(resolvedBasePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();

        internal static string NormalizeRelativePath(string path, string basePath, string baseDirectory)
        {
            // Does this look like a URI at all or does it have any invalid path characters? If so, just use it as is.
            if (path.IndexOf("://", StringComparison.Ordinal) >= 0 || path.IndexOfAny(s_invalidPathChars) >= 0)
            {
                return null;
            }

            string resolvedPath = ResolveRelativePath(path, basePath, baseDirectory);
            if (resolvedPath == null)
            {
                return null;
            }

            string normalizedPath = TryNormalizeAbsolutePath(resolvedPath);
            if (normalizedPath == null)
            {
                return null;
            }

            return normalizedPath;
        }

        /// <summary>
        /// Normalizes an absolute path.
        /// </summary>
        /// <param name="path">Path to normalize.</param>
        /// <exception cref="IOException"/>
        /// <returns>Normalized path.</returns>
        internal static string NormalizeAbsolutePath(string path)
        {
            // we can only call GetFullPath on an absolute path to avoid dependency on process state (current directory):
            Debug.Assert(PathUtilities.IsAbsolute(path));

            try
            {
                return Path.GetFullPath(path);
            }
            catch (ArgumentException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (System.Security.SecurityException e)
            {
                throw new IOException(e.Message, e);
            }
            catch (NotSupportedException e)
            {
                throw new IOException(e.Message, e);
            }
        }

        internal static string NormalizeDirectoryPath(string path)
        {
            return NormalizeAbsolutePath(path).TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        internal static string TryNormalizeAbsolutePath(string path)
        {
            Debug.Assert(PathUtilities.IsAbsolute(path));

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        internal static Stream OpenRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            try
            {
                return new FileStream(fullPath, FileMode.Open, 
                    FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        internal static Stream OpenAsyncRead(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            return RethrowExceptionsAsIOException(() => new FileStream(fullPath, 
                FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous));
        }

        internal static T RethrowExceptionsAsIOException<T>(Func<T> operation)
        {
            try
            {
                return operation();
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        /// <summary>
        /// Used to create a file given a path specified by the user.
        /// paramName - Provided by the Public surface APIs to have a clearer message. Internal API just rethrow the exception
        /// </summary>
        internal static Stream CreateFileStreamChecked(Func<string, Stream> factory, string path, string paramName = null)
        {
            try
            {
                return factory(path);
            }
            catch (ArgumentNullException)
            {
                if (paramName == null)
                {
                    throw;
                }
                else
                {
                    throw new ArgumentNullException(paramName);
                }
            }
            catch (ArgumentException e)
            {
                if (paramName == null)
                {
                    throw;
                }
                else
                {
                    throw new ArgumentException(e.Message, paramName);
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }

        /// <exception cref="IOException"/>
        internal static DateTime GetFileTimeStamp(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            try
            {
                return File.GetLastWriteTimeUtc(fullPath);
            }
            catch (Exception e)
            {
                throw new IOException(e.Message);
            }
        }

        internal static Stream OpenFileStream(string path)
        {
            try
            {
                // return PortableShim.File.OpenRead(path);
                return File.OpenRead(path);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (IOException e)
            {
                if (e.GetType().Name == "DirectoryNotFoundException")
                {
                    throw new FileNotFoundException(e.Message, path, e);
                }

                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }
    }


    internal static class PathUtilities
    {
        // We consider '/' a directory separator on Unix like systems. 
        // On Windows both / and \ are equally accepted.
        internal static readonly char DirectorySeparatorChar = IsUnixLikePlatform ? '/' : '\\';
        internal static readonly char AltDirectorySeparatorChar = '/';
        internal static readonly string DirectorySeparatorStr = new string(DirectorySeparatorChar, 1);
        internal const char VolumeSeparatorChar = ':';

        private static bool IsUnixLikePlatform
        {
            get
            {
                return Path.DirectorySeparatorChar == '/';
            }
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
        }

        internal static string TrimTrailingSeparators(string s)
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

        internal static string GetExtension(string path)
        {
            return FileNameUtilities.GetExtension(path);
        }

        internal static string ChangeExtension(string path, string extension)
        {
            return FileNameUtilities.ChangeExtension(path, extension);
        }

        internal static string RemoveExtension(string path)
        {
            return FileNameUtilities.ChangeExtension(path, extension: null);
        }

        internal static string GetFileName(string path, bool includeExtension = true)
        {
            return FileNameUtilities.GetFileName(path, includeExtension);
        }

        /// <summary>
        /// Get directory name from path.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="System.IO.Path.GetDirectoryName"/> it
        ///     doesn't check for invalid path characters, 
        ///     doesn't strip any trailing directory separators (TODO: tomat),
        ///     doesn't recognize UNC structure \\computer-name\share\directory-name\file-name (TODO: tomat).
        /// </remarks>
        /// <returns>Prefix of path that represents a directory. </returns>
        internal static string GetDirectoryName(string path)
        {
            int fileNameStart = FileNameUtilities.IndexOfFileName(path);
            if (fileNameStart < 0)
            {
                return null;
            }

            return path.Substring(0, fileNameStart);
        }

        internal static PathKind GetPathKind(string path)
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

        internal static bool IsAbsolute(string path)
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
        /// Get a prefix of given path which is the root of the path.
        /// </summary>
        /// <returns>
        /// Root of an absolute path or null if the path isn't absolute or has invalid format (e.g. "\\").
        /// It may or may not end with a directory separator (e.g. "C:\", "C:\foo", "\\machine\share", etc.) .
        /// </returns>
        internal static string GetPathRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            int length = GetPathRootLength(path);
            return (length != -1) ? path.Substring(0, length) : null;
        }

        private static int GetPathRootLength(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (IsUnixLikePlatform)
            {
                if (IsDirectorySeparator(path[0]))
                {
                    //  "/*"
                    return 1;
                }
            }
            else
            {
                // "C:\"
                if (IsDriveRootedAbsolutePath(path))
                {
                    return 3;
                }

                if (IsDirectorySeparator(path[0]))
                {
                    // "\\machine\share"
                    return GetUncPathRootLength(path);
                }
            }

            return -1;
        }

        /// <summary>
        /// Calculates the length of root of an UNC path.
        /// </summary>
        /// <remarks>
        /// "\\server\share" is root of UNC path "\\server\share\dir1\dir2\file".
        /// </remarks>
        private static int GetUncPathRootLength(string path)
        {
            Debug.Assert(IsDirectorySeparator(path[0]));

            // root:
            // [directory-separator]{2,}[^directory-separator]+[directory-separator]+[^directory-separator]+

            int serverIndex = IndexOfNonDirectorySeparator(path, 1);
            if (serverIndex < 2)
            {
                return -1;
            }

            int separator = IndexOfDirectorySeparator(path, serverIndex);
            if (separator == -1)
            {
                return -1;
            }

            int shareIndex = IndexOfNonDirectorySeparator(path, separator);
            if (shareIndex == -1)
            {
                return -1;
            }

            int rootEnd = IndexOfDirectorySeparator(path, shareIndex);
            return rootEnd == -1 ? path.Length : rootEnd;
        }

        private static int IndexOfDirectorySeparator(string path, int start)
        {
            for (int i = start; i < path.Length; i++)
            {
                if (IsDirectorySeparator(path[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOfNonDirectorySeparator(string path, int start)
        {
            for (int i = start; i < path.Length; i++)
            {
                if (!IsDirectorySeparator(path[i]))
                {
                    return i;
                }
            }

            return -1;
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
        internal static string CombineAbsoluteAndRelativePaths(string root, string relativePath)
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
        internal static string CombinePossiblyRelativeAndRelativePaths(string rootOpt, string relativePath)
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

        internal static string CombinePathsUnchecked(string root, string relativePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(root));

            char c = root[root.Length - 1];
            if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
            {
                return root + DirectorySeparatorStr + relativePath;
            }

            return root + relativePath;
        }

        internal static string RemoveTrailingDirectorySeparator(string path)
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
        internal static bool IsFilePath(string assemblyDisplayNameOrPath)
        {
            Debug.Assert(assemblyDisplayNameOrPath != null);

            string extension = FileNameUtilities.GetExtension(assemblyDisplayNameOrPath);
            return string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
                || assemblyDisplayNameOrPath.IndexOf(DirectorySeparatorChar) != -1
                || assemblyDisplayNameOrPath.IndexOf(AltDirectorySeparatorChar) != -1;
        }
    }

    internal static class FileNameUtilities
    {
        private const string DirectorySeparatorStr = "\\";
        internal const char DirectorySeparatorChar = '\\';
        internal const char AltDirectorySeparatorChar = '/';
        internal const char VolumeSeparatorChar = ':';

        /// <summary>
        /// Returns true if the string represents an unqualified file name. 
        /// The name may contain any characters but directory and volume separators.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>
        /// True if <paramref name="path"/> is a simple file name, false if it is null or includes a directory specification.
        /// </returns>
        internal static bool IsFileName(string path)
        {
            return IndexOfFileName(path) == 0;
        }

        /// <summary>
        /// Returns the offset in <paramref name="path"/> where the dot that starts an extension is, or -1 if the path doesn't have an extension.
        /// </summary>
        /// <remarks>
        /// Returns 0 for path ".foo".
        /// Returns -1 for path "foo.".
        /// </remarks>
        private static int IndexOfExtension(string path)
        {
            if (path == null)
            {
                return -1;
            }

            int length = path.Length;
            int i = length;

            while (--i >= 0)
            {
                char c = path[i];
                if (c == '.')
                {
                    if (i != length - 1)
                    {
                        return i;
                    }

                    return -1;
                }

                if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar)
                {
                    break;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns an extension of the specified path string.
        /// </summary>
        /// <remarks>
        /// The same functionality as <see cref="System.IO.Path.GetExtension(string)"/> but doesn't throw an exception
        /// if there are invalid characters in the path.
        /// </remarks>
        internal static string GetExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            return (index >= 0) ? path.Substring(index) : string.Empty;
        }

        /// <summary>
        /// Removes extension from path.
        /// </summary>
        /// <remarks>
        /// Returns "foo" for path "foo.".
        /// Returns "foo.." for path "foo...".
        /// </remarks>
        private static string RemoveExtension(string path)
        {
            if (path == null)
            {
                return null;
            }

            int index = IndexOfExtension(path);
            if (index >= 0)
            {
                return path.Substring(0, index);
            }

            // trim last ".", if present
            if (path.Length > 0 && path[path.Length - 1] == '.')
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        /// <summary>
        /// Returns path with the extension changed to <paramref name="extension"/>.
        /// </summary>
        /// <returns>
        /// Equivalent of <see cref="System.IO.Path.ChangeExtension(string, string)"/>
        /// 
        /// If <paramref name="path"/> is null, returns null. 
        /// If path does not end with an extension, the new extension is appended to the path.
        /// If extension is null, equivalent to <see cref="RemoveExtension"/>.
        /// </returns>
        internal static string ChangeExtension(string path, string extension)
        {
            if (path == null)
            {
                return null;
            }

            var pathWithoutExtension = RemoveExtension(path);
            if (extension == null || path.Length == 0)
            {
                return pathWithoutExtension;
            }

            if (extension.Length == 0 || extension[0] != '.')
            {
                return pathWithoutExtension + "." + extension;
            }

            return pathWithoutExtension + extension;
        }

        /// <summary>
        /// Returns the position in given path where the file name starts.
        /// </summary>
        /// <returns>-1 if path is null.</returns>
        internal static int IndexOfFileName(string path)
        {
            if (path == null)
            {
                return -1;
            }

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Get file name from path.
        /// </summary>
        /// <remarks>Unlike <see cref="System.IO.Path.GetFileName"/> doesn't check for invalid path characters.</remarks>
        internal static string GetFileName(string path, bool includeExtension = true)
        {
            int fileNameStart = IndexOfFileName(path);
            var fileName = (fileNameStart <= 0) ? path : path.Substring(fileNameStart);
            return includeExtension ? fileName : RemoveExtension(fileName);
        }
    }

    // internal 
    public static class FilePathUtilities
    {
        private static readonly char[] s_pathChars;

        static FilePathUtilities()
        {
            char[] chArray = new char[3];
            int index1 = 0;
            int num1 = (int)System.IO.Path.VolumeSeparatorChar;
            chArray[index1] = (char)num1;
            int index2 = 1;
            int num2 = (int)System.IO.Path.DirectorySeparatorChar;
            chArray[index2] = (char)num2;
            int index3 = 2;
            int num3 = (int)System.IO.Path.AltDirectorySeparatorChar;
            chArray[index3] = (char)num3;
            FilePathUtilities.s_pathChars = chArray;
        }

        public static bool IsNestedPath(string basePath, string fullPath)
        {
            return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetNestedPath(string baseDirectory, string fullPath)
        {
            if (!FilePathUtilities.IsNestedPath(baseDirectory, fullPath))
                return fullPath;
            string str = fullPath.Substring(baseDirectory.Length);
            while (str.Length > 0 && PathUtilities.IsDirectorySeparator(str[0]))
                str = str.Substring(1);
            return str;
        }

        public static string GetRelativePath(string baseDirectory, string fullPath)
        {
            string path1 = string.Empty;
            if (FilePathUtilities.IsNestedPath(baseDirectory, fullPath))
                return FilePathUtilities.GetNestedPath(baseDirectory, fullPath);
            string[] strArray1 = baseDirectory.Split(FilePathUtilities.s_pathChars);
            string[] strArray2 = fullPath.Split(FilePathUtilities.s_pathChars);
            if (strArray1.Length == 0 || strArray2.Length == 0)
                return fullPath;
            int index1 = 0;
            while (index1 < strArray1.Length && FilePathUtilities.PathsEqual(strArray1[index1], strArray2[index1]))
                ++index1;
            if (index1 == 0)
                return fullPath;
            int num = strArray1.Length - index1;
            if (num > 0)
            {
                string str = System.IO.Path.DirectorySeparatorChar.ToString();
                for (int index2 = 0; index2 < num; ++index2)
                    path1 = path1 + ".." + str;
            }
            for (int index2 = index1; index2 < strArray2.Length; ++index2)
                path1 = System.IO.Path.Combine(path1, strArray2[index2]);
            return path1;
        }

        internal static void RequireAbsolutePath(string path, string argumentName)
        {
            if (path == null)
                throw new ArgumentNullException(argumentName);
            if (!PathUtilities.IsAbsolute(path))
                throw new ArgumentException("AbsolutePathExpected", argumentName);
        }

        public static bool PathsEqual(string path1, string path2)
        {
            return string.Compare(path1, path2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool TryCombine(string path1, string path2, out string result)
        {
            try
            {
                result = System.IO.Path.Combine(path1, path2);
                return true;
            }
            catch
            {
                result = (string)null;
                return false;
            }
        }
    }

    // internal 
    public struct FileKey : IEquatable<FileKey>
    {
        public readonly string FullPath;
        public readonly DateTime Timestamp;

        public FileKey(string fullPath, DateTime timestamp)
        {
            this.FullPath = fullPath;
            this.Timestamp = timestamp;
        }

        public static FileKey Create(string fullPath)
        {
            return new FileKey(fullPath, FileUtilities.GetFileTimeStamp(fullPath));
        }

        public override int GetHashCode()
        {
            return
                //Hash.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(this.FullPath)
                + this.Timestamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is FileKey)
                return this.Equals((FileKey)obj);
            else
                return false;
        }

        public override string ToString()
        {
            return string.Format("'{0}'@{1}", (object)this.FullPath, (object)this.Timestamp);
        }

        public bool Equals(FileKey other)
        {
            if (this.Timestamp == other.Timestamp)
                return string.Equals(this.FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
            else
                return false;
        }
    }
}
