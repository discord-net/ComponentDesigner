using System.Collections.Generic;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class Stage1GraphParametersComparer : IEqualityComparer<GraphParameters>
{
    public static readonly Stage1GraphParametersComparer Instance = new();
    
    public bool Equals(GraphParameters? x, GraphParameters? y)
    {
        if (ReferenceEquals(x, y)) return true;
        
        if (x is null) return y is null;
        if (y is null) return false;

        return x.CX.Equals(y.CX) && x.Options.Equals(y.Options);
    }

    public int GetHashCode(GraphParameters obj)
        => Hash.Combine(obj.CX, obj.Options);
}