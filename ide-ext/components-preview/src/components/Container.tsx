import type { APIContainerComponent } from "discord-api-types/v10";
import type { CSSProperties } from "react";
import DiscordComponent from "./DiscordComponent";

export default function Container(
    component: APIContainerComponent
) {
    const accentColor =  component.accent_color && `#${component.accent_color.toString(16).padStart(6, "0")}`;

    return (
        <div className="container" style={
            {"--__accentColor": accentColor} as CSSProperties
        }>
            {component.components.map(c => 
                <DiscordComponent {...c} />
            )}
        </div>
    )
}