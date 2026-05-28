// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Held;

public sealed partial class HeldGrantComponentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeldGrantComponentComponent, GotEquippedHandEvent>(OnCompEquip);
        SubscribeLocalEvent<HeldGrantComponentComponent, GotUnequippedHandEvent>(OnCompUnequip);
    }

    private void OnCompEquip(Entity<HeldGrantComponentComponent> ent, ref GotEquippedHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        ent.Comp.Active.Clear();
        var user = args.User;
        foreach (var name in ent.Comp.Components.Keys)
        {
            var type = Factory.GetRegistration(name).Type;
            if (!HasComp(user, type))
                ent.Comp.Active.Add(name);
        }
        EntityManager.AddComponents(user, ent.Comp.Components);
    }

    private void OnCompUnequip(Entity<HeldGrantComponentComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var user = args.User;
        foreach (var name in ent.Comp.Active)
        {
            var type = Factory.GetRegistration(name).Type;
            RemComp(user, type);
        }
        ent.Comp.Active.Clear();
    }
}
