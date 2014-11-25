using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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

            // Only for a type declaration node that doesn't match the current file name
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null || context.Document.Name.ToLowerInvariant() == string.Format("{0}.cs",typeDecl.Identifier.ToString().ToLowerInvariant()))
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

            var @namespace = typeSymbol.ContainingNamespace;

            //remove type from current files
            var currentSyntaxTree = await document.GetSyntaxTreeAsync();
            var currentRoot = await currentSyntaxTree.GetRootAsync();
            var replacedRoot = currentRoot.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia);

            document = document.WithSyntaxRoot(replacedRoot);

            //create new tree for a new file
            //we omit the namespaces cause there is no easy way to drag only the correct ones along. or should we take all of them?
            var newFileTree = SyntaxFactory.CompilationUnit()
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.NamespaceDeclaration(
                                    SyntaxFactory.IdentifierName(@namespace.Name))
                        .WithMembers(
                             SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl))))
                .NormalizeWhitespace();

            //move to new File
            //TODO: handle name conflicts
            var newDocument = document.Project.AddDocument(string.Format("{0}.cs", identifierToken.Text), SourceText.From(newFileTree.ToFullString()));
            return newDocument.Project.Solution;
        }
    }
}