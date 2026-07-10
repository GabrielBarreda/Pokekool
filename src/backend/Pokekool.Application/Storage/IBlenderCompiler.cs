namespace Pokekool.Application.Storage;

/// <summary>Résultat d'une compilation Blender (.blend → .glb).</summary>
public sealed record BlenderCompileResult(bool IsSuccess, string? OutputGlbPath, string? Error)
{
    public static BlenderCompileResult Success(string outputGlbPath) => new(true, outputGlbPath, null);
    public static BlenderCompileResult Failure(string error) => new(false, null, error);
}

/// <summary>
/// Compile un projet Blender source en modèle 3D (.glb) exploitable par three.js.
/// Implémentation par défaut : Blender headless via CLI (<c>BlenderCliCompiler</c>).
/// </summary>
public interface IBlenderCompiler
{
    Task<BlenderCompileResult> CompileAsync(string blendPath, string outputGlbPath, CancellationToken ct);
}
