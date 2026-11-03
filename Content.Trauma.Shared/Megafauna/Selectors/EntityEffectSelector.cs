using Content.Lavaland.Shared.Megafauna;
using Content.Lavaland.Shared.Megafauna.Selectors;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Megafauna.Selectors;

/// <summary>
/// Runs entity effects on the entity
/// </summary>
public sealed partial class EntityEffectSelector : MegafaunaSelector
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    private SharedEntityEffectsSystem? _effects;

    protected override float InvokeImplementation(MegafaunaCalculationBaseArgs args)
    {
        var entMan = args.EntMan;
        _effects ??= entMan.System<SharedEntityEffectsSystem>();

        _effects.ApplyEffects(args.Entity, Effects);

        return DelaySelector.Get(args);
    }
}
