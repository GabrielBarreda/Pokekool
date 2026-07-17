namespace Pokekool.Application.Storage.Settings;

/// <summary>Clés (et valeurs par défaut) des paramètres du module Stockage, stockés en AppSetting.</summary>
public static class StorageSettingsKeys
{
    /// <summary>Suppression en chaîne (cascade) activée. Défaut : true (comportement historique).</summary>
    public const string CascadeDelete = "storage.cascade_delete";
    public const bool CascadeDeleteDefault = true;
}
