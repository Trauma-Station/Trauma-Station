// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LastRefugeComponent : Component
{
    [DataField]
    public float Visibility = 0.3f;

    [DataField]
    public LocId ExamineMessage = "heretic-last-refuge-examine-message";

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    [DataField]
    public EntProtoId<HereticCloakedStatusEffectComponent> Status = "LastRefugeStatusEffect";

    [DataField, AutoNetworkedField]
    public bool HadStealth;

    [DataField, AutoNetworkedField]
    public bool HadGodmode;

    [DataField, AutoNetworkedField]
    public bool HadSlowdownImmunity;

    [DataField, AutoNetworkedField]
    public bool HadStrippable;
}
