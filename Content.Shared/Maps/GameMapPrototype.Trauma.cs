namespace Content.Shared.Maps;

/// <summary>
/// Trauma - store lavaland planets for each map
/// </summary>
public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Contains info about planets that we have to spawn assigned from this game map.
    /// Not protoid because its in lavaland.shared
    /// </summary>
    [DataField]
    public List<string> Planets = new() { "Lavaland" };
}
