// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;
using Robust.Shared.Serialization.Manager;

namespace Content.Goobstation.Shared.Held;

public sealed class HeldGrantComponentSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeldGrantComponentComponent, GotEquippedHandEvent>(OnCompEquip);
        SubscribeLocalEvent<HeldGrantComponentComponent, GotUnequippedHandEvent>(OnCompUnequip);
    }

    private void OnCompEquip(Entity<HeldGrantComponentComponent> ent, ref GotEquippedHandEvent args)
    {
        foreach (var (name, data) in ent.Comp.Components)
        {
            var newComp = (Component) Factory.GetComponent(name);
            if (HasComp(args.User, newComp.GetType()))
                continue;

            object? temp = newComp;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(args.User, (Component)temp!);

            ent.Comp.Active[name] = true;
        }
    }

    private void OnCompUnequip(Entity<HeldGrantComponentComponent> ent, ref GotUnequippedHandEvent args)
    {
        // Goobstation
        //if (!component.IsActive) return;

        foreach (var (name, data) in ent.Comp.Components)
        {
            // Goobstation
            if (!ent.Comp.Active.ContainsKey(name) || !ent.Comp.Active[name])
                continue;

            var newComp = (Component) Factory.GetComponent(name);

            RemComp(args.User, newComp.GetType());
            ent.Comp.Active[name] = false;
        }
    }
}
