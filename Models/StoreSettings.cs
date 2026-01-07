namespace PosPokemon.App.Models;

/// <summary>
/// Configuración de la tienda para tickets
/// </summary>
public sealed class StoreSettings
{
    public string Name { get; set; } = "POS POKÉMON TCG";
    public string Address { get; set; } = "Lima, Perú";
    public string Phone { get; set; } = "";
    public string Ruc { get; set; } = "";
    public string LogoPath { get; set; } = "";

    /// <summary>
    /// Valida si el logo existe y es accesible
    /// </summary>
    public bool HasValidLogo()
    {
        return !string.IsNullOrWhiteSpace(LogoPath)
               && System.IO.File.Exists(LogoPath);
    }
}