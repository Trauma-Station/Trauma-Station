// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Viewcone.Components;

/// <summary>
/// Intended to be used on inventory items, mutations or status effects (i.e. this is relayed).
/// Modifies the viewcone angle of the relevant entity multiplicatively.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ViewconeModifierComponent : Component
{
    [DataField(required: true)]
    public float AngleModifier = 1f;
}
