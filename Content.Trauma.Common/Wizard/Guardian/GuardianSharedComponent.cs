// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Wizard.Guardian;

// I hate server components I hate server components I hate server components
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GuardianSharedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Host;
}
