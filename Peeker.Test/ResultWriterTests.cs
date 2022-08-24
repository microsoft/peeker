using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Moq;

namespace Peeker.Test
{
    public class ResultWriterTests
    {
        private ResultWriter CreateWriter()
        {
            return new ResultWriter(new Mock<ILogger<ResultWriter>>().Object);
        }

        [Fact]
        public void ProcessResults_Expected()
        {
            var writer = CreateWriter();
            var mockDecompilation = new Mock<IDecompilation>();

            var fakeDiagnostics = new Diagnostic[]
            {
                Diagnostic.Create(CreateDescriptor("FA0001"), null),
                Diagnostic.Create(CreateDescriptor("FA0001"), Microsoft.CodeAnalysis.Location.Create(
                    "FakeFile.cs",
                    new TextSpan(0, 10),
                    new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 3)))),
                Diagnostic.Create(CreateDescriptor("FA0002"), Microsoft.CodeAnalysis.Location.Create(
                    "FakeFile.cs",
                    new TextSpan(3, 200),
                    new LinePositionSpan(new LinePosition(0, 0), new LinePosition(27, 0)))),
                Diagnostic.Create(CreateDescriptor("FA0001"), Microsoft.CodeAnalysis.Location.Create(
                    "FakeFile.cs",
                    new TextSpan(0, 5),
                    new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 3))))
            };

            var fakeResolvedLocation = new FileLinePositionSpan(
                "FakeResolvedFile.cs",
                new LinePositionSpan(new LinePosition(0, 0), new LinePosition(1, 100)));
            int resolutionIndex = 3;

            mockDecompilation.Setup(mock => mock.GetDiagnostics()).Returns(fakeDiagnostics);
            mockDecompilation.Setup(mock => mock.TryResolveOriginalLocation(fakeDiagnostics[resolutionIndex])).Returns(fakeResolvedLocation);
            mockDecompilation.SetupGet(mock => mock.FileName).Returns("Filename.fake");

            SarifLog actual = writer.ProcessResults(mockDecompilation.Object);

            Assert.Equal(1, actual.Runs.Count);
            Assert.Equal(2, actual.Runs[0].Tool.Driver.Rules.Count);
            Assert.Equal(fakeDiagnostics.Length, actual.Runs[0].Results.Count);

            for (int i = 0; i < fakeDiagnostics.Length; i++)
            {
                var inputDiagnostic = fakeDiagnostics[i];
                var actualResult = actual.Runs[0].Results[i];

                Assert.Equal(inputDiagnostic.Id, actualResult.RuleId);

                if (i == resolutionIndex) // Location resolution test case
                {
                    continue;
                }

                if (inputDiagnostic.Location != Microsoft.CodeAnalysis.Location.None)
                {
                    Assert.Equal(mockDecompilation.Object.FileName, actualResult.Locations[0].PhysicalLocation.ArtifactLocation.Uri.ToString());
                    Assert.Equal(inputDiagnostic.Location.ToString(), actualResult.Locations[0].LogicalLocations[0].DecoratedName);
                }
            }

            Assert.Equal(fakeResolvedLocation.Path, actual.Runs[0].Results[3].Locations[0].PhysicalLocation.ArtifactLocation.Uri.ToString());
        }

        [Fact]
        public void ProcessResults_NullDecompilation()
        {
            var writer = CreateWriter();
            Assert.Throws<ArgumentNullException>(() => writer.ProcessResults(null));
        }

        private DiagnosticDescriptor CreateDescriptor(string id)
        {
            return new DiagnosticDescriptor(id, id, id, "Category.fake", DiagnosticSeverity.Error, true);
        }
    }
}
