using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

public sealed partial class PerformMartialArtComboEvent : InstantActionEvent
{
    // This allows you to tell the system which specific button was pressed
    [DataField]
    public ProtoId<ComboPrototype>? Combo;
}
