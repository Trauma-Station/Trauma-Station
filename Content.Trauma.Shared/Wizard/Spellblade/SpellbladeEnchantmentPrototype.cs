// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Wizard.Spellblade;

[Prototype]
public sealed partial class SpellbladeEnchantmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public string Desc = string.Empty;

    [DataField(required: true)]
    public object? Event;
}
