using System.Collections.Immutable;
using System.Reflection;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;

namespace UnitTests;

public static class IncrementalGeneratorDebugUtils
{
    public static string ToDOTTree<T>(
        this IncrementalValueProvider<T> provider,
        IReadOnlyDictionary<string, IncrementalStepRunReason>? steps = null
    )
    {
        var node = GetFieldValue(provider, provider.GetType(), "Node");

        if (node is null) return "graph {}";

        var parts = new List<string>();

        var visited = new HashSet<object>();

        steps ??= ImmutableDictionary<string, IncrementalStepRunReason>.Empty;

        AddNodeToTree(node, parts, visited, steps);

        return
            $$"""
              digraph {
                node [
                  style=filled
                  shape=plaintext
                ]
                
                {{string.Join(Environment.NewLine, parts).WithNewlinePadding(2)}}
              }
              """;
    }

    private static void AddNodeToTree(
        object? node,
        List<string> graph,
        HashSet<object> visited,
        IReadOnlyDictionary<string, IncrementalStepRunReason> steps
    )
    {
        if (node is null) return;

        if (!visited.Add(node)) return;

        var type = node.GetType();
        var hash = node.GetHashCode();
        switch (type.Name)
        {
            case "BatchNode`1":
            {
                var input = GetNodeFieldValue(node, type);
                var name = GetFieldValue(node, type, "_name")?.ToString();
                IncrementalStepRunReason? step = null;
                string? color = null;

                if (name is not null && steps.TryGetValue(name, out var stepV))
                {
                    step = stepV;
                    color = $"fillcolor=\"{(
                        step switch
                        {
                            IncrementalStepRunReason.Modified => "#ff880022",
                            IncrementalStepRunReason.New => "#88ff0022",
                            IncrementalStepRunReason.Removed => "#88000022",
                            IncrementalStepRunReason.Cached => $"#0044ff22",
                            _ => "#dfdfdf"
                        }
                    )}\"";
                }
                
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Batch Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", name),
                           ("Incremental State", step?.ToString())
                       )}
                       {color}
                     ]
                     {input!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(input, graph, visited, steps);
                break;
            }
            case "CombineNode`2":
            {
                var left = GetNodeFieldValue(node, type, "_input1");
                var right = GetNodeFieldValue(node, type, "_input2");
                
                var name = GetFieldValue(node, type, "_name")?.ToString();
                IncrementalStepRunReason? step = null;
                string? color = null;

                if (name is not null && steps.TryGetValue(name, out var stepV))
                {
                    step = stepV;
                    color = $"fillcolor=\"{(
                        step switch
                        {
                            IncrementalStepRunReason.Modified => "#ff880022",
                            IncrementalStepRunReason.New => "#88ff0022",
                            IncrementalStepRunReason.Removed => "#88000022",
                            IncrementalStepRunReason.Cached => $"#0044ff22",
                            _ => "#dfdfdf"
                        }
                    )}\"";
                }
                
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Combine Node",
                           ("Left Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Right Type", PrettyTypeName(type.GenericTypeArguments[1])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString()),
                           ("Incremental State", step?.ToString())
                       )}
                       {color}
                     ]
                     {left!.GetHashCode()} -> {hash}
                     {right!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(left, graph, visited, steps);
                AddNodeToTree(right, graph, visited, steps);
                break;
            }
            case "InputNode`1":
            {
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Input Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     """
                );
                break;
            }
            case "SyntaxInputNode`1":
            {
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Syntax Input Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     """
                );
                break;
            }
            case "TransformNode`2":
            {
                var name = GetFieldValue(node, type, "_name")?.ToString();
                IncrementalStepRunReason? step = null;
                string? color = null;

                if (name is not null && steps.TryGetValue(name, out var stepV))
                {
                    step = stepV;
                    color = $"fillcolor=\"{(
                        step switch
                        {
                            IncrementalStepRunReason.Modified => "#ff880022",
                            IncrementalStepRunReason.New => "#88ff0022",
                            IncrementalStepRunReason.Removed => "#88000022",
                            IncrementalStepRunReason.Cached => $"#0044ff22",
                            _ => "#dfdfdf"
                        }
                    )}\"";
                }
                
                var input = GetNodeFieldValue(node, type);
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Transform Node",
                           ("From", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("To", PrettyTypeName(type.GenericTypeArguments[1])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString()),
                           ("Incremental State", step?.ToString())
                       )}
                       {color}
                     ]
                     {input!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(input, graph, visited, steps);
                break;
            }
        }
    }

    private static string PrettyTypeName(Type t)
    {
        if (t.IsArray)
        {
            return PrettyTypeName(t.GetElementType()!) + "[]";
        }

        if (t.IsGenericType)
        {
            return string.Format(
                "{0}<{1}>",
                t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
        }

        return t.Name;
    }

    private static string Table(string title, params IEnumerable<(string Name, string? Value)?> parts)
    {
        var partsArr = parts
            .Where(x => x.HasValue && !string.IsNullOrWhiteSpace(x.Value.Value))
            .Select(x => x!.Value)
            .ToArray();

        if (partsArr.Length is 0) return $"<<b>{title}</b>>";

        return
            $"""
             <<table border="0" cellborder="1" cellspacing="0" cellpadding="4">
             <tr><td colspan="2"><b>{title}</b></td></tr>
             {string.Join(Environment.NewLine, partsArr.Select(x =>
                 $"<tr><td>{Clean(x.Name)}</td><td>{Clean(x.Value!)}</td></tr>"))}
             </table>>
             """;

        static string Clean(string v)
            => v.Replace("<", "&lt;").Replace(">", "&gt;");
    }


    private static object? GetNodeFieldValue(object node, Type type, string name = "_sourceNode")
        => GetFieldValue(node, type, name);

    private static object? GetFieldValue(object node, Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        return field?.GetValue(node);
    }
}