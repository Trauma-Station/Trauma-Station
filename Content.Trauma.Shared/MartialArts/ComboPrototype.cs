using Content.Shared.EntityEffects;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Events;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

[Prototype]
public sealed partial class ComboPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    /// <summary>
    /// Events that should happen on user that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Events that should happen on the opponent that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? OpponentEffects;

    /// <summary>
    /// Events that should happen on both users that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? CombinedEffects;

    /// <summary>
    /// Level required to perform?
    /// </summary>
    [DataField]
    public int LevelRequired = 0;

    /// <summary>
    /// Level required to perform?
    /// </summary>
    [DataField]
    public int LevelExceeded = -1;

    /// <summary>
    /// How much extra damage should this move do on perform?
    /// </summary>
    [DataField]
    public float ExtraDamage;

    /// <summary>
    /// Stun time.
    /// </summary>
    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.Zero;

    /// <summary>
    /// Can a lying person perform this combo
    /// </summary>
    [DataField]
    public bool CanDoWhileProne = true;

    /// <summary>
    /// Should the target drop items on knockdown?
    /// </summary>
    [DataField]
    public bool DropItems = true;

    /// <summary>
    /// How much stamina damage should this move do on perform.
    /// </summary>
    [DataField]
    public float StaminaDamage;

    /// <summary>
    /// Blunt, Slash, etc.
    /// </summary>
    [DataField]
    public string DamageType = "Blunt";

    /// <summary>
    /// How fast people are thrown on combo
    /// </summary>
    [DataField]
    public float ThrownSpeed = 7f;

    /// <summary>
    /// Name of the move
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Is this combo performed on self only or only on other targets
    /// </summary>
    [DataField]
    public bool PerformOnSelf;
}

[Prototype]
public sealed partial class ComboListPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<ComboPrototype>> Combos = new();
}
