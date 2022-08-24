using ICSharpCode.Decompiler.IL;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
    public class AstNodeExtended : IAstNode
    {
        internal readonly AstNode Node;
        private IAstNode? parentNode = null;

        public IAstNode? Parent
        {
            get
            {
                if (parentNode == null && Node?.Parent != null)
                {
                    parentNode = new AstNodeExtended(Node.Parent);
                }

                return parentNode;
            }
        }

        public AstNodeExtended(AstNode baseNode)
        {
            Node = baseNode ?? throw new ArgumentNullException(nameof(baseNode));
        }


        public IILFunction? FunctionAnnotation()
        {
            var annotation = Node.Annotation<ILFunction>();
            if (annotation is not null)
            {
                return new ILFunctionExtended(annotation);
            }
            return null;
        }
    }
}
