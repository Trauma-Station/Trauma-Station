// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.StatusEffects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CounterStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Count;
}
