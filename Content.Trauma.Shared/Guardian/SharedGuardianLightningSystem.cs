using Content.Shared.Guardian.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Guardian.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Guardian;

/// <summary>
/// Handles the Lightning holoparasite variant: its targeted bolt action. The bolt cannot
/// target creatures with the electricity resistance gene, and the server half applies the
/// actual bolt (damage, stun and chaining), the passive arcs and the host protection.
/// </summary>
public abstract partial class SharedGuardianLightningSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianLightningComponent, GuardianLightningBoltEvent>(OnLightningBolt);
    }

    private void OnLightningBolt(Entity<GuardianLightningComponent> ent, ref GuardianLightningBoltEvent args)
    {
        if (args.Handled || !Exists(args.Target) || args.Target == ent.Owner)
            return;

        GuardianComponent? guardian = null;
        if (!Resolve(ent, ref guardian) || !guardian.GuardianLoose)
        {
            _popup.PopupEntity(Loc.GetString("guardian-lightning-not-manifested"), ent, ent, PopupType.MediumCaution);
            return;
        }

        if (args.Target == guardian.Host)
            return;

        var target = args.Target;

        var range = (_transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(ent).Position).Length();
        if (range > ent.Comp.BoltRange)
            return;

        // People with electricity resistance cannot be targeted.
        if (_mutation.HasMutation(target, ent.Comp.GeneId))
        {
            _popup.PopupEntity(Loc.GetString("guardian-lightning-target-resistant"), ent, ent, PopupType.MediumCaution);
            return;
        }

        ShootLightning(ent.Owner, target, ent.Comp.BoltProto, ent.Comp.BoltDamage);

        _popup.PopupEntity(Loc.GetString("guardian-lightning-bolt-hit"), target, target, PopupType.MediumCaution);

        args.Handled = true;
    }

    /// <summary>
    /// Fires a bolt of lightning from <paramref name="performer"/> to <paramref name="target"/>.
    /// The server-side implementation creates the actual lightning and applies the damage.
    /// </summary>
    public virtual void ShootLightning(EntityUid performer, EntityUid target, EntProtoId lightningPrototype, float damage)
    {
    }
}
