// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Actions;

namespace Content.Trauma.Shared.MartialArts;

public sealed partial class PerformMartialArtComboEvent : InstantActionEvent
{
    // This allows you to tell the system which specific button was pressed
    [DataField]
    public ProtoId<ComboPrototype> Combo;
}
