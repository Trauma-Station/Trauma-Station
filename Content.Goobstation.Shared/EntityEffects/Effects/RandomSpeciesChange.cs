using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class RandomSpeciesChange : EntityEffectBase<RandomSpeciesChange>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class RandomSpeciesChangeEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, RandomSpeciesChange>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedSpeciesChangeEffectSystem _speciesChange = default!;

    private List<string> _species = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ref EntityEffectEvent<RandomSpeciesChange> args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        var species = rand.Pick(_species);
        _speciesChange.Polymorph(ent, species);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SpeciesPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        _species.Clear();
        foreach (var species in _proto.EnumeratePrototypes<SpeciesPrototype>())
        {
            _species.Add(species.ID);
        }
    }
}
