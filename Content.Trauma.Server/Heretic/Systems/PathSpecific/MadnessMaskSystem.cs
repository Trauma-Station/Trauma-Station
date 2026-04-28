// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Flammability;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Atmos;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class MadnessMaskSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public static readonly EntProtoId StatusEffectSeeingRainbow = "StatusEffectSeeingRainbow";

    private HashSet<Entity<Content.Shared.StatusEffect.StatusEffectsComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MadnessMaskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MadnessMaskComponent, BeingUnequippedAttemptEvent>(OnUnequip);
        SubscribeLocalEvent<MadnessMaskComponent, InventoryRelayedEvent<GetFireProtectionEvent>>(OnGetProtection);
        SubscribeLocalEvent<MadnessMaskComponent, InventoryRelayedEvent<ModifyChangedTemperatureEvent>>(
            OnTemperatureChangeAttempt);
    }

    private void OnMapInit(Entity<MadnessMaskComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
    }

    private void OnUnequip(Entity<MadnessMaskComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (_heretic.IsHereticOrGhoul(args.Unequipee))
            return;

        if (TryComp<ClothingComponent>(ent, out var clothing) && (clothing.Slots & args.SlotFlags) == SlotFlags.NONE)
            return;

        args.Cancel();
    }

    private void OnTemperatureChangeAttempt(Entity<MadnessMaskComponent> ent,
        ref InventoryRelayedEvent<ModifyChangedTemperatureEvent> args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Args.Target))
            return;

        if (args.Args.TemperatureDelta > 0)
            args.Args.TemperatureDelta = 0;
    }

    private void OnGetProtection(Entity<MadnessMaskComponent> ent, ref InventoryRelayedEvent<GetFireProtectionEvent> args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Args.Target) || HasComp<VeryFlammableComponent>(args.Args.Target))
            return;

        args.Args.Multiplier = -10f; // Basically ignore fire AP
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MadnessMaskComponent, ClothingComponent>();
        while (query.MoveNext(out var uid, out var mask, out var clothing))
        {
            if (clothing.InSlot == null || now < mask.NextUpdate)
                continue;

            mask.NextUpdate = now + mask.UpdateDelay;

            var coords = Transform(uid).Coordinates;
            _targets.Clear();
            _lookup.GetEntitiesInRange(coords, 5f, _targets);
            foreach (var target in _targets)
            {
                // heathens exclusive
                var look = target.Owner;
                if (_heretic.IsHereticOrGhoul(look))
                    continue;

                if (HasComp<StaminaComponent>(look) && _random.Prob(.4f))
                    _stamina.TakeOvertimeStaminaDamage(look, 10f);

                if (_random.Prob(.4f))
                    _jitter.DoJitter(look, TimeSpan.FromSeconds(.5f), true, amplitude: 5, frequency: 10);

                if (_random.Prob(.25f))
                {
                    _statusEffect.TryAddStatusEffectDuration(look,
                        StatusEffectSeeingRainbow,
                        out _,
                        TimeSpan.FromSeconds(10f));
                }
            }
        }
    }
}
