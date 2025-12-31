using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public static partial class Diagnostics
{
    public static DiagnosticDescriptor CreateParsingDiagnostic(CXDiagnostic diagnostic)
        => new DiagnosticDescriptor(
            $"DCP{((int)diagnostic.Code).ToString().PadLeft(3, '0')}",
            diagnostic.Message,
            diagnostic.Message,
            "CX Parser",
            diagnostic.Severity,
            true
        );

    public static DiagnosticDescriptor InvalidEnumVariant(string variant, string target) => new(
        "DC0001",
        "Invalid enum variant",
        $"'{variant}' is not a valid variant of '{target}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor TypeMismatch(string expected, string actual) => new(
        "DC0002",
        "Type mismatch",
        $"'{actual}' is not of expected type '{expected}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor OutOfRange(string a, string b) => new(
        "DC0003",
        "Type mismatch",
        "'{0}' must be {1}",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor UnknownComponent(string identifier) => new(
        "DC0004",
        "Unknown component",
        $"'{identifier}' is not a known component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonCustomIdUrlConflict = new(
        "DC0005",
        "Invalid button",
        "Buttons cannot contain both a 'url' and a 'customid'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonCustomIdOrUrlMissing = new(
        "DC0006",
        "Invalid button",
        "A button must specify either a 'customId' or a 'url'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor LinkButtonUrlMissing = new(
        "DC0007",
        "Invalid button",
        "A 'link' button must specify 'url'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor PremiumButtonSkuMissing = new(
        "DC0008",
        "Invalid button",
        "A 'premium' button must specify 'skuId'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor PremiumButtonPropertyNotAllowed = new(
        "DC0009",
        "Invalid button",
        "A 'premium' button cannot specify '{0}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonLabelDuplicate = new(
        "DC0010",
        "Duplicate label definition",
        "A button cannot specify both a body and a 'label'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor EmptyActionRow = new(
        "DC0011",
        "Empty Action Row",
        "An action row must contain at least one child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor MissingRequiredProperty(string? owner, string property) => new(
        "DC0012",
        "Missing Property",
        $"'{owner ?? "Unknown"}' requires the property '{property}' to be specified",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor UnknownProperty(string property, string? owner) => new(
        "DC0013",
        "Unknown Property",
        $"'{property}' is not a known property of '{owner ?? "Unknown"}'",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor EmptyAccessory = new(
        "DC0014",
        "Empty Accessory",
        "An accessory must have 1 child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyAccessoryChildren = new(
        "DC0015",
        "Too many accessory children",
        "An accessory must have 1 child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor EmptySection = new(
        "DC0016",
        "Section cannot be empty",
        "A section must have an accessory and a child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidAccessoryChild(string accessory) => new(
        "DC0017",
        "Invalid accessory child",
        $"'{accessory}' is not a valid accessory, only buttons and thumbnails are allowed",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingAccessory = new(
        "DC0018",
        "Missing accessory",
        "A section must contain an accessory",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyAccessories = new(
        "DC0019",
        "Too many accessories",
        "A section can only contain one accessory",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingSectionChild = new(
        "DC0020",
        "Missing section child",
        "A section must contain at least 1 non-accessory component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManySectionChildren = new(
        "DC0021",
        "Too many section children",
        "A section must contain at most 3 non-accessory components",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidSectionChildComponentType(string child) => new(
        "DC0022",
        "Invalid section child component type",
        $"'{child}' is not a valid child component of a section; only text displays are allowed",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingSelectMenuType = new(
        "DC0023",
        "Missing select menu type",
        "You must specify the type of the select menu, being one of 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidSelectMenuType = new(
        "DC0024",
        "Invalid select menu type",
        "Select menu type must be either 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor SpecifiedInvalidSelectMenuType(string type) => new(
        "DC0025",
        "Invalid select menu type",
        $"'{type}' is not a valid elect menu type; must be either 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ActionRowInvalidChild = new(
        "DC0026",
        "Invalid action row child component",
        "An action row can only contain 1 select menu OR at most 5 buttons",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidPropertyValueSyntax(string expected) => new(
        "DC0027",
        "Invalid syntax",
        $"Expected '{expected}' as the property value",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonMustHaveALabelOrEmoji = new(
        "DC0028",
        "A button must have a label or emoji",
        "A button must have a label or emoji",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor PossibleInvalidEmote(string emote) => new(
        "DC0029",
        "Possible invalid emote",
        $"'{emote}' doesn't look like a unicode emoji or a custom emote",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );

    public static DiagnosticDescriptor InvalidChildComponentCardinality(string owner) => new(
        "DC0030",
        "Too many children",
        $"'{owner}' only accepts one child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor FileUploadNotInLabel = new(
        "DC0031",
        "A file upload component must be placed in a label",
        "File uploads' parent must be a label",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor MissingTypeInAssembly(string type) => new(
        "DC0032",
        "Missing type in assembly",
        $"Could not find '{type}' in your assembly",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingLabelComponent = new(
        "DC0033",
        "Label is missing a child component",
        "Label requires a child component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyChildrenInLabel = new(
        "DC0034",
        "Too many children in Label",
        "Labels can only contain 1 component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidLabelChild = new(
        "DC0035",
        "Invalid label child component",
        "Labels can only contain a text input, file upload, or select menu",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidContainerChild(string child) => new(
        "DC0036",
        "Invalid container child component",
        $"'{child}' is not a valid child component of 'container'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidMediaGalleryChild(string child) => new(
        "DC0037",
        "Invalid media gallery child component",
        $"'{child}' is not a valid child component of 'media gallery'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor PropertyNotAllowed(string owner, string property) => new(
        "DC0038",
        "Property not allowed",
        $"'{owner}' doesn't allow the property '{property}' to be specified in the current configuration",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor CardinalityForcedToRuntime(string target) => new(
        "DC0040",
        "Cardinality forced to runtime check",
        $"'{target}' can be more than 1 component, a runtime check will occur to enforce a single component",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor LabelComponentDuplicate = new(
        "DC0041",
        "Duplicate component definition",
        "A label cannot specify 'component' both in an attribute and in the children",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor ComponentDoesntAllowChildren(string owner) => new(
        "DC0042",
        "Component doesn't allow children",
        $"'{owner}' doesn't allow children",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MediaGalleryIsEmpty = new(
        "DC0043",
        "Empty media gallery",
        "A media gallery must have at least one 'media-gallery-item'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyItemsInMediaGallery = new(
        "DC0044",
        "Too many items in media gallery",
        $"A media gallery can have at most {Constants.MAX_MEDIA_ITEMS} 'media-gallery-item's",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidRange(string lower, string upper) => new(
        "DC0045",
        "Invalid range",
        $"'{lower}' must be less than or equal to '{upper}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidSelectMenuDefaultKind(string kind) => new(
        "DC0046",
        "Invalid select menu default kind",
        $"'{kind}' is not a valid default kind, valid kinds are: 'user', 'role', and 'channel'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingSelectMenuDefaultValue = new(
        "DC0047",
        "Missing value for default option",
        "A value is required for a default select menu option",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyValuesInSelectMenuDefault = new(
        "DC0048",
        "Too many values in default option",
        "At most 1 value is allowed for a select menu default option",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidSelectMenuDefaultChild(string child) => new(
        "DC0049",
        "Invalid child of select menu default option",
        $"'{child}' is not a valid value, expected a scalar or interpolation",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidSelectMenuDefaultKindInCurrentMenu(string kind, string menu) => new(
        "DC0050",
        "Invalid default value kind",
        $"'{kind}' is not a valid default kind for the menu '{menu}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor EmptyStringSelectMenu = new(
        "DC0051",
        "A string select menu requires at least one option",
        "A string select menu requires at least one option",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidStringSelectChild(string child) => new(
        "DC0052",
        "Invalid child of string select menu",
        $"'{child}' is not a valid child of a string select menu; valid children are: 'select-menu-option' or <interpolation>",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyStringSelectMenuChildren = new(
        "DC0053",
        "Too many string select children",
        $"A string select menu must contain at most {Constants.STRING_SELECT_MAX_VALUES} options",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor FallbackToRuntimeValueParsing(string method) => new(
        "DC0054",
        "Using a runtime parse method",
        $"The value may be invalid or out of range, falling back to the runtime parsing method '{method}'",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );

    public static DiagnosticDescriptor
        InvalidInterleavedComponentInCurrentContext(string interleaved, string context) => new(
        "DC0055",
        "Invalid interpolated component",
        $"'{interleaved}' cannot be used in an expected context of '{context}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor DuplicateChildParameter(string method) => new(
        "DC0056",
        "Duplicate child parameter",
        $"'{method}' cannot specify a children parameter twice",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidFunctionalComponentReturnType(string method, string type) => new(
        "DC0056",
        "Invalid functional component return type",
        $"'{method}' returns `{type}` instead of a valid component type",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidFunctionalComponentKind(string symbol, string reason) => new(
        "DC0057",
        "Invalid functional component",
        $"cannot use '{symbol}' as a functional component: {reason}",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor AmbiguousFunctionalComponent(params IEnumerable<string> symbols) => new(
        "DC0058",
        "Invalid functional component",
        $"Ambiguous functional component: {string.Join(", ", symbols.Select(x => $"'{x}'"))}",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ExpectedScalarFunctionalComponentChildValue = new(
        "DC0059",
        "Invalid functional component child",
        $"expected scalar values (text or interpolations)",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor DuplicateProperty(string a, string b) => new(
        "DC0060",
        "Duplicate property",
        $"You can only specify either '{a}' or '{b}', not both",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ExpectedAConstantValue = new(
        "DC0061",
        "Expected a constant value",
        $"A constant value is expected here",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static DiagnosticDescriptor InvalidChild(string parent, string child) => new(
        "DC0062",
        $"'{parent}' doesn't allow '{child}' as children",
        $"'{parent}' doesn't allow '{child}' as children",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor AutoTextDisplayDisabled = new(
        "DC0063",
        $"Text must be wrapped in a 'text-display' node",
        $"Text related components must be wrapped in a 'text-display' node, you can enable auto text displays to automatically wrap text in a text-display",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor AutoRowsDisabled = new(
        "DC0064",
        $"Input elements must be wrapped in an 'action-row' node",
        $"Input elements must be wrapped in an 'action-row', you can enable auto rows to automatically wrap inputs in rows",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static DiagnosticDescriptor TooManyChildren(string owner) => new(
        "DC0065",
        $"Too many children in '{owner}'",
        $"'{owner}' can only have at most one child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static DiagnosticDescriptor InvalidValue(string type) => new(
        "DC0066",
        $"Invalid value '{type}'",
        $"'{type}' cannot be used here",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
}