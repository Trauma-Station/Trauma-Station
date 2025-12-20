using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolyFlammableComponent : Component
{
    [DataField]
    public bool Resisting;

    [DataField]
    public bool OnFire;

    [DataField]
    public float FireStacks;

    [DataField]
    public float FireStacksDropoff = 10f;

    [DataField]
    public float MaximumFireStacks = 50f;

    [DataField]
    public float MinimumFireStacks = -10f;

    [DataField]
    public string FlammableFixtureID = "flammable";

    [DataField]
    public DamageSpecifier Damage = new DamageSpecifier()
    {
        DamageDict =
        {
            ["Holy"] = 0.5
        }
    };

    /// <summary>
    ///     Should the component be set on fire by interactions with isHot entities
    /// </summary>
    [DataField]
    public bool AlwaysCombustible = false;

    /// <summary>
    ///     Can the component anyhow lose its FireStacks?
    /// </summary>
    [DataField]
    public bool CanExtinguish = true;

    /// <summary>
    ///     How many firestacks should be applied to component when being set on fire?
    /// </summary>
    [DataField]
    public float FirestacksOnIgnite = 2.0f;

    /// <summary>
    /// Determines how quickly the object will fade out. With positive values, the object will flare up instead of going out.
    /// </summary>
    [DataField]
    public float FirestackFade = -1f;

    [DataField]
    public ProtoId<AlertPrototype> FireAlert = "HolyFire";

    /// <summary>
    /// Represents the time interval, in seconds, at which updates occur.
    /// </summary>
    [DataField]
    public float UpdateTime = 1f;

    /// <summary>
    /// Contains the timer for the next update tick.
    /// </summary>
    [DataField]
    public float Timer;

    /// <summary>
    /// Contains the rest time left for the next update tick.
    /// </summary>
    [DataField]
    public float ResistTimer;
}
