// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class ClothingGrantTagComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;

    [DataField]
    public bool IsActive;
}
