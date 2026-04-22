import type { APITextDisplayComponent } from "discord-api-types/v10";
import Markdown from "../util/Markdown";

export default function TextDisplay(component: APITextDisplayComponent) {
  return (
    <div className="text-display">
      <Markdown text={component.content ?? ""} />
    </div>
  );
}
