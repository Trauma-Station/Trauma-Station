// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Weapons;

[RegisterComponent, NetworkedComponent]
public sealed partial class InjectOnHitComponent : Component
{
    [DataField(required: true)]
    public List<ReagentQuantity> Reagents;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField("limit")]
    public FixedPoint2? ReagentLimit;

    [DataField]
    public bool NeedsRestrain;

    [DataField]
    public int InjectionDelay = 10000;
}
