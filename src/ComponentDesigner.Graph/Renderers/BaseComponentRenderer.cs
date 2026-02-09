// using Discord.CX.Nodes;
//
// namespace Discord.CX;
//
// public abstract class BaseComponentRenderer : IComponentRenderer
// {
//     public abstract string Name { get; }
//     
//     public Result<string> Render(
//         IRendererContext context,
//         IComponentNode component,
//         ComponentState state,
//         CancellationToken token = default
//     )
//     {
//         switch (component)
//         {
//             case ContainerComponentNode container:
//                 return Container(
//                     context,
//                     container,
//                     state,
//                     state.GetPropertyValue(container.Id),
//                     state.GetPropertyValue(container.AccentColor),
//                     state.GetPropertyValue(container.IsSpoiler),
//                     token
//                 );
//             default:
//                 return state.TextSpan.Report(
//                     Diagnostic.MissingImplementationForRenderer(component, this)
//                 );
//                 
//         }
//     }
//
//     protected abstract Result<string> Container(
//         IRendererContext context,
//         ContainerComponentNode component,
//         ComponentState state,
//         ComponentPropertyValue id,
//         ComponentPropertyValue accentColor,
//         ComponentPropertyValue isSpoiler,
//         CancellationToken token = default
//     );
// }