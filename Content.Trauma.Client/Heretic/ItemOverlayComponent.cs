// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Client.Heretic;

/// <summary>
/// Saves another entity sprite texture (by getting it via <see cref="GetItemOverlayEntityEvent"/>) into render target
/// Then renders this texture, passing it as ItemOverlay post shader parameter, also passes ratio uniform
/// </summary>
[RegisterComponent]
public sealed partial class ItemOverlayComponent : Component
{
    public IRenderTexture? RT;

    [DataField]
    public ProtoId<ShaderPrototype> Shader = "ItemOverlay";

    [DataField]
    public Vector2i RenderTargetSize = new(32, 32);

    [DataField]
    public Vector2 Ratio = Vector2.One;
}
