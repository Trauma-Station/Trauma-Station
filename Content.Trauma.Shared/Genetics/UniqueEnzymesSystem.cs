// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared.Body.Systems;
using Content.Shared.DetailExaminable;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Trauma.Shared.Genetics.Mutations;
using System.Linq;

namespace Content.Trauma.Shared.Genetics;

/// <summary>
/// Simple API for getting and changing <see cref="UniqueEnzymes"/> for mobs.
/// </summary>
public sealed class UniqueEnzymesSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    private EntityQuery<DetailExaminableComponent> _detailQuery;
    private EntityQuery<FingerprintComponent> _printsQuery;
    private EntityQuery<HumanoidAppearanceComponent> _humanoidQuery;

    public override void Initialize()
    {
        base.Initialize();

        _detailQuery = GetEntityQuery<DetailExaminableComponent>();
        _printsQuery = GetEntityQuery<FingerprintComponent>();
        _humanoidQuery = GetEntityQuery<HumanoidAppearanceComponent>();
    }

    /// <summary>
    /// Change a mob's unique enzymes, if it is mutatable (i.e. no renaming mice and shit).
    /// </summary>
    public void ChangeEnzymes(EntityUid mob, UniqueEnzymes enzymes)
    {
        if (!_mutation.CanMutate(mob))
            return;

        _meta.SetEntityName(mob, enzymes.Name);
        if (enzymes.Prints is {} print && _printsQuery.TryComp(mob, out var prints))
        {
            prints.Fingerprint = print;
            Dirty(mob, prints);
        }

        if (!_humanoidQuery.TryComp(mob, out var humanoid))
            return;

        // i hate this
        var hairStyle = HairStyles.DefaultHairStyle;
        var facialHairStyle = HairStyles.DefaultFacialHairStyle;
        var markings = humanoid.MarkingSet.Markings;
        if (markings.TryGetValue(MarkingCategories.Hair, out var hairs) && hairs.Count > 0)
            hairStyle = hairs[0].MarkingId;
        if (markings.TryGetValue(MarkingCategories.FacialHair, out var facialHairs) && facialHairs.Count > 0)
            facialHairStyle = facialHairs[0].MarkingId;

        var appearance = new HumanoidCharacterAppearance(
            hairStyleId: hairStyle,
            hairColor: humanoid.CachedHairColor ?? Color.Black,
            facialHairStyleId: facialHairStyle,
            facialHairColor: humanoid.CachedFacialHairColor ?? Color.Black,
            eyeColor: enzymes.EyeColor ?? humanoid.EyeColor,
            skinColor: enzymes.SkinColor ?? humanoid.SkinColor,
            markings: humanoid.MarkingSet.GetForwardEnumerator().ToList());

        var flavortext = _detailQuery.CompOrNull(mob)?.Content;
        var profile = new HumanoidCharacterProfile(
            enzymes.Name, // this was already changed
            flavortext,
            humanoid.Species,
            humanoid.Height,
            humanoid.Width,
            humanoid.Age,
            // below actually get changed
            enzymes.Sex ?? humanoid.Sex,
            enzymes.Gender ?? humanoid.Gender,
            appearance,
            // below aren't used
            SpawnPriorityPreference.None,
            new(),
            PreferenceUnavailableMode.SpawnAsOverflow,
            new(),
            new(),
            new(),
            humanoid.BarkVoice);

        // need this shitcode so the limbs dont overwrite the new skin colour
        foreach (var part in _body.GetBodyChildren(mob))
        {
            RemComp<BodyPartAppearanceComponent>(part.Id);
        }

        humanoid.ProfileLoaded = false;
        Dirty(mob, humanoid);

        _humanoid.LoadProfile(mob, profile, humanoid);
    }

    /// <summary>
    /// Get the unique enzymes for a mob.
    /// </summary>
    public UniqueEnzymes GetEnzymes(EntityUid mob)
    {
        var humanoid = _humanoidQuery.CompOrNull(mob);
        return new UniqueEnzymes(
            Name(mob),
            _printsQuery.CompOrNull(mob)?.Fingerprint,
            humanoid?.Sex,
            humanoid?.Gender,
            humanoid?.EyeColor,
            humanoid?.SkinColor
        );
    }
}
