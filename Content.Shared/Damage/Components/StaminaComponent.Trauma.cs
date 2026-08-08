namespace Content.Shared.Damage.Components;

public sealed partial class StaminaComponent
{
    /// <summary>
    /// A dictionary of active stamina drains, with the key being the source of the drain,
    /// DrainRate how much it changes per tick, and ModifiesSpeed if it should slow down the user.
    /// </summary>
    /// <remarks>
    /// TODO: Refactor into a struct in another component at some point
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Dictionary<string, (float DrainRate, bool ModifiesSpeed, NetEntity? Source, bool ApplyResistances)> ActiveDrains = new();

    [DataField]
    public float StaminaOnShove = 7.5f;
}
