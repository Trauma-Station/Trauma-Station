// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Held;

[RegisterComponent]
public sealed partial class HeldGrantComponentComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    [DataField]
    public HashSet<string> Active = new();
}
