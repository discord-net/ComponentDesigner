using System;
using System.Collections.Generic;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components;

public sealed class AutoTextDisplayComponentNode : TextDisplayComponentNode
{
    public static readonly AutoTextDisplayComponentNode Instance = new();

    protected override bool IsUserAccessible => false;

    public override void AddGraphNode(ComponentGraphInitializationContext context)
        => throw new InvalidOperationException("Auto nodes don't use default graph initialization");

    public override TextDisplayState? CreateState(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    ) => throw new InvalidOperationException("Auto nodes don't use default state creation");

    public override void Validate(
        TextDisplayState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (state.Content is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty("auto text", Content.Name),
                default(CXTextSpan)
            );
        }
    }
}