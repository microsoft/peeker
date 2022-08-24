using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp
{
    public class CSharpDecompilerExtended : ICSharpDecompiler
    {
        private CSharpDecompiler Decompiler;

        public IDecompilerTypeSystem TypeSystem => Decompiler.TypeSystem;

        public IDebugInfoProvider DebugInfoProvider { get => Decompiler.DebugInfoProvider; set { Decompiler.DebugInfoProvider = value; } }

        public CSharpDecompilerExtended(CSharpDecompiler baseDecompiler)
        {
            Decompiler = baseDecompiler;
        }

        public CSharpDecompilerExtended(string filename, IAssemblyResolver resolver, DecompilerSettings settings)
            : this(new CSharpDecompiler(filename, resolver, settings))
        {

        }

        public Dictionary<IILFunction, List<SequencePoint>> CreateSequencePoints(SyntaxTree syntaxTree) => 
            Decompiler.CreateSequencePoints(syntaxTree).ToDictionary(p => (IILFunction)new ILFunctionExtended(p.Key), p => p.Value);

        public IEnumerable<SyntaxTree> DecompileTrees(PEFile module, DecompilerSettings decompilerSettings, IAssemblyResolver assemblyResolver, bool oneTreePerType)
        {
            if (oneTreePerType)
            {
                var metadata = module.Metadata;

                DecompilerTypeSystem ts = new DecompilerTypeSystem(module, assemblyResolver, decompilerSettings);
                var typesToDecompile = module.Metadata.GetTopLevelTypeDefinitions();
                foreach (var dt in typesToDecompile)
                {
                    CSharpDecompiler childDecompiler = CreateDecompiler(Decompiler, ts, decompilerSettings);
                    var typedef = module.Metadata.GetTypeDefinition(dt);
                    var syntaxTree = Decompiler.DecompileTypes(new[] { dt });
                    var filename = $"{typedef.GetFullTypeName(metadata)}.cs";
                    syntaxTree.FileName = filename;
                    yield return syntaxTree;
                }

                var misc = Decompiler.DecompileModuleAndAssemblyAttributes();
                misc.FileName = "AssemblyInfo__.cs";
                yield return misc;
            }
            else
            {
                var tree = Decompiler.DecompileWholeModuleAsSingleFile();
                tree.FileName = $"{(Path.GetFileNameWithoutExtension(module.FileName) ?? "module")}.cs";
                yield return tree;
            }
        }

        static CSharpDecompiler CreateDecompiler(CSharpDecompiler baseDecompiler, DecompilerTypeSystem ts, DecompilerSettings decompilerSettings)
        {
            var decompiler = new CSharpDecompiler(ts, decompilerSettings);
            decompiler.DebugInfoProvider = baseDecompiler.DebugInfoProvider;
            decompiler.AstTransforms.Add(new EscapeInvalidIdentifiers());
            decompiler.AstTransforms.Add(new RemoveCLSCompliantAttribute());
            return decompiler;
        }

        public string SyntaxTreeToString(SyntaxTree syntaxTree, DecompilerSettings settings)
        {
            StringWriter w = new StringWriter();

            syntaxTree.AcceptVisitor(new OutputVisitor.InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            OutputVisitor.TokenWriter tokenWriter = new OutputVisitor.TextWriterTokenWriter(w) { IndentationString = settings.CSharpFormattingOptions.IndentationString };
            tokenWriter = OutputVisitor.TokenWriter.WrapInWriterThatSetsLocationsInAST(tokenWriter);
            syntaxTree.AcceptVisitor(new OutputVisitor.CSharpOutputVisitor(tokenWriter, settings.CSharpFormattingOptions));

            return w.ToString();
        }
    }
}
