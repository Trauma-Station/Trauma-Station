// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Client.Emoting;

[RegisterComponent]
public sealed partial class ShowSpriteLayerStatusEffectComponent : Component
{
    [DataField(required: true)]
    public Enum Layer;

    [DataField]
    public bool SetVisible = true;
}
