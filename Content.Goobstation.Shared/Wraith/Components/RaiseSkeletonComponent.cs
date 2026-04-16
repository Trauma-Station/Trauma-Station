// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Wraith.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaiseSkeletonComponent : Component
{
    [DataField]
    public EntProtoId SkeletonProto = "MobSkeletonGoon";
}
