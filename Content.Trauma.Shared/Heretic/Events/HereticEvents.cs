// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Store;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Events;

[ByRefEvent]
public readonly record struct SetGhoulBoundHereticEvent(EntityUid Heretic, EntityUid HereticMind, EntityUid? Ritual);

[ByRefEvent]
public readonly record struct IncrementHereticObjectiveProgressEvent(EntProtoId Proto, int Amount = 1);

[ByRefEvent]
public readonly record struct SpawnHereticInfluenceEvent(EntProtoId Proto, int Amount = 1);

[ByRefEvent]
public readonly record struct UserInvokeTouchSpellEvent;

[DataDefinition]
public sealed partial class EventHereticAscension : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticRerollTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticUpdateTargets : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticResolveStarGazer : EntityEventArgs;

[DataDefinition]
public sealed partial class EventHereticAddKnowledge : EntityEventArgs
{
    [DataField(required: true)]
    public List<ProtoId<HereticKnowledgePrototype>> Knowledge;
}

[DataDefinition]
public sealed partial class HereticModifySideKnowledgeDraftsEvent : EntityEventArgs
{
    [DataField(required: true)]
    public Dictionary<ProtoId<StoreCategoryPrototype>, int> SideKnowledgeDrafts;
}

[DataDefinition]
public sealed partial class HereticGraspUpgradeEvent : EntityEventArgs
{
    [DataField]
    public EntProtoId GraspAction = "ActionHereticMansusGrasp";

    [DataField(required: true)]
    public ComponentRegistry AddedComponents = new();
}

[DataDefinition]
public sealed partial class HereticAddMindComponentsEvent : EntityEventArgs
{
    [DataField(required: true)]
    public ComponentRegistry AddedComponents = new();
}

[DataDefinition]
public sealed partial class IncreaseFleshGhoulLimitEvent : EntityEventArgs
{
    [DataField(required: true)]
    public int GhoulLimitIncrease;

    [DataField(required: true)]
    public int VoicelessDeadLimitIncrease;

    [DataField]
    public ProtoId<TagPrototype> ImperfectRitual = "RitualImperfect";
}

[DataDefinition]
public sealed partial class HereticRemoveActionEvent : EntityEventArgs
{
    [DataField(required: true)]
    public EntProtoId Action;
}

public sealed partial class CrucibleSoulRecallEvent : BaseAlertEvent
{
    [DataField]
    public EntProtoId EffectProto = "StatusEffectCrucibleSoul";
}

[ByRefEvent]
public record struct TouchSpellUsedEvent(
    EntityUid User,
    EntityUid Target,
    bool Invoke = false,
    TimeSpan? CooldownOverride = null);

[ByRefEvent]
public record struct BeforeTouchSpellAbilityUsedEvent(
    TouchSpellEvent Args,
    EntProtoId? TouchSpell = null,
    bool Cancelled = false);

[ByRefEvent]
public readonly record struct AfterTouchSpellAbilityUsedEvent(EntityUid TouchSpell);

/// <summary>
/// Raised when all hands are occupied.
/// Used for blade heretic quick blade empowering.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class TouchSpellSpecialEvent : EntityEventArgs
{
    [DataField]
    public LocId? Speech;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public TimeSpan Cooldown;

    public bool Invoke;
}

public sealed partial class MansusGraspSpecialEvent : TouchSpellSpecialEvent;

[ByRefEvent]
public record struct TouchSpellAttemptEvent(EntityUid User, EntityUid Target, bool Cancelled = false);

[ImplicitDataDefinitionForInheritors]
public abstract partial class HereticBladeBonusEvent : EntityEventArgs
{
    public MeleeHitEvent Args;

    public int PathStage;
}

[Virtual]
public partial class HereticBladeBonusDamageEvent : HereticBladeBonusEvent
{
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = default!;
}

public sealed partial class HereticBladeBonusWoundingEvent : HereticBladeBonusEvent
{
    /// <summary>
    /// Path stage -> bonus
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, float> WoundingBonus = default!;
}

public sealed partial class CosmosBladeBonusEvent : HereticBladeBonusDamageEvent;

public sealed partial class BladeBladeBonusEvent : HereticBladeBonusDamageEvent;

[ByRefEvent]
public record struct ShouldHideHereticAuraEvent(bool Hide) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
