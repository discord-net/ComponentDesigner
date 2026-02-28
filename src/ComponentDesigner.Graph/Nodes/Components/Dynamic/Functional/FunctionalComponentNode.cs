using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class FunctionalComponentNode : ComponentNode<FunctionalState>
{
    public static readonly FunctionalComponentNode Instance = new();
    
    public override string Name => "<functional component>";

    public override bool IsUserAccessible => false;

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);

    public override FunctionalState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        return SearchForTargetMethod(element, context.GraphContext, cancellationToken)
            .Map(symbol => FunctionalState
                .FromSymbol(
                    context,
                    context.GraphContext,
                    symbol,
                    context.GraphNode,
                    element,
                    diagnostics,
                    cancellationToken
                )
            )
            .Unwrap(diagnostics);
    }

    public override FunctionalState UpdateState(
        FunctionalState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    ) => SearchForTargetMethod(state.CXNode, context, cancellationToken)
        .Map(symbol => FunctionalState
            .FromSymbol(
                initializationContext: null,
                context,
                symbol,
                state.GraphNode,
                state.CXNode,
                diagnostics,
                cancellationToken
            )
        )
        .Unwrap(diagnostics, state);

    #region Search

    private static Result<ICSharpMethodSymbol> SearchForTargetMethod(
        CXElement element,
        IComponentContext context,
        CancellationToken cancellationToken
    )
    {
        var containingType = element
            .OpeningTag
            .Identifier is CXIdentifier.Interpolated { InterpolationToken: { } interpolation }
            ? context.GetInterpolationInfo(interpolation).Symbol
            : null;

        var candidates = context.CompilationProvider.LookupSymbols(
            context.CX.Location,
            element.Identifier,
            container: containingType
        );

        var requiresStaticContext = element.OpeningTag.Identifier is CXIdentifier.Simple;

        using var _ = ObjectPool<List<SearchResult>>.GetScoped(out var results);
        results.Clear();

        foreach (var candidate in candidates)
        {
            results.Add(Classify(context, candidate, requiresStaticContext, cancellationToken));
        }

        switch (results.Count)
        {
            case 0:
                return element.IdentifierTextSpanOrElementTextSpan.Report(
                    Diagnostic.UnknownComponentElement(element.Identifier)
                );

            case 1: return Single(element, results[0], requiresStaticContext, containingType);

            default:
                var bestGroup = results
                    .GroupBy(x => x.Kind)
                    .OrderBy(x => x.Key)
                    .First()
                    .ToArray();

                if (bestGroup.Length is 1) return Single(element, bestGroup[0], requiresStaticContext, containingType);

                // TODO: infer based on usage
                return element.IdentifierTextSpanOrElementTextSpan.Report(
                    Diagnostic.AmbiguousFunctionalComponent(bestGroup)
                );
        }

        static Result<ICSharpMethodSymbol> Single(
            CXElement element,
            SearchResult result,
            bool isStaticContext,
            ICSharpTypeSymbol? container
        )
        {
            if (result.Kind is SearchResultKind.Ok) return new((ICSharpMethodSymbol)result.Symbol);

            return element.IdentifierTextSpanOrElementTextSpan.Report(
                Diagnostic.InvalidFunctionalComponent(result, isStaticContext, container)
            );
        }

        static SearchResult Classify(
            IComponentContext context,
            ICSharpSymbol symbol,
            bool inStaticContext,
            CancellationToken cancellationToken
        )
        {
            if (symbol is not ICSharpMethodSymbol methodSymbol)
                return new(symbol, SearchResultKind.NotAMethod);

            if (!context.ComponentTypingProvider.IsValidComponentType(context, methodSymbol.ReturnType, cancellationToken))
                return new(symbol, SearchResultKind.DoesntReturnAComponent);

            if (methodSymbol.IsStatic != inStaticContext)
                return new(symbol, SearchResultKind.DoesntMatchStaticContext);

            if (methodSymbol is { IsInternal: false, IsPublic: false })
                return new(symbol, SearchResultKind.NotAccessible);

            return new(symbol, SearchResultKind.Ok);
        }
    }

    #endregion

    public override Result<RenderedComponent> Emit(
        FunctionalState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateFunctionalComponent,
        context.Renderer.RenderFunctionalComponent,
        cancellationToken
    );
}