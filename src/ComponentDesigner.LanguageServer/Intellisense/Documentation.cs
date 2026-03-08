using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner.LanguageServer;

public static class Documentation
{
    public static string? GetDescriptionOfProperty(
        IComponentNode node,
        ComponentProperty property,
        ComponentState? state = null
    ) => node switch
    {
        ButtonComponentNode => property.Name switch
        {
            "id" => "Optional identifier for component",
            "style" => "A [button style](https://docs.discord.com/developers/components/reference#button)",
            "label" => "Text that appears on the button; max 80 characters",
            "emoji" => "A partial [emoji](https://docs.discord.com/developers/resources/emoji#emoji-object)",
            "customId" => "Developer-defined identifier for the button; 1-100 characters",
            "skuId" =>
                "Identifier for a purchasable [SKU](https://docs.discord.com/developers/resources/sku#sku-object), only available when using premium-style buttons",
            "url" => "URL for link-style buttons; max 512 characters",
            "disabled" => "Whether the button is disabled (defaults to `false`)",
            _ => null,
        },
        not IDynamicComponentNode => property.Name switch
        {
            "id" => "Optional identifier for component",
            _ => null
        },
        _ => null
    };

    public static string? GetDescriptionOfComponent(IComponentNode node, ComponentState? state = null)
        => node switch
        {
            ButtonComponentNode =>
                """
                A Button is an interactive component that can only be used in messages. It creates clickable elements that users can interact with, sending an interaction to your app when clicked.

                Buttons must be placed inside an Action Row or a Section’s accessory field.
                """,
            ActionRowComponentNode =>
                """
                An Action Row is a top-level layout component.

                Action Rows can contain one of the following:
                 - Up to 5 contextually grouped buttons
                 - A single select component (string select, user select, role select, mentionable select, or channel select)
                """,
            ContainerComponentNode =>
                """
                A Container is a top-level layout component. Containers offer the ability to visually encapsulate a collection of components and have an optional customizable accent color bar.

                Containers are currently only available in messages.
                """,
            FileComponentNode =>
                """
                A File is a top-level content component that allows you to display an uploaded file as an attachment to the message and reference it in the component. Each file component can only display 1 attached file, but you can upload multiple files and add them to different file components within your payload.

                Files are currently only available in messages.
                """,
            FileUploadComponentNode =>
                """
                File Upload is an interactive component that allows users to upload files in modals. File Uploads can be configured to have a minimum and maximum number of files between 0 and 10, along with `required` for if the upload is required to submit the modal. The max file size a user can upload is based on the user’s upload limit in that channel.

                File Uploads are available on modals. They must be placed inside a Label.
                """,
            LabelComponentNode =>
                """
                A Label is a top-level layout component. Labels wrap modal components with text as a label and optional description.
                """,
            MediaGalleryComponentNode =>
                """
                A Media Gallery is a top-level content component that allows you to display 1-10 media attachments in an organized gallery format. Each item can have optional descriptions and can be marked as spoilers.

                Media Galleries are currently only available in messages.
                """,
            SectionComponentNode =>
                """
                A Section is a top-level layout component that allows you to contextually associate content with an accessory component. The typical use-case is to contextually associate text content with an accessory.

                Sections are currently only available in messages.
                """,
            SelectMenuComponentNode when state is SelectMenuState { Kind: SelectMenuKind.Channel } =>
                """
                A Channel Select is an interactive component that allows users to select one or more channels in a message or modal. Options are automatically populated based on available channels in the server and can be filtered by channel types.

                Channel Selects can be configured for both single-select and multi-select behavior. When a user finishes making their choice(s) your app receives an interaction.

                Channel Selects are available in messages and modals. They must be placed inside an Action Row in messages and a Label in modals.
                """,
            SelectMenuComponentNode when state is SelectMenuState { Kind: SelectMenuKind.Mentionable } =>
                """
                A Mentionable Select is an interactive component that allows users to select one or more mentionables in a message or modal. Options are automatically populated based on available mentionables in the server.

                Mentionable Selects can be configured for both single-select and multi-select behavior. When a user finishes making their choice(s), your app receives an interaction.

                Mentionable Selects are available in messages and modals. They must be placed inside an Action Row in messages and a Label in modals.
                """,

            SelectMenuComponentNode when state is SelectMenuState { Kind: SelectMenuKind.Role } =>
                """
                A Role Select is an interactive component that allows users to select one or more roles in a message or modal. Options are automatically populated based on the server’s available roles.

                Role Selects can be configured for both single-select and multi-select behavior. When a user finishes making their choice(s) your app receives an interaction.

                Role Selects are available in messages and modals. They must be placed inside an Action Row in messages and a Label in modals.
                """,

            SelectMenuComponentNode when state is SelectMenuState { Kind: SelectMenuKind.String } =>
                """
                A String Select is an interactive component that allows users to select one or more provided `options`.

                String Selects can be configured for both single-select and multi-select behavior. When a user finishes making their choice(s) your app receives an interaction.

                String Selects are available in messages and modals. They must be placed inside an Action Row in messages and a Label in modals.
                """,

            SelectMenuComponentNode when state is SelectMenuState { Kind: SelectMenuKind.User } =>
                """
                A User Select is an interactive component that allows users to select one or more users in a message or modal. Options are automatically populated based on the server’s available users.

                User Selects can be configured for both single-select and multi-select behavior. When a user finishes making their choice(s) your app receives an interaction.

                User Selects are available in messages and modals. They must be placed inside an Action Row in messages and a Label in modals.
                """,
            SeparatorComponentNode =>
                """
                A Separator is a top-level layout component that adds vertical padding and visual division between other components.

                Separators are currently only available in messages.
                """,
            TextInputComponentNode =>
                """
                Text Input is an interactive component that allows users to enter free-form text responses in modals. It supports both short, single-line inputs and longer, multi-line paragraph inputs.

                Text Inputs can only be used within modals and must be placed inside a Label.
                """,
            ThumbnailComponentNode =>
                """
                A Thumbnail is a content component that displays visual media in a small form-factor. It is intended as an accessory for to other content, and is primarily usable with sections. The media displayed is defined by the unfurled media item structure, which supports both uploaded media and externally hosted media.

                Thumbnails are currently only available in messages as an accessory in a section.

                Thumbnails currently only support images, including animated formats like GIF and WEBP. Videos are not supported at this time.
                """,
            TextDisplayComponentNode =>
                """
                A Text Display is a content component that allows you to add markdown formatted text, including mentions (users, roles, etc) and emojis. The behavior of this component is extremely similar to the content field of a message, but allows you to add multiple text components, controlling the layout of your message.

                When sent in a message, pingable mentions (@user, @role, etc) present in this component will ping and send notifications based on the value of the allowed mention object set in message.allowed_mentions.
                """,
            _ => null
        };
}