using ICSharpCode.Decompiler.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;

namespace Peeker.Test
{
    public class DecompilationTests
    {
        private Mock<Decompilation>? MockDecompilation;
        private Mock<IILMapper>? MockILMapper;
        private Mock<ICSharpDecompiler>? MockDecompiler;

        private void SetupMocks()
        {
            MockILMapper = new Mock<IILMapper>();
            MockDecompilation = new Mock<Decompilation>(MockBehavior.Strict, MockILMapper.Object, new Mock<ILogger<Decompilation>>().Object);
            MockDecompiler = new Mock<ICSharpDecompiler>();
            MockDecompilation.Object.Decompiler = MockDecompiler.Object;
        }

        #region Process
        [Fact]
        public void Process()
        {
            SetupMocks();

            var mockConfig = new AnalyzerConfig()
            {
                Analyzers = Array.Empty<string>()
            };

            // Turn most of the calls in Process() into no-ops and simply verify their invocation
            MockDecompilation.Setup(mock => mock.InitializeILSpy());
            MockDecompilation.Setup(mock => mock.CreateSyntaxTrees());
            MockDecompilation.Setup(mock => mock.DumpTreesToDisk());
            MockDecompilation.Setup(mock => mock.CreateCompilation()).Returns(default(CompilationWithAnalyzers));
            MockDecompilation.Setup(mock => mock.GetAndProcessDiagnostics(It.IsAny<CompilationWithAnalyzers>()));

            // Call Process()
            MockDecompilation.Object.Process(mockConfig, ImmutableArray.Create<DiagnosticAnalyzer>(), new MergedRuleSet(), "Filename.fake");

            // Verify invocations
            MockDecompilation.Verify(mock => mock.InitializeILSpy(), Times.Once());
            MockDecompilation.Verify(mock => mock.CreateSyntaxTrees(), Times.Once());
            MockDecompilation.Verify(mock => mock.CreateCompilation(), Times.Once());
            MockDecompilation.Verify(mock => mock.GetAndProcessDiagnostics(It.IsAny<CompilationWithAnalyzers>()), Times.Once());
        }

        [Fact]
        public void Process_Null()
        {
            SetupMocks();
            Assert.Throws<ArgumentNullException>(() => MockDecompilation.Object.Process(null, ImmutableArray.Create<DiagnosticAnalyzer>(), new MergedRuleSet(), "Filename.fake"));
        }
        #endregion

        #region CreateSyntaxTrees
        [Fact]
        public void CreateSyntaxTrees()
        {
            SetupMocks();
            MockDecompilation.Object.Config = new AnalyzerConfig();

            var syntaxTrees = Enumerable.Repeat(default(ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree), 10).ToArray();

            MockDecompiler.Setup(mock => mock.DecompileTrees(
                It.IsAny<ICSharpCode.Decompiler.Metadata.PEFile>(),
                It.IsAny<ICSharpCode.Decompiler.DecompilerSettings>(),
                It.IsAny<ICSharpCode.Decompiler.Metadata.IAssemblyResolver>(),
                It.IsAny<bool>()))
                .Returns(syntaxTrees);
            MockDecompiler.Setup(mock => mock.SyntaxTreeToString(default, It.IsAny<ICSharpCode.Decompiler.DecompilerSettings>()))
                .Returns("Source.fake");
            MockDecompilation.Setup(mock => mock.GenerateRoslynTrees());
            MockDecompilation.Setup(mock => mock.CreateSyntaxTrees()).CallBase();

            MockDecompilation.Object.CreateSyntaxTrees();

            Assert.Equal(syntaxTrees, MockDecompilation.Object.ILSpySyntaxTrees);
            Assert.Equal(syntaxTrees.Length, MockDecompilation.Object.SyntaxTreesText.Length);
        }
        #endregion
    }
}
