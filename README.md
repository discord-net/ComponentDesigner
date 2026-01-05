# Discord.Net Component Designer

An elegant way to design and write [Discord Message Components](https://discord.com/developers/docs/components/reference) with compile-time validation

<p float="left" align="center">
    <img width="45%"src="./media/interpolations.gif" alt="compile time validation demo" />
    <img width="45%" src="./media/validation.gif" alt="compile time validation demo" />
</p>

### How does it work?

The Component Designer uses a [source generator](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) to find any calls to `Discord.ComponentDesigner.cx`, using a [custom parser](./src/Discord.Net.ComponentDesigner.Parser/) to parse the syntax, validates the structure against Discord's component contraints, and if everything looks good then an [interceptor](https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md) is emitted, replacing the `cx` call with the Discord.Net component builder structure.

### Getting started

Install the `Discord.Net.ComponentDesigner` package from one of the following feeds:
- [Discord.Net Baget](https://baget.discordnet.dev/)
- [Github Packages](https://github.com/discord-net/ComponentDesigner/pkgs/nuget/Discord.Net.ComponentDesigner)

Thats it! You can now use the [Discord.ComponentDesigner.cx](#component-designer) method:

```cs
var myComponent = ComponentDesigner.cx(
    """
    <text>Hello, World!</text>
    """
);
```

### Features

- [Component Syntax (CX)](#component-syntax)
  - [Elements](#elements)
  - [Attributes](#attributes)
  - [Escape Characters](#escape-characters)
  - [String Literals](#string-literals)
  - [Interpolation Syntax](#interpolation-syntax)
- [Component Designer](#component-designer)
  - [Built-in Components](#built-in-components)
    - [Action Row](#action-row)
    - [Button](#button)
    - [Container](#container)
    - [File](#file)
    - [Label](#label)
    - [Media Gallery](#media-gallery)
    - [Section](#section)
    - [Select Menu](#select-menu)
    - [Separator](#separator)
    - [Text Display](#text-display)
    - [Text Input](#text-input)
    - [Thumbnail](#thumbnail)
  - [Text Formatting Elements](#text-formatting-elements)
  - [Auto Text Display](#auto-text-display)
  - [Auto Rows](#auto-rows)
  - [String Interpolation](#string-interpolation)
  - [Interpolated Components](#interpolated-components)
  - [Compile-time Diagnostics](#compile-time-diagnostics)
  - [Reusable Custom Components](#reusable-custom-components)
    - [Functional Components](#functional-components)
    - [Functional Components with children](#functional-components-with-children)
  - [Multi-Cardinality Components](#component-cardinality)

## Component Syntax

The syntax for writing components is based off of JSX, and is referred as 'CX' within the codebase.

### Elements

An element consists of a name, some attributes, and some children:
```xml
<Example />
<Example foo="bar" />
<Parent>
    <Child1 />
    Some text
    <Child2 />
    {DateTime.UtcNow}
</Parent>
```

Valid children of an element are:
- Another element.
- Text
- Interpolations

### Attributes

Attributes consist of a name and an optional value, valid values can be:
- A [string literal](#string-literals).
- An [interpolation](#interpolation-syntax)
- An [inline element](#inline-elements-in-attributes)


```xml
<Example
    foo="abc"
    bar='def{123}'
    baz={123}
    switch1
    switch2
    inline=(<Foo/>)
/>
```


#### Inline elements in attributes

Inline elements are valid for attribute values, and are defined as one [element](#elements) wrapped in parenthesis `()`:

```xml
<Example
    foo=(<Bar/>)
/>
```


### String literals

String literals are text and/or interpolations wrapped in quotes. You can either use a single quote or a double quote to denote a string literal. The quote character can be used to simply escapes; double quotes don't need to be escaped in a single-quote literal, and vice versa.

```js
'my string'
"my other string with 'single quotes'"
'escaped \' quotes \''
```

### Escape characters

escaping rules for the syntax are based off of the C# string they're contained in, for example if you're in a multi-line string literal with 3 quotes, you don't need to escape a single quote within the syntax, but if you're in a simple string literal, you may need to.

For the CX language specific escape control, you can use `\` to escape characters.


### Interpolation Syntax

String interpolation is controlled by the C# syntax, and is parsed based off of what C# says is an interpolation. 

Using multiple dollar signs on the containing string to control interpolation character count is supported by the syntax.

# Component Designer

The component designer is class `Discord.ComponentDesigner` contains the `cx` methods used to write components. It's recommended to import the class statically for shorter inline use of CX:
```cs
using static Discord.ComponentDesigner;

// you can now just call 'cx'
var myAwesomeComponent = cx(...);
```

## Built-in Components

The Component Designer comes with all the [components provided by Discord](https://discord.com/developers/docs/components/overview) as built-ins.

### Action Row

Action rows wrap buttons or select menus providing top-level layout.

#### Aliases
- `row`

#### Attributes

| Name | Type | Description                        |
| ---- | ---- | ---------------------------------- |
| id?  | int  | The optonal identifier for the row |

#### Valid children
- [Button](#button)
- [Select Menu](#select-menu)

```html
<action-row id={1}>
    ...
</action-row>
```

### Button

A button is an interactive component that can only be used in messages. Buttons must be placed in an [Action Row](#action-row) or as an accessory to a [Section](#section)

#### Attributes

| Name      | Type                          | Description                            |
| --------- | ----------------------------- | -------------------------------------- |
| id?       | int                           | The optional identifier for the button |
| style?    | string \| Discord.ButtonStyle | The style of the button                |
| label?    | string \| children            | The label of the button                |
| emoji?    | string \| Discord.IEmote      | The emoji of the button                |
| customId? | string                        | The custom id of the button            |
| sku?      | ulong \| string               | A SKU id                               |
| url?      | string                        | A url for a link style button          |
| disabled? | boolean                       | Whether the button is disabled         |

Example basic button
```html
<button 
    customId="my-button"
    style="success"
>
    Click me!
</button>
```

Example URL button
```html
<button
    style="link"
    url="http://example.com"
    emoji="🔗"
/>
```

### Container

A container is a top-level layout component, offering the ability to visually encapsulate a collection of components and can have an optional accent color.

#### Attributes

| Name    | Type                           | Description                               |
| ------- | ------------------------------ | ----------------------------------------- |
| id?     | int                            | The optional identifier for the container |
| color?  | string \| int \| Discord.Color | The accent color of the container         |
| spoiler | boolean                        | Whether or not the container is a spoiler |

#### Valid children
- [Action Row](#action-row)
- [Text Display](#text-display)
- [Section](#section)
- [Media Gallery](#media-gallery)
- [Separator](#separator)
- [File](#file)

```html
<container color="#00FF00">
    ...
</container>
```

### File

A file allows you to display an uploaded file as an attachment.

You can use `attachment://` to reference attachments uploaded in a message

#### Attributes

| Name     | Type    | Description                          |
| -------- | ------- | ------------------------------------ |
| id?      | int     | The optional identifier for the file |
| url      | string  | The URL of the file to attach        |
| spoiler? | boolean | Whether or not the file is a spoiler |

```html
<file
    id={123}
    url="attachment://file.png"
    spoiler
/>
```

### Label

A label wraps modal components with text and an optional description

#### Attributes

| Name         | Type               | Description                           |
| ------------ | ------------------ | ------------------------------------- |
| id?          | int                | The optional identifier for the label |
| value        | string \| children | The value text of the label           |
| description? | string             | The description of the label          |

#### Valid children
- Text\*
- [Text Input](#text-input)
- [Select Menu](#select-menu)

\* Text must be the first child of a label to be mapped to the `value`

```html
<label description="My Description">
    My Label
    <button {...}/>
</label>
```

### Media Gallery

A Media Gallery allows you to display media attachments in an organized format.

#### Aliases
- `gallery`

#### Attributes
| Name | Type | Description                             |
| ---- | ---- | --------------------------------------- |
| id?  | int  | The optional identifier for the gallery |

#### Valid children
- [Media Gallery Item](#media-gallery-item)
- Interpolations of supported types:
  - `Uri`
  - `string` 
  - `UnfurledMediaItemProperties`
  - `IEnumerable<T>` of the above

```html
<media-gallery id={123}>
    <item url="https://example.com/1.png" />
    {myUri}
    {myStringUrl}
    {myUnfurledItem}
    {myUriCollection}
</media-gallery>
```

### Media Gallery Item

Used within a [Media Gallery](#media-gallery) representing a single media item.

#### Aliases
- `gallery-item`
- `media`
- `item`

#### Attributes
| Name         | Type    | Description                     |
| ------------ | ------- | ------------------------------- |
| url          | string  | The url of the media to display |
| description? | string  | The description of the media    |
| spoiler?     | boolean | Whether the media is a spoiler  |

```html
<media-gallery-item
    url="attachment://media1.png"
    description="My Description"
    spoiler
/>
```

### Section
A Section allows you to contextually associate content with an accessory component.

#### Attributes

| Name      | Type                  | Description                             |
| --------- | --------------------- | --------------------------------------- |
| id?       | int                   | The optional identifier for the section |
| accessory | component \| children | The accessory of the section            |

#### Valid children
- [Text Display](#text)
- [Accessory](#accessory)

#### Valid accessories
- [Button](#button)
- [Thumbnail](#thumbnail)

```html
<section
    id={123}
>
    <text>...</text>
    <accessory>...</accessory>
</section>

<section
    id={123}
    accessory={myAccessory}
>
    ...
</section>
```

### Accessory

An Accessory is used within the [Section](#section) to denote which child is to be treated as an accessory.
It does not contain any attributes and must contain a valid accessory type as its child.

### Select Menu

A Select Menu allows users to select one or more provided choices.

#### Aliases
- `select`

#### Attributes

| Name         | Type                                   | Description                                                          |
| ------------ | -------------------------------------- | -------------------------------------------------------------------- |
| type         | [Select Menu Type](#select-menu-types) | The type of the select menu |
| id?          | int                                    | The optional identifier for the select menu                          |
| customId     | string                                 | The custom id of the select menu                                     |
| placeholder? | string                                 | A placeholder to show if nothing is selected                         |
| min?         | int                                    | The minimum number of items that can be chosen                       |
| max?         | int                                    | The maximum number of items that can be chosen                       |
| required?    | boolean                                | Whether or not the select menu is required to be answered in a modal |
| disabled?    | boolean                                | Whether or not the select menu is disabled                           |

#### Select menu types
Valid select menu types are:
- `string` or `text`
- `user`
- `role`
- `channel`
- `mention` or `mentionable`

#### Valid children
- `user` when type is `user` or `mentionable`
- `role` when type is `role` or `mentionable`
- `channel` when type is `channel` or `mentionable`
- [Select Menu Option](#select-menu-option) when type is `string`

for children of `user`, `role`, and `channel`, the element should contain the ID/entity as the child, ex: `<user>{userId}</user>`

```html
<select-menu
    type="string"
    customId="menu-1"
    placeholder="Select an option..."
    max={1}
    min={1}
    required
>
    <select-menu-option
        label="Choice 1"
        value="value-1"
    />
    <select-menu-option
        label="Choice 2"
        value="value-2"
        description="The better choice"   
    />
</select-menu>

<select-menu
    type="user"
    customId="menu-2"
    placeholder="Select a user"
    required
>
    <user>{userId1}</user>
    <user>{user2}</user>
</select-menu>
```

### Select Menu Option

A Select Menu Option represents a choice within a [Select Menu](#select-menu)

#### Aliases
- `option`

#### Attributes

| Name         | Type                     | Description                                       |
| ------------ | ------------------------ | ------------------------------------------------- |
| label        | string                   | The label of the option                           |
| value        | string                   | The developer-defined value of the option         |
| description? | string                   | Additional description of the option              |
| emoji?       | string \| Discord.IEmote | An emoji for the option                           |
| default?     | boolean                  | Whether the option is shown as the default option |

```html
<select-menu-option
    label="My Option"
    value="option1"
    description="This is option 1"
    emoji={myEmote}
    default={false}
/>
```

### Separator

A Separator adds vertical padding and visual division between components

#### Attributes
| Name     | Type                                    | Description                                 |
| -------- | --------------------------------------- | ------------------------------------------- |
| id?      | int                                     | The optional identifier for the separator   |
| divider? | boolean                                 | Whether the separator has a visible divider |
| spacing? | string  \| Discord.SeparatorSpacingSize | The size of the separator                   |

```html
<separator spacing="large" divider/>
```

### Text Display

A Text Display renders content as markdown

### Aliases
- `text`

#### Attributes

| Name    | Type               | Description                                 |
| ------- | ------------------ | ------------------------------------------- |
| id?     | int                | The optional identifier of the text display |
| content | string \| children | The text to display                         |

```html
<text-display>
    # Hello, World!
</text-display>
```

### Text Input

A Text Input allows users to enter free-form text in a modal

#### Aliases
- `input`

#### Attributes
| Name         | Type                             | Description                                   |
| ------------ | -------------------------------- | --------------------------------------------- |
| id?          | int                              | The optional identifier of the text input     |
| customId     | string                           | The custom id of the text input               |
| style        | string \| Discord.TextInputStyle | The style of the text input                   |
| min?         | int                              | The minimum input text length in characters   |
| max?         | int                              | The maximum input text length in characters   |
| required?    | boolean                          | Whether the text input is required in a modal |
| value?       | string                           | The prefilled value of the input              |
| placeholder? | string                           | A placeholder to show if the input is empty   |

```html
<text-input
    customId="input-1"
    style="short"
    min={2}
    max={36}
    required
    placeholder="This is a placeholder"
/>
```

### Thumbnail

A Thumbnail displays media in a compact form-factor.

#### Attributes
| Name         | Type    | Description                              |
| ------------ | ------- | ---------------------------------------- |
| id?          | int     | The optional identifier of the thumbnail |
| url          | string  | The URL of the thumbnail                 |
| description? | string  | The description of the thumbnail         |
| spoiler?     | boolean | Whether the thumbnail is a spoiler |

```html
<thumbnail url="attachment://image.png" spoiler />
```

## Text Formatting Elements

You can use html-like text formatting elements to render as markdown within a [text-display](#text-display) component

```html
<text>
    <h1>Hello World!</h1>
    This renders to <b>markdown!</b>
</text>
```

| Tag                            | Attributes                                                                                                                  | Preview                                                  |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `b` `bold` `strong`            | N/A                                                                                                                         | **bold**                                                 |
| `i` `italic` `italics` `em`    | N/A                                                                                                                         | *italics*                                                |
| `u` `mark` `underline` `ins`   | N/A                                                                                                                         | <ins>underline</ins>                                     |
| `del` `strike` `strikethrough` | N/A                                                                                                                         | <del>strikethrough</del>                                 |
| `sub` `subtext` `small`        | N/A                                                                                                                         | <sub>subtext</sub>                                       |
| `a` `link`                     | `href` or `url`                                                                                                             | [click me](https://www.example.com/)                     |
| `ul` `list`                    | N/A                                                                                                                         | - first item<br/>- second item                           |
| `ol`                           | N/A                                                                                                                         | 1. first item<br/>  2. second item                       |
| `li`                           | N/A                                                                                                                         | used in `ul` or `ol` to denote a list item               |
| `c` `code` `block` `codeblock` | `inline`: whether to force as an inline codeblock<br/>`lang` `language`: the syntax highlighting language for the codeblock | `inline`<br/><pre><code>This is a<br/>block</code></pre> |
| `q` `quote` `blockquote`       | N/A                                                                                                                         | <blockquote>quote</blockquote>                           |
| `spoiler` `obfuscated`         | N/A                                                                                                                         | \|\|spoiler\|\|                                          |
| `br` `break`                   | N/A                                                                                                                         | line<br/>break                                           |
| `h1`                           | N/A                                                                                                                         | <h1>heading 1</h1>                                       |
| `h2`                           | N/A                                                                                                                         | <h2>heading 2</h2>                                       |
| `h3`                           | N/A                                                                                                                         | <h3>heading 3</h3>                                       |

## Auto Text Display

By enabling the [generator option](#generator-options) `EnableAutoTextDisplay`, text, non-component interpolations, and [text formatting elements](#text-formatting-elements) will automatically be placed in a [text-display](#text-display) component:

```html
<container>
    This is some <b>cool</b> text!
</container>
```

## Auto Rows

By enabling the [generator option](#generator-options) `EnableAutoRows`, [buttons](#button) and [select menus](#select-menu) will automatically be placed in an [action row](#action-row):

```html
<container>
    <button customId="foo" label="bar" />
</container>
```


## String Interpolation

You can use string interpolations to inject values almost anywhere

```cs
using static Discord.ComponentDesigner;

var buttonId = "my-custom-id";
var label = "my label";

var button = cx(
   $"""
    <button customId={buttonId}>
        {label}
    </button>
    """
);
```

> [!NOTE]
> You can't use string interpolations for element identifiers

Most components have constraints on what type of value is allowed (ex select.min should be an integer) and diagnostics will be reported on type mismatches

```tsx
<select
  min={1} // OK
  max={"this isn't a number"} // Error: 'String' is not of expected type 'int'
/>
```

> [!NOTE]
> non-constant values passed into interpolation arguments usually have a runtime fallback (ex `int.Parse`). In the future, you'll be able to enable/disable theses fallbacks and treat them as compile errors.

## Interpolated Components

Using string interpolation, you can also supply components as children:

```cs
using static Discord.ComponentDesigner;

var myComponent = cx("<text>Hello</text>");

var myOtherComponent = cx(
   $"""
    <container>
        {myComponent}
        <separator />
        <text>World</text>
    </container>
    """
);
```

Valid types of interpolated components are:

- `Discord.IMessageComponentBuilder`
- `Discord.IMessageComponent`
- `Discord.MessageComponent`
- `Discord.CXMessageComponent`
- Any collection type implementing `IEnumerable<T>` of the previous types.

> [!NOTE]
> By using interpolated components, some compile-time checks can't be done, these include but are not limited to:
>
> - child length checks (ex action row children)
> - child component type (ex text in action row)

## Compile-time Diagnostics

The generator for the Component Designer comes with compile-time diagnostics for validating most constraints of components, example diagnostics include:

- label/description lengths
- correct types of nested components
- property type validation
- required properties

These are provided as roslyn analyzer diagnostics and can be enabled/disabled using that flow.

```jsx
<row>
    <text>Hello!</text> // Error: 'row' cannot contain 'text' components
    <button /> // Error: A button must specify either a 'customId' or 'url'
    <button customId="abc" /> // Error: A button must have a 'label' or 'emoji' 
</row>
```

## Generator Options

You can control some of the generators behaviour with the following generator options
| Name                  | Type              | Description                                                                                                                  |
| --------------------- | ----------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| EnableAutoRows        | `true` or `false` | Whether to allow the [automatic creation](#auto-rows) of [action rows](#action-row)                                          |
| EnableAutoTextDisplay | `true` or `false` | Whether to [automatically wrap](#auto-text-display) text/interpolation/text controls in a [text display](#text-display) node |


## Reusable Custom Components

You can define basic custom components which can be used within the CX syntax. As of now, there is only one type way of writing a custom component, and that is a functional component. In future versions there are plans for more component types and ways of writing components.

### Functional components

A functional component is a function that is:
- `static`.
- either `public` or `internal`.
- returns a valid interpolation component type.

Functional components can have zero to many arguments, with limited support for compile-time validation against them. Some primitive argument types (string, int, emoji, etc) can make use of the CX syntax, but any complex or non-standard argument types must be supplied using [string interpolation](#string-interpolation)

```cs
using static Discord.ComponentDesigner;

var user = new User(name: "Example");

var component = cx(
   $"""
    <container>
        <SayHello user={user} />
    </container>
    """
)

public static CXMessageComponent SayHello(User)
    => cx(
       $"""
        <text>
            Hello, {user.Name}!
        </text>
        """
    );
```

> [!TIP]
> You can use default values for parameters to indicate an optional property.

#### Functional components with children

It is possible to take in children of a component using the `CXChildren` attribute, children are treated the same way that parameters as props are handles, but with a few key differences:
- Built-in conversion for text -> type (colors, ints, etc) is not supported.
- Children types that are not a string or valid component type are only allowed to be supplied by interpolation.

You can specify the cardinality of the number of children you wish to support by using a collection type, any non-collection type will only support a single child.

```cs
using static Discord.ComponentDesigner;

var user = new User(Name: "Example", Avatar: null);

var component = cx(
   $"""
    <container>
        <MaybeIcon url={user.Avatar}>
            <text>
                Hello, {user.Name}
            </text>
        </MaybeIcon>
    </container>
    """
);

public static CXMessageComponent MaybeIcon(
    string? url,
    [CXChildren] CXMessageComponent children
) {
    if (url is null) return children;

    return cx(
       $"""
        <section>
            {children}
            <accessory>
                <thumbnail url={url} />
            </accessory>
        </section>
        """
    );
}
```

### Component cardinality

Some accepted component types have default cardinality, such as `CXMessageComponent` allowing more than one component. This can restrict and effect custom components with children, specifying which component type you accept as children controls how many child components can be supplied to your custom component.

| Type                     | Cardinality |
|--------------------------|-------------|
| CXMessageComponent       | Many        |
| MessageComponent         | Many        |
| IMessageComponent        | One         |
| IMessageComponentBuilder | One         |

Wrapping any of these types in a collection type gives it a cardinality of many, as well as ensuring that the collection represents the actual children supplied to your component.

> [!NOTE]
> Splatting is automatically handled by the generator, an interpolation collection of components is unwrapped and supplied to your child type.

