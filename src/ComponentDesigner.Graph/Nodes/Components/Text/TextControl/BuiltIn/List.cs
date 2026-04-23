using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public enum ListKind
    {
        Unordered,
        Ordered
    }

    public sealed class List(
        CXElement element,
        ListKind kind,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => $"{kind} List";

        public override IReadOnlyList<TextControlElement> Children { get; } = children;

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => Join(RenderChildren(context, options, cancellationToken))
            .Map(children => children with
            {
                LeadingTrivia = element.LeadingTrivia,
                TrailingTrivia = element.TrailingTrivia
            });

        private new Result<EquatableArray<TextControl>> RenderChildren(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        )
        {
            var mapper = kind switch
            {
                ListKind.Ordered => BuildOrderedChildren,
                ListKind.Unordered => BuildUnorderedChildren,
                _ => (Func<EquatableArray<TextControl>, EquatableArray<TextControl>>?)null
            };

            if (mapper is null)
                return Diagnostic
                    .UnknownComponentElement(element.Identifier)
                    .At(element.IdentifierTextSpanOrElementTextSpan);

            return base.RenderChildren(context, options, cancellationToken).Map(mapper);
        }
        
        private EquatableArray<TextControl> BuildUnorderedChildren(
            EquatableArray<TextControl> renderedChildren
        )
        {
            var result = new TextControl[renderedChildren.Count];
            const string pad = "  ";
            const string liPrefix = "- ";

            for (var i = 0; i < renderedChildren.Count; i++)
            {
                var child = Children[i];
                var renderedChild = renderedChildren[i];

                var prefix = child is ListItem
                    ? liPrefix
                    : pad;

                result[i] = renderedChild with
                {
                    Value = $"{prefix}{renderedChild.Value}",
                    LeadingTrivia = renderedChild.LeadingTrivia.NewlinesOnly()
                };
            }

            return [..result];
        }
        
        private EquatableArray<TextControl> BuildOrderedChildren(
            EquatableArray<TextControl> renderedChildren
        )
        {
            var orderNumber = 1;
            var itemCount = Children.Count(x => x is ListItem);

            var result = new TextControl[renderedChildren.Count];

            var padWidth = Math.Max(
                3,
                (int)Math.Floor(Math.Log10(itemCount)) + 1
            );

            var pad = new string(' ', padWidth + 3);

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var renderedChild = renderedChildren[i];

                var prefix = child is ListItem
                    ? $"{$"{orderNumber++}".PadLeft(padWidth)}. "
                    : pad;

                result[i] = renderedChild with
                {
                    Value = $"{prefix}{renderedChild.Value}",
                    LeadingTrivia = renderedChild.LeadingTrivia.NewlinesOnly()
                };
            }

            return [..result];
        }
    }
}