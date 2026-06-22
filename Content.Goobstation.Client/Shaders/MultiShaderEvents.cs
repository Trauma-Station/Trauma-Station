// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Client.Shaders;

[ByRefEvent]
public readonly record struct BeforePostMultiShaderRenderEvent(
    ProtoId<ShaderPrototype> Shader,
    ShaderInstance Instance,
    SpriteComponent? Sprite,
    IClydeViewport Viewport);
