// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Bible;
using Content.Goobstation.Shared.Wraith.Curses;
using Content.Shared.Popups;

namespace Content.Goobstation.Server.Wraith.Curses;

public sealed partial class CurseHolderSystem : SharedCurseHolderSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurseHolderComponent, BibleUsedEvent>(OnBibleSmite);
    }

    private void OnBibleSmite(Entity<CurseHolderComponent> ent, ref BibleUsedEvent args)
    {
        _popup.PopupEntity(Loc.GetString("curse-not-anymore"), ent.Owner, ent.Owner, PopupType.Medium);
        RemCompDeferred(ent, ent.Comp);
    }
}
