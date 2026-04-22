import { ComponentType, type APIMessageComponent } from "discord-api-types/v10"
import type { JSX } from "react"
import Container from "./Container"
import ActionRow from "./ActionRow"
import Button from "./Button"
import TextDisplay from "./TextDisplay"
import Separator from "./Separator"
import Section from "./Section"
import Thumbnail from "./Thumbnail"

const unknownComponent = (component: APIMessageComponent) => (
    <div>Unknown component {component.type}</div>
)


export default function DiscordComponent(
    component: APIMessageComponent
): JSX.Element {
    if(component.type === ComponentType.Button) {
        return <Button {...component}/>
    }

    if(component.type === ComponentType.ActionRow) {
        return <ActionRow {...component}/>
    }

    if(component.type === ComponentType.Container) {
        return <Container {...component} />
    }

    if(component.type === ComponentType.TextDisplay) {
        return <TextDisplay {...component} />
    }

    if(component.type === ComponentType.Separator) {
        return <Separator {...component} />
    }

    if(component.type === ComponentType.Section) {
        return <Section {...component} />
    }

    if(component.type === ComponentType.Thumbnail) {
        return <Thumbnail {...component} />
    }

    return unknownComponent(component)
}