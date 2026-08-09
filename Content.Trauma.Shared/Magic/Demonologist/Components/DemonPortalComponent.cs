namespace Content.Trauma.Shared.Magic.Demonologist.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DemonPortalComponent : Component
{
    /// <summary>
    /// The Demonologist that summoned this portal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Summoner;

    /// <summary>
    /// The time between each demon summoned by the portal.
    /// </summary>
    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The next time this portal can summon a demon.
    /// </summary>
    [AutoPausedField]
    public TimeSpan NextSpawnTime;

    /// <summary>
    /// The demon prototypes that can be summoned by this portal.
    /// </summary>
    [DataField]
    public List<EntProtoId> Demons = new();

    /// <summary>
    /// The maximum number of demons this portal can summon.
    /// </summary>
    [DataField]
    public int MaxDemons = 5;

    /// <summary>
    /// The number of demons this portal has summoned.
    /// </summary>
    public int DemonsSpawned;
}
