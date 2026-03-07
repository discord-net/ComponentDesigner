using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Parser;

/// <summary>
///     An abstract class representing a non-terminal AST node.
/// </summary>
public abstract class CXNode : ICXNode
{
    /// <inheritdoc cref="ICXNode.Parent"/>
    public CXNode? Parent { get; internal set; }

    /// <inheritdoc/>
    public int Width { get; private set; }

    /// <inheritdoc/>
    public int GraphWidth
        => _graphWidth ??= (
            _slots.Count > 0
                ? _slots.Count + _slots.Sum(node => node.GraphWidth)
                : 0
        );
    
    /// <inheritdoc/>
    public bool HasErrors
        => _diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error) ||
           Slots.Any(x => x.HasErrors);

    /// <inheritdoc/>
    public CXDocument? Document
        => TryGetDocument(out var doc)
            ? doc
            : null;

    /// <inheritdoc/>
    public LexedCXTrivia LeadingTrivia => FirstTerminal?.LeadingTrivia ?? LexedCXTrivia.Empty;

    /// <inheritdoc/>
    public LexedCXTrivia TrailingTrivia => LastTerminal?.TrailingTrivia ?? LexedCXTrivia.Empty;

    /// <summary>
    ///     Gets the first terminal token within this <see cref="CXNode"/>.
    /// </summary>
    public CXToken? FirstTerminal
    {
        get
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                switch (_slots[i])
                {
                    case CXToken token: return _firstTerminal = token;
                    case CXNode { FirstTerminal: { } firstTerminal }: return _firstTerminal = firstTerminal;
                    default: continue;
                }
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets the last terminal token within this <see cref="CXNode"/>.
    /// </summary>
    public CXToken? LastTerminal
    {
        get
        {
            for (var i = _slots.Count - 1; i >= 0; i--)
            {
                switch (_slots[i])
                {
                    case CXToken token: return _lastTerminal = token;
                    case CXNode { LastTerminal: { } lastTerminal }: return _lastTerminal = lastTerminal;
                    default: continue;
                }
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets all descendant AST nodes within this <see cref="CXNode"/>.
    /// </summary>
    public IReadOnlyList<ICXNode> Descendants =>
    [
        .._slots.SelectMany(x => (ICXNode[])
        [
            x,
            ..(x as CXNode)?.Descendants ?? []
        ])
    ];

    /// <summary>
    ///     Gets all ancestor AST nodes of this <see cref="CXNode"/>.
    /// </summary>
    public IEnumerable<CXNode> Ancestors
    {
        get
        {
            var current = Parent;

            while (current is not null)
            {
                yield return current;
                current = current.Parent;
            }
        }
    }

    /// <inheritdoc/>
    public CXTextSpan FullTextSpan => new(this.Offset, Width);

    /// <inheritdoc />
    public CXTextSpan TextSpan
        => FirstTerminal is { } first && LastTerminal is { } last
            ? CXTextSpan.FromBounds(first.TextSpan.Start, last.TextSpan.End)
            : FullTextSpan;
    
    /// <inheritdoc/>
    public IReadOnlyList<ICXNode> Slots => _slots;

    internal IReadOnlyList<CXDiagnosticDescriptor> DiagnosticDescriptors
    {
        get => _diagnostics;
        init => _diagnostics = [..value];
    }
    
    private readonly List<ICXNode> _slots;
    private readonly List<CXDiagnosticDescriptor> _diagnostics;

    // cached state
    private CXToken? _firstTerminal;
    private CXToken? _lastTerminal;
    private CXDocument? _doc;
    private int? _graphWidth;

    /// <summary>
    ///     Constructs a new <see cref="CXNode"/>.
    /// </summary>
    protected CXNode()
    {
        _diagnostics = [];
        _slots = [];
    }

    /// <summary>
    ///     Attempts to get the root <see cref="CXDocument"/> by traversing up the AST tree.
    /// </summary>
    /// <param name="result">The root <see cref="CXDocument"/> if found; otherwise <see langword="null"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the root <see cref="CXDocument"/> was found; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryGetDocument(out CXDocument result)
    {
        if (_doc is not null)
        {
            result = _doc;
            return true;
        }

        var current = this;

        while (current is not null)
        {
            if (current is CXDocument document)
            {
                result = _doc = document;
                return true;
            }

            current = current.Parent;
        }

        result = null!;
        return false;
    }

    /// <summary>
    ///     Gets the index of a node within this nodes' <see cref="Slots"/>.
    /// </summary>
    /// <param name="node">The node to find the index of.</param>
    /// <returns>
    ///     The index of the node within this nodes' <see cref="Slots"/>; <c>-1</c> if not found.
    /// </returns>
    public int GetIndexOfSlot(ICXNode node)
    {
        for (var i = 0; i < _slots.Count; i++)
            if (ReferenceEquals(_slots[i], node))
                return i;

        return -1;
    }

    /// <summary>
    ///     Swaps 2 values at the given slot indexes.
    /// </summary>
    /// <param name="index1">The first index to swap.</param>
    /// <param name="index2">The second index to swap.</param>
    protected void SwapSlots(int index1, int index2)
    {
        var a = _slots[index1];
        var b = _slots[index2];
        _slots[index1] = b;
        _slots[index2] = a;
    }

    /// <summary>
    ///     Determines if a provided node is a child of this <see cref="CXNode"/>.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="index">The index of the child node.</param>
    /// <returns>
    ///     <see langword="true"/> if the node is a child of this <see cref="CXNode"/>; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    protected bool IsGraphChild(CXNode node, out int index)
    {
        index = -1;

        if (node.Parent is null || !node.Parent.Equals(this)) return false;

        index = node.GetParentSlotIndex();

        return index >= 0 && index < _slots.Count && _slots[index].Equals(node);
    }

    /// <summary>
    ///     Adds a node to this nodes' <see cref="Slots"/>.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <typeparam name="T">The inner type of the collection node.</typeparam>
    [return: NotNullIfNotNull(nameof(node))]
    protected CXCollection<T>? Slot<T>(CXCollection<T>? node) where T : class, ICXNode
    {
        Slot<CXNode>(node);
        return node;
    }

    /// <summary>
    ///     Adds a node to this nodes' <see cref="Slots"/>.
    /// </summary>
    /// <param name="node">The node to add.</param>
    [return: NotNullIfNotNull(nameof(node))]
    protected T? Slot<T>(T? node)
        where T : ICXNode
    {
        if (node is null) return node;

        Width += node.Width;

        node.Parent = this;
        _slots.Add(node);

        return node;
    }

    /// <summary>
    ///     Adds a collection of nodes to this nodes' <see cref="Slots"/>.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    protected void Slot(IEnumerable<ICXNode> nodes)
    {
        foreach (var node in nodes) Slot(node);
    }

    /// <inheritdoc/>
    public void ResetCachedState()
    {
        _firstTerminal = null;
        _lastTerminal = null;
        _doc = null;
        _graphWidth = null;

        // reset any descendants
        foreach (var descendant in Descendants.OfType<CXNode>())
            descendant.ResetCachedState();
    }

    protected internal virtual CXDiagnostic CreateDiagnostic(CXDiagnosticDescriptor descriptor)
        => new(descriptor, TextSpan);

    /// <inheritdoc/>
    public bool Equals(ICXNode? other)
        => CXNodeEqualityComparer.Default.Equals(this, other);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is CXNode node && Equals(node);

    /// <inheritdoc/>
    public override int GetHashCode()
        => _slots.Aggregate(0, Hash.Combine);

    /// <inheritdoc/>
    public override string ToString() => ToString(false, false);
    
    /// <inheritdoc/>
    public string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia)
    {
        var sb = new StringBuilder();

        var tokens = this.Walk().OfType<CXToken>().ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            sb.Append(token.ToString(
                includeLeadingTrivia: includeLeadingTrivia || i > 0,
                includeTrailingTrivia: includeTrailingTrivia || i < tokens.Length - 1
            ));
        }

        return sb.ToString();
    }

    public CXNode Clone()
    {
        var shallow = (CXNode)MemberwiseClone();

        for (var i = 0; i < shallow._slots.Count; i++)
        { 
            var slot = shallow._slots[i];

            slot = (ICXNode)slot.Clone();

            slot.Parent = shallow;
            
            shallow._slots[i] = slot;
        }

        shallow.ResetCachedState();
        
        return shallow;
    }
    
    IReadOnlyList<CXDiagnosticDescriptor> ICXNode.DiagnosticDescriptors
    {
        get => DiagnosticDescriptors;
        init => DiagnosticDescriptors = value;
    }

    object ICloneable.Clone() => Clone();
    
    CXDiagnostic ICXNode.CreateDiagnostic(CXDiagnosticDescriptor descriptor)
        => CreateDiagnostic(descriptor);

    CXNode? ICXNode.Parent
    {
        get => Parent;
        set => Parent = value;
    }
}