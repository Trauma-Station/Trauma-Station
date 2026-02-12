// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Abductor;
using Content.Medical.Shared.ItemSwitch;
using Content.Medical.Shared.Surgery;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Medical.Server.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedItemSwitchSystem _itemSwitch = default!;

    public static readonly SoundSpecifier ExperimentSound = new SoundPathSpecifier(new ResPath("/Audio/Voice/Human/wilhelm_scream.ogg"));

    public void InitializeConsole()
    {
        SubscribeLocalEvent<AbductorConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<AbductConditionComponent, ObjectiveGetProgressEvent>(OnAbductGetProgress);

        Subs.BuiEvents<AbductorConsoleComponent>(AbductorConsoleUIKey.Key, subs =>
        {
            subs.Event<AbductorAttractBuiMsg>(OnAttractBuiMsg);
            subs.Event<AbductorCompleteExperimentBuiMsg>(OnCompleteExperimentBuiMsg);
            subs.Event<AbductorVestModeChangeBuiMsg>(OnVestModeChangeBuiMsg);
            subs.Event<AbductorLockBuiMsg>(OnVestLockBuiMsg);
        });
        SubscribeLocalEvent<AbductorConsoleComponent, AbductorAttractDoAfterEvent>(OnDoAfterAttract);
    }

    private void OnAbductGetProgress(Entity<AbductConditionComponent> ent, ref ObjectiveGetProgressEvent args)
        => args.Progress = AbductProgress(ent.Comp, _number.GetTarget(ent.Owner));

    private float AbductProgress(AbductConditionComponent comp, int target)
        => target == 0 ? 1f : MathF.Min(comp.TotalAbducted / (float) target, 1f);

    private void OnVestModeChangeBuiMsg(EntityUid uid, AbductorConsoleComponent component, AbductorVestModeChangeBuiMsg args)
    {
        if (component.Armor != null)
            _itemSwitch.Switch(GetEntity(component.Armor.Value), args.Mode.ToString());
    }

    private void OnVestLockBuiMsg(Entity<AbductorConsoleComponent> ent, ref AbductorLockBuiMsg args)
    {
        if (ent.Comp.Armor != null && GetEntity(ent.Comp.Armor.Value) is {} armor)
            if (!RemComp<UnremoveableComponent>(armor))
                EnsureComp<UnremoveableComponent>(armor);
    }

    private void OnCompleteExperimentBuiMsg(EntityUid uid, AbductorConsoleComponent component, AbductorCompleteExperimentBuiMsg args)
    {
        if (GetEntity(component.Experimentator) is not {} experimentator ||
            !TryComp<AbductorExperimentatorComponent>(experimentator, out var comp))
            return;

        var container = _container.GetContainer(experimentator, comp.ContainerId);
        var victim = container.ContainedEntities.FirstOrDefault(HasComp<AbductorVictimComponent>);
        if (victim != default && TryComp(victim, out AbductorVictimComponent? victimComp))
        {
            if (victimComp.Implanted
                && TryComp<MindContainerComponent>(args.Actor, out var mindContainer)
                && mindContainer.Mind.HasValue
                && TryComp<MindComponent>(mindContainer.Mind.Value, out var mind)
                && mind.Objectives.FirstOrDefault(HasComp<AbductConditionComponent>) is {} objId
                && TryComp<AbductConditionComponent>(objId, out var condition))
            {
                condition.Abducted.Add(GetNetEntity(victim));
            }
            _audio.PlayPvs(ExperimentSound, experimentator);

            if (victimComp.Position is {} pos)
                _xform.SetCoordinates(victim, pos);
        }
    }

    private void OnAttractBuiMsg(Entity<AbductorConsoleComponent> ent, ref AbductorAttractBuiMsg args)
    {
        if (GetEntity(ent.Comp.Target) is not {} target || GetEntity(ent.Comp.AlienPod) is not {} telepad)
            return;

        AddTeleportationEffect(target, TeleportationEffectEntityShort);
        AddTeleportationEffect(telepad, TeleportationEffectShort);

        var coords = Transform(telepad).Coordinates;
        var ev = new AbductorAttractDoAfterEvent(GetNetCoordinates(coords), GetNetEntity(target));
        ent.Comp.Target = null;
        var doAfter = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(3), ev, eventTarget: ent)
        {
            BreakOnDamage = false,
            BreakOnDropItem = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }
    private void OnDoAfterAttract(Entity<AbductorConsoleComponent> ent, ref AbductorAttractDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var victim = GetEntity(args.Victim);
        StopPulls(victim);
        _xform.SetCoordinates(victim, GetCoordinates(args.TargetCoordinates));
    }

    private void OnBeforeActivatableUIOpen(Entity<AbductorConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
        => UpdateGui(ent.Comp.Target, ent);

    protected override void UpdateGui(NetEntity? target, Entity<AbductorConsoleComponent> computer)
    {
        string? targetName = null;
        string? victimName = null;
        if (target.HasValue && TryComp(GetEntity(target.Value), out MetaDataComponent? metadata))
            targetName = metadata?.EntityName;

        var armorLock = false;
        var armorMode = AbductorArmorModeType.Stealth;

        if (GetEntity(computer.Comp.Armor) is {} armor)
        {
            if (HasComp<UnremoveableComponent>(armor))
                armorLock = true;
            if (TryComp<ItemSwitchComponent>(armor, out var switchVest) && Enum.TryParse<AbductorArmorModeType>(switchVest.State, ignoreCase: true, out var State))
                armorMode = State;
        }

        var coords = Transform(computer).Coordinates;
        if (computer.Comp.AlienPod == null)
        {
            // goidabrained device linking...
            var alienpad = _lookup.GetEntitiesInRange<AbductorAlienPadComponent>(coords, 4, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (alienpad != default)
                computer.Comp.AlienPod = GetNetEntity(alienpad);
        }

        if (computer.Comp.Experimentator == null)
        {
            var foundExp = _lookup.GetEntitiesInRange<AbductorExperimentatorComponent>(coords, 4, LookupFlags.Approximate | LookupFlags.Dynamic)
                .FirstOrDefault().Owner;
            if (foundExp != default)
                computer.Comp.Experimentator = GetNetEntity(foundExp);
        }

        if (GetEntity(computer.Comp.Experimentator) is {} experimentator &&
            TryComp<AbductorExperimentatorComponent>(experimentator, out var expComp))
        {
            var container = _container.GetContainer(experimentator, expComp.ContainerId);
            var victim = container.ContainedEntities.FirstOrDefault(e => HasComp<AbductorVictimComponent>(e));
            if (victim != default)
                victimName = Name(victim);
        }

        _ui.SetUiState(computer.Owner, AbductorConsoleUIKey.Key, new AbductorConsoleBuiState()
        {
            Target = target,
            TargetName = targetName,
            VictimName = victimName,
            AlienPadFound = computer.Comp.AlienPod != default,
            ExperimentatorFound = computer.Comp.Experimentator != default,
            ArmorFound = computer.Comp.Armor != default,
            ArmorLocked = armorLock,
            CurrentArmorMode = armorMode
        });
    }
}
