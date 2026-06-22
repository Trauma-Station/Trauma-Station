// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class StatusEffectsOnStatusRemoveComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, TimeSpan> StatusEffects;
}
