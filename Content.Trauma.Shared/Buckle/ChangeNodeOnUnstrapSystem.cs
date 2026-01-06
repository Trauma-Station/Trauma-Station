// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Buckle.Components;
using Content.Shared.Construction;

namespace Content.Trauma.Shared.Buckle;

public sealed class ChangeNodeOnUnstrapSystem : EntitySystem
{
    [Dependency] private readonly SharedConstructionSystem _construction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeNodeOnUnstrapComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnUnstrapped(Entity<ChangeNodeOnUnstrapComponent> ent, ref UnstrappedEvent args)
    {
        _construction.ChangeNode(ent.Owner, null, ent.Comp.Node);
    }
}
