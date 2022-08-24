using Microsoft.CodeAnalysis.Sarif;

namespace Peeker
{
    public interface IResultWriter
    {
        public SarifLog ProcessResults(IDecompilation decompilation);
        public void WriteResults(SarifLog log, string path, bool debug);
    }
}
