using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components.SelectMenus;

public abstract class SelectMenuDefaultValue : IEquatable<SelectMenuDefaultValue>
{
    public abstract ICXNode Owner { get; }
    public abstract SelectMenuDefaultValueKind Kind { get; }

    public void Validate(
        IComponentContext context,
        SelectMenuComponentNode.SelectState selectState,
        IList<DiagnosticInfo> diagnostics
    ) => ValidateInternal(context, selectState, diagnostics);

    protected abstract void ValidateInternal(
        IComponentContext context,
        SelectMenuComponentNode.SelectState selectState,
        IList<DiagnosticInfo> diagnostics
    );

    public abstract Result<string> Render(IComponentContext context, SelectMenuComponentNode.SelectState selectState);

    private static string FromIdAndKind(IComponentContext context, string id, SelectMenuDefaultValueKind kind)
    {
        var dotnetType = context.KnownTypes
            .SelectMenuDefaultValueType!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var kindType = context.KnownTypes
            .SelectDefaultValueTypeEnumType!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return
            $"""
             new {dotnetType}(
                 id: {id},
                 type: {kindType}.{kind}
             )
             """;
    }

    public static SelectMenuDefaultValue Create(ICXNode node)
    {
        switch (node)
        {
            case CXElement element: return new Element(element);
            case CXValue.Interpolation interpolation: return new Interpolation(interpolation.Token);
            case CXToken token when token.Document?.IsInterpolation(token) is true: return new Interpolation(token);
            default: return new InvalidNode(node);
        }
    }

    public virtual bool Equals(SelectMenuDefaultValue other)
    {
        if (ReferenceEquals(this, other)) return true;

        return Kind == other.Kind && Owner.Equals(other.Owner);
    }
    
    
    private sealed class InvalidNode(ICXNode node) : SelectMenuDefaultValue
    {
        public override ICXNode Owner => node;
        public override SelectMenuDefaultValueKind Kind => SelectMenuDefaultValueKind.Unknown;

        protected override void ValidateInternal(
            IComponentContext context,
            SelectMenuComponentNode.SelectState selectState,
            IList<DiagnosticInfo> diagnostics
        )
        {
            diagnostics.Add(
                Diagnostics.InvalidSelectMenuDefaultKind(node.GetType().Name),
                node
            );
        }

        public override Result<string> Render(
            IComponentContext context,
            SelectMenuComponentNode.SelectState selectState
        ) => Result<string>.FromDiagnostic(
            Diagnostics.InvalidSelectMenuDefaultKind(node.GetType().Name),
            node
        );

    }

    public sealed class Interpolation(CXToken interpolation) : SelectMenuDefaultValue
    {
        // KEEP IN SYNC WITH SelectMenuDefaultValueKind
        [Flags]
        private enum InterpolationKind
        {
            Unknown,

            User = 0b001,
            Role = 0b010,
            Channel = 0b011,

            LibrarySymbol = 0b100,

            EnumerableOf = 0b1000,

            LibraryDefaultValueKindMask = 0b0011,
            BasicKindMask = 0b0111,
        }

        public override ICXNode Owner => interpolation;

        public override SelectMenuDefaultValueKind Kind
            => _kind is null
                ? SelectMenuDefaultValueKind.Unknown
                : (SelectMenuDefaultValueKind)((int)(_kind.Value & InterpolationKind.LibraryDefaultValueKindMask));

        private InterpolationKind? _kind;

        private bool TryGetKind(
            IComponentContext context,
            out InterpolationKind kind
        )
        {
            var info = context.GetInterpolationInfo(interpolation);

            var symbol = info.Symbol;

            kind = InterpolationKind.Unknown;

            if (symbol.TryGetEnumerableType(out var innerSymbol))
            {
                kind |= InterpolationKind.EnumerableOf;
                symbol = innerSymbol;
            }

            if (
                context.Compilation.HasImplicitConversion(
                    symbol,
                    context.KnownTypes.IUserType
                )
            )
            {
                kind |= InterpolationKind.User;
                return true;
            }

            if (
                context.Compilation.HasImplicitConversion(
                    symbol,
                    context.KnownTypes.IChannelType
                )
            )
            {
                kind |= InterpolationKind.Channel;
                return true;
            }

            if (
                context.Compilation.HasImplicitConversion(
                    symbol,
                    context.KnownTypes.IRoleType
                )
            )
            {
                kind |= InterpolationKind.Role;
                return true;
            }

            if (
                context.Compilation.HasImplicitConversion(
                    symbol,
                    context.KnownTypes.SelectMenuDefaultValueType
                )
            )
            {
                kind |= InterpolationKind.LibrarySymbol;
                return true;
            }

            kind = InterpolationKind.Unknown;
            return false;
        }

        private string RenderKind(IComponentContext context, InterpolationKind kind)
        {
            var info = context.GetInterpolationInfo(interpolation);
            var designer = context.GetDesignerValue(
                info,
                info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            );

            var basicKind = kind & InterpolationKind.BasicKindMask;

            if (kind.HasFlag(InterpolationKind.EnumerableOf))
            {
                if (basicKind is InterpolationKind.LibrarySymbol)
                    return $"..{designer}";

                return
                    $"""
                     ..{designer}.Select(x => 
                         {FromIdAndKind(context, "x.Id", Kind).WithNewlinePadding(4)}    
                     )
                     """;
            }

            if (basicKind is InterpolationKind.LibrarySymbol)
                return designer;

            return FromIdAndKind(context, $"{designer}.Id", Kind);
        }

        protected override void ValidateInternal(
            IComponentContext context,
            SelectMenuComponentNode.SelectState selectState,
            IList<DiagnosticInfo> diagnostics
        )
        {
            if (!TryGetKind(context, out var kind))
            {
                diagnostics.Add(
                    Diagnostics.TypeMismatch(
                        "select menu default",
                        context.GetInterpolationInfo(interpolation).Symbol!.ToDisplayString()
                    ),
                    interpolation
                );

                return;
            }

            if (kind is not InterpolationKind.Unknown and not InterpolationKind.LibrarySymbol)
            {
                switch (selectState.Kind)
                {
                    case SelectKind.Channel when !kind.HasFlag(InterpolationKind.Channel):
                    case SelectKind.User when !kind.HasFlag(InterpolationKind.User):
                    case SelectKind.Role when !kind.HasFlag(InterpolationKind.Role):
                        diagnostics.Add(
                            Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu(
                                kind.ToString(),
                                selectState.Kind.ToString()
                            ),
                            interpolation
                        );
                        break;
                }
            }


            _kind = kind;
        }

        public override Result<string> Render(
            IComponentContext context,
            SelectMenuComponentNode.SelectState selectState
        )
        {
            if (_kind is null or InterpolationKind.Unknown) return default;

            return RenderKind(context, _kind.Value);
        }
    }

    public sealed class Element(CXElement element) : SelectMenuDefaultValue
    {
        public override ICXNode Owner => element;
        public override SelectMenuDefaultValueKind Kind => _kind ?? SelectMenuDefaultValueKind.Unknown;

        private SelectMenuDefaultValueKind? _kind;
        private CXValue? _value;

        protected override void ValidateInternal(
            IComponentContext context,
            SelectMenuComponentNode.SelectState selectState,
            IList<DiagnosticInfo> diagnostics
        )
        {
            _kind = element.Identifier.ToLowerInvariant() switch
            {
                "user" => SelectMenuDefaultValueKind.User,
                "role" => SelectMenuDefaultValueKind.Role,
                "channel" => SelectMenuDefaultValueKind.Channel,
                _ => null
            };

            if (_kind is null)
            {
                diagnostics.Add(
                    Diagnostics.InvalidSelectMenuDefaultKind(element.Identifier),
                    element
                );

                return;
            }

            if (element.Children.Count is 0)
            {
                diagnostics.Add(
                    Diagnostics.MissingSelectMenuDefaultValue,
                    element
                );

                return;
            }

            if (element.Children.Count > 1)
            {
                diagnostics.Add(
                    Diagnostics.TooManyValuesInSelectMenuDefault,
                    CXTextSpan.FromBounds(
                        element.Children[1].Span.Start,
                        element.Children.Last().Span.End
                    )
                );

                return;
            }

            if (element.Children[0] is not CXValue value)
            {
                diagnostics.Add(
                    Diagnostics.InvalidSelectMenuDefaultChild(element.Children[0].GetType().Name),
                    element.Children[0]
                );

                return;
            }

            _value = value;
        }

        public override Result<string> Render(IComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            if (_value is null || !_kind.HasValue) return default;

            switch (_value)
            {
                case CXValue.Scalar scalar:
                    if (!ulong.TryParse(scalar.Value, out var id))
                    {
                        return Result<string>.FromDiagnostic(
                            Diagnostics.TypeMismatch("ulong", "text"),
                            scalar
                        );
                    }

                    return FromId(id.ToString());

                case CXValue.Interpolation interpolation:
                    var info = context.GetInterpolationInfo(interpolation);

                    if (
                        context.Compilation.HasImplicitConversion(
                            info.Symbol,
                            context.Compilation.GetSpecialType(SpecialType.System_UInt64)
                        )
                    )
                    {
                        return FromId(
                            context.GetDesignerValue(
                                info,
                                info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            )
                        );
                    }

                    switch (_kind)
                    {
                        case SelectMenuDefaultValueKind.User
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IUserType
                            ):
                        case SelectMenuDefaultValueKind.Role
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IRoleType
                            ):
                        case SelectMenuDefaultValueKind.Channel
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IChannelType
                            ):
                            return FromId(
                                $"{
                                    context.GetDesignerValue(
                                        info,
                                        info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                    )
                                }.Id"
                            );
                    }

                    return Result<string>.FromDiagnostic(
                        Diagnostics.TypeMismatch(_kind.Value.ToString(), info.Symbol!.ToDisplayString()),
                        interpolation
                    );
                default:
                    return Result<string>.FromDiagnostic(
                        Diagnostics.TypeMismatch(_kind.Value.ToString(), _value?.GetType().Name ?? "null"),
                        _value?.Span ?? element.Span
                    );
            }

            string FromId(string id)
            {
                if (_kind is null) return string.Empty;

                return FromIdAndKind(context, id, _kind.Value);
            }
        }
    }

}