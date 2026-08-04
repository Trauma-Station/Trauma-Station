using Content.Shared.Actions;

namespace Content.Trauma.Shared.Magic.Demonologist.Events;

public sealed partial class BindApprenticeEvent : EntityTargetActionEvent
{
    [DataField]
    public Dictionary<string, EntProtoId> Gear = new()
    {
        {"outerClothing", "ClothingOuterRobesDemonologist"},
        {"jumpsuit", "ClothingUniformJumpsuitColorBlack"}, // TODO: add more once graves sprites more
    };

    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(5);
}
