// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Durability.Components;

/// <summary>
/// Allows the entity to be damaged and repaired with items or specific interactions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DurabilityComponent : Component
{
    /// <summary>
    /// The current <see cref="DurabilityState"/> of the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DurabilityState DurabilityState = DurabilityState.Pristine;

    /// <summary>
    /// Thresholds for when to change state. Entities will attempt to break when reaching <see cref="DurabilityState.Broken"/>.
    /// </summary>
    [DataField]
    public SortedDictionary<FixedPoint2, DurabilityState> DurabilityThresholds = [];

    /// <summary>
    /// The total amount of damage this entity has sustained.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Damage = 0;

    /// <summary>
    /// The probability that this entity will take damage, between 0 and 1.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageProbability = 0.35f;

    /// <summary>
    /// Sorted by durability state so you can have 2-3 popups that can show when initially damaging weapon, some that
    /// it shows when taking damage and is already damaged, as well as some that show when it breaks. All entries
    /// in the LocId HashSets have an equal chance of being selected.
    /// </summary>
    [DataField]
    public SortedDictionary<DurabilityState, HashSet<LocId>> DamagePopups = [];

    /// <summary>
    /// What to do when the entity has depleted all of its durability.
    /// Supports any <see cref="IThresholdBehavior"/> so it can be done like how you would do it on a <see cref="Content.Shared.Destructible.DestructibleComponent"/>.
    /// Can omit destruction act as the entity will be deleted anyway once it breaks.
    /// </summary>
    [DataField(serverOnly: true)]
    public IThresholdBehavior? OnBreakBehavior;

    /// <summary>
    /// Whether to actually delete the entity when <see cref="DurabilityState"/> is <see cref="DurabilityState.Destroyed"/>.
    /// This could be set to false in order to allow the entity to be repaired while remaining unusable.
    /// Entities in the <see cref="DurabilityState.Destroyed"/> will cause <see cref="Content.Shared.Weapons.Melee.Events.AttemptMeleeEvent"/> to be canceled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteOnDestroyed = true;

    /// <summary>
    /// The popup to show when attempting to swing a weapon entity that is in the <see cref="DurabilityState.Destroyed"/> state.
    /// </summary>
    [DataField]
    public LocId? DestroyedSwingAttemptPopup = new LocId("durability-attempt-melee-destroyed");

    /// <summary>
    /// Minimum damage that can be dealt to the entity when passing the damage chance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinDamageRoll = 3;

    /// <summary>
    /// Maximum damage that can be dealt to the entity when passing the damage chance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxDamageRoll = 6;

    /// <summary>
    /// Modifiers that apply to the weapon depending on the state. <see cref="DurabilityState.Destroyed"/> can be omitted as
    /// the entity will be deleted upon reaching that state anyway. <see cref="DurabilityState.Pristine"/> can also be
    /// omitted unless you want to give bonus for being fully repaired (same with <see cref="DurabilityState.Reinforced"/>).
    /// This modifier can be used for anything ranging form damage on a melee weapon or accuracy on a gun etc.
    /// Note that for guns, you still always want the number lower for lower durability. values will be multiplied by or
    /// divided by it based on whether higher number or lower number is more or less beneficial.
    /// </summary>
    [DataField]
    public SortedDictionary<DurabilityState, float> DurabilityModifiers = new()
    {
        { DurabilityState.Reinforced, 1.2f },
        { DurabilityState.Worn, 0.9f },
        { DurabilityState.Damaged, 0.7f },
        { DurabilityState.Broken, 0.45f },
    };

    /// <summary>
    /// Set of entity prototypes that can be used to repair this entity, and how much it will repair the entity by.
    /// Yes, the minmax is a vec2. Yes I hate it too.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<MaterialPrototype>, Vector2> RepairMaterials = [];

    /// <summary>
    /// Set of tool qualities that can be used to repair this entity, and how much it will repair the entity by.
    /// Tools are not consumed by the durability system when used to repair the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype>? RepairTool;

    /// <summary>
    /// How much the repair tool should repair the entity by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 ToolRepairAmount = Vector2.Zero;

    /// <summary>
    /// If using a welder to repair this, how much fuel it should cost.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FuelCost;

    /// <summary>
    /// How long it takes to repair this entity via using another entity on it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RepairDoAfter = TimeSpan.Zero;

    /// <summary>
    /// Whether this entity can be repaired by any means or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Repairable = true;

    /// <summary>
    /// How many points of damage to allow overhealing by? For example, if this is 10, then the damage value will be able to go to -10 damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxRepairBonus;
}
