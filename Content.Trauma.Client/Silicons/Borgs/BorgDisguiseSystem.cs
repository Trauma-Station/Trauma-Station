// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Silicons.Borgs;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Shared.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;


namespace Content.Trauma.Client.Silicons.Borgs;

public sealed partial class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BorgSystem _borgSystem = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SpriteSystem _sprites = default!;


    /// <summary>
    /// Swaps the displayed access on the borg's AccessReaderComponent to match the
    /// disguised prototype's access, so examine text shows the cover access instead of leaking
    /// syndicate access.
    /// </summary>
    private void UpdateAccessDisplay(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return;

        if (comp.Disguised)
        {
            if (!_protoMan.TryIndex(comp.DisguisedPrototype, out var disguisedProto))
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgDisguiseComponent, BorgDisguiseToggleActionEvent>(OnDisguiseToggle);
        SubscribeLocalEvent<BorgDisguiseComponent, AppearanceChangeEvent>(OnBorgAppearanceChanged);
    }

    /// <summary>
    /// Toggles the disguise.
    /// </summary>
    /// <param name="uid">The entity to toggle the disguise of.</param>
    /// <param name="comp">The disguise component of the entity.</param>
    /// <param name="args"></param>
    private void OnDisguiseToggle(EntityUid uid, BorgDisguiseComponent comp, BorgDisguiseToggleActionEvent args)
    {
        UpdateAppearance(uid, comp);
        args.Handled = true;
    }

    /// <summary>
    /// Handles updates to the appearance of the entity.
    /// </summary>
    /// <param name="uid">The entity updated.</param>
    /// <param name="comp">The disguise component of the updated entity.</param>
    /// <param name="args"></param>
    private void OnBorgAppearanceChanged(EntityUid uid, BorgDisguiseComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        UpdateAppearance(uid, comp);
    }

    /// <summary>
    /// Updates the appearance data of the entity.
    /// </summary>
    /// <param name="uid">The entity to update.</param>
    /// <param name="comp">The component holding the disguise data.</param>
    private void UpdateAppearance(EntityUid uid, BorgDisguiseComponent comp)
    {
        AppearanceComponent? appearance = null;
        SpriteComponent? sprite = null;

        if (!Resolve(uid, ref appearance, ref sprite))
            return;
        _appearance.SetData(uid, BorgDisguiseVisuals.IsDisguised, comp.Disguised, appearance);
        // Change method in BorgSystem gets automatically called via observer

        if (TryPrototype(uid, out var entityPrototype))
        {
            if (entityPrototype.TryComp<BorgChassisComponent>(out var borgPrototype, _compFactory)
                && borgPrototype != null)
            {
                _borgSystem.SetMindStates(new Entity<BorgChassisComponent>(uid, Comp<BorgChassisComponent>(uid)),
                    comp.Disguised ? comp.HasMindState : borgPrototype.HasMindState,
                    comp.Disguised ? comp.NoMindState : borgPrototype.NoMindState);
            }

            if (entityPrototype.TryComp<PointLightComponent>(out var lightPrototype, _compFactory)
                && lightPrototype != null)
            {
                _pointLightSystem.SetColor(uid,
                    comp.Disguised
                        ? comp.DisguisedLightColor
                        : lightPrototype.Color);
            }
        }


        _sprites.LayerSetRsiState((uid, sprite), "light", comp.Disguised ? comp.DisguisedLight : comp.RealLight);


        UpdateSharedAppearance(uid, comp);
    }
}
