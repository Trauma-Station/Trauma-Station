// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Medical.Shared.Traumas;

/// <summary>
/// Component given to bodyparts that have bones.
/// </summary>
[RegisterComponent, AutoGenerateComponentState(fieldDeltas: true), NetworkedComponent]
public sealed partial class BoneComponent : Component
{
    [DataField]
    public FixedPoint2 IntegrityCap = 60f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BoneIntegrity = 60f;

    [DataField, AutoNetworkedField]
    public BoneSeverity BoneSeverity = BoneSeverity.Normal;

    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundCollectionSpecifier("BoneGone");
}
