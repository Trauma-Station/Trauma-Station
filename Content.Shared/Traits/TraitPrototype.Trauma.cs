using Content.Shared._EinsteinEngines.Language;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

public sealed partial class TraitPrototype
{
    /// <summary>
    /// Hides traits from specific species
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> ExcludedSpecies = new();

    /// <summary>
    /// Only shows traits to specific species
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> IncludedSpecies = new();

    /// <summary>
    /// Entity effects applied to the mob after spawning.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];

    /// <summary>
    /// The list of all Spoken Languages that this trait adds.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>>? LanguagesSpoken;

    /// <summary>
    /// The list of all Understood Languages that this trait adds.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>>? LanguagesUnderstood;

    /// <summary>
    /// The list of all Spoken Languages that this trait removes.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>>? RemoveLanguagesSpoken;

    /// <summary>
    /// The list of all Understood Languages that this trait removes.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>>? RemoveLanguagesUnderstood;
}
