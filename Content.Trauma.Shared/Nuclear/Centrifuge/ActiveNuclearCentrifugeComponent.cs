// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Nuclear.Centrifuge;

/// <summary>
/// Component added while a nuclear centrifuge is working.
/// Automatically added/removed depending when power state changes.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NuclearCentrifugeSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveNuclearCentrifugeComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextExtract;

    /// <summary>
    /// Sound played while the centrifuge is processing
    /// </summary>
    [DataField]
    public SoundPathSpecifier SoundProcess = new("/Audio/Machines/spinning.ogg")
    {
        Params = new()
        {
            Loop = true,
            Volume = -2f
        }
    };

    [DataField, AutoNetworkedField]
    public EntityUid? AudioProcess;
}
