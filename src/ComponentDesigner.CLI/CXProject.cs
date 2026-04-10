using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ComponentDesigner;

public sealed class CXProject
{
    private readonly MSBuildWorkspace _workspace;
    private readonly Project _project;
    private readonly CSharpCompilation _compilation;

    public CXProject(
        MSBuildWorkspace workspace,
        Project project,
        CSharpCompilation compilation
    )
    {
        _workspace = workspace;
        _project = project;
        _compilation = compilation;
    }

    public static async Task<CXProject?> TryLoadAsync(
        string csprojFile,
        CancellationToken cancellationToken,
        IProgress<ProjectLoadProgress>? progress = null,
        ILogger? logger = null
    )
    {
        var workspace = MSBuildWorkspace.Create();

        var project = await workspace.OpenProjectAsync(csprojFile, logger, progress, cancellationToken);

        var compilation = await project.GetCompilationAsync(cancellationToken);

        if (compilation is not CSharpCompilation cSharpCompilation) return null;

        return new(workspace, project, cSharpCompilation);
    }
}