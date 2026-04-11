// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchInfluenceDrainerComponent : Component
{
    [DataField]
    public float Time = 8f;

    [DataField]
    public bool Hidden;

    [DataField]
    public Dictionary<int, ProtoId<StoreCategoryPrototype>> TierToCategory = new()
    {
        { 1, "HereticPathSideT1" },
        { 2, "HereticPathSideT2" },
        { 3, "HereticPathSideT3" },
    };
}
