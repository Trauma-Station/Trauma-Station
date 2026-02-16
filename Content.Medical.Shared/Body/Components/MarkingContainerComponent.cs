// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

// TODO NUBODY: kill this shit
// This is a uh, very shitty copout to not wanting to modify the prototypes for felinids, and entities at large so they have ears.
// I will do that at some point, for now I just want the funny surgery to work lol.
[RegisterComponent, NetworkedComponent]
public sealed partial class MarkingContainerComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;
}
