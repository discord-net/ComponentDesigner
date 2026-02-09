using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public enum DefaultValueKind
{
    User,
    Role,
    Channel,
    Mentionable
}

public sealed record DefaultValueState(
    GraphNode GraphNode,
    CXElement CXNode,
    DefaultValueKind Kind
) : ComponentState(GraphNode, CXNode)
{
    public new CXElement CXNode { get; init; } = CXNode;
}

public sealed class SelectMenuDefaultValueComponentNode : ComponentNode<DefaultValueState>
{
    public override string Name => "<default value>";

    public override IReadOnlyList<string> Aliases { get; } = ["user", "role", "channel", "mentionable", "mention"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }
    
    public ComponentProperty Id { get; }

    public SelectMenuDefaultValueComponentNode()
    {
        Properties =
        [
            Id = new("id")
        ];
    }

    public override DefaultValueState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        return InferKind(element)
            .Map(kind => new DefaultValueState(context.GraphNode, element, kind))
            .Unwrap(diagnostics);
    }

    private static Result<DefaultValueKind> InferKind(CXElement element)
    {
        if (element.Identifier.Equals("user", StringComparison.InvariantCultureIgnoreCase))
            return DefaultValueKind.User;
        
        if (element.Identifier.Equals("role", StringComparison.InvariantCultureIgnoreCase))
            return DefaultValueKind.Role;
        
        if (element.Identifier.Equals("channel", StringComparison.InvariantCultureIgnoreCase))
            return DefaultValueKind.Channel;
        
        if (element.Identifier.StartsWith("mention", StringComparison.InvariantCultureIgnoreCase))
            return DefaultValueKind.Mentionable;

        return element.IdentifierTextSpanOrElementTextSpan.Report(
            Diagnostic.NotAValidEnumVariant("DefaultValueKind", element.Identifier)
        );
    }

    public override Result<RenderedComponent> Emit(
        DefaultValueState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateSelectMenuDefaultValue,
        context.Renderer.RenderSelectMenuDefaultValue,
        cancellationToken
    );
}