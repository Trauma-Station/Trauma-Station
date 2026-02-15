// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CardboardBox;

/// <summary>
/// Component added to boxes that are disabled from its opening being witnessed.
/// When removed it will try to re-enable stealth.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(JustABoxSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DisabledBoxComponent : Component
{
    /// <summary>
    /// When stealth can next be enabled.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextStealth;
}
