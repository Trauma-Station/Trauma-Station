using Content.Shared.EntityEffects;
using Content.Shared.Implants;

namespace Content.Trauma.Shared.Implants;

public sealed partial class ImplantEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnImplanted(Entity<ImplantEffectsComponent> ent, ref ImplantImplantedEvent args)
    {
        if (ent.Comp.Added is { } added)
            _effects.ApplyEffects(args.Implanted, added, user: args.User);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<ImplantEffectsComponent> ent, ref ImplantRemovedEvent args)
    {
        if (ent.Comp.Removed is { } removed)
            _effects.ApplyEffects(args.Implanted, removed);
    }
}
