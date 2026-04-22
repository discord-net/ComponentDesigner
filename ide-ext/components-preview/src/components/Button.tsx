import classNames from "classnames";
import { ButtonStyle, type APIButtonComponent, type APIButtonComponentWithCustomId } from "discord-api-types/v10";
import Emoji from "../util/Emoji";

export default function Button(component: APIButtonComponent) {
  const style = component.style ?? ButtonStyle.Primary;

  const buttonClassNames = classNames(
    "button",
    style === ButtonStyle.Primary && "button-primary",
    style === ButtonStyle.Secondary && "button-secondary",
    style === ButtonStyle.Success && "button-success",
    style === ButtonStyle.Danger && "button-danger",
    style === ButtonStyle.Link && "button-link",
  );

  const label = (component as any).label;
  const emoji = (component as APIButtonComponentWithCustomId).emoji && <Emoji {...(component as APIButtonComponentWithCustomId).emoji}/>;;

  return (
    <div className={buttonClassNames}>
      {emoji}
      {label}
    </div>
  );
}
