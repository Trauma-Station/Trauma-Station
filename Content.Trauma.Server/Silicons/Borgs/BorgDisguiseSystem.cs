using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Trauma.Shared.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.Silicons.Borgs;

public sealed partial class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private SharedPointLightSystem _pointLightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgDisguiseComponent, BorgDisguiseToggleActionEvent>(OnDisguiseToggle);
        SubscribeLocalEvent<BorgDisguiseComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<BorgDisguiseComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    /// Swaps the displayed access on the borg's AccessReaderComponent to match the
    /// disguised prototype's access, so examine text shows the cover access instead of
    /// leaking syndicate access.
    /// </summary>
    private void UpdateAccessDisplay(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return;

        if (comp.Disguised)
        {
            if (!ProtoMan.TryIndex(comp.DisguisedPrototype, out var disguisedProto))
                return;

            if (!disguisedProto.TryComp<AccessReaderComponent>(out var disguisedAccessReader, _compFactory))
                return;

            comp.RealAccessListsOriginal = accessReader.AccessListsOriginal;
            _accessReader.SetAccessListsOriginal((uid, accessReader), new(disguisedAccessReader.AccessLists));
        }
        else
        {
            if (comp.RealAccessListsOriginal == null)
                return;

            _accessReader.SetAccessListsOriginal((uid, accessReader), comp.RealAccessListsOriginal);
            comp.RealAccessListsOriginal = null;
        }
    }

    /// <summary>
    /// Toggles the disguise.
    /// </summary>
    /// <param name="uid">The entity to toggle the disguise of.</param>
    /// <param name="comp">The disguise component of the entity.</param>
    /// <param name="args"></param>
    private void OnDisguiseToggle(EntityUid uid, BorgDisguiseComponent comp, BorgDisguiseToggleActionEvent args)
    {
        if (args.Handled)
            return;
        comp.Disguised = !comp.Disguised;
        Dirty(uid, comp);
        args.Handled = true;
        UpdateAccessDisplay(uid, comp);
        UpdateApperance(uid, comp);
    }

    /// <summary>
    /// Disables the disguise.
    /// </summary>
    /// <param name="uid">The entity having their disguise disabled.</param>
    /// <param name="comp">The disguise component being disabled.</param>
    private void DisableDisguise(EntityUid uid, BorgDisguiseComponent comp)
    {
        comp.Disguised = false;
        Dirty(uid, comp);
        UpdateAccessDisplay(uid, comp);
        UpdateApperance(uid, comp);
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer powered.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="comp">The disguise component.</param>
    /// <param name="args">State change event.</param>
    private void OnToggled(EntityUid uid, BorgDisguiseComponent comp, ref ItemToggledEvent args)
    {
        if (!args.Activated)
        {
            DisableDisguise(uid, comp);
        }
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer alive.
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="component">The disguise component.</param>
    /// <param name="args">State change event.</param>
    private void OnMobStateChanged(EntityUid uid, BorgDisguiseComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            DisableDisguise(uid, component);
        }
    }

    /// <summary>
    /// Updates the appearance data of the entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="comp">The component holding the disguise data.</param>
    private void UpdateApperance(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (TryPrototype(uid, out var entityPrototype))
        {
            if (entityPrototype.TryGetComponent<PointLightComponent>("PointLight", out var lightPrototype))
            {
                _pointLightSystem.SetColor(uid,
                    comp.Disguised
                        ? comp.DisguisedLightColor
                        : lightPrototype.Color);
            }
        }

        UpdateSharedAppearance(uid, comp);
    }
}
