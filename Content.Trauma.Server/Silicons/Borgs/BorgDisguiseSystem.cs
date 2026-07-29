// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Trauma.Shared.Silicons.Borgs;
using Content.Trauma.Shared.Silicons.Borgs.Components;
using Robust.Server.GameObjects;

namespace Content.Trauma.Server.Silicons.Borgs;

public sealed partial class BorgDisguiseSystem : SharedBorgDisguiseSystem
{
    private CompName _lightName;

    public override void Initialize()
    {
        base.Initialize();

        _lightName = Factory.CompName<PointLightComponent>();
    }

    /// <summary>
    /// Swaps the displayed access on the borg's AccessReaderComponent to match the
    /// disguised prototype's access, so examine text shows the cover access instead of
    /// leaking syndicate access.
    /// </summary>
    private void UpdateAccessDisplay(Entity<BorgDisguiseComponent> ent)
    {
        if (!TryComp<AccessReaderComponent>(ent.Owner, out var accessReader))
            return;

        if (ent.Comp.Disguised)
        {
            if (!TryGetDisguisedPrototype(ent.Comp, out var disguisedProto))
                return;

            if (!disguisedProto.TryComp(_accessName, out AccessReaderComponent? disguisedAccessReader))
                return;

            ent.Comp.RealAccessListsOriginal = accessReader.AccessListsOriginal;
            _access.SetAccessListsOriginal((ent.Owner, accessReader), new(disguisedAccessReader.AccessLists));
        }
        else
        {
            if (ent.Comp.RealAccessListsOriginal == null)
                return;

            _access.SetAccessListsOriginal((ent.Owner, accessReader), ent.Comp.RealAccessListsOriginal);
            ent.Comp.RealAccessListsOriginal = null;
        }
    }

    /// <summary>
    /// Toggles the disguise.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDisguiseToggle(Entity<BorgDisguiseComponent> ent, ref BorgDisguiseToggleActionEvent args)
    {
        if (args.Handled)
            return;
        ent.Comp.Disguised = !ent.Comp.Disguised;
        Dirty(ent);
        args.Handled = true;
        UpdateAccessDisplay(ent);
        UpdateAppearance(ent);
    }

    /// <summary>
    /// Disables the disguise.
    /// </summary>
    private void DisableDisguise(Entity<BorgDisguiseComponent> ent)
    {
        ent.Comp.Disguised = false;
        Dirty(ent);
        UpdateAccessDisplay(ent);
        UpdateAppearance(ent);
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer powered.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnToggled(Entity<BorgDisguiseComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            DisableDisguise(ent);
    }

    /// <summary>
    /// Disables the disguise if the borg is no longer alive.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<BorgDisguiseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            DisableDisguise(ent);
    }

    /// <summary>
    /// Updates the appearance data of the entity.
    /// </summary>
    private void UpdateAppearance(Entity<BorgDisguiseComponent> ent)
    {
        if (TryPrototype(ent.Owner, out var entityPrototype)
            && entityPrototype.TryComp(_lightName, out PointLightComponent? lightPrototype))
        {
            _light.SetColor(ent.Owner,
                ent.Comp.Disguised
                    ? ent.Comp.DisguisedLightColor
                    : lightPrototype.Color);
        }

        UpdateSharedAppearance(ent);
    }
}
