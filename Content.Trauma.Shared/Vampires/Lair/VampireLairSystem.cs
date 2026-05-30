// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Lair;

public sealed partial class VampireLairSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<DamageTypePrototype> Heat = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireLairComponent, DamageDealtEvent>(OnDamage);
    }

    private void OnDamage(Entity<VampireLairComponent> ent, ref DamageDealtEvent args)
    {
        // Only heat can damage this entity, and we don't want to notify the vampire for non-heat damage types cause its gonna spam
        if (ent.Comp.Vampire is not { } vamp|| !args.Damage.DamageDict.ContainsKey(Heat))
            return;

        // Random chance of getting the popup
        if (!SharedRandomExtensions.PredictedProb(_timing, 0.2f, GetNetEntity(ent)))
            return;

        if (_net.IsServer)
            _popup.PopupEntity("Your lair is being attacked!", vamp, vamp, PopupType.LargeCaution);
    }
}
