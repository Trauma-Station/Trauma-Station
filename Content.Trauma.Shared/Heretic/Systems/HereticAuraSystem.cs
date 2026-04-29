using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed class HereticAuraSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HideHereticAuraComponent, ComponentStartup>((uid, _, _) => _heretic.RemoveAura(uid));
        SubscribeLocalEvent<HideHereticAuraComponent, ComponentShutdown>((uid, _, _) =>
            _heretic.UpdateHereticAura(uid));
        SubscribeLocalEvent<HideHereticAuraComponent, GotEquippedEvent>((_, _, ev) => _heretic.RemoveAura(ev.Equipee));
        SubscribeLocalEvent<HideHereticAuraComponent, GotUnequippedEvent>((_, _, ev) =>
            _heretic.UpdateHereticAura(ev.Equipee));

        Subs.SubscribeWithRelay<HideHereticAuraComponent, ShouldHideHereticAuraEvent>(OnHide, held: false);

        SubscribeLocalEvent<StatusEffectContainerComponent, ShouldHideHereticAuraEvent>(_status
            .RefRelayStatusEffectEvent);

        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, ShouldHideHereticAuraEvent>(OnHide);
        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectAppliedEvent>((_, _, ev) =>
            _heretic.RemoveAura(ev.Target));
        SubscribeLocalEvent<HideHereticAuraStatusEffectComponent, StatusEffectRemovedEvent>((_, _, ev) =>
            _heretic.UpdateHereticAura(ev.Target));
    }

    private void OnHide(EntityUid uid, Component comp, ref ShouldHideHereticAuraEvent args)
    {
        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Hide = true;
    }
}
