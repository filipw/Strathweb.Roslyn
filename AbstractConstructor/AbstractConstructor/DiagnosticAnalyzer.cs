using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AbstractConstructor
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AbstractConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AbstractConstructor";
        internal const string Title = "Type name contains lowercase letters";
        internal const string MessageFormat = "Type name '{0}' contains lowercase letters";
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context) {
            var semanticModel = context.SemanticModel;
            var constructorDeclaration = context.Node as ConstructorDeclarationSyntax;

            var containingClass = constructorDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (containingClass == null || !containingClass.Modifiers.Any(SyntaxKind.AbstractKeyword)) return;

            if (!constructorDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword)) return;

            var location = constructorDeclaration.Modifiers.FirstOrDefault().GetLocation();

            var diagnostic = Diagnostic.Create(Rule, location, constructorDeclaration.Identifier.ToString());

            context.ReportDiagnostic(diagnostic);
        }

        //private static void AnalyzeSymbol(SymbolAnalysisContext context)
        //{
        //    // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
        //    var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        //    // Find just those named type symbols with names containing lowercase letters.
        //    if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
        //    {
        //        // For all such symbols, produce a diagnostic.
        //        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

        //        context.ReportDiagnostic(diagnostic);
        //    }
        //}
    }
}
