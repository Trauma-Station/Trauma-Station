// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.SpaceImmunityOnBuckle;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SpaceImmunityOnBuckleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HadPressure;

    [DataField, AutoNetworkedField]
    public bool HadLowTemp;
}
