using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pokekool.Application.Storage;
using Pokekool.Infrastructure.Options;

namespace Pokekool.Infrastructure.Storage;

/// <summary>
/// Compile un .blend en .glb en invoquant Blender headless via CLI.
/// Le .blend est chargé par Blender (<c>--background "{blendPath}"</c>) avant l'exécution
/// du script Python (<c>--python "{ScriptPath}"</c>), qui exporte le .glb vers le chemin passé après <c>--</c>.
/// </summary>
internal sealed class BlenderCliCompiler : IBlenderCompiler
{
    private readonly BlenderOptions _options;
    private readonly ILogger<BlenderCliCompiler> _logger;

    public BlenderCliCompiler(IOptions<BlenderOptions> options, ILogger<BlenderCliCompiler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BlenderCompileResult> CompileAsync(string blendPath, string outputGlbPath, CancellationToken ct)
    {
        var scriptPath = Path.GetFullPath(_options.ScriptPath);
        if (!File.Exists(scriptPath))
            return BlenderCompileResult.Failure($"Script Blender introuvable : {scriptPath}");

        var psi = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            Arguments = $"--background \"{blendPath}\" --python \"{scriptPath}\" -- \"{outputGlbPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Timeout : kill du process si dépassé.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        Process? process = null;
        try
        {
            process = Process.Start(psi)
                ?? throw new InvalidOperationException("Impossible de démarrer le process Blender.");

            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token);

            var stderr = await stderrTask;
            _ = await stdoutTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("Blender a échoué (exit {Code}) : {Err}", process.ExitCode, stderr);
                return BlenderCompileResult.Failure(
                    string.IsNullOrWhiteSpace(stderr) ? $"Blender exit code {process.ExitCode}" : stderr);
            }

            if (!File.Exists(outputGlbPath))
                return BlenderCompileResult.Failure("Blender terminé sans produire le fichier .glb attendu.");

            return BlenderCompileResult.Success(outputGlbPath);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            TryKill(process);
            return BlenderCompileResult.Failure($"Compilation Blender interrompue (timeout {_options.TimeoutSeconds}s).");
        }
        catch (Exception ex)
        {
            TryKill(process);
            _logger.LogError(ex, "Erreur lors de l'invocation de Blender.");
            return BlenderCompileResult.Failure(ex.Message);
        }
    }

    private static void TryKill(Process? process)
    {
        try
        {
            if (process is { HasExited: false }) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best-effort
        }
    }
}
