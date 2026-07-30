// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Shared.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;

namespace Content.Trauma.Client.Silicons.Borgs;

public sealed partial class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BorgSystem _borg = default!;
    [Dependency] private SpriteSystem _borgSprite = default!;

    private CompName _chassisName;
    private CompName _lightName;

    public override void Initialize()
    {
        base.Initialize();

        _chassisName = Factory.CompName<BorgChassisComponent>();
        _lightName = Factory.CompName<PointLightComponent>();
    }

    [SubscribeLocalEvent]
    private void OnStateUpdate(Entity<BorgDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;
        UpdateAppearance(ent, sprite);
    }

    /// <summary>
    /// Handles updates to the appearance of the entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBorgAppearanceChanged(Entity<BorgDisguiseComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        UpdateAppearance(ent, args.Sprite);
    }

    /// <summary>
    /// Updates the appearance data of the entity.
    /// </summary>
    private void UpdateAppearance(Entity<BorgDisguiseComponent> ent, SpriteComponent sprite)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        _appearance.SetData(ent.Owner, BorgDisguiseVisuals.IsDisguised, ent.Comp.Disguised, appearance);

        // Change method in BorgSystem gets automatically called via observer
        if (TryPrototype(ent.Owner, out var entityPrototype))
        {
            if (entityPrototype.TryComp(_chassisName, out BorgChassisComponent? borgPrototype))
            {
                _borg.SetMindStates((ent.Owner, Comp<BorgChassisComponent>(ent.Owner)),
                    ent.Comp.Disguised ? ent.Comp.HasMindState : borgPrototype.HasMindState,
                    ent.Comp.Disguised ? ent.Comp.NoMindState : borgPrototype.NoMindState);
            }

            if (entityPrototype.TryComp(_lightName, out PointLightComponent? lightPrototype))
            {
                _light.SetColor(ent.Owner,
                    ent.Comp.Disguised
                        ? ent.Comp.DisguisedLightColor
                        : lightPrototype.Color);
            }
        }

        _borgSprite.LayerSetRsiState((ent.Owner, sprite), "light",
            ent.Comp.Disguised ? ent.Comp.DisguisedLight : ent.Comp.RealLight);

        UpdateSharedAppearance(ent);
    }
}
