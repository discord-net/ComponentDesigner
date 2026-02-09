using System.Collections;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public sealed class GraphNodeEnumerator
{
    public static IEnumerable<ICXNode> GetNext<T>(CXCollection<T> nodes) where T : class, ICXNode
        => nodes.SelectMany(GetNext);
    
    public static IEnumerable<ICXNode> GetNext(IEnumerable<ICXNode> nodes) 
        => nodes.SelectMany(GetNext);
    
    public static IEnumerable<ICXNode> GetNext(ICXNode node)
    {
        switch (node)
        {
            case CXElement {IsFragment: true} element: return GetNext((IEnumerable<ICXNode>)element.Children);
            case CXValue.Multipart multipart: return multipart.Tokens;
            default: return [node];
        }
    }
}