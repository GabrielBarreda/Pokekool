namespace Pokekool.Infrastructure.Options;

/// <summary>Configuration du pipeline de compilation Blender (.blend → .glb).</summary>
public sealed class BlenderOptions
{
    public const string SectionName = "Blender";

    /// <summary>Exécutable Blender (dans le PATH par défaut). Blender headless requis sur l'hôte/worker.</summary>
    public string ExecutablePath { get; set; } = "blender";

    /// <summary>Script Python companion exécuté par Blender pour exporter le .glb.</summary>
    public string ScriptPath { get; set; } = "scripts/blender/export_glb.py";

    /// <summary>Timeout de compilation (kill du process si dépassé).</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Dossier de dépôt des .blend uploadés.</summary>
    public string UploadDirectory { get; set; } = "App_Data/blender-uploads";

    /// <summary>Dossier de sortie des .glb (servi statiquement depuis wwwroot).</summary>
    public string OutputDirectory { get; set; } = "wwwroot/models";

    /// <summary>Préfixe d'URL publique correspondant au dossier de sortie (sous wwwroot).</summary>
    public string PublicUrlPrefix { get; set; } = "/models";
}
