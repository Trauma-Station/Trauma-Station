using Content.Shared.Inventory;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.RadioIconsEvents;

/// <summary>
/// Goob - Raised whenever a radio message is sent, contains the job icon and name of the sender.
/// </summary>
[ByRefEvent]
public record struct TransformSpeakerJobIconEvent(EntityUid Sender, ProtoId<JobIconPrototype> JobIcon, string? JobName) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
