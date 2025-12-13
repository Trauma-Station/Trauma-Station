using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Genetics;

/// <summary>
/// Simple API for getting and changing <see cref="UniqueEnzymes"/> for mobs.
/// </summary>
public sealed class UniqueEnzymesSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    private EntityQuery<FingerprintComponent> _printsQuery;
    private EntityQuery<HumanoidAppearanceComponent> _humanoidQuery;

    public override void Initialize()
    {
        base.Initialize();

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

        if (enzymes.Sex is {} sex)
            _humanoid.SetSex(mob, sex, humanoid: humanoid);

        if (enzymes.EyeColor is {} eyeColor)
        {
            humanoid.EyeColor = eyeColor;
            Dirty(mob, humanoid);
        }

        if (enzymes.SkinColor is {} skinColor)
            _humanoid.SetSkinColor(mob, skinColor, humanoid: humanoid);
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
            humanoid?.EyeColor,
            humanoid?.SkinColor
        );
    }
}
