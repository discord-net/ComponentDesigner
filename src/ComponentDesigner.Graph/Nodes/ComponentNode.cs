using System.Diagnostics.CodeAnalysis;

namespace ComponentDesigner.Nodes;

public abstract class ComponentNode : ComponentNode<ComponentState>
{
    public static readonly IReadOnlyDictionary<string, IComponentNode> AccessibleComponents;
    
    public override ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    ) => new(context, cancellationToken);

    static ComponentNode()
    {
        AccessibleComponents = typeof(ComponentNode)
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
    ) => AccessibleComponents.TryGetValue(name, out node);

    public static T GetNodeInstance<T>() where T : IComponentNode
        => (T)AccessibleComponents.Values.First(x => x.GetType() == typeof(T));
}