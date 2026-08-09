// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Shared.AER;
using Robust.Shared.Timing;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using System.Linq;
using Content.Shared.Eye.Blinding.Components;
using Content.Trauma.Shared.Viewcone.Components;
using Content.Trauma.Shared.Viewcone;

namespace Content.Trauma.Server.AER;

/// <summary>
/// system for Aer-169, lets them summon a restricted devil contract
/// TO DO: add id gear and research event for spawning player version of Magic Bullet
/// </summary>
public sealed partial class AerShyGuySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedVisionSystem _vision = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get the current server time.
        var curTime = _timing.CurTime;

        // Find all entities with the AnnoyingSoundComponent
        // and turn them into an enumerator so we can loop over them.
        // Note that EntityQueryEnumerator ignores paused entities,
        // for example those that are currently located in nullspace.
        // This means paused entities don't get updated.
        var query = EntityQueryEnumerator<AerShyGuyComponent>();

        // Loop over all shyguys. usually there should be only one shyguy since aers are supposed to be unique in a given round
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextCheck > curTime)
                continue;

            var ent = new Entity<AerShyGuyComponent>(uid, comp);
            List<EntityUid> beholders = ObserverCheck(ent);
            // add all who saw shy guy to his kill list
            foreach (EntityUid beholder in beholders)
            {
                comp.KillList.Add(beholder);
            }

            if (comp.KillList.Count() > 0)
            {
                //maybe stuff to unpacify shyguy
            }
            else
            {
                //maybe stuff to pacify shyguy
            }

            //plays the debug scream on shy guy position
            //maybe having moderate popup message spam on shyguy could be good
            if (beholders.Count() > 0)
                _audio.PlayPredicted(comp.Scream, uid, uid);

            // Now we update set the next update time.
            comp.NextCheck += comp.UpdateCooldown;
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// check if there are humanoid crew seeing shyguy and returns a list of all the valid entities seeing them
    /// stolen from slasher
    /// </summary>
    private List<EntityUid> ObserverCheck(Entity<AerShyGuyComponent> ent)
    {
        List<EntityUid> killList = [];
        var checkRange = ent.Comp.ObserverCheckRange;
        var uid = ent.Owner;
        foreach (var other in _lookup.GetEntitiesInRange(uid, checkRange))
        {
            //dont go apeshit if others
            if (other == uid
                || !HasComp<EyeComponent>(other) //dont have eyes
                || HasComp<GhostComponent>(other) //are ghosts
                || !HasComp<HumanoidProfileComponent>(other)//are not humanoids
                || _mobState.IsDead(other)//are dead
                || _mobState.IsCritical(other)// are dying
                || TryComp<BlindableComponent>(other, out var blind) && blind.IsBlind)//are blind
                continue;

            if (_interaction.InRangeUnobstructed(other, uid, checkRange, CollisionGroup.Opaque))
            {
                if (TryComp<ViewconeComponent>(other, out var cone))
                {
                    if (_vision.IsVisible((other, cone), _transform.GetWorldPosition(other), _transform.GetWorldPosition(ent.Owner)))
                        killList.Add(other);
                }
                else
                {
                    killList.Add(other);
                }
            }
        }
        return killList;
    }

    /// <summary>
    /// initialization of shy guys update timer
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<AerShyGuyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCheck = _timing.CurTime + ent.Comp.UpdateCooldown;
    }
}
