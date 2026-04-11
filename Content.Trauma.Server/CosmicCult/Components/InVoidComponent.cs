// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Server.CosmicCult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class InVoidComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan ExitVoidTime = default!;

    [DataField]
    public EntityUid OriginalBody;
}
