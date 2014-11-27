using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AbstractConstructor
{
    [ExportCodeFixProvider("AbstractConstructorCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AbstractConstructorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AbstractConstructorAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();

            var previousWhiteSpacesToken = SyntaxFactory.Token(declaration.GetLeadingTrivia(), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty);
            var newList = SyntaxTokenList.Create(previousWhiteSpacesToken);

            var newDeclaration = declaration.WithModifiers(newList).
                AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)).
                AddModifiers(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Space), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty));

            // Register a code action that will invoke the fix.
                context.RegisterFix(
                CodeAction.Create("Make constructor protected", c => ReplaceConstructorInDocumentAsync(context.Document, declaration, newDeclaration, c)),
                diagnostic);
        }

        private static async Task<Document> ReplaceConstructorInDocumentAsync(
            Document document,
            ConstructorDeclarationSyntax oldConstructor,
            ConstructorDeclarationSyntax newConstructor,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(oldConstructor, new[] { newConstructor });

            return document.WithSyntaxRoot(newRoot);
        }
    }
}