// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Silicon.Bots;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using System.Linq;
using Content.Shared.Repairable;

namespace Content.Goobstation.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class WeldbotWeldOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private ChatSystem _chat = default!;
    private WeldbotSystem _weldbot = default!;
    private SharedAudioSystem _audio = default!;
    private SharedInteractionSystem _interaction = default!;
    private SharedPopupSystem _popup = default!;
    private DamageableSystem _damageable = default!;

    /// <summary>
    /// Target entity to inject.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _weldbot = sysManager.GetEntitySystem<WeldbotSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _popup = sysManager.GetEntitySystem<SharedPopupSystem>();
        _damageable = sysManager.GetEntitySystem<DamageableSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<RepairableComponent>(target, out var repairComp)
            || !_entMan.TryGetComponent<WeldbotComponent>(owner, out var botComp)
            || !_entMan.TryGetComponent<DamageableComponent>(target, out var damageable)
            || !_interaction.InRangeUnobstructed(owner, target))
            return HTNOperatorStatus.Failed;

        var damage = _damageable.GetAllDamage((target, damageable));
        var emagged = _entMan.HasComponent<EmaggedComponent>(owner);
        if (!emagged && damage.DamageDict.Keys.Intersect(botComp.DamageAmount.DamageDict.Keys).All(key => damage.DamageDict[key] == 0))
            return HTNOperatorStatus.Failed; // nothing to heal

        var dealt = botComp.IsEmagged ? -botComp.DamageAmount : botComp.DamageAmount;
        _damageable.ChangeDamage((target, damageable), dealt, true, false, origin: owner);

        _audio.PlayPvs(botComp.WeldSound, target);

        if (damage.DamageDict.Keys.Intersect(botComp.DamageAmount.DamageDict.Keys).All(key => damage.DamageDict[key] == 0)) // only say "all done!" if we're actually done
            _chat.TrySendInGameICMessage(owner, Loc.GetString("weldbot-finish-weld"), InGameICChatType.Speak, hideChat: true, hideLog: true);

        return HTNOperatorStatus.Finished;
    }
}
