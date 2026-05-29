// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.Wizard.EventSpells;

[RegisterComponent]
public sealed partial class GlobalTileMovementRuleComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
