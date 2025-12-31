using Discord.CX.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public delegate Result<string> ComponentNodeRenderer<in TState>(
    TState state,
    IComponentContext context,
    ComponentRenderingOptions options = default
) where TState : ComponentState;

public delegate Result<string> BoundComponentNodeRenderer(
    IComponentContext context,
    ComponentRenderingOptions options = default
);

public abstract class ComponentNode
{
    protected virtual bool IsUserAccessible => true;

    public abstract string Name { get; }
    public virtual IReadOnlyList<string> Aliases { get; } = [];

    public virtual bool HasChildren => false;

    public virtual ImmutableArray<ComponentProperty> Properties { get; } = [];

    protected virtual bool AllowChildrenInCX => HasChildren;

    public virtual void Validate(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        ValidateProperties(state, Properties, context, diagnostics);
        ValidateChildren(state, context, diagnostics);
    }

    protected void ValidateProperties(
        ComponentState state,
        ImmutableArray<ComponentProperty> properties,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics,
        Predicate<ComponentProperty>? ignorePredicate = null
    )
    {
        // validate properties
        foreach (var property in properties)
        {
            if(ignorePredicate?.Invoke(property) is true) continue;
            
            var propertyValue = state.GetProperty(property);

            propertyValue.ReportPropertyConfigurationDiagnostics(context, state, diagnostics);

            foreach (var validator in property.Validators)
            {
                validator(context, propertyValue, diagnostics);
            }
        }

        if (state.Source is CXElement element)
        {
            // report any unknown properties
            foreach (var attribute in element.Attributes)
            {
                if (!TryGetPropertyFromName(properties, attribute.Identifier, out _))
                {
                    diagnostics.Add(
                        Diagnostics.UnknownProperty(
                            attribute.Identifier,
                            Name
                        ),
                        attribute
                    );
                }
            }
        }
    }

    protected void ValidateChildren(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics,
        bool? allowsChildrenInCX = null,
        bool? hasChildren = null
    ) => ValidateChildren(
        Name,
        state,
        context,
        diagnostics,
        allowsChildrenInCX ?? AllowChildrenInCX,
        hasChildren ?? HasChildren
    );

    protected static void ValidateChildren(
        string name,
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics,
        bool allowsChildrenInCX,
        bool hasChildren
    )
    {
        if (state.Source is not CXElement element) return;

        // report invalid children
        if (!allowsChildrenInCX && !hasChildren && element.Children.Count > 0)
        {
            diagnostics.Add(
                Diagnostics.ComponentDoesntAllowChildren(name),
                element.Children
            );
        }
    }

    private bool TryGetPropertyFromName(
        string name,
        out ComponentProperty result
    ) => TryGetPropertyFromName(Properties, name, out result);

    private static bool TryGetPropertyFromName(
        ImmutableArray<ComponentProperty> properties,
        string name,
        out ComponentProperty result
    )
    {
        foreach (var property in properties)
        {
            if (property.Name == name || property.Aliases.Contains(name))
            {
                result = property;
                return true;
            }
        }

        result = null!;
        return false;
    }

    public abstract Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    );

    public virtual ComponentState UpdateState(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    ) => state;

    public virtual ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        return new ComponentState(
            context.GraphNode,
            context.CXNode
        );
    }

    public virtual void AddGraphNode(ComponentGraphInitializationContext context)
    {
        context.Push(
            this,
            cxNode: context.CXNode,
            children: HasChildren && context.CXNode is CXElement element
                ? element.Children
                : null
        );
    }


    private static readonly Dictionary<string, ComponentNode> _nodes;

    static ComponentNode()
    {
        _nodes = typeof(ComponentNode)
            .Assembly
            .GetTypes()
            .Where(x =>
                !x.IsAbstract &&
                typeof(ComponentNode).IsAssignableFrom(x) &&
                x.GetConstructor(Type.EmptyTypes) is not null
            )
            .Select(x => (ComponentNode)Activator.CreateInstance(x)!)
            .Where(x => x.IsUserAccessible)
            .SelectMany(x => x
                .Aliases
                .Prepend(x.Name)
                .Select(y => new KeyValuePair<string, ComponentNode>(y, x)))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public static bool TryGetComponentNode<T>(out T node)
        where T : ComponentNode
        => (node = (T?)_nodes.Values
                .FirstOrDefault(x => x.GetType() == typeof(T))!)
            is not null;

    public static T GetComponentNode<T>() where T : ComponentNode
        => _nodes.Values.OfType<T>().First();

    public static bool TryGetNode(string name, out ComponentNode node)
        => _nodes.TryGetValue(name, out node);
}