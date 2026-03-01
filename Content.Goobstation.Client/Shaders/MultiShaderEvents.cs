// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Shaders;

[ByRefEvent]
public readonly record struct BeforePostMultiShaderRenderEvent(
    ProtoId<ShaderPrototype> Shader,
    ShaderInstance Instance,
    SpriteComponent? Sprite,
    IClydeViewport Viewport);
