using Content.Server.Lathe;
using Content.Server.Lathe.Components;
using Content.Server.Power.Components;
using Content.Shared.Lathe;

namespace Content.Trauma.Server.Lathe;

/// <summary>
/// Makes unpowered lathes stop and start producing depending on being anchored.
/// Similar to powered lathes when they lose or gain power.
/// </summary>
public sealed class LatheAnchorSystem : EntitySystem
{
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<ApcPowerReceiverComponent> _powerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _powerQuery = GetEntityQuery<ApcPowerReceiverComponent>();

        SubscribeLocalEvent<LatheComponent, AnchorStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(Entity<LatheComponent> ent, ref AnchorStateChangedEvent args)
    {
        // don't double dip, powered lathes handle it via power changed
        if (_powerQuery.HasComp(ent))
            return;

        if (!args.Anchored)
        {
            RemComp<LatheProducingComponent>(ent);
            _appearance.SetData(ent.Owner, LatheVisuals.IsRunning, false);
        }
        else if (ent.Comp.CurrentRecipe != null)
        {
            EnsureComp<LatheProducingComponent>(ent);
            _lathe.TryStartProducing(ent, ent.Comp);
        }
    }
}
