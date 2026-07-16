using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Trauma.Shared.AER;

public sealed partial class AerMobActiveSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AerMobActiveComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AerMobActiveComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// handling the aer active state on map init
    /// </summary>
    private void OnMapInit(Entity<AerMobActiveComponent> aerMob, ref MapInitEvent args)
    {
        if (TryComp<MobStateComponent>(aerMob.Owner, out var mobComponent))
        {
            bool active = MobStateToActiveEvent(mobComponent.CurrentState);

            var activeEvent = new AerUpdateActiveStatusEvent(aerMob.Owner, active);
            RaiseLocalEvent(aerMob.Owner, ref activeEvent);
        }
    }

    /// <summary>
    /// handling of the aer active status for mobs it determines if aer is healty enough to produce rd points
    /// </summary>
    private void OnMobStateChanged(Entity<AerMobActiveComponent> ent, ref MobStateChangedEvent args)
    {
        bool active = MobStateToActiveEvent(args.NewMobState);

        var activeEvent = new AerUpdateActiveStatusEvent(ent.Owner, active);
        RaiseLocalEvent(ent.Owner, ref activeEvent);
    }

    /// <summary>
    /// helper function returns the active flag value correspondent to state value 
    /// </summary>
    private bool MobStateToActiveEvent(MobState state)
    {
        switch (state)
        {
            case MobState.Dead:
                return false;
            case MobState.Critical:
                return true;
            case MobState.SoftCrit:
                return true;
            case MobState.Alive:
                return true;
            case MobState.Invalid:
                return false;
            default:
                return false;
        }
    }

}