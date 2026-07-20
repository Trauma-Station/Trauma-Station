// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Silicon.DeadStartupButton;

/// <summary>
/// This creates a Button that can be activated after an entity, usually a silicon or an IPC, died.
/// This will activate a doAfter and then revive the entity, playing a custom sound afterwards.
/// </summary>
public abstract partial class SharedDeadStartupButtonSystem : EntitySystem
{
    [Dependency] protected MobStateSystem Mob = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadStartupButtonComponent, GetVerbsEvent<AlternativeVerb>>(AddTurnOnVerb);
    }

    private void AddTurnOnVerb(EntityUid uid, DeadStartupButtonComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!Mob.IsDead(uid)
            || !args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (!TryComp(uid, out MobStateComponent? mob) || !Mob.IsDead(uid, mob))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryStartup(args.User, uid, component),
            Text = Loc.GetString(component.VerbText),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Priority = component.VerbPriority
        });
    }

    private void TryStartup(EntityUid user, EntityUid target, DeadStartupButtonComponent comp)
    {
        Audio.PlayPredicted(comp.ButtonSound, target, user);
        var args = new DoAfterArgs(EntityManager, user, comp.StartupDelay, new DeadStartupDoAfterEvent(), target, target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MultiplyDelay = false,
        };
        _doAfter.TryStartDoAfter(args);
    }
}

[Serializable, NetSerializable]
public sealed partial class DeadStartupDoAfterEvent : SimpleDoAfterEvent;
