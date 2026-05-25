// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Clothing.Systems;

public sealed partial class ClothingCoatingSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingCoatingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CoatedClothingComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(Entity<ClothingCoatingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target ||
            !TryComp<ClothingComponent>(target, out var clothing))
            return;

        EntityManager.AddComponents(target, ent.Comp.Components, false);
        if (TryComp<ToggleableClothingComponent>(target, out var toggleable))
        {
            // apply it to modsuit parts etc as well
            foreach (var part in toggleable.ClothingUids.Keys)
            {
                EntityManager.AddComponents(part, ent.Comp.Components, false);
            }
        }

        var coated = EnsureComp<CoatedClothingComponent>(target);
        coated.CoatingNames.Add(ent.Comp.CoatingName);
        _popup.PopupEntity(Loc.GetString("clothing-coating-success", ("target", target), ("source", ent)), target);
        Dirty(target, coated);

        QueueDel(ent);
        args.Handled = true;
    }

    private void OnExamined(Entity<CoatedClothingComponent> ent, ref ExaminedEvent args)
    {
        var names = string.Join(", ", ent.Comp.CoatingNames);
        args.PushMarkup(Loc.GetString("clothing-coating-inspect", ("coatings", names)));
    }
}
