// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Shared.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;

namespace Content.Trauma.Client.Silicons.Borgs;

public sealed partial class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BorgSystem _borgSystem = default!;
    [Dependency] private SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private SpriteSystem _sprites = default!;

    private CompName _chassisName;
    private CompName _lightName;

    public override void Initialize()
    {
        base.Initialize();

        _chassisName = Factory.CompName<BorgChassisComponent>();
        _lightName = Factory.CompName<PointLightComponent>();

        SubscribeLocalEvent<BorgDisguiseComponent, AfterAutoHandleStateEvent>(OnStateUpdate);
        SubscribeLocalEvent<BorgDisguiseComponent, AppearanceChangeEvent>(OnBorgAppearanceChanged);
    }

    private void OnStateUpdate(EntityUid uid, BorgDisguiseComponent comp, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(uid, comp);
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
            if (entityPrototype.TryComp(_chassisName, out BorgChassisComponent? borgPrototype))
            {
                _borgSystem.SetMindStates((uid, Comp<BorgChassisComponent>(uid)),
                    comp.Disguised ? comp.HasMindState : borgPrototype.HasMindState,
                    comp.Disguised ? comp.NoMindState : borgPrototype.NoMindState);
            }

            if (entityPrototype.TryComp(_lightName, out PointLightComponent? lightPrototype))
            {
                _pointLightSystem.SetColor(uid,
                    comp.Disguised
                        ? comp.DisguisedLightColor
                        : lightPrototype.Color);
            }
        }

        _sprites.LayerSetRsiState((uid, sprite), "light",
            comp.Disguised ? comp.DisguisedLight : comp.RealLight);

        UpdateSharedAppearance(uid, comp);
    }
}
