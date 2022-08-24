using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Sarif;

namespace Peeker.Test
{
    public class RuleBagTests
    {
        [Fact]
        public void RegisterRule_Expected()
        {
            var bag = new RuleBag();

            int actualIndex;
            ReportingDescriptor? actualDescriptor;

            (actualIndex, actualDescriptor) = bag.RegisterRule(CreateDescriptor("FA0001"));
            Assert.Equal(0, actualIndex);
            Assert.Equal("FA0001", actualDescriptor?.Id);
            Assert.Equal(actualDescriptor, bag.Rules[actualIndex]);

            (actualIndex, actualDescriptor) = bag.RegisterRule(CreateDescriptor("FA0002"));
            Assert.Equal(1, actualIndex);
            Assert.Equal("FA0002", actualDescriptor?.Id);
            Assert.Equal(actualDescriptor, bag.Rules[actualIndex]);

            (actualIndex, actualDescriptor) = bag.RegisterRule(CreateDescriptor("FA0001"));
            Assert.Equal(0, actualIndex);
            Assert.Equal("FA0001", actualDescriptor?.Id);
            Assert.Equal(actualDescriptor, bag.Rules[actualIndex]);
        }

        [Fact]
        public void RegisterRule_Null()
        {
            var bag = new RuleBag();

            int actualIndex;
            ReportingDescriptor? actualDescriptor;

            (actualIndex, actualDescriptor) = bag.RegisterRule(null);
            Assert.Equal(-1, actualIndex);
            Assert.Null(actualDescriptor);
        }

        private DiagnosticDescriptor CreateDescriptor(string id)
        {
            return new DiagnosticDescriptor(id, id, id, "Category.fake", DiagnosticSeverity.Error, true);
        }
    }
}
