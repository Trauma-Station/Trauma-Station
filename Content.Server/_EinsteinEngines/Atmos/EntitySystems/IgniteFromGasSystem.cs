// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Skubman <ba.fallaria@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server._EinsteinEngines.Atmos.Components;
using Content.Server.Cloning.Components;
using Content.Shared.Bed.Components;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._EinsteinEngines.Atmos.EntitySystems;

public sealed class IgniteFromGasSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    // All ignitions tick at the same time because FlammableSystem is also the same
    private const float UpdateTimer = 1f;
    private float _timer;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlammableComponent, OrganInsertedIntoEvent>(OnOrganInsertedInto);
        SubscribeLocalEvent<IgniteFromGasComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);

        SubscribeLocalEvent<IgniteFromGasImmunityComponent, GotEquippedEvent>(OnIgniteFromGasImmunityEquipped);
        SubscribeLocalEvent<IgniteFromGasImmunityComponent, GotUnequippedEvent>(OnIgniteFromGasImmunityUnequipped);
    }

    private void OnOrganInsertedInto(Entity<FlammableComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (!TryComp<IgniteFromGasPartComponent>(args.Organ, out var ignitePart) ||
            args.Organ.Comp.Category is not {} category)
            return;

        var ignite = EnsureComp<IgniteFromGasComponent>(ent);
        ignite.Gas = ignitePart.Gas;
        ignite.IgnitableBodyParts[category] = ignitePart.FireStacks;

        UpdateIgniteImmunity((ent, ignite));
    }

    private void OnOrganRemovedFrom(Entity<IgniteFromGasComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (!HasComp<IgniteFromGasPartComponent>(args.Organ) || args.Organ.Comp.Category is not {} category)
            return;

        ent.Comp.IgnitableBodyParts.Remove(category);

        if (ent.Comp.IgnitableBodyParts.Count == 0)
            RemCompDeferred(ent, ent.Comp);
        else
            UpdateIgniteImmunity((ent, ent.Comp));
    }

    private void OnIgniteFromGasImmunityEquipped(Entity<IgniteFromGasImmunityComponent> ent, ref GotEquippedEvent args) =>
        UpdateIgniteImmunity(args.Equipee);
    private void OnIgniteFromGasImmunityUnequipped(Entity<IgniteFromGasImmunityComponent> ent, ref GotUnequippedEvent args) =>
        UpdateIgniteImmunity(args.Equipee);

    public void UpdateIgniteImmunity(Entity<IgniteFromGasComponent?, InventoryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var exposedBodyParts = new Dictionary<ProtoId<OrganCategoryPrototype>, float>(ent.Comp1.IgnitableBodyParts);

        var slots = _inventory.GetSlotEnumerator((ent, ent.Comp2));
        while (slots.NextItem(out var item, out _))
        {
            if (!TryComp<IgniteFromGasImmunityComponent>(item, out var immunity))
                continue;

            foreach (var immunePart in immunity.Parts)
                exposedBodyParts.Remove(immunePart);
        }

        if (exposedBodyParts.Count == 0)
        {
            ent.Comp1.FireStacksPerUpdate = 0;
            return;
        }

        ent.Comp1.FireStacksPerUpdate = ent.Comp1.BaseFireStacksPerUpdate + exposedBodyParts.Values.Sum();
    }

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        if (_timer < UpdateTimer)
            return;
        _timer -= UpdateTimer;

        var enumerator = EntityQueryEnumerator<IgniteFromGasComponent, MobStateComponent, FlammableComponent>();
        while (enumerator.MoveNext(out var uid, out var ignite, out var mobState, out var flammable))
        {
            if (ignite.FireStacksPerUpdate == 0 ||
                mobState.CurrentState is MobState.Dead ||
                HasComp<BeingClonedComponent>(uid) ||
                HasComp<StasisBedBuckledComponent>(uid) ||
                _atmos.GetContainingMixture(uid, excite: true) is not { } gas ||
                gas[(int) ignite.Gas] < ignite.MolesToIgnite
                )
                continue;

            _flammable.AdjustFireStacks(uid, ignite.FireStacksPerUpdate, flammable, true, 10f);
        }
    }
}
