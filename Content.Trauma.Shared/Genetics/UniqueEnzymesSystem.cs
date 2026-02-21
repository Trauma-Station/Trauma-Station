// SPDX-License-Identifier: AGPL-3.0-or-later
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
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;

    private EntityQuery<DetailExaminableComponent> _detailQuery;
    private EntityQuery<FingerprintComponent> _printsQuery;
    private EntityQuery<HumanoidProfileComponent> _humanoidQuery;

    public override void Initialize()
    {
        base.Initialize();

        _detailQuery = GetEntityQuery<DetailExaminableComponent>();
        _printsQuery = GetEntityQuery<FingerprintComponent>();
        _humanoidQuery = GetEntityQuery<HumanoidProfileComponent>();
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

        if (enzymes.EyeColor is {} eyeColor)
            _humanoid.SetEyeColor(mob, eyeColor);
        if (enzymes.SkinColor is {} skinColor)
            _humanoid.SetSkinColor(mob, skinColor, enzymes.EyeColor);

        if (!_humanoidQuery.TryComp(mob, out var humanoid))
            return;

        // no age because peter thiel
        // no species because lol
        if (enzymes.Sex is {} sex)
            _humanoid.SetSex((mob, humanoid), sex);
        if (enzymes.Gender is {} gender)
            _humanoid.SetGender((mob, humanoid), gender);
        return;
    }

    /// <summary>
    /// Get the unique enzymes for a mob.
    /// </summary>
    public UniqueEnzymes GetEnzymes(EntityUid mob)
    {
        var humanoid = _humanoidQuery.CompOrNull(mob);
        var organs = _humanoid.GetOrgansData(mob);
        return new UniqueEnzymes(
            Name(mob),
            _printsQuery.CompOrNull(mob)?.Fingerprint,
            humanoid?.Sex,
            humanoid?.Gender,
            _humanoid.GetEyeColor(organs),
            _humanoid.GetSkinColor(organs)
        );
    }
}
