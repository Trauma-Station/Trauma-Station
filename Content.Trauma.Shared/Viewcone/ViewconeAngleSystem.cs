// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Shared.Viewcone.Components;

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Provides public API for getting the actual modified viewcone angle (including equipment etc) rather than just the base angle
/// </summary>
public sealed partial class ViewconeAngleSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private EntityQuery<ViewconeComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ModifyViewconeAngleEvent>(_body.RelayEvent);
        SubscribeLocalEvent<StatusEffectContainerComponent, ModifyViewconeAngleEvent>(_status.RelayEvent);

        SubscribeLocalEvent<ViewconeModifierComponent, ExaminedEvent>(OnExamined);
        Subs.SubscribeWithRelay<ViewconeModifierComponent, ModifyViewconeAngleEvent>(OnModifyAngle, held: false);
        SubscribeLocalEvent<ViewconeModifierComponent, StatusEffectRelayedEvent<ModifyViewconeAngleEvent>>(OnEffectModifyAngle);
        SubscribeLocalEvent<ViewconeModifierComponent, BodyRelayedEvent<ModifyViewconeAngleEvent>>(OnOrganModifyAngle);
    }

    private void OnExamined(Entity<ViewconeModifierComponent> ent, ref ExaminedEvent args)
    {
        var dir = ent.Comp.AngleModifier < 1f ? "decrease" : "increase";
        var loc = "viewcone-modifier-examine-" + dir;

        var degrees = (int) MathF.Abs(ent.Comp.AngleModifier);
        args.PushMarkup(Loc.GetString(loc, ("degrees", degrees)));
    }

    private void OnModifyAngle(Entity<ViewconeModifierComponent> ent, ref ModifyViewconeAngleEvent args)
    {
        args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnEffectModifyAngle(Entity<ViewconeModifierComponent> ent, ref StatusEffectRelayedEvent<ModifyViewconeAngleEvent> args)
    {
        var ev = args.Args;
        ev.ModifyAngle(ent.Comp.AngleModifier);
        args.Args = ev; // holy dogshit please never ever do this
    }

    private void OnOrganModifyAngle(Entity<ViewconeModifierComponent> ent, ref BodyRelayedEvent<ModifyViewconeAngleEvent> args)
    {
        args.Args.ModifyAngle(ent.Comp.AngleModifier);
    }

    /// <summary>
    /// Returns the modified viewcone angle for an entity, calculated from the base,
    /// taking into account equipment & status effects & whatnot
    /// </summary>
    public float GetAngle(Entity<ViewconeComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp))
            return 0f;

        var ev = new ModifyViewconeAngleEvent();
        RaiseLocalEvent(ent, ref ev);

        // clamps to 0, 360 since this is could easily go over with stacking equipment items and shit
        return Math.Clamp(ent.Comp.BaseConeAngle * ev.AngleModifier, 0f, 360f);
    }
}

/// <summary>
/// Raised clientside by-ref and broadcast on an entity with a viewcone, and relayed to inventory & status effects.
/// Modifies their viewcone angle multiplicatively.
/// </summary>
[ByRefEvent]
public record struct ModifyViewconeAngleEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK;

    private float _angleModifier = 1f;

    public float AngleModifier => _angleModifier;

    public void ModifyAngle(float angle)
    {
        _angleModifier *= angle;
    }
}
