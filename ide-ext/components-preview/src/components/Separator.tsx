import classNames from "classnames";
import { SeparatorSpacingSize, type APISeparatorComponent } from "discord-api-types/v10";

export default function Separator(
    component: APISeparatorComponent
) {


    return (
        <div className={classNames(
            (component.divider !== false) && "separator-divider",
            component.spacing === SeparatorSpacingSize.Large && "separator-spacing-large",
            component.spacing === SeparatorSpacingSize.Small && "separator-spacing-small",
        )}>
        </div>
    )
}