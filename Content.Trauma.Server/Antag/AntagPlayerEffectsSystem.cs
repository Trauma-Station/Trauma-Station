using Content.Server.Antag;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.Antag;

public sealed class AntagPlayerEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagPlayerEffectsComponent, AfterAntagEntitySelectedEvent>(OnEntitySelected);
    }

    private void OnEntitySelected(Entity<AntagPlayerEffectsComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _effects.ApplyEffects(args.EntityUid, ent.Comp.Effects);
    }
}
