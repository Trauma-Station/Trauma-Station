using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseHereticRitualEvent : CancellableEntityEventArgs
{
    public EntityUid Performer;
    public Entity<HereticComponent> Mind;
    public EntityUid Platform;
    public string? CancelStringOverride;

    [DataField]
    public LocId? CancelLoc;
}

public abstract partial class InputHereticRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public string InputKey;
}

public abstract partial class OutputHereticRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public string OutputKey;

    [DataField]
    public bool CancelOnEmptyOutput = true;
}

public abstract partial class InputOutputHereticRitualEvent : OutputHereticRitualEvent
{
    [DataField(required: true)]
    public string InputKey;
}

public sealed partial class LookupRitualEvent : OutputHereticRitualEvent
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public LookupFlags Flags = LookupFlags.Uncontained;
}

public sealed partial class FilterHereticsRitualEvent : InputOutputHereticRitualEvent;

public sealed partial class FilterRitualEvent : InputOutputHereticRitualEvent
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}

public sealed partial class FilterByMobStateRitualEvent : InputOutputHereticRitualEvent
{
    [DataField]
    public MobState MobState = MobState.Alive;

    [DataField]
    public bool Invert = true;
}

public sealed partial class FilterTargetsRitualEvent : InputOutputHereticRitualEvent;

public sealed partial class CombineEntityHashSetRitualEvent : InputOutputHereticRitualEvent
{
    [DataField(required: true)]
    public string InputKey2;
}

public sealed partial class TakeNumberEntitiesRitualEvent : InputOutputHereticRitualEvent
{
    [DataField(required: true)]
    public int Number;
}

public sealed partial class SacrificeRitualEvent : InputHereticRitualEvent
{
    [DataField]
    public EntProtoId SacrificeObjective = "HereticSacrificeObjective";

    [DataField]
    public EntProtoId SacrificeHeadObjective = "HereticSacrificeHeadObjective";
}

public sealed partial class SpawnRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Output;
}

public sealed partial class PathBasedSpawnRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public EntProtoId FallbackOutput;

    [DataField(required: true)]
    public Dictionary<string, EntProtoId> Output;
}

public sealed partial class ProcessIngredientsRitualEvent : InputHereticRitualEvent
{
    [DataField]
    public List<RitualIngredient> Ingredients = new();
}

public sealed partial class RaiseHereticEventRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true), NonSerialized]
    public object? Event;
}

public sealed partial class AddKnowledgeRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public ProtoId<HereticKnowledgePrototype> Knowledge;
}

public sealed partial class FindLostLimitedOutputRitualEvent : OutputHereticRitualEvent
{
    [DataField]
    public float MinRange = 1.5f;
}

public sealed partial class CanAscendRitualEvent : BaseHereticRitualEvent;

public sealed partial class ObjectivesCompleteRitualEvent : BaseHereticRitualEvent;

public sealed partial class FilterOnFireRitualEvent : InputOutputHereticRitualEvent;

public sealed partial class FilterHeadlessRitualEvent : InputOutputHereticRitualEvent;

public sealed partial class FilterReagentPuddleRitualEvent : InputOutputHereticRitualEvent
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Reagents = new()
    {
      "Blood",
      "AmmoniaBlood",
      "InsectBlood",
      "CopperBlood",
      "ZombieBlood",
      "AlienBlood",
      "BlackBlood",
      "BloodChangeling",
    };

    [DataField]
    public LocId ReagentLoc = "reagent-name-blood";
}

public sealed partial class DeleteEntityHashsetRitualEvent : InputHereticRitualEvent;

public sealed partial class GhoulifyRitualEvent : InputOutputHereticRitualEvent
{
    [DataField]
    public FixedPoint2 TotalHealth = 100f;

    [DataField]
    public bool GiveBlade = true;
}

public sealed partial class AddComponentsRitualEvent : InputHereticRitualEvent
{
    [DataField(required: true)]
    public ComponentRegistry Components;
}

public sealed partial class LowTemperatureRitualEvent : BaseHereticRitualEvent
{
    [DataField]
    public float Threshold;
}

public sealed partial class FilterKnowledgeTagsRitualEvent : InputOutputHereticRitualEvent;

public sealed partial class UpdateKnowledgeRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public float Amount;
}

public sealed partial class RemoveRitualsRitualEvent : BaseHereticRitualEvent
{
    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> RitualTags = new();
}

public sealed partial class FeastOfOwlsMenuRitualEvent : BaseHereticRitualEvent;

public sealed partial class TeleportToRuneRitualEvent : InputHereticRitualEvent;

public sealed partial class RaiseRitualEventsRitualEvent : BaseHereticRitualEvent
{
    /// <summary>
    /// Raise HereticRitualComponent Events from starting from and including this index
    /// Skips <see cref="RaiseRitualEventsRitualEvent"/>
    /// </summary>
    [DataField(required: true)]
    public int FromIndex;

    /// <summary>
    /// Raise HereticRitualComponent Events from ending with but not including this index
    /// Skips <see cref="RaiseRitualEventsRitualEvent"/>
    /// </summary>
    [DataField(required: true)]
    public int ToIndex;
}
