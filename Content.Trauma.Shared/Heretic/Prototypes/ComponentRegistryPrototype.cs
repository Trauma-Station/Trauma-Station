// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Prototypes;

[Prototype]
public sealed partial class ComponentRegistryPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ComponentRegistry Components = default!;
}
