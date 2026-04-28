// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Server.ComponentsRegistry;

[RegisterComponent]
public sealed partial class GrantComponentsStatusEffectComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}
