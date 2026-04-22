
import type { APIActionRowComponent, APIComponentInMessageActionRow } from "discord-api-types/v10";
import DiscordComponent from "./DiscordComponent";

export default function ActionRow(
  component: APIActionRowComponent<APIComponentInMessageActionRow>,
) {
  return (
    <div className="action-row">
      {component.components.map((c) => (
        <DiscordComponent {...c} />
      ))}
    </div>
  );
}
