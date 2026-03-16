// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitcode.Heretic.Effects;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticActionComponent : Component
{
    /// <summary>
    ///     Indicates that a user should wear a heretic amulet, a hood or something else.
    /// </summary>
    [DataField]
    public bool RequireMagicItem = true;

    [DataField]
    public string? MessageLoc = null;
}

#region DoAfters

[Serializable, NetSerializable]
public sealed partial class EldritchInfluenceDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class DrawRitualRuneDoAfterEvent : DoAfterEvent
{
    // TODO: NetCoordinates / NetEntity
    [NonSerialized] public EntityCoordinates Coords;
    [NonSerialized] public EntityUid RitualRune;

    public DrawRitualRuneDoAfterEvent(EntityUid ritualRune, EntityCoordinates coords)
    {
        RitualRune = ritualRune;
        Coords = coords;
    }

    public override DoAfterEvent Clone() => new DrawRitualRuneDoAfterEvent(RitualRune, Coords);
}

[Serializable, NetSerializable]
public sealed partial class HereticMansusLinkDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EventHereticFleshSurgeryDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StarGazeDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity OrbEffect = NetEntity.Invalid;

    public StarGazeDoAfterEvent(NetEntity orbEffect)
    {
        OrbEffect = orbEffect;
    }

    public override DoAfterEvent Clone() => new StarGazeDoAfterEvent(OrbEffect);
}

#endregion

#region Abilities

/// <summary>
///     Raised whenever we need to check for a magic item before casting a spell that requires one to be worn.
/// </summary>
public sealed partial class CheckMagicItemEvent : HandledEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

[ByRefEvent]
public readonly record struct HereticLostFocusEvent;

[ByRefEvent]
public record struct HereticMagicCastAttemptEvent(EntityUid User, EntityUid Action, bool Cancelled = false);

// basic
public sealed partial class HereticStartupEvent : HereticKnowledgeEvent;
public sealed partial class EventHereticOpenStore : InstantActionEvent;
public sealed partial class EventHereticMansusGrasp : InstantActionEvent;
public sealed partial class EventHereticLivingHeart : InstantActionEvent; // opens ui

[ByRefEvent]
public readonly record struct HereticStateChangedEvent(EntityUid Mind, bool IsDead, bool Temporary);

public sealed partial class EventHereticCloak : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId<HereticCloakedStatusEffectComponent> Status;

    [DataField]
    public TimeSpan? Lifetime;
}

// living heart
[Serializable, NetSerializable] public sealed class EventHereticLivingHeartActivate : BoundUserInterfaceMessage // triggers the logic
{
    public NetEntity? Target { get; set; }
}
[Serializable, NetSerializable] public enum HereticLivingHeartKey : byte
{
    Key
}

// ghoul specific
public sealed partial class EventHereticMansusLink : EntityTargetActionEvent;

// ash
public sealed partial class EventHereticAshenShift : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Jaunt = "AshJaunt";
}

public sealed partial class EventHereticVolcanoBlast : InstantActionEvent
{
    [DataField]
    public float Radius = 5;
}

public sealed partial class EventHereticNightwatcherRebirth : InstantActionEvent
{
    [DataField]
    public float Range = 14f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            { "Heat", 20 },
        },
    };

    [DataField]
    public float FireProtectionPenetration = 0.5f;

    [DataField]
    public float HealAmount = -10f;

    [DataField]
    public EntProtoId Effect = "NightwatcherEffect";
}

public sealed partial class EventHereticFlames : InstantActionEvent;

public sealed partial class EventHereticCascade : InstantActionEvent
{
    [DataField]
    public EntProtoId CascadeEnt = "HereticCascade";
}

// flesh
public sealed partial class EventHereticFleshSurgery : InstantActionEvent, ITouchSpellEvent
{
    [DataField]
    public EntProtoId TouchSpell { get; set; } = "TouchSpellFleshSurgery";
}

// void (+ upgrades)
public sealed partial class HereticVoidBlastEvent : InstantActionEvent;

public sealed partial class HereticVoidBlinkEvent : WorldTargetActionEvent
{
    [DataField]
    public DamageSpecifier Damage = new ()
    {
        DamageDict =
        {
            {"Cold", 20},
        },
    };

    [DataField]
    public float Radius = 1.5f;

    [DataField]
    public EntProtoId InEffect = "EffectVoidBlinkIn";

    [DataField]
    public EntProtoId OutEffect = "EffectVoidBlinkOut";
}

public sealed partial class HereticVoidPullEvent : InstantActionEvent
{
    [DataField]
    public DamageSpecifier Damage = new ()
    {
        DamageDict =
        {
            {"Cold", 30},
        },
    };

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan KnockDownTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float Radius = 3f;

    [DataField]
    public EntProtoId InEffect = "EffectVoidBlinkIn";
}

public sealed partial class HereticVoidPrisonEvent : EntityTargetActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "VoidPrison";
}

public sealed partial class HereticVoidConduitEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId VoidConduit = "VoidConduit";
}

// blade (+ upgrades)
public sealed partial class EventHereticSacraments : InstantActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(6.7);

    [DataField]
    public EntProtoId Status = "SacramentsOfPowerStatusEffect";
}

public sealed partial class HereticChampionStanceEvent : HereticKnowledgeEvent;
public sealed partial class EventHereticFuriousSteel : InstantActionEvent;

// lock
public sealed partial class EventHereticBulglarFinesse : InstantActionEvent;
public sealed partial class EventHereticLastRefugee : InstantActionEvent;

public sealed partial class EventHereticShapeshift : InstantActionEvent;

// rust
public sealed partial class EventHereticRustConstruction : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId RustedWall = "WallSolidRust";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/constructform.ogg");

    [DataField]
    public float ObstacleCheckRange = 0.05f;

    [DataField]
    public float MobCheckRange = 0.6f;

    [DataField]
    public float ThrowSpeed = 15f;

    [DataField]
    public float ThrowRange = 5f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(5f);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            { "Blunt", 20 },
        },
    };
}

public sealed partial class EventHereticEntropicPlume : InstantActionEvent
{
    [DataField]
    public EntProtoId Proto = "EntropicPlume";

    [DataField]
    public float Offset = 2.5f;

    [DataField]
    public float Speed = 0.1f;

    [DataField]
    public float Radius = 2.5f;

    [DataField]
    public float LookupRange = 0.1f;

    [DataField]
    public int RustStrength = 7; // Toxic blade level

    [DataField]
    public EntProtoId TileRune = "TileHereticRustRune";
}

public sealed partial class EventHereticAggressiveSpread : InstantActionEvent
{
    [DataField]
    public float AoeRadius = 2f;

    [DataField]
    public float Range = 4f;

    [DataField]
    public float LookupRange = 0.1f;

    [DataField]
    public int RustStrength = 2;

    [DataField]
    public EntProtoId TileRune = "TileHereticRustRune";
}

public sealed partial class EventHereticRustCharge : WorldTargetActionEvent
{
    [DataField]
    public float Distance = 10f;

    [DataField]
    public float Speed = 10f;
}

// cosmos
public sealed partial class EventHereticCosmicRune : InstantActionEvent
{
    [DataField]
    public EntProtoId Rune = "HereticRuneCosmos";
}

public sealed partial class EventHereticStarTouch : InstantActionEvent, ITouchSpellEvent
{
    [DataField]
    public EntProtoId TouchSpell { get; set; } = "TouchSpellStar";
}

public sealed partial class EventHereticStarBlast : InstantWorldTargetActionEvent
{
    [DataField]
    public EntProtoId Projectile = "ProjectileStarBall";

    [DataField]
    public float ProjectileSpeed = 2f;
}

public sealed partial class EventHereticCosmicExpansion : InstantActionEvent
{
    [DataField]
    public EntProtoId Effect = "EffectCosmicDomain";

    [DataField]
    public float Range = 7f;
}

public sealed partial class StarGazeEvent : InstantActionEvent // Giga lazor
{
    [DataField]
    public TimeSpan DoAfterDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public EntProtoId OrbEffect = "EffectGazerOrb";

    [DataField]
    public SoundSpecifier BeamStartSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/stargazer/beam_open.ogg");
}

public sealed partial class ResetStarGazerConsciousnessEvent : InstantActionEvent;

public sealed partial class StarGazerSeekMasterEvent : InstantActionEvent;

// side
public sealed partial class EventHereticIceSpear : InstantActionEvent;

public sealed partial class EventHereticCleave : WorldTargetActionEvent
{
    [DataField]
    public float Range = 1f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            {"Heat", 20f},
            {"Bloodloss", 10f},
        },
    };

    [DataField]
    public FixedPoint2 BloodModifyAmount = -50f;

    [DataField]
    public EntProtoId Effect = "EffectCleave";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/blood3.ogg");
}

public sealed partial class EventHereticSpacePhase : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "SpaceJaunt";

    [DataField]
    public EntProtoId Effect = "EffectSpaceExplosion";
}

public sealed partial class EventMirrorJaunt : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "MirrorJaunt";

    [DataField]
    public EntProtoId ActionProto = "ActionMirrorJaunt";

    [DataField]
    public float LookupRange = 1f;
}

public sealed partial class EventEmp : InstantActionEvent
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public float EnergyConsumption = 50000f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20f);
}

public sealed partial class EventHereticRealignment : InstantActionEvent
{
    [DataField]
    public EntProtoId RealignmentStatus = "RealignmentStatusEffect";

    [DataField]
    public EntProtoId StunStatus = "RealignmentStatusEffect";

    [DataField]
    public EntProtoId SleepStatus = "StatusEffectForcedSleeping";

    [DataField]
    public EntProtoId DrowsinessStatus = "StatusEffectDrowsiness";

    [DataField]
    public TimeSpan EffectTime = TimeSpan.FromSeconds(10);
}

// ascensions
public sealed partial class HereticAscensionCosmosEvent : HereticKnowledgeEvent;
#endregion

public interface ITouchSpellEvent
{
    EntProtoId TouchSpell { get; set; }
}

public abstract partial class InstantWorldTargetActionEvent : WorldTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed class LaserBeamEndpointPositionEvent(NetEntity uid, MapCoordinates coords) : EntityEventArgs
{
    public NetEntity Uid = uid;

    public MapCoordinates Coordinates = coords;
}

[Virtual, DataDefinition, ImplicitDataDefinitionForInheritors]
public partial class HereticKnowledgeEvent : EntityEventArgs
{
    public EntityUid Heretic;

    public bool Negative;

    [DataField]
    public ComponentRegistry AddedComponents = new();
}
