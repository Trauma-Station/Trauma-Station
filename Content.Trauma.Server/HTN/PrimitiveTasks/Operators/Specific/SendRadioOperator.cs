// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Trauma.Server.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class SendRadioOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private RadioSystem _radio = default!;
    private NavMapSystem _navMap = default!;
    private SharedAudioSystem _audio = default!;

    [DataField(required: true)]
    public LocId Message;

    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype> RadioChannel = default!;

    [DataField]
    public string Key = string.Empty;

    [DataField]
    public bool KeyIsEntity = true;

    /// <summary>
    /// The sound to play when target arrested
    /// </summary>
    [DataField(required: true)]
    public SoundCollectionSpecifier TargetArrestedSound;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _radio = sysManager.GetEntitySystem<RadioSystem>();
        _navMap = sysManager.GetEntitySystem<NavMapSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var message = string.Empty;

        if (KeyIsEntity)
        {
            if (!blackboard.TryGetValue<EntityUid>(Key, out var value, _entManager) || _entManager.Deleted(value))
                return HTNOperatorStatus.Failed;

            var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((value, _entManager.GetComponent<TransformComponent>(value))));

            message = Loc.GetString(Message, ("entity", Identity.Entity(value, _entManager)), ("location", location));
        }
        else
        {
            if (!blackboard.TryGetValue<object>(Key, out var value, _entManager))
                return HTNOperatorStatus.Failed;

            message = Loc.GetString(Message, ("key", value));
        }

        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        _radio.SendRadioMessage(speaker, message, RadioChannel, speaker, escapeMarkup: false);

        var targetFoundSound = _audio.ResolveSound(TargetArrestedSound);
        _audio.PlayPvs(targetFoundSound, speaker);

        return HTNOperatorStatus.Finished;
    }
}
