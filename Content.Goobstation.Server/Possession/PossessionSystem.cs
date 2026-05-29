// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Magic;
using Content.Goobstation.Shared.Possession;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Goobstation.Server.Possession;

public sealed partial class PossessionSystem : SharedPossessionSystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private FollowerSystem _follower = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PossessedComponent, EndPossessionEarlyEvent>(OnEarlyEnd);
        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeMindPossessEvent>(OnPossessPolymorph);
    }

    private void OnEarlyEnd(EntityUid uid, PossessedComponent comp, ref EndPossessionEarlyEvent args)
    {
        if (args.Handled)
            return;

        // if polymorphed, undo
        _polymorph.Revert(uid);
        RemCompDeferred(uid, comp);

        args.Handled = true;
    }

    protected override void PossessionEnded(Entity<PossessedComponent> possessed)
    {
        if (possessed.Comp.PolymorphEntity)
            _polymorph.Revert(possessed.Owner);

        // Remove associated components.
        if (!possessed.Comp.WasPacified)
            RemComp<PacifiedComponent>(possessed.Comp.OriginalEntity);

        var ev = new UnholyStatusChangedEvent(possessed, possessed, false);
        RaiseLocalEvent(possessed, ref ev);

        // Transfer followers from possessed entity to possessor's original entity
        // TODO: polymorph revert should handle that...
        if (!TerminatingOrDeleted(possessed.Comp.PossessorOriginalEntity))
        {
            UpdateFollowersToNewEntity(possessed.Owner, possessed.Comp.PossessorOriginalEntity);
        }

        // Return the possessors mind to their body, and the target to theirs.
        if (!TerminatingOrDeleted(possessed.Comp.PossessorMindId))
            _mind.TransferTo(possessed.Comp.PossessorMindId, possessed.Comp.PossessorOriginalEntity);
        if (!TerminatingOrDeleted(possessed.Comp.OriginalMindId))
            _mind.TransferTo(possessed.Comp.OriginalMindId, possessed.Comp.OriginalEntity);

        MapCoordinates? coordinates = null;
        if (!TerminatingOrDeleted(possessed.Comp.OriginalEntity))
            coordinates = _transform.ToMapCoordinates(possessed.Comp.OriginalEntity.ToCoordinates());

        // Teleport to the entity, kinda like you're popping out of their head!
        if (!TerminatingOrDeleted(possessed.Comp.PossessorOriginalEntity) && coordinates is not null)
            _transform.SetMapCoordinates(possessed.Comp.PossessorOriginalEntity, coordinates.Value);

        _container.CleanContainer(possessed.Comp.PossessedContainer);
    }

    /// <summary>
    /// Attempts to temporarily possess a target.
    /// </summary>
    /// <param name="possessed">The entity being possessed.</param>
    /// <param name="possessor">The entity possessing the previous entity.</param>
    /// <param name="possessionDuration">How long does the possession last in seconds.</param>
    /// <param name="pacifyPossessed">Should the possessor be pacified while inside the possessed body?</param>
    /// <param name="doesMindshieldBlock">Does having a mindshield block being possessed?</param>
    /// <param name="doesChaplainBlock">Is the chaplain immune to this possession?</param>
    /// <param name="HideActions">Should all actions be hidden during?</param>
    public bool TryPossessTarget(EntityUid possessed, EntityUid possessor, TimeSpan possessionDuration, bool pacifyPossessed, bool doesMindshieldBlock = false, bool doesChaplainBlock = true, bool hideActions = true, bool polymorphPossessor = true, bool doesImmuneBlock = true)
    {
        var swapEv = new BeforeMindSwappedEvent();
        RaiseLocalEvent(possessed, ref swapEv);

        var possessEv = new BeforeMindPossessEvent(doesImmuneBlock, doesMindshieldBlock, doesChaplainBlock);
        RaiseLocalEvent(possessed, ref swapEv);

        if (swapEv.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString($"possession-fail-target-{swapEv.Message}"), possessor, possessor);
            return false;
        }

        if (possessEv.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString($"possession-fail-target-{possessEv.Message}"), possessor, possessor);
            return false;
        }

        if (!_mind.TryGetMind(possessor, out var possessorMind, out _))
            return false;

        DoPossess(possessed, possessor, possessionDuration, possessorMind, pacifyPossessed, hideActions, polymorphPossessor);
        return true;
    }

    private void DoPossess(EntityUid? possessedNullable, EntityUid possessor, TimeSpan possessionDuration, EntityUid possessorMind, bool pacifyPossessed, bool hideActions, bool polymorphPossessor)
    {
        if (possessedNullable is not { } possessed)
            return;

        var possessedComp = EnsureComp<PossessedComponent>(possessed);
        possessedComp.HideActions = hideActions;

        if (pacifyPossessed)
        {
            if (!HasComp<PacifiedComponent>(possessed))
                EnsureComp<PacifiedComponent>(possessed);
            else
                possessedComp.WasPacified = true;
        }

        possessedComp.PolymorphEntity = polymorphPossessor;

        EntityUid currentFollowedEntity = possessor;

        if (polymorphPossessor)
        {
            var polymorphedEntity = _polymorph.PolymorphEntity(possessor, possessedComp.Polymorph);

            if (polymorphedEntity != null && !TerminatingOrDeleted(polymorphedEntity.Value))
            {
                UpdateFollowersToNewEntity(possessor, polymorphedEntity.Value);
                currentFollowedEntity = polymorphedEntity.Value;
            }
        }

        // Get the possession time.
        possessedComp.PossessionEndTime = _timing.CurTime + possessionDuration;
        Dirty(possessed, possessedComp);

        // Store possessors original information.
        possessedComp.PossessorOriginalEntity = possessor;
        possessedComp.PossessorMindId = possessorMind;

        // Store possessed original info
        possessedComp.OriginalEntity = possessed;

        if (_mind.TryGetMind(possessed, out var possessedMind, out _))
        {
            possessedComp.OriginalMindId = possessedMind;

            // Nobodies gonna know.
            var dummy = Spawn("FoodSnackLollypop", MapCoordinates.Nullspace);
            _container.Insert(dummy, possessedComp.PossessedContainer);

            _mind.TransferTo(possessedMind, dummy);
        }

        // Transfer into target
        _mind.TransferTo(possessorMind, possessed);

        // After the mind transfer, ghosts should follow the possessed entity (where the mind now is)
        if (!TerminatingOrDeleted(currentFollowedEntity))
        {
            UpdateFollowersToNewEntity(currentFollowedEntity, possessed);
        }

        // SFX
        _popup.PopupEntity(Loc.GetString("possession-popup-self"), possessedMind, possessedMind, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("possession-popup-others", ("target", possessed)), possessed, PopupType.MediumCaution);
        _audio.PlayPvs(possessedComp.PossessionSound, possessed);

        Log.Info($"{ToPrettyString(possessor)} possessed {ToPrettyString(possessed)}");
        _admin.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(possessor)} possessed {ToPrettyString(possessed)}");
    }

    private void UpdateFollowersToNewEntity(EntityUid oldEntity, EntityUid newEntity)
    {
        if (!TryComp<FollowedComponent>(oldEntity, out var followed))
            return;

        var followers = new List<EntityUid>(followed.Following);

        foreach (var follower in followers)
        {
            if (HasComp<GhostComponent>(follower))
            {
                _follower.StartFollowingEntity(follower, newEntity);
            }
        }
    }

    // Can't possess polymorphed entities. Sends you straight to the shadow realm if you do.
    private void OnPossessPolymorph(Entity<PolymorphedEntityComponent> ent, ref BeforeMindPossessEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "polymorphed";
        args.Cancelled = true;
    }
}
