import type { APIMessageComponent } from "discord-api-types/v9";
import DiscordComponent from "./components/DiscordComponent";
import './message.scss';

const defaultAuthorIcon = 'https://i.pinimg.com/236x/c8/ff/bd/c8ffbd58e4f1f218ee72c23bfddfc8a6.jpg';
const defaultAuthorName = "CX Preview";

export interface MessageProps {
    components: APIMessageComponent[]
    authorIcon?: string;
    authorName?: string;
}

export default function Message(
    {components, authorIcon, authorName}: MessageProps
) {


    return (
        <div className="message">
            <img className="message-author-icon" src={authorIcon ?? defaultAuthorIcon}/>
            <h3 className="message-author-name">{authorName ?? defaultAuthorName}</h3>
            <div className="message-content">
                {components.map(c => 
                    <DiscordComponent {...c} />
                )}
            </div>
        </div>
    )
}