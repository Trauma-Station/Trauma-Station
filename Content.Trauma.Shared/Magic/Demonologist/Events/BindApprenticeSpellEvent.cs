// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Magic.Demonologist.Events;

public sealed partial class BindApprenticeSpellEvent : EntityTargetActionEvent
{
    [DataField]
    public Dictionary<string, EntProtoId> Gear = new()
    {
        {"outerClothing", "ClothingOuterRobesDemonologist"},
        {"jumpsuit", "ClothingUniformJumpsuitColorBlack"}, // TODO: add more once graves sprites more
        {"mask", "ClothingMaskDemonologist"}
    };

    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(5);
}
