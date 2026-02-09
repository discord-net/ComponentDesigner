namespace ComponentDesigner.Nodes;

public enum SearchResultKind
{
    Ok,

    NotAccessible,
    DoesntMatchStaticContext,
    DoesntReturnAComponent,
    NotAMethod
}