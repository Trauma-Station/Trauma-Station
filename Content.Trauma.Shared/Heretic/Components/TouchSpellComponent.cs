// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TouchSpellComponent : Component
{
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    [DataField]
    public EntityWhitelist? TargetBlacklist;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField]
    public TimeSpan Cooldown;

    [DataField]
    public LocId? Speech;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool CanUseOnSelf;

    [DataField]
    public bool BypassNullrod;
}
