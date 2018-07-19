using System;
using System.Threading;
using CSharpCorrect;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tests
{
    public class ParserTests
    {
        [Fact]
        public void T01_ShouldFindEmptyCatch()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(TestSamples.GetSample1());
            var analizer = new CatchCorrector
            {
                DoCorrection = false
            };
            var r = analizer.Process(tree, CancellationToken.None);

            Assert.False(analizer.WasModified);
            Assert.Equal(3, r.Count);
            Assert.Equal(495, r[0].Start);
            Assert.Equal(97, r[0].Length);
        }


        [Fact]
        public void T02_ShouldCorrectEmptyCatch()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(TestSamples.GetSample1());
            var analizer = new CatchCorrector
            {
                DoCorrection        = true,
                CorrectionStatement = "ExceptionReporter.Report({0});"
            };
            int changes = 0;
            analizer.Progress.Subscribe(a =>
            {
                changes++;
            });
            var r = analizer.Process(tree, CancellationToken.None);
            Assert.Equal(0, r.Count);
            Assert.True(analizer.WasModified);
            Assert.Equal(3, changes);           
            var expected = TestSamples.GetSample1Expected();
            expected = CatchCorrector.Format(expected);
            var got = CatchCorrector.Format(analizer.ModifiedCode);
            Assert.Equal(expected, got);
        }
    }
}