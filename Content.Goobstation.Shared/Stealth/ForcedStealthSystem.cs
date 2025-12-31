using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Stealth;
public sealed partial class ForcedStealthSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public static readonly EntProtoId ForcedStealth = "ForcedStealthStatusEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcedStealthStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
        SubscribeLocalEvent<ForcedStealthStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
    }

    private void OnStatusApplied(Entity<ForcedStealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (EnsureComp<StealthComponent>(args.Target, out var stealth))
        {
            ent.Comp.OldVisibility = _stealth.GetVisibility(args.Target, stealth);
        }
        _stealth.SetVisibility(args.Target, ent.Comp.Visibility, stealth);
    }

    private void OnStatusRemoved(Entity<ForcedStealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.OldVisibility is {} visibility)
            _stealth.SetVisibility(args.Target, visibility);
        else
            RemComp<StealthComponent>(args.Target);
    }
}
