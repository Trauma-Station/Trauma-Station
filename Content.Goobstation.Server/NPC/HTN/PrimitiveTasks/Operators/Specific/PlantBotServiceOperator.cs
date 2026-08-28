// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Silicon.Bots;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chat;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PlantbotServiceOperator : HTNOperator
{
    [Dependency] private IEntityManager _ent = default!;

    private ChatSystem _chat = default!;
    private SharedAudioSystem _audio = default!;
    private SharedInteractionSystem _interaction = default!;
    private SharedPopupSystem _popup = default!;
    private PlantHarvestSystem _harvest = default!;
    private PlantHolderSystem _holder = default!;
    private PlantTraySystem _tray = default!;

    public const float RequiredWaterLevelToService = 80f;
    public const float RequiredWeedsAmountToWeed = 1f;
    public const float WaterTransferAmount = 10f;
    public const float WeedsRemovedAmount = 1f;

    /// <summary>
    /// Target tray to service.
    /// </summary>
    public const string TargetKey = "PlantTarget";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _popup = sysManager.GetEntitySystem<SharedPopupSystem>();
        _harvest = sysManager.GetEntitySystem<PlantHarvestSystem>();
        _holder = sysManager.GetEntitySystem<PlantHolderSystem>();
        _tray = sysManager.GetEntitySystem<PlantTraySystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _ent) || _ent.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_ent.TryGetComponent<PlantbotComponent>(owner, out var botComp)
            || !_ent.TryGetComponent<PlantTrayComponent>(target, out var tray)
            || !_interaction.InRangeUnobstructed(owner, target))
            return HTNOperatorStatus.Failed;

        if (botComp.IsEmagged)
        {
            _tray.AdjustWater((target, tray), -WaterTransferAmount);
            _audio.PlayPvs(botComp.RemoveWaterSound, target);
        }
        else
        {
            if (tray.WaterLevel <= RequiredWaterLevelToService)
            {
                _tray.AdjustWater((target, tray), 10);
                _audio.PlayPvs(botComp.WaterSound, target);
                _chat.TrySendInGameICMessage(owner, Loc.GetString("plantbot-add-water"), InGameICChatType.Speak, hideChat: true, hideLog: true);
            }
            else if (tray.WeedLevel >= RequiredWeedsAmountToWeed)
            {
                _tray.AdjustWeed((target, tray), -WeedsRemovedAmount);
                _audio.PlayPvs(botComp.WeedSound, target);
                _chat.TrySendInGameICMessage(owner, Loc.GetString("plantbot-remove-weeds"), InGameICChatType.Speak, hideChat: true, hideLog: true);
            }
            else if (tray.PlantEntity is { } plant && _ent.TryGetComponent<PlantHolderComponent>(plant, out var holder) && holder.ReadyForHarvest)
            {
                _harvest.DoHarvest((target, holder), owner);
                _chat.TrySendInGameICMessage(owner, Loc.GetString("plantbot-harvest"), InGameICChatType.Speak, hideChat: true, hideLog: true);
            }
            else
                return HTNOperatorStatus.Failed;
        }

        return HTNOperatorStatus.Finished;
    }
}
