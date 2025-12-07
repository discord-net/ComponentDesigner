using Discord.CX.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public delegate string ComponentNodeRenderer<in TState>(
    TState state,
    IComponentContext context,
    ComponentRenderingOptions options = default
) where TState : ComponentState;

public delegate string ComponentNodeRenderer(
    ComponentState state,
    IComponentContext context,
    ComponentRenderingOptions options = default
);

public abstract class ComponentNode<TState> : ComponentNode
    where TState : ComponentState
{
    public abstract string Render(TState state, IComponentContext context, ComponentRenderingOptions options);

    public virtual void UpdateState(ref TState state, IComponentContext context)
    {
    }

    public sealed override void UpdateState(ref ComponentState state, IComponentContext context)
        => UpdateState(ref Unsafe.As<ComponentState, TState>(ref state), context);

    public abstract TState? CreateState(ComponentStateInitializationContext context);

    public sealed override ComponentState? Create(ComponentStateInitializationContext context)
        => CreateState(context);

    public sealed override string Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => Render((TState)state, context, options);

    public virtual void Validate(TState state, IComponentContext context)
    {
        base.Validate(state, context);
    }

    public sealed override void Validate(ComponentState state, IComponentContext context)
        => Validate((TState)state, context);
}

public abstract class ComponentNode
{
    protected virtual bool IsUserAccessible => true;

    public abstract string Name { get; }
    public virtual IReadOnlyList<string> Aliases { get; } = [];

    public virtual bool HasChildren => false;

    public virtual IReadOnlyList<ComponentProperty> Properties { get; } = [];

    protected virtual bool AllowChildrenInCX => HasChildren;

    public virtual void Validate(ComponentState state, IComponentContext context)
    {
        // validate properties
        foreach (var property in Properties)
        {
            var propertyValue = state.GetProperty(property);

            propertyValue.ReportPropertyConfigurationDiagnostics(context, state);

            foreach (var validator in property.Validators)
            {
                validator(context, propertyValue);
            }
        }

        if (state.Source is CXElement element)
        {
            // report any unknown properties
            foreach (var attribute in element.Attributes)
            {
                if (!TryGetPropertyFromName(attribute.Identifier.Value, out _))
                {
                    context.AddDiagnostic(
                        Diagnostics.UnknownProperty,
                        attribute,
                        attribute.Identifier.Value,
                        Name
                    );
                }
            }

            // report invalid children
            if (!AllowChildrenInCX && !HasChildren && element.Children.Count > 0)
            {
                context.AddDiagnostic(
                    Diagnostics.ComponentDoesntAllowChildren,
                    element.Children,
                    Name
                );
            }
        }
    }

    private bool TryGetPropertyFromName(string name, out ComponentProperty result)
    {
        foreach (var property in Properties)
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

    public abstract string Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    );

    public virtual void UpdateState(ref ComponentState state, IComponentContext context)
    {
    }

    public virtual ComponentState? Create(ComponentStateInitializationContext context)
    {
        if (HasChildren && context.Node is CXElement element)
        {
            context.AddChildren(element.Children);
        }

        return new ComponentState() { Source = context.Node };
    }

    public virtual void AddGraphNode(ComponentGraphInitializationContext context)
    {
        context.Push(this, cxNode: context.CXNode);
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

    public static bool TryGetProviderNode(
        SemanticModel cxSemanticModel,
        int position,
        string name,
        out ComponentNode node
    )
    {
        foreach (var candidate in cxSemanticModel.LookupSymbols(position, name: name))
        {
            if (candidate is INamedTypeSymbol typeSymbol)
            {
                var providerInterface = typeSymbol
                    .AllInterfaces
                    .FirstOrDefault(x =>
                        x.IsGenericType &&
                        x.ConstructedFrom.Equals(
                            cxSemanticModel.Compilation.GetKnownTypes().ICXProviderType,
                            SymbolEqualityComparer.Default
                        )
                    );

                if (providerInterface is null ||
                    providerInterface.TypeArguments[0] is not INamedTypeSymbol stateSymbol) continue;

                node = new ProviderComponentNode(stateSymbol, typeSymbol, cxSemanticModel.Compilation);
                return true;
            }

            if (candidate is IMethodSymbol methodSymbol)
            {
                if (!methodSymbol.IsStatic)
                {
                    continue;
                }

                if (methodSymbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
                    continue;

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        methodSymbol.ReturnType,
                        cxSemanticModel.Compilation,
                        out var kind
                    )
                ) continue;

                node = FunctionalComponentNode.Create(methodSymbol, kind, cxSemanticModel.Compilation);
                return true;
            }
        }


        node = null!;
        return false;
    }
}