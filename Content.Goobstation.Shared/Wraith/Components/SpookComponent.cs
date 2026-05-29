// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.RadialSelector;

namespace Content.Goobstation.Shared.Wraith.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpookComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Actions = new();
}
