// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Clothing.Systems;

public sealed partial class ClothingCoatingSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
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

        PredictedQueueDel(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<CoatedClothingComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.CoatingNames.Count == 0)
            return;

        StringBuilder sb = new();
        foreach (var coating in ent.Comp.CoatingNames)
        {
            sb.Append($"{Loc.GetString(coating)}, ");
        }

        sb.Remove(sb.Length - 2, 2);

        args.PushMarkup(Loc.GetString("clothing-coating-inspect", ("coatings", sb.ToString())));
    }
}
