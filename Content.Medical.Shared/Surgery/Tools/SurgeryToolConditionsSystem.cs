// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Smoking;
using Content.Shared.Smoking.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Medical.Shared.Surgery.Tools;

/// <summary>
///  Prevents using esword or welder when off, laser when no charges.
/// </summary>
public sealed class SurgeryToolConditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, SurgeryToolUsedEvent>(OnToggleUsed);
        SubscribeLocalEvent<GunComponent, SurgeryToolUsedEvent>(OnGunUsed);
        SubscribeLocalEvent<MatchstickComponent, SurgeryToolUsedEvent>(OnMatchUsed);
        SubscribeLocalEvent<SmokableComponent, SurgeryToolUsedEvent>(OnSmokableUsed);
    }

    private void OnToggleUsed(Entity<ItemToggleComponent> ent, ref SurgeryToolUsedEvent args)
    {
        if (ent.Comp.Activated || args.IgnoreToggle)
            return;

        _popup.PopupClient(Loc.GetString("surgery-tool-turn-on"), ent, args.User);
        args.Cancelled = true;
    }

    private void OnGunUsed(Entity<GunComponent> ent, ref SurgeryToolUsedEvent args)
    {
        var coords = Transform(args.User).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), coords, args.User);
        if (ev.Ammo.Count > 0)
            return;

        _popup.PopupClient(Loc.GetString("surgery-tool-reload"), ent, args.User);
        args.Cancelled = true;
    }

    private void OnMatchUsed(Entity<MatchstickComponent> ent, ref SurgeryToolUsedEvent args)
    {
        SmokableUsed(ent, ent.Comp.CurrentState, ref args);
    }

    private void OnSmokableUsed(Entity<SmokableComponent> ent, ref SurgeryToolUsedEvent args)
    {
        SmokableUsed(ent, ent.Comp.State, ref args);
    }

    private void SmokableUsed(EntityUid uid, SmokableState state, ref SurgeryToolUsedEvent args)
    {
        if (state == SmokableState.Lit)
            return;

        var key = "surgery-tool-match-" + (state == SmokableState.Burnt ? "replace" : "light");
        _popup.PopupClient(Loc.GetString(key), uid, args.User);
        args.Cancelled = true;
    }
}
