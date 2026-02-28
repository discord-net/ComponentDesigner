using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public static class ComponentContextExtensions
{
    extension(IComponentContext context)
    {
        public IComponentRenderer Renderer => context.Implementation.Renderer;
        public ITextControlProvider TextControlProvider => context.Implementation.TextControlProvider;
        public IComponentTypingProvider? ComponentTypingProvider => context.Implementation.ComponentTypingProvider;

        public bool HasTypedCustomComponentSupport => context.ComponentTypingProvider is not null;
        
        public bool IsInterpolatedComponent(ICXNode? node, CancellationToken cancellationToken = default)
            => context.ComponentTypingProvider is not null && node switch
            {
                CXValue.Interpolation { InterpolationIndex: { } index } => context
                    .ComponentTypingProvider
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        cancellationToken
                    ),
                CXToken { Kind: CXTokenKind.Interpolation, InterpolationIndex: { } index } => context
                    .ComponentTypingProvider
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        cancellationToken
                    ),
                _ => false
            };

        public IInterpolationInfo GetInterpolationInfo(CXValue.Interpolation interpolation)
            => context.CX.Interpolations[interpolation.InterpolationIndex];

        public IInterpolationInfo GetInterpolationInfo(int id)
            => context.CX.Interpolations[id];

        public IInterpolationInfo GetInterpolationInfo(CXToken token)
            => token.InterpolationIndex is { } index
                ? context.GetInterpolationInfo(index)
                : throw new InvalidOperationException($"token type {token.Kind} is not an interpolated token");

        public string GetReferenceToDesignerValue(
            int index,
            string? type = null
        ) => type is null
            ? $"{context.CX.DesignerParameterName}.GetValueAsString({index})"
            : $"{context.CX.DesignerParameterName}.GetValue<{type}>({index})";

        public string GetReferenceToDesignerValue(
            CXValue.Interpolation interpolation,
            string? type = null
        ) => context.GetReferenceToDesignerValue(interpolation.InterpolationIndex, type);

        public string GetReferenceToDesignerValue(
            IInterpolationInfo info,
            string? type = null
        ) => context.GetReferenceToDesignerValue(info.Id, type);

        public string GetReferenceToDesignerValue(
            int index,
            ICSharpTypeSymbol? type
        ) => context.GetReferenceToDesignerValue(index, type?.ToQualifiedName());

        public string GetReferenceToDesignerValue(
            CXValue.Interpolation interpolation,
            ICSharpTypeSymbol? type
        ) => context.GetReferenceToDesignerValue(interpolation.InterpolationIndex, type);

        public string GetReferenceToDesignerValue(
            IInterpolationInfo info,
            ICSharpTypeSymbol? type
        ) => context.GetReferenceToDesignerValue(info.Id, type);
    }
}