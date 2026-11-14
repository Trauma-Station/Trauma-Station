// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Chaplain.ItemBless;

[RegisterComponent]
public sealed partial class BlessableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId BlessResult;
    // TODO: Add additional fields for BibleUser or minimum MagLit
}
