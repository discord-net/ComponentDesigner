using System;
using System.Text.Json;
using System.Threading;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace ComponentDesigner.Json;

public sealed class JsonComponentImplementation : IComponentImplementation
{
    public string Name => "Json";

    public IComponentRenderer Renderer { get; }
    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;
    public IComponentTypingProvider? ComponentTypingProvider => null;
    public IComponentExtensionProvider? ComponentExtensionProvider => null;

    public JsonComponentImplementation(JsonSerializerOptions? options = null)
    {
        Renderer = new JsonRenderer(options);
    }

    public bool TryAnalyzeNumberOfValues(
        IComponentContext context,
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    )
    {
        range = default;
        return false;
    }
}