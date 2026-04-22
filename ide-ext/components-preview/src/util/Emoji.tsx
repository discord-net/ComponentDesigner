import type { APIMessageComponentEmoji } from "discord-api-types/v10";
import type { JSX } from "react";

const unicodeEmoji = (unicode: string) => {
    return (
        <span className="emoji">{unicode}</span>
    )
}

const customEmoji = (id: string, name: string, animated: boolean) => {
    return <div></div>
};

export default function Emoji(
    emoji: APIMessageComponentEmoji
): JSX.Element {
    if(emoji.id === undefined && emoji.name) {
        return unicodeEmoji(emoji.name);
    }

    return customEmoji(emoji.id!, emoji.name!, emoji.animated ?? false); 
}