namespace ComponentDesigner;

public static class ResultExtensions
{
    extension(Result<RenderedComponent> result)
    {
        public Result<string> AsSource => result.Map(x => x.Source);
    }
}