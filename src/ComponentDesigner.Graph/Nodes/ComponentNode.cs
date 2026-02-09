using System.Diagnostics.CodeAnalysis;

namespace ComponentDesigner.Nodes;

public abstract class ComponentNode : ComponentNode<ComponentState>
{
    private static readonly Dictionary<string, IComponentNode> _nodes;

    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    ) => new(context);

    static ComponentNode()
    {
        _nodes = typeof(ComponentNode)
            .Assembly
            .GetTypes()
            .Where(x =>
                !x.IsAbstract &&
                typeof(IComponentNode).IsAssignableFrom(x) &&
                x.GetConstructor(Type.EmptyTypes) is not null
            )
            .Select(x => (IComponentNode)Activator.CreateInstance(x)!)
            .Where(x => x.IsUserAccessible)
            .SelectMany(x => x
                .Aliases
                .Prepend(x.Name)
                .Select(y => new KeyValuePair<string, IComponentNode>(y, x)))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public static bool TryGetNode(
        string name,
        [MaybeNullWhen(false)] out IComponentNode node
    ) => _nodes.TryGetValue(name, out node);

    public static T GetNode<T>() where T : IComponentNode
        => (T)_nodes.Values.First(x => x.GetType() == typeof(T));
}