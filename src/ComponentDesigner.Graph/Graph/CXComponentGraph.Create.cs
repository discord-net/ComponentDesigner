using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

partial class CXComponentGraph
{
    public static CXComponentGraph Create(
        GraphParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var reader = new CXSourceReader(
            CXSourceText.From(parameters.CX.Syntax),
            parameters.CX.Interpolations.Select(x => x.TextSpan).ToArray(),
            parameters.CX.QuoteCount
        );

        var document = CXParser.Parse(reader, cancellationToken);

        return Create(parameters, document, cancellationToken);
    }

    public static CXComponentGraph Create(
        GraphParameters parameters,
        CXDocument document,
        CancellationToken token = default
    )
    {
        var parserDiagnostics = document
            .AllDiagnostics
            .Select(x => x.ToNormalDiagnostic())
            .ToArray();

        using var diagnostics = PooledDiagnosticBag.Get(parserDiagnostics);

        var context = new GraphInitializationContext(
            document,
            parameters.CX,
            parameters.Options,
            parameters.Implementation,
            parameters.CompilationProvider,
            diagnostics
        );

        CreateNodes(document.RootNodes, null, context, token);

        return new CXComponentGraph(
            document,
            context.Tree,
            diagnostics.ToCollection(),
            parameters
        );
    }

    internal static void CreateNodes(
        IReadOnlyList<ICXNode> nodes,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        using var enumerator = GraphNodeEnumerator.GetNext(nodes).GetEnumerator();

        while (enumerator.MoveNext())
        {
            var node = enumerator.Current;

            if (
                !context.IsInterpolatedComponent(node, cancellationToken) &&
                TextControlElement.TryCreate(
                    context,
                    enumerator,
                    context.Diagnostics,
                    out var result,
                    out var enumeratorHasMore,
                    cancellationToken
                )
            )
            {
                if (context.Options.AllowAutoTextDisplays)
                {
                    var autoTextDisplayGraphNode = context.Tree.Push(
                        AutoTextDisplayComponentNode.Instance,
                        parent: parent
                    );

                    autoTextDisplayGraphNode.State = new TextDisplayState(
                        autoTextDisplayGraphNode
                    );

                    var textControlGraphNode = context.Tree.Push(
                        TextControlNode.Instance,
                        parent: autoTextDisplayGraphNode
                    );

                    textControlGraphNode.State = new TextControlState(
                        result,
                        textControlGraphNode
                    );

                    // set the contents property to the 'textControlGraphNode'
                    autoTextDisplayGraphNode.State.SetPropertyValueToChild(
                        AutoTextDisplayComponentNode.Instance.Content,
                        textControlGraphNode
                    );
                }
                else
                {
                    context.Diagnostics.Add(
                        result.TextSpan.Report(
                            Diagnostic.FeatureAutoTextDisplaysDisabled
                        )
                    );
                }

                // if the text control consumed all the nodes, return out
                if (!enumeratorHasMore) return;

                node = enumerator.Current;
            }

            CreateNodes(node, parent, context, cancellationToken);
        }
    }


    internal static void CreateNodes(
        ICXNode? cxNode,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
                CreateInterpolationNodes(
                    interpolation,
                    context.CX.Interpolations[interpolation.InterpolationIndex],
                    parent,
                    context,
                    cancellationToken
                );
                return;

            case CXValue.Multipart multipart:
            {
                // TODO: handle text control vs interpolation
                return;
            }

            case CXElement element:
                CreateElementNodes(element, parent, context, cancellationToken);
                return;

            default:
                if (cxNode is not null)
                {
                    context.Diagnostics.Add(
                        cxNode.Report(
                            Diagnostic.UnsupportedSyntaxKindForGraphNode(cxNode)
                        )
                    );
                }

                return;
        }
    }

    internal static void CreateInterpolationNodes(
        ICXNode cxNode,
        IInterpolationInfo info,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (context.ComponentTypingProvider is null)
        {
            context.Diagnostics.Add(
                Diagnostic
                    .TypedComponentsAreNotSupported(context.Implementation)
                    .At(cxNode)
            );
            return;
        }

        if (!context.ComponentTypingProvider.IsValidComponentType(context, info.Symbol, cancellationToken))
        {
            // TODO: diagnostic can be improved to include type info etc
            context.Diagnostics.Add(
                cxNode.Report(
                    Diagnostic.UnsupportedSyntaxKindForGraphNode(cxNode)
                )
            );
            return;
        }

        var graphNode = context.Tree.Push(
            InterpolationComponentNode.Instance,
            parent: parent
        );

        var numDiagnostics = context.Diagnostics.Count;

        var state = graphNode.Component.CreateState(
            new(cxNode, graphNode, context),
            context.Diagnostics,
            cancellationToken
        );

        graphNode.ComponentInitializationProducedDiagnostics = numDiagnostics != context.Diagnostics.Count;

        if (state is null) return;

        graphNode.State = state;
    }

    internal static void CreateElementNodes(
        CXElement element,
        GraphNode? parent,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (element.IsFragment)
        {
            foreach (var child in element.Children)
                CreateNodes(child, parent, context, cancellationToken);

            return;
        }

        if (!TryGetComponentForElementSyntax(context, element, parent, out var componentNode))
        {
            context.Diagnostics.Add(
                element.Report(Diagnostic.UnknownComponentElement(element.Identifier))
            );
            return;
        }

        var initializationContext = new ComponentGraphInitializationContext(
            parent,
            element,
            context
        );

        componentNode.RegisterGraphNode(initializationContext, cancellationToken);
    }

    private static bool TryGetComponentForElementSyntax(
        GraphInitializationContext context,
        CXElement element,
        GraphNode? parent,
        [MaybeNullWhen(false)] out IComponentNode componentNode
    )
    {
        if (element.Identifier is "option" && parent?.Component is { } parentComponent)
        {
            componentNode = parentComponent switch
            {
                CheckboxGroupComponentNode => ComponentNode.GetNodeInstance<CheckboxGroupOptionComponentNode>(),
                RadioGroupComponentNode => ComponentNode.GetNodeInstance<RadioGroupOptionComponentNode>(),
                SelectMenuComponentNode => ComponentNode.GetNodeInstance<SelectMenuOptionComponentNode>(),
                _ => null
            };

            if (componentNode is not null) return true;
        }

        if (ComponentNode.TryGetNode(element.Identifier, out componentNode))
            return true;
        
        if (context.HasTypedCustomComponentSupport)
        {
            // for now, just assume it to be a functional component
            componentNode = FunctionalComponentNode.Instance;
            return true;
        }

        componentNode = null;
        return false;
    }


    public static GraphNode? CreateFromInitializationRequest(
        GraphNodeInitializationRequest request,
        GraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        var node = context.Tree.Push(
            request.Component,
            parent: request.Parent
        );

        if (!ReinitializeNode(node, request, context, cancellationToken))
        {
            context.Tree.DereferenceFromTree(node);
            return null;
        }

        return node;
    }

    /// <summary>
    /// Recreates the children, attributes, and state of an existing
    /// <paramref name="node"/> using <paramref name="request"/> as the source
    /// of truth for CX syntax and children. Callers must ensure that the
    /// node's previous children have already been dereferenced before calling
    /// this method.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <see cref="IComponentNode.CreateState"/>
    /// produced a non-null state; <see langword="false"/> otherwise.
    /// </returns>
    internal static bool ReinitializeNode(
        GraphNode node,
        GraphNodeInitializationRequest request,
        GraphInitializationContext context,
        CancellationToken cancellationToken
    )
    {
        // Map attribute nodes first.
        if (request.CXNode is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is not CXValue.Element nestedElement) continue;

                CreateNodes(nestedElement.Value, node, context, cancellationToken);
            }
        }

        // Then process children syntax nodes.
        if (request.Children?.Count > 0)
        {
            CreateNodes(request.Children, node, context, cancellationToken);
        }

        var initContext = new ComponentNodeInitializationContext(
            request.CXNode,
            node,
            context
        );

        var numDiagnostics = context.Diagnostics.Count;

        var state = node.Component.CreateState(
            initContext,
            context.Diagnostics,
            cancellationToken
        );

        node.ComponentInitializationProducedDiagnostics =
            numDiagnostics != context.Diagnostics.Count;

        if (state is null)
            return false;

        node.State = state;
        return true;
    }
}