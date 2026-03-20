// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Prototypes;

[Prototype]
public sealed partial class ComponentRegistryPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
