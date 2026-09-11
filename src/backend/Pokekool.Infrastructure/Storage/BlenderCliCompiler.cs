using System.Diagnostics;
using Microsoft.Extensions.Hosting;
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
    private readonly IHostEnvironment _environment;
    private readonly ILogger<BlenderCliCompiler> _logger;

    public BlenderCliCompiler(IOptions<BlenderOptions> options, IHostEnvironment environment, ILogger<BlenderCliCompiler> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<BlenderCompileResult> CompileAsync(string blendPath, string outputGlbPath, CancellationToken ct)
    {
        // Résolu par rapport au ContentRoot de l'app (stable), pas au cwd du process
        // (Environment.CurrentDirectory varie selon IDE/service/conteneur — cf. incident chemin Blender).
        var scriptPath = Path.IsPathRooted(_options.ScriptPath)
            ? _options.ScriptPath
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.ScriptPath));
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
            var stdout = await stdoutTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "Blender a échoué (exit {Code}).\n--- STDOUT ---\n{Out}\n--- STDERR ---\n{Err}",
                    process.ExitCode, stdout, stderr);

                // Blender écrit souvent les erreurs Python sur stdout : on remonte le détail le plus utile.
                var detail = !string.IsNullOrWhiteSpace(stderr) ? stderr
                    : !string.IsNullOrWhiteSpace(stdout) ? stdout
                    : $"Blender exit code {process.ExitCode}";
                return BlenderCompileResult.Failure(Tail(detail.Trim(), 1500));
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

    /// <summary>Ne conserve que les <paramref name="max"/> derniers caractères (les erreurs Blender utiles sont en fin de log).</summary>
    private static string Tail(string text, int max)
        => text.Length <= max ? text : "…" + text[^max..];

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
