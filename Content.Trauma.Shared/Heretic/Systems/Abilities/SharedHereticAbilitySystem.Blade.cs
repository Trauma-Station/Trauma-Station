// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [Dependency] private SharedStaminaSystem _stam = default!;

    protected virtual void SubscribeBlade()
    {
        SubscribeLocalEvent<SilverMaelstromComponent, GetClothingStunModifierEvent>(OnBladeStunModify);
        SubscribeLocalEvent<SilverMaelstromComponent, DropHandItemsEvent>(OnBladeDropItems,
            before: new[] { typeof(SharedHandsSystem) });
        SubscribeLocalEvent<SilverMaelstromComponent, ComponentStartup>(OnMaelstromStartup);
        SubscribeLocalEvent<SilverMaelstromComponent, ComponentShutdown>(OnMaelstromShutdown);

        SubscribeLocalEvent<EventHereticSacraments>(OnSacraments);
        SubscribeLocalEvent<HereticBladePassiveRiposteEvent>(OnRiposte);
    }

    private void OnRiposte(HereticBladePassiveRiposteEvent args)
    {
        TryComp(args.Heretic, out RiposteeComponent? riposte);
        if (args.Negative)
        {
            if (riposte == null)
                return;

            riposte.Data.Remove(args.RiposteDataId);
            if (riposte.Data.Count == 0)
                RemCompDeferred(args.Heretic, riposte);

            return;
        }

        if (riposte?.Data.GetValueOrDefault(args.RiposteDataId) is { } data)
        {
            data.Cooldown = MathF.Min(data.Cooldown, args.Cooldown);
            return;
        }

        EnsureComp<RiposteeComponent>(args.Heretic).Data = new()
        {
            { args.RiposteDataId, new(args.Cooldown) }
        };
    }

    private void OnMaelstromShutdown(Entity<SilverMaelstromComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        StatusNew.TryRemoveStatusEffect(ent, ent.Comp.Status);
    }

    private void OnMaelstromStartup(Entity<SilverMaelstromComponent> ent, ref ComponentStartup args)
    {
        StatusNew.TryUpdateStatusEffectDuration(ent, ent.Comp.Status, out _);
    }

    private void OnSacraments(EventHereticSacraments args)
    {
        if (!TryUseAbility(args))
            return;

        StatusNew.TryUpdateStatusEffectDuration(args.Performer, args.Status, args.Time);
    }

    private void OnBladeDropItems(Entity<SilverMaelstromComponent> ent, ref DropHandItemsEvent args)
    {
        args.Handled = true;
    }

    private void OnBladeStunModify(Entity<SilverMaelstromComponent> ent, ref GetClothingStunModifierEvent args)
    {
        args.Modifier *= 0.5f;
    }
}
