using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace CSharpCorrect
{
    public class CatchCorrector
    {
        public static string Format(string expected)
        {
            var tree           = CSharpSyntaxTree.ParseText(expected);
            var root           = (CompilationUnitSyntax)tree.GetRoot();
            var workspace      = MSBuildWorkspace.Create();
            var afterFormatted = Formatter.Format(root, workspace);
            return afterFormatted.ToFullString();
        }
    
        public List<TextSpan> Process(SyntaxTree tree, CancellationToken token)
        {
            ModifiedStatements = 0;
            try
            {
                try
                {
                    WasModified = false;
                    _tree       = tree;
                    _root       = (CompilationUnitSyntax)tree.GetRoot();

                    _result = new List<TextSpan>();

                    while (true)
                    {
                        var canExit = true;
                        var catches = _root.DescendantNodes()
                            .OfType<CatchClauseSyntax>()
                            .ToList();
                        foreach (var catchSyntax in catches)
                        {
                            token.ThrowIfCancellationRequested();
                            if (!ProcessTry(catchSyntax)) continue;
                            canExit     = false;
                            WasModified = true;
                            break;
                        }

                        if (canExit) break;
                    }

                    _tree = null;
                    var result = _result;
                    _result = null;
                    if (WasModified)
                        ModifiedCode = _root.ToFullString();
                    return result;
                }
                finally
                {
                    _progress.OnCompleted();
                }
            }
            catch (Exception e)
            {
                _progress.OnError(e);
                throw;
            }
        }

        public Task<List<TextSpan>> ProcessAsync(SyntaxTree tree, CancellationToken token)
        {
            return Task.Run(() => Process(tree, token), token);
        }

        private bool Correct(CatchClauseSyntax catchSyntax)
        {
            var variableName = "exception";
            if (catchSyntax.Declaration?.Identifier.ValueText != null)
            {
                var existing = catchSyntax.Declaration.Identifier.ValueText;
                if (!string.IsNullOrEmpty(existing))
                    variableName = existing;
            }
            else
            {
                var type = "System.Exception";
                if (catchSyntax.Declaration != null)
                    type = catchSyntax.Declaration.Type.ToFullString();
                var a = (TryStatementSyntax)SyntaxFactory.ParseStatement(
                    "try {} catch (" + type + " " + variableName + "){}");
                var declaration = a.Catches[0].Declaration;
                if (catchSyntax.Declaration == null)
                {
                    var newCatch = catchSyntax.WithDeclaration(declaration);
                    Replace(catchSyntax, newCatch);
                    return true;
                }

                Replace(catchSyntax.Declaration, declaration);
                return true;
            }

            var             code = "/*AutoCorrection*/\r\n" + string.Format(CorrectionStatement, variableName);
            StatementSyntax s    = SyntaxFactory.ParseStatement(code);
            BlockSyntax     b1   = catchSyntax.Block.AddStatements(s);
            Replace(catchSyntax.Block, b1);
            _progress.OnNext(1);
            ModifiedStatements++;
            return true;
        }

        public int ModifiedStatements { get; private set; }

        private bool ProcessTry(CatchClauseSyntax catchSyntax)
        {
            if (catchSyntax.Block.Statements.Any())
                return false;
            if (DoCorrection)
                return Correct(catchSyntax);
            _result.Add(catchSyntax.Span);
            return false;
        }

        private void Replace(SyntaxNode before, SyntaxNode after)
        {
            var workspace      = MSBuildWorkspace.Create();
            var afterFormatted = Formatter.Format(after, workspace);
            _root = _root.ReplaceNode(before, afterFormatted);
        }

        public IObservable<int> Progress
        {
            get { return _progress; }
        }

        public string ModifiedCode { get; private set; }

        public bool   DoCorrection        { get; set; }
        public bool   WasModified         { get; private set; }
        public string CorrectionStatement { get; set; }

        private readonly Subject<int> _progress = new Subject<int>();


        private List<TextSpan> _result;
        private SyntaxTree _tree;
        private CompilationUnitSyntax _root;
    }
}