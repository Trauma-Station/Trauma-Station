// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Implants;
using Content.Shared.Administration.Systems;
using Content.Shared.Jittering;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Implants;

public sealed class NaniteMenderImplantSystem : EntitySystem
{
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NaniteMendEvent>(OnNaniteMend);
    }

    private void OnNaniteMend(NaniteMendEvent args)
    {
        var popup = Loc.GetString("nanite-mend-popup");
        _popup.PopupEntity(popup, args.Target, args.Target, PopupType.Medium);

        _jittering.AddJitter(args.Target);
        _rejuvenate.PerformRejuvenate(args.Target);
        args.Handled = true;
    }
}
