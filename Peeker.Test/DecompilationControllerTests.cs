using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;

namespace Peeker.Test
{
    public class DecompilationControllerTests
    {
        private Mock<ResultWriter>? MockResultWriter;
        private Mock<IDecompilation>? MockDecompilation;
        private Mock<DecompilationController> MockDecompilationController;

        internal void SetupMocks()
        {
            MockResultWriter = new Mock<ResultWriter>(new Mock<ILogger<ResultWriter>>().Object);
            MockResultWriter.Setup(mock => mock.ProcessResults(It.IsAny<IDecompilation>())).CallBase();

            MockDecompilation = new Mock<IDecompilation>();
            MockDecompilation.SetupGet(mock => mock.FileName).Returns("Filename.fake.exe");

            MockDecompilationController = new Mock<DecompilationController>(MockResultWriter.Object, () => MockDecompilation.Object, new Mock<ILogger<DecompilationController>>().Object);
            MockDecompilationController.Setup(mock => mock.LoadAnalyzers(It.IsAny<IEnumerable<string>>())).Returns(ImmutableArray.Create<DiagnosticAnalyzer>());
        }

        [Fact]
        public void Process_Null()
        {
            SetupMocks();
            Assert.Throws<ArgumentNullException>(() => MockDecompilationController.Object.Process(null));
        }

        [Fact]
        public void Process_NoAnalyzers()
        {
            SetupMocks();
            Assert.Throws<ArgumentException>(() => MockDecompilationController.Object.Process(new AnalyzerConfig() { Analyzers = Array.Empty<string>() }));
        }

        [Fact]
        public void Process_FileNotFound()
        {
            SetupMocks();

            string invalidFileName = Path.GetRandomFileName();

            while (File.Exists(invalidFileName))
            {
                invalidFileName = Path.GetRandomFileName();
            }

            Assert.Throws<FileNotFoundException>(() => MockDecompilationController.Object.Process(new AnalyzerConfig() { Analyzers = new[] { invalidFileName } }));
        }

        [Fact]
        public void Process()
        {
            SetupMocks();

            var locations = new Location[]
            {
                Location.Create("", new TextSpan(0, 10), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 10))),
                Location.Create("", new TextSpan(0, 11), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 11))),
                Location.Create("", new TextSpan(0, 12), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 12))),
                Location.Create("", new TextSpan(0, 13), new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 13))),
            };

            var mappedLocations = new FileLinePositionSpan?[]
            {
                new FileLinePositionSpan("", new LinePosition(100, 0), new LinePosition(100, 10)),
                new FileLinePositionSpan("", new LinePosition(101, 0), new LinePosition(101, 10)),
                new FileLinePositionSpan("", new LinePosition(102, 0), new LinePosition(102, 10)),
                null
            };

            var diagnosticsToReturn = new[]
            {
                Diagnostic.Create(CreateDescriptor("FA0001"), locations[0]),
                Diagnostic.Create(CreateDescriptor("FA0001"), locations[1]),
                Diagnostic.Create(CreateDescriptor("FA0001"), locations[2]),
                Diagnostic.Create(CreateDescriptor("FA0002"), locations[3]),
            };

            for (int i = 0; i < locations.Length; i++)
            {
                MockDecompilation.Setup(mock => mock.TryResolveOriginalLocation(diagnosticsToReturn[i])).Returns(mappedLocations[i]);
            }

            MockDecompilation.Setup(mock => mock.GetDiagnostics()).Returns(diagnosticsToReturn);

            // This is dirty...
            var mockFileName = Path.GetTempFileName();
            var analyzerConfig = new AnalyzerConfig() { Analyzers = new[] { mockFileName }, Target = new[] { mockFileName } };
            MockDecompilationController.Object.Process(analyzerConfig);
            File.Delete(mockFileName);

            MockDecompilation.Verify(mock => mock.Process(analyzerConfig, It.IsAny<ImmutableArray<DiagnosticAnalyzer>>(), It.IsAny<MergedRuleSet>(), mockFileName), Times.Once());
            MockDecompilation.Verify(mock => mock.GetDiagnostics(), Times.Once());

            for (int i = 0; i < locations.Length; i++)
            {
                MockDecompilation.Verify(mock => mock.TryResolveOriginalLocation(diagnosticsToReturn[i]), Times.Once());
            }

            MockResultWriter.Verify(mock => mock.WriteResults((Microsoft.CodeAnalysis.Sarif.SarifLog)MockResultWriter.Invocations[0].ReturnValue, It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
        }

        private DiagnosticDescriptor CreateDescriptor(string id)
        {
            return new DiagnosticDescriptor(id, id, id, "Category.fake", DiagnosticSeverity.Error, true);
        }
    }
}
