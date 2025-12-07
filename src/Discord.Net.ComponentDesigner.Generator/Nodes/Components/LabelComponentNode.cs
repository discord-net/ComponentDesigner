using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components.SelectMenus;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class LabelComponentState : ComponentState
{
    public CXValue? ChildValue { get; set; }
    public CXElement? ChildElement { get; set; }
}

public sealed class LabelComponentNode : ComponentNode<LabelComponentState>
{
    public override string Name => "label";

    public ComponentProperty Component { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }

    public override bool HasChildren => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }
    
    private static readonly ComponentRenderingOptions ChildRenderingOptions = new(
        TypingContext: new(
            CanSplat: false,
            ConformingType: ComponentBuilderKind.IMessageComponentBuilder
        )
    );

    public LabelComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Component = new(
                "component",
                renderer: Renderers.ComponentAsProperty
            ),
            Value = new(
                "value",
                renderer: Renderers.String
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            )
        ];
    }

    public override LabelComponentState? CreateState(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return null;

        var state = new LabelComponentState()
        {
            Source = element
        };

        CXElement? component = null;

        switch (element.Children.FirstOrDefault())
        {
            case CXValue value:
            {
                state.SubstitutePropertyValue(Value, value);
                state.ChildValue = value;

                if (element.Children.Count > 1 && element.Children[1] is CXElement labelComponent)
                    component = labelComponent;
                break;
            }
            case CXElement labelComponent:
                component = labelComponent;
                break;
        }

        if (component is not null)
        {
            state.ChildElement = component;
            state.SubstitutePropertyValue(
                Component,
                new CXValue.Element(
                    CXToken.CreateSynthetic(CXTokenKind.OpenParenthesis),
                    component,
                    CXToken.CreateSynthetic(CXTokenKind.CloseParenthesis)
                )
            );
        }

        return state;
    }

    public override void Validate(LabelComponentState state, IComponentContext context)
    {
        base.Validate(state, context);

        var component = state.GetProperty(Component);

        if (component.Attribute is not null && state.ChildValue is not null)
        {
            // specified both in attribute and in the children
            context.AddDiagnostic(
                Diagnostics.LabelComponentDuplicate,
                component.Attribute
            );

            context.AddDiagnostic(
                Diagnostics.LabelComponentDuplicate,
                state.ChildValue
            );
        }

        if (context.KnownTypes.LabelBuilderType is null)
        {
            context.AddDiagnostic(
                Diagnostics.MissingTypeInAssembly,
                state.Source,
                nameof(context.KnownTypes.LabelBuilderType)
            );
        }

        foreach (var child in ((CXElement)state.Source).Children)
        {
            if (child != state.ChildValue && child != state.ChildElement)
            {
                context.AddDiagnostic(
                    Diagnostics.TooManyChildrenInLabel,
                    child
                );
            }
        }

        var labelChild = state.GetProperty(Component).Node;

        if (labelChild is not null && !IsValidLabelChild(labelChild.Inner))
        {
            context.AddDiagnostic(
                Diagnostics.InvalidLabelChild,
                labelChild.State.Source
            );
        }
    }

    private static bool IsValidLabelChild(ComponentNode node)
        => node is IDynamicComponentNode
            or SelectMenuComponentNode
            or TextInputComponentNode
            or FileUploadComponentNode;

    public override string Render(
        LabelComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var props = string.Join(
            $",{Environment.NewLine}",
            [
                state.RenderProperties(this, context),
                state.Children.FirstOrDefault()
                    ?.Render(context, options: ChildRenderingOptions)
                    .PrefixIfSome("component: ")
            ]
        );

        return
            $"new {context.KnownTypes.LabelBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{
                props
                    .Prefix(4)
                    .WithNewlinePadding(4)
                    .WrapIfSome(Environment.NewLine)
                    .Map(x => $"({x})")
            }";
    }
}