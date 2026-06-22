// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Shaders;

namespace Content.Goobstation.Client.Shaders;

[RegisterComponent]
public sealed partial class MultiShaderSpriteComponent : Component
{
    // shader protoId -> data
    [DataField]
    public Dictionary<string, MultiShaderData> PostShaders = new();

    // shader protoId -> instance
    public Dictionary<string, ShaderInstance> CurrentShaders = new();

    public IRenderTexture? RenderTarget;
}
