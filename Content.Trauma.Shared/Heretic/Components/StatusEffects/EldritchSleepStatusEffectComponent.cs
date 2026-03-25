// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchSleepStatusEffectComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry ComponentsToAdd = new();

    [DataField]
    public ComponentRegistry ComponentDifference = new();
}
