using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public static class ComponentContextExtensions
{
    extension(IComponentContext context)
    {
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