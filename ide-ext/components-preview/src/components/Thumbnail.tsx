import type { APIThumbnailComponent } from "discord-api-types/v10";

export default function Thumbnail(
    component: APIThumbnailComponent
) {
    return (
        <div className="thumbnail-wrapper">
            <img className="thumbnail" src={component.media.url} />
        </div>
    )
}