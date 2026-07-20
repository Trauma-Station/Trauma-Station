// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.RadialSelector;

namespace Content.Goobstation.Shared.SetSelector;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadialItemSelectorComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();
}
