﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract partial class AbstractSyntaxTreeFactoryService : ISyntaxTreeFactoryService
    {
        // Recoverable trees only save significant memory for larger trees
        internal readonly int MinimumLengthForRecoverableTree;
        private readonly bool _hasCachingService;

        internal HostLanguageServices LanguageServices { get; }

        public AbstractSyntaxTreeFactoryService(HostLanguageServices languageServices)
        {
            this.LanguageServices = languageServices;
            this.MinimumLengthForRecoverableTree = languageServices.WorkspaceServices.Workspace.Options.GetOption(CacheOptions.RecoverableTreeLengthThreshold);
            _hasCachingService = languageServices.WorkspaceServices.GetService<IProjectCacheHostService>() != null;
        }

        public abstract ParseOptions GetDefaultParseOptions();
        public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SyntaxNode root);
        public abstract SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
        public abstract SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string filePath, ParseOptions options, ValueSource<TextAndVersion> text, Encoding encoding, SyntaxNode root);
        public abstract SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);

        public virtual bool CanCreateRecoverableTree(SyntaxNode root)
        {
            return _hasCachingService && root.FullSpan.Length >= this.MinimumLengthForRecoverableTree;
        }

        protected static SyntaxNode RecoverNode(SyntaxTree tree, TextSpan textSpan, int kind)
        {
            var token = tree.GetRoot().FindToken(textSpan.Start, findInsideTrivia: true);
            var node = token.Parent;

            while (node != null)
            {
                if (node.Span == textSpan && node.RawKind == kind)
                {
                    return node;
                }

                var structuredTrivia = node as IStructuredTriviaSyntax;
                if (structuredTrivia != null)
                {
                    node = structuredTrivia.ParentTrivia.Token.Parent;
                }
                else
                {
                    node = node.Parent;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
