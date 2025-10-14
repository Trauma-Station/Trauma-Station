using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics.Tools;

public sealed class GenomeExtractorSystem : EntitySystem
{
    /* idk if i want this
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<MutatableComponent> _mutatableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mutatableQuery = GetEntityQuery<MutatableComponent>();

        SubscribeLocalEvent<GenomeExtractorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GenomeExtractorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GenomeExtractorComponent, GenomeExtractorDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(Entity<GenomeExtractorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("genome-extractor-examine", ("empty", ent.Comp.IsEmpty)));
    }

    private void OnInteractUsing(Entity<GenomeExtractorComponent> ent, ref InteractUsingEvent args)
    {
        var user = args.User;
        var target = args.Target;

        // TODO: interact with computer to deposit genome
        if (!_mutatableQuery.HasComp(target))
            return;

        TryStartExtract(ent, target, user);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<GenomeExtractorComponent> ent, ref GenomeExtractorDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Args.Target is not {} target ||
            !_mutatableQuery.TryComp(target, out var comp))
        {
            return;
        }

        TryExtract(ent, (target, comp), args.Args.User);
        args.Handled = true;
    }

    #region Public API

    /// <summary>
    /// Try to start the extracting doafter, returning false and showing a popup to the user if it can't be done.
    /// </summary>
    public bool TryStartExtract(Entity<GenomeExtractorComponent> ent, EntityUid target, EntityUid user)
    {
        if (!ent.Comp.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("genome-extractor-fail-full", ("item", ent)), user, user);
            return false;
        }

        // more fiddly to extract your own genome...
        var delay = ent.Comp.Delay;
        if (user == target)
            delay *= 2;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new GenomeExtractorDoAfterEvent(),
            target: target,
            eventTarget: ent,
            used: ent
        ));
    }

    public bool TryExtract(Entity<GenomeExtractorComponent> ent, Entity<MutatableComponent> target, EntityUid user)
    {
        if (!ent.Comp.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("genome-extractor-fail-full", ("item", ent)), user, user);
            return false;
        }

        var name = Identity.Entity(target, EntityManager);
        if (_mob.IsDead(target))
        {
            _popup.PopupClient(Loc.GetString("genome-extractor-fail-dead", ("target", name)), user, user);
            return false;
        }

        if (IsDamaged(ent, target))
        {
            _popup.PopupClient(Loc.GetString("genome-extractor-fail-genetic", ("target", name)), user, user);
            return false;
        }

        // TODO
        //Extract(ent, target);
        return true;
    }

    public bool IsDamaged(Entity<GenomeExtractorComponent> ent, EntityUid target)
    {
        if (!_damageableQuery.TryComp(target, out var comp))
            return false;

        var damages = comp.Damage.DamageDict;
        foreach (var ty in ent.Comp.Damage.DamageDict.Keys)
        {
            if (damages.TryGetValue(ty, out var damage) && damage > 0)
                return true;
        }

        return false;
    }

    #endregion
    */
}

[Serializable, NetSerializable]
public sealed partial class GenomeExtractorDoAfterEvent : SimpleDoAfterEvent;
