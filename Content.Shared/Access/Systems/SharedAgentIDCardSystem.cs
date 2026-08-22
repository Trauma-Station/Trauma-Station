// <Trauma>
using Content.Trauma.Common.NanoChat;
// </Trauma>
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Content.Shared.VoiceMask;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems;

/// <summary>
/// Handles things related to the agent ID, such as copying access and the UI.
/// </summary>
public abstract partial class SharedAgentIdCardSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private CommonNanoChatSystem _nanoChat = default!;
    // </Trauma>
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedIdCardSystem _card = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private SharedJobStatusSystem _jobStatus = default!;

    /// <summary>
    /// Steals access from interacted ids.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<AgentIDCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || _lock.IsLocked(ent.Owner) ||
            !TryComp<AccessComponent>(args.Target, out var targetAccess) || !HasComp<IdCardComponent>(args.Target))
            return;

        // Am I an id?
        if (!TryComp<AccessComponent>(ent, out var access) || !HasComp<IdCardComponent>(ent))
            return;

        var beforeLength = access.Tags.Count;
        access.Tags.UnionWith(targetAccess.Tags);
        var addedLength = access.Tags.Count - beforeLength;

        // <Trauma> - Copy NanoChat data if available
        if (TryComp<NanoChatCardComponent>(args.Target, out var targetNanoChat) &&
            TryComp<NanoChatCardComponent>(ent, out var agentNanoChat))
        {
            var src = (args.Target.Value, targetNanoChat);
            var dest = (ent, agentNanoChat);
            // First clear existing data
            _nanoChat.Clear(dest);

            // Copy the number
            if (_nanoChat.GetNumber(src) is { } number)
                _nanoChat.SetNumber(dest, number);

            // Copy all recipients and their messages
            foreach (var (recipientNumber, recipient) in _nanoChat.GetRecipients(src))
            {
                _nanoChat.SetRecipient(dest, recipientNumber, recipient);

                if (_nanoChat.GetMessagesForRecipient(src, recipientNumber) is not { } messages)
                    continue;

                foreach (var message in messages)
                {
                    _nanoChat.AddMessage(dest, recipientNumber, message);
                }
            }
        }
        // </Trauma>

        _popup.PopupPredicted(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)),
            args.Target.Value,
            args.User);
        if (addedLength > 0)
            Dirty(ent, access);
    }

    [SubscribeLocalEvent]
    private void OnVoiceMaskNameChanged(Entity<AgentIDCardComponent> ent,
        ref InventoryRelayedEvent<VoiceMaskNameUpdatedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCard))
            return;

        if (!args.Args.VoiceMask.Comp.ChangeIDName)
            return;

        _card.TryChangeFullName(ent, args.Args.NewName, idCard);
    }

    [SubscribeLocalEvent]
    private void OnNameChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNameChangedMessage args)
    {
        if (!_card.TryChangeFullName(ent, args.Name))
            return;

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnJobChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobChangedMessage args)
    {
        if (!_card.TryChangeJobTitle(ent, args.Job))
            return;

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnJobIconChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardJobIconChangedMessage args)
    {
        if (!ProtoMan.Resolve(args.JobIconId, out var jobIcon) ||
            !_card.TryChangeJobIcon(ent, jobIcon))
            return;

        if (_job.TryGetJobFromIcon(jobIcon.ID, out var job))
            _card.TryChangeJobDepartment(ent, job);

        _jobStatus.UpdateIdHolderStatus(ent);
        UpdateUi(ent);
    }

    /// <summary>
    /// Update the agent id UI with new component info.
    /// </summary>
    public virtual void UpdateUi(EntityUid entity)
    {
        // Overridden on client
    }
}

/// <summary>
/// Key representing which bound user interface is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>
[Serializable, NetSerializable]
public enum AgentIDCardUiKey : byte
{
    Key,
}

/// <summary>
/// Sent from the agent ID UI to change the card name.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardNameChangedMessage(string name) : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

/// <summary>
/// Sent from the agent ID UI to change the job title.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobChangedMessage(string job) : BoundUserInterfaceMessage
{
    public string Job { get; } = job;
}

/// <summary>
/// Sent from the agent ID UI to change the job icon.
/// </summary>
[Serializable, NetSerializable]
public sealed class AgentIDCardJobIconChangedMessage(ProtoId<JobIconPrototype> icon) : BoundUserInterfaceMessage
{
    public ProtoId<JobIconPrototype> JobIconId { get; } = icon;
}
