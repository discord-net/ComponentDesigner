import type { APISectionComponent } from "discord-api-types/v10";
import DiscordComponent from "./DiscordComponent";

export default function Section(
    component: APISectionComponent
) {
    return (
        <div className="section">
            <div className="section-children">
                {component.components.map(c => 
                    <DiscordComponent {...c} />
                )}
            </div>
            <div className="section-accessory">
                <DiscordComponent {...component.accessory} />
            </div>
        </div>
    )
}