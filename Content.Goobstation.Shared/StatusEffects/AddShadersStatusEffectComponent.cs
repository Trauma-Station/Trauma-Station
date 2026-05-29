// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Shaders;

namespace Content.Goobstation.Shared.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class AddShadersStatusEffectComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, MultiShaderData> PostShaders;
}
