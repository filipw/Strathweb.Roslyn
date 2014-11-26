using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MoveClassToFile
{
    [ExportCodeRefactoringProvider(RefactoringId, LanguageNames.CSharp), Shared]
    internal class MoveClassToFileCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "MoveClassToFile";

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // only for a type declaration node that doesn't match the current file name
            // also omit all private classes
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null ||
                context.Document.Name.ToLowerInvariant() == string.Format("{0}.cs", typeDecl.Identifier.ToString().ToLowerInvariant()) ||
                typeDecl.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return;
            }

            var action = CodeAction.Create("Move class to file", c => ReverseTypeNameAsync(context.Document, typeDecl, c));
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> ReverseTypeNameAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var identifierToken = typeDecl.Identifier;

            // symbol representing the type
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // remove type from current files
            var currentSyntaxTree = await document.GetSyntaxTreeAsync();
            var currentRoot = await currentSyntaxTree.GetRootAsync();
            var replacedRoot = currentRoot.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia);

            document = document.WithSyntaxRoot(replacedRoot);

            // create new tree for a new file
            // we drag all the usings because we don't know which are needed
            // and there is no easy way to find out which
            var currentUsings = currentRoot.DescendantNodesAndSelf().Where(s => s is UsingDirectiveSyntax);

            var newFileTree = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>(currentUsings.Select(i => (UsingDirectiveSyntax)i)))
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.NamespaceDeclaration(
                                    SyntaxFactory.IdentifierName(typeSymbol.ContainingNamespace.ToString()))
                        .WithMembers(
                             SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl))))
                .NormalizeWhitespace();

            //move to new File
            //TODO: handle name conflicts
            var newDocument = document.Project.AddDocument(string.Format("{0}.cs", identifierToken.Text), SourceText.From(newFileTree.ToFullString()), document.Folders);
            return newDocument.Project.Solution;
        }
    }
}