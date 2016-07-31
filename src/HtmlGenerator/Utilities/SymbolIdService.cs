using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class SymbolIdService
    {
        public static string GetId(ISymbol symbol)
        {
            string result = GetSymbolString(symbol);
            return GetId(result);
        }

        public static ulong GetIdULong(ISymbol symbol)
        {
            string result = GetSymbolString(symbol);
            return GetIdULong(result);
        }

        private static string GetSymbolString(ISymbol symbol)
        {
            string result = null;

            if (symbol.Kind == SymbolKind.Parameter ||
                symbol.Kind == SymbolKind.Local)
            {
                string parent = GetDocumentationCommentId(symbol.ContainingSymbol);
                result = parent + ":" + symbol.MetadataName;
            }
            else
            {
                result = GetDocumentationCommentId(symbol);
            }

            return result;
        }

        public static readonly SymbolDisplayFormat CSharpFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                // Not showing the name is important because we visit parameters to display their
                // types.  If we visited their types directly, we wouldn't get ref/out/params.
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        public static readonly SymbolDisplayFormat VisualBasicFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                kindOptions:
                    SymbolDisplayKindOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);

        public static readonly SymbolDisplayFormat ShortNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.None,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

        public static string GetDisplayString(ISymbol symbol)
        {
            var format = symbol.Language == LanguageNames.CSharp ? CSharpFormat : VisualBasicFormat;
            var result = symbol.ToDisplayString(format);
            return result;
        }

        public static string GetId(string result)
        {
            result = Paths.GetMD5Hash(result, 16);
            return result;
        }

        public static ulong GetIdULong(string content)
        {
            return Paths.GetMD5HashULong(content, 16);
        }

        public static string GetId(Document document)
        {
            var documentRelativePath = Paths.GetRelativeFilePathInProject(document);
            return GetId(documentRelativePath);
        }

        public static string GetAssemblyId(string assemblyName)
        {
            return assemblyName;
        }

        public static string GetAssemblyId(IAssemblySymbol assemblySymbol)
        {
            return assemblySymbol.Name;
        }

        public static string GetName(ISymbol symbol)
        {
            return symbol.ToDisplayString(ShortNameFormat);
        }

        private static MethodInfo getGlyph
        {
            get
            {
                MethodInfo method = null;
                try
                {
                    method = Assembly.Load("Microsoft.CodeAnalysis.Features")
                            .GetType("Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions2")
                            .GetMethod("GetGlyph");
                }
                catch (Exception) { };
                return method;
            }
        }

        // "../../0000000000"
        public static readonly byte[] ZeroId = new byte[] { 46, 46, 47, 46, 46, 47, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48 };

private static string GetDocumentationCommentId(ISymbol symbol)
{
    string result = null;
    if (!symbol.IsDefinition)
    {
        symbol = symbol.OriginalDefinition;
    }

    result = symbol.GetDocumentationCommentId();

    result = result.Replace("#ctor", "ctor");

    return result;
}

public static ushort GetGlyphNumber(ISymbol declaredSymbol)
{
    var glyph = (Glyph)getGlyph.Invoke(null, new object[] { declaredSymbol });
    ushort result = (ushort)((ushort)GetStandardGlyphGroup(glyph) + (ushort)GetStandardGlyphItem(glyph));
    return result;
}

private static StandardGlyphGroup GetStandardGlyphGroup(Glyph glyph)
{
    switch (glyph)
    {
        case Glyph.Assembly:
            return StandardGlyphGroup.GlyphAssembly;

        case Glyph.ClassPublic:
        case Glyph.ClassProtected:
        case Glyph.ClassPrivate:
        case Glyph.ClassInternal:
            return StandardGlyphGroup.GlyphGroupClass;

        case Glyph.ConstantPublic:
        case Glyph.ConstantProtected:
        case Glyph.ConstantPrivate:
        case Glyph.ConstantInternal:
            return StandardGlyphGroup.GlyphGroupConstant;

        case Glyph.CSharpFile:
            return StandardGlyphGroup.GlyphCSharpFile;

        case Glyph.DelegatePublic:
        case Glyph.DelegateProtected:
        case Glyph.DelegatePrivate:
        case Glyph.DelegateInternal:
            return StandardGlyphGroup.GlyphGroupDelegate;

        case Glyph.EnumPublic:
        case Glyph.EnumProtected:
        case Glyph.EnumPrivate:
        case Glyph.EnumInternal:
            return StandardGlyphGroup.GlyphGroupEnum;

        case Glyph.EnumMember:
            return StandardGlyphGroup.GlyphGroupEnumMember;

        case Glyph.Error:
            return StandardGlyphGroup.GlyphGroupError;

        case Glyph.ExtensionMethodPublic:
            return StandardGlyphGroup.GlyphExtensionMethod;

        case Glyph.ExtensionMethodProtected:
            return StandardGlyphGroup.GlyphExtensionMethodProtected;

        case Glyph.ExtensionMethodPrivate:
            return StandardGlyphGroup.GlyphExtensionMethodPrivate;

        case Glyph.ExtensionMethodInternal:
            return StandardGlyphGroup.GlyphExtensionMethodInternal;

        case Glyph.EventPublic:
        case Glyph.EventProtected:
        case Glyph.EventPrivate:
        case Glyph.EventInternal:
            return StandardGlyphGroup.GlyphGroupEvent;

        case Glyph.FieldPublic:
        case Glyph.FieldProtected:
        case Glyph.FieldPrivate:
        case Glyph.FieldInternal:
            return StandardGlyphGroup.GlyphGroupField;

        case Glyph.InterfacePublic:
        case Glyph.InterfaceProtected:
        case Glyph.InterfacePrivate:
        case Glyph.InterfaceInternal:
            return StandardGlyphGroup.GlyphGroupInterface;

        case Glyph.Intrinsic:
            return StandardGlyphGroup.GlyphGroupIntrinsic;

        case Glyph.Keyword:
            return StandardGlyphGroup.GlyphKeyword;

        case Glyph.Label:
            return StandardGlyphGroup.GlyphGroupIntrinsic;

        case Glyph.Local:
            return StandardGlyphGroup.GlyphGroupVariable;

        case Glyph.Namespace:
            return StandardGlyphGroup.GlyphGroupNamespace;

        case Glyph.MethodPublic:
        case Glyph.MethodProtected:
        case Glyph.MethodPrivate:
        case Glyph.MethodInternal:
            return StandardGlyphGroup.GlyphGroupMethod;

        case Glyph.ModulePublic:
        case Glyph.ModuleProtected:
        case Glyph.ModulePrivate:
        case Glyph.ModuleInternal:
            return StandardGlyphGroup.GlyphGroupModule;

        case Glyph.OpenFolder:
            return StandardGlyphGroup.GlyphOpenFolder;

        case Glyph.Operator:
            return StandardGlyphGroup.GlyphGroupOperator;

        case Glyph.Parameter:
            return StandardGlyphGroup.GlyphGroupVariable;

        case Glyph.PropertyPublic:
        case Glyph.PropertyProtected:
        case Glyph.PropertyPrivate:
        case Glyph.PropertyInternal:
            return StandardGlyphGroup.GlyphGroupProperty;

        case Glyph.RangeVariable:
            return StandardGlyphGroup.GlyphGroupVariable;

        case Glyph.Reference:
            return StandardGlyphGroup.GlyphReference;

        case Glyph.StructurePublic:
        case Glyph.StructureProtected:
        case Glyph.StructurePrivate:
        case Glyph.StructureInternal:
            return StandardGlyphGroup.GlyphGroupStruct;

        case Glyph.TypeParameter:
            return StandardGlyphGroup.GlyphGroupType;

        case Glyph.Up:
        case Glyph.Down:
        case Glyph.Left:
        case Glyph.Right:
        case Glyph.Dot:
            return StandardGlyphGroup.GlyphArrow;

        default:
            throw new ArgumentException("glyph");
    }
}

private static StandardGlyphItem GetStandardGlyphItem(Glyph icon)
{
    switch (icon)
    {
        case Glyph.ClassProtected:
        case Glyph.ConstantProtected:
        case Glyph.DelegateProtected:
        case Glyph.EnumProtected:
        case Glyph.EventProtected:
        case Glyph.FieldProtected:
        case Glyph.InterfaceProtected:
        case Glyph.MethodProtected:
        case Glyph.ModuleProtected:
        case Glyph.PropertyProtected:
        case Glyph.StructureProtected:
            return StandardGlyphItem.GlyphItemProtected;

        case Glyph.ClassPrivate:
        case Glyph.ConstantPrivate:
        case Glyph.DelegatePrivate:
        case Glyph.EnumPrivate:
        case Glyph.EventPrivate:
        case Glyph.FieldPrivate:
        case Glyph.InterfacePrivate:
        case Glyph.MethodPrivate:
        case Glyph.ModulePrivate:
        case Glyph.PropertyPrivate:
        case Glyph.StructurePrivate:
            return StandardGlyphItem.GlyphItemPrivate;

        case Glyph.ClassInternal:
        case Glyph.ConstantInternal:
        case Glyph.DelegateInternal:
        case Glyph.EnumInternal:
        case Glyph.EventInternal:
        case Glyph.FieldInternal:
        case Glyph.InterfaceInternal:
        case Glyph.MethodInternal:
        case Glyph.ModuleInternal:
        case Glyph.PropertyInternal:
        case Glyph.StructureInternal:
            return StandardGlyphItem.GlyphItemFriend;

        default:
            // We don't want any overlays
            return StandardGlyphItem.GlyphItemPublic;
    }
}
    }
}

#if COREFX
namespace Microsoft.VisualStudio.Language.Intellisense
{
    //
    // Summary:
    //     Describes the different types of glyphs that can be displayed in the default
    //     completion tool implementation.
    public enum StandardGlyphGroup
    {
        //
        // Summary:
        //     Describes symbols for classes.
        GlyphGroupClass = 0,
        //
        // Summary:
        //     Describes symbols for constants.
        GlyphGroupConstant = 6,
        //
        // Summary:
        //     Describes symbols for delegates.
        GlyphGroupDelegate = 12,
        //
        // Summary:
        //     Describes symbols for enumerations.
        GlyphGroupEnum = 18,
        //
        // Summary:
        //     Describes symbols for enumeration members.
        GlyphGroupEnumMember = 24,
        //
        // Summary:
        //     Describes symbols for events.
        GlyphGroupEvent = 30,
        //
        // Summary:
        //     Describes symbols for exceptions.
        GlyphGroupException = 36,
        //
        // Summary:
        //     Describes symbols for fields.
        GlyphGroupField = 42,
        //
        // Summary:
        //     Describes symbols for interfaces.
        GlyphGroupInterface = 48,
        //
        // Summary:
        //     Describes symbols for macros.
        GlyphGroupMacro = 54,
        //
        // Summary:
        //     Describes symbols for maps.
        GlyphGroupMap = 60,
        //
        // Summary:
        //     Describes symbols for map items.
        GlyphGroupMapItem = 66,
        //
        // Summary:
        //     Describes symbols for methods.
        GlyphGroupMethod = 72,
        //
        // Summary:
        //     Describes symbols for overloads.
        GlyphGroupOverload = 78,
        //
        // Summary:
        //     Describes symbols for modules.
        GlyphGroupModule = 84,
        //
        // Summary:
        //     Describes symbols for namespaces.
        GlyphGroupNamespace = 90,
        //
        // Summary:
        //     Describes symbols for operators.
        GlyphGroupOperator = 96,
        //
        // Summary:
        //     Describes symbols for properties.
        GlyphGroupProperty = 102,
        //
        // Summary:
        //     Describes symbols for structures.
        GlyphGroupStruct = 108,
        //
        // Summary:
        //     Describes symbols for templates.
        GlyphGroupTemplate = 114,
        //
        // Summary:
        //     Describes symbols for typedefs.
        GlyphGroupTypedef = 120,
        //
        // Summary:
        //     Describes symbols for types.
        GlyphGroupType = 126,
        //
        // Summary:
        //     Describes symbols for unions.
        GlyphGroupUnion = 132,
        //
        // Summary:
        //     Describes symbols for variables.
        GlyphGroupVariable = 138,
        //
        // Summary:
        //     Describes symbols for value types.
        GlyphGroupValueType = 144,
        //
        // Summary:
        //     Describes intrinsic symbols.
        GlyphGroupIntrinsic = 150,
        //
        // Summary:
        //     Describes symbols for J# methods.
        GlyphGroupJSharpMethod = 156,
        //
        // Summary:
        //     Describes symbols for J# fields.
        GlyphGroupJSharpField = 162,
        //
        // Summary:
        //     Describes symbols for J# classes.
        GlyphGroupJSharpClass = 168,
        //
        // Summary:
        //     Describes symbols for J# namespaces.
        GlyphGroupJSharpNamespace = 174,
        //
        // Summary:
        //     Describes symbols for J# interfaces.
        GlyphGroupJSharpInterface = 180,
        //
        // Summary:
        //     Describes symbols for errors.
        GlyphGroupError = 186,
        //
        // Summary:
        //     Describes symbols for BSC files.
        GlyphBscFile = 191,
        //
        // Summary:
        //     Describes symbols for assemblies.
        GlyphAssembly = 192,
        //
        // Summary:
        //     Describes symbols for libraries.
        GlyphLibrary = 193,
        //
        // Summary:
        //     Describes symbols for VB projects.
        GlyphVBProject = 194,
        //
        // Summary:
        //     Describes symbols for C# projects.
        GlyphCoolProject = 196,
        //
        // Summary:
        //     Describes symbols for C++ projects.
        GlyphCppProject = 199,
        //
        // Summary:
        //     Describes symbols for dialog identifiers.
        GlyphDialogId = 200,
        //
        // Summary:
        //     Describes symbols for open folders.
        GlyphOpenFolder = 201,
        //
        // Summary:
        //     Describes symbols for closed folders.
        GlyphClosedFolder = 202,
        //
        // Summary:
        //     Describes arrow symbols.
        GlyphArrow = 203,
        //
        // Summary:
        //     Describes symbols for C# files.
        GlyphCSharpFile = 204,
        //
        // Summary:
        //     Describes symbols for C# expansions.
        GlyphCSharpExpansion = 205,
        //
        // Summary:
        //     Describes symbols for keywords.
        GlyphKeyword = 206,
        //
        // Summary:
        //     Describes symbols for information.
        GlyphInformation = 207,
        //
        // Summary:
        //     Describes symbols for references.
        GlyphReference = 208,
        //
        // Summary:
        //     Describes symbols for recursion.
        GlyphRecursion = 209,
        //
        // Summary:
        //     Describes symbols for XML items.
        GlyphXmlItem = 210,
        //
        // Summary:
        //     Describes symbols for J# projects.
        GlyphJSharpProject = 211,
        //
        // Summary:
        //     Describes symbols for J# documents.
        GlyphJSharpDocument = 212,
        //
        // Summary:
        //     Describes symbols for forwarded types.
        GlyphForwardType = 213,
        //
        // Summary:
        //     Describes symbols for callers graphs.
        GlyphCallersGraph = 214,
        //
        // Summary:
        //     Describes symbols for call graphs.
        GlyphCallGraph = 215,
        //
        // Summary:
        //     Describes symbols for build warnings.
        GlyphWarning = 216,
        //
        // Summary:
        //     Describes symbols for something that may be a reference.
        GlyphMaybeReference = 217,
        //
        // Summary:
        //     Describes symbols for something that may be a caller.
        GlyphMaybeCaller = 218,
        //
        // Summary:
        //     Describes symbols for something that may be a call.
        GlyphMaybeCall = 219,
        //
        // Summary:
        //     Describes symbols for extension methods.
        GlyphExtensionMethod = 220,
        //
        // Summary:
        //     Describes symbols for internal extension methods.
        GlyphExtensionMethodInternal = 221,
        //
        // Summary:
        //     Describes symbols for friend extension methods.
        GlyphExtensionMethodFriend = 222,
        //
        // Summary:
        //     Describes symbols for protected extension methods.
        GlyphExtensionMethodProtected = 223,
        //
        // Summary:
        //     Describes symbols for private extension methods.
        GlyphExtensionMethodPrivate = 224,
        //
        // Summary:
        //     Describes symbols for extension method shortcuts.
        GlyphExtensionMethodShortcut = 225,
        //
        // Summary:
        //     Describes symbols for XML attributes.
        GlyphXmlAttribute = 226,
        //
        // Summary:
        //     Describes symbols for child XML elements.
        GlyphXmlChild = 227,
        //
        // Summary:
        //     Describes symbols for descendant XML elements.
        GlyphXmlDescendant = 228,
        //
        // Summary:
        //     Describes symbols for XML namespaces.
        GlyphXmlNamespace = 229,
        //
        // Summary:
        //     Describes symbols with a question mark for XML attributes.
        GlyphXmlAttributeQuestion = 230,
        //
        // Summary:
        //     Describes symbols with a check mark for XML attributes.
        GlyphXmlAttributeCheck = 231,
        //
        // Summary:
        //     Describes symbols with a question mark for XML child elements.
        GlyphXmlChildQuestion = 232,
        //
        // Summary:
        //     Describes symbols with a check mark for XML child elements.
        GlyphXmlChildCheck = 233,
        //
        // Summary:
        //     Describes symbols with a question mark for XML descendant elements.
        GlyphXmlDescendantQuestion = 234,
        //
        // Summary:
        //     Describes symbols with a check mark for XML descendant elements.
        GlyphXmlDescendantCheck = 235,
        //
        // Summary:
        //     Describes symbols for completion warnings
        GlyphCompletionWarning = 236,
        //
        // Summary:
        //     Describes symbols for unknown types.
        GlyphGroupUnknown = 237
    }

    //
    // Summary:
    //     Describes icons or glyphs that are used in statement completion.
    public enum StandardGlyphItem
    {
        //
        // Summary:
        //     Describes a public symbol.
        GlyphItemPublic = 0,
        //
        // Summary:
        //     Describes an internal symbol.
        GlyphItemInternal = 1,
        //
        // Summary:
        //     Describes a friend symbol.
        GlyphItemFriend = 2,
        //
        // Summary:
        //     Describes a protected symbol.
        GlyphItemProtected = 3,
        //
        // Summary:
        //     Describes a private symbol.
        GlyphItemPrivate = 4,
        //
        // Summary:
        //     Describes a shortcut symbol.
        GlyphItemShortcut = 5,
        //
        // Summary:
        //     Describes a symbol that has all (or none) of the standard attributes.
        TotalGlyphItems = 6
    }
}

#endif