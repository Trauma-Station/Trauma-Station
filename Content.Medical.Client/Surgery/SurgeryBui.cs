// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Client.Administration.UI.CustomControls;
using Content.Medical.Client.Choice.UI;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Surgery;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Medical.Client.Surgery;

public sealed class SurgeryBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly SurgerySystem _system;
    [ViewVariables]
    private SurgeryWindow? _window;
    private EntityUid? _part;
    private bool _isBody;
    private (EntityUid Ent, EntProtoId Proto)? _surgery;
    private readonly List<EntProtoId> _previousSurgeries = new();
    public SurgeryBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) => _system = _entMan.System<SurgerySystem>();

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window is null
            || message is not SurgeryBuiRefreshMessage)
            return;

        RefreshUI();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not SurgeryBuiState s)
            return;

        Update(s);
    }

    private void Update(SurgeryBuiState state)
    {
        if (_window == null)
        {
            _window = new SurgeryWindow();
            _window.OnClose += Close;
            _window.Title = Loc.GetString("surgery-ui-window-title");

            _window.PartsButton.OnPressed += _ =>
            {
                _part = null;
                _isBody = false;
                _surgery = null;
                _previousSurgeries.Clear();
                View(ViewType.Parts);
            };

            _window.SurgeriesButton.OnPressed += _ =>
            {
                _surgery = null;
                _previousSurgeries.Clear();

                if (!_entMan.TryGetNetEntity(_part, out var netPart)
                    || State is not SurgeryBuiState s
                    || !s.Choices.TryGetValue(netPart.Value, out var surgeries))
                    return;

                OnPartPressed(netPart.Value, surgeries);
            };

            _window.StepsButton.OnPressed += _ =>
            {
                if (!_entMan.TryGetNetEntity(_part, out var netPart)
                    || _previousSurgeries.Count == 0)
                    return;

                var last = _previousSurgeries[^1];
                _previousSurgeries.RemoveAt(_previousSurgeries.Count - 1);

                if (_system.GetSingleton(last) is not { } previousId
                    || !_entMan.TryGetComponent(previousId, out SurgeryComponent? previous))
                    return;

                OnSurgeryPressed((previousId, previous), netPart.Value, last);
            };
        }

        _window.Surgeries.DisposeAllChildren();
        _window.Steps.DisposeAllChildren();
        _window.Parts.DisposeAllChildren();
        View(ViewType.Parts);

        var oldSurgery = _surgery;
        var oldPart = _part;
        _part = null;
        _surgery = null;

        var options = new List<(NetEntity netEntity, EntityUid entity, string Name, BodyPartType? PartType)>();
        foreach (var choice in state.Choices.Keys)
        {
            if (!_entMan.TryGetEntity(choice, out var ent))
                continue;

            if (_entMan.TryGetComponent(ent, out BodyPartComponent? part))
                options.Add((choice, ent.Value, _entMan.GetComponent<MetaDataComponent>(ent.Value).EntityName, part.PartType));
            else if (_entMan.TryGetComponent(ent, out BodyComponent? body))
                options.Add((choice, ent.Value, _entMan.GetComponent<MetaDataComponent>(ent.Value).EntityName, null));
        }

        options.Sort((a, b) =>
        {
            int GetScore(BodyPartType? partType)
            {
                return partType switch
                {
                    BodyPartType.Head => 1,
                    BodyPartType.Torso => 2,
                    BodyPartType.Arm => 2,
                    BodyPartType.Hand => 3,
                    BodyPartType.Leg => 4,
                    BodyPartType.Foot => 5,
                    // BodyPartType.Tail => 6, No tails yet!
                    BodyPartType.Other => 7,
                    _ => 8
                };
            }

            return GetScore(a.PartType) - GetScore(b.PartType);
        });

        foreach (var (netEntity, entity, partName, _) in options)
        {
            //var netPart = _entMan.GetNetEntity(part.Owner);
            var surgeries = state.Choices[netEntity];
            var partButton = new ChoiceControl();

            partButton.Set(partName, null);
            partButton.Button.OnPressed += _ => OnPartPressed(netEntity, surgeries);

            _window.Parts.AddChild(partButton);

            foreach (var surgeryId in surgeries)
            {
                if (_system.GetSingleton(surgeryId) is not { } surgery ||
                    !_entMan.TryGetComponent(surgery, out SurgeryComponent? surgeryComp))
                    continue;

                if (oldPart == entity && oldSurgery?.Proto == surgeryId)
                    OnSurgeryPressed((surgery, surgeryComp), netEntity, surgeryId);
            }

            if (oldPart == entity && oldSurgery == null)
                OnPartPressed(netEntity, surgeries);
        }


        if (!_window.IsOpen)
            _window.OpenCentered();
    }

    private void AddStep(EntProtoId stepId, NetEntity netPart, EntProtoId surgeryId)
    {
        if (_window == null
            || _system.GetSingleton(stepId) is not { } step)
            return;

        var stepName = new FormattedMessage();
        stepName.AddText(_entMan.GetComponent<MetaDataComponent>(step).EntityName);
        var stepButton = new SurgeryStepButton { Step = step };
        stepButton.Button.OnPressed += _ => SendPredictedMessage(new SurgeryStepChosenBuiMsg(netPart, surgeryId, stepId, _isBody));

        _window.Steps.AddChild(stepButton);
    }

    private void OnSurgeryPressed(Entity<SurgeryComponent> surgery, NetEntity netPart, EntProtoId surgeryId)
    {
        if (_window == null)
            return;

        _part = _entMan.GetEntity(netPart);
        _isBody = _entMan.HasComponent<BodyComponent>(_part);
        _surgery = (surgery, surgeryId);

        _window.Steps.DisposeAllChildren();

        // This apparently does not consider if theres multiple surgery requirements in one surgery. Maybe thats fine.
        if (surgery.Comp.Requirement is { } requirementId && _system.GetSingleton(requirementId) is { } requirement)
        {
            var label = new ChoiceControl();
            label.Button.OnPressed += _ =>
            {
                _previousSurgeries.Add(surgeryId);

                if (_entMan.TryGetComponent(requirement, out SurgeryComponent? requirementComp))
                    OnSurgeryPressed((requirement, requirementComp), netPart, requirementId);
            };

            var msg = new FormattedMessage();
            var surgeryName = _entMan.GetComponent<MetaDataComponent>(requirement).EntityName;
            msg.AddMarkupOrThrow($"[bold]{Loc.GetString("surgery-ui-window-require")}: {surgeryName}[/bold]");
            label.Set(msg, null);

            _window.Steps.AddChild(label);
            _window.Steps.AddChild(new HSeparator { Margin = new Thickness(0, 0, 0, 1) });
        }
        foreach (var stepId in surgery.Comp.Steps)
            AddStep(stepId, netPart, surgeryId);

        View(ViewType.Steps);
        RefreshUI();
    }

    private void OnPartPressed(NetEntity netPart, List<EntProtoId> surgeryIds)
    {
        if (_window == null)
            return;

        _part = _entMan.GetEntity(netPart);
        _isBody = _entMan.HasComponent<BodyComponent>(_part);
        _window.Surgeries.DisposeAllChildren();

        var surgeries = new List<(Entity<SurgeryComponent> Ent, EntProtoId Id, string Name)>();
        foreach (var surgeryId in surgeryIds)
        {
            if (_system.GetSingleton(surgeryId) is not { } surgery ||
                !_entMan.TryGetComponent(surgery, out SurgeryComponent? surgeryComp))
            {
                continue;
            }

            var name = _entMan.GetComponent<MetaDataComponent>(surgery).EntityName;
            surgeries.Add(((surgery, surgeryComp), surgeryId, name));
        }

        surgeries.Sort((a, b) =>
        {
            var priority = a.Ent.Comp.Priority.CompareTo(b.Ent.Comp.Priority);
            if (priority != 0)
                return priority;

            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        foreach (var surgery in surgeries)
        {
            var surgeryButton = new ChoiceControl();
            surgeryButton.Set(surgery.Name, null);

            surgeryButton.Button.OnPressed += _ => OnSurgeryPressed(surgery.Ent, netPart, surgery.Id);
            _window.Surgeries.AddChild(surgeryButton);
        }

        RefreshUI();
        View(ViewType.Surgeries);
    }

    private void RefreshUI()
    {
        if (_window == null
            || !_window.IsOpen
            || _part == null
            || !_entMan.HasComponent<SurgeryComponent>(_surgery?.Ent)
            || _player.LocalEntity is not {} player)
            return;

        var next = _system.GetNextStep(Owner, _part.Value, _surgery.Value.Ent, player);
        var i = 0;
        foreach (var child in _window.Steps.Children)
        {
            if (child is not SurgeryStepButton stepButton)
                continue;

            var status = StepStatus.Incomplete;
            if (next == null)
                status = StepStatus.Complete;
            else if (next.Value.Step < 0 && i > -next.Value.Step - 1)
                status = StepStatus.Complete;
            else if (next.Value.Step < 0 && i <= -next.Value.Step - 1)
                status = StepStatus.Next;
            else if (next.Value.Surgery.Owner != _surgery.Value.Ent)
                status = StepStatus.Incomplete;
            else if (next.Value.Step == i)
                status = StepStatus.Next;
            else if (i < next.Value.Step)
                status = StepStatus.Complete;

            stepButton.Button.Disabled = status != StepStatus.Next;

            var stepName = new FormattedMessage();
            stepName.AddText(_entMan.GetComponent<MetaDataComponent>(stepButton.Step).EntityName);

            if (status == StepStatus.Complete)
                stepButton.Button.Modulate = Color.Green;
            else
            {
                stepButton.Button.Modulate = Color.White;
                if (status == StepStatus.Next
                    && !_system.CanPerformStepWithHeld(player, Owner, _part.Value, stepButton.Step, false, out var popup))
                    stepButton.ToolTip = popup;
            }

            var texture = _entMan.GetComponentOrNull<SpriteComponent>(stepButton.Step)?.Icon?.Default;
            stepButton.Set(stepName, texture);
            i++;
        }
    }

    private void View(ViewType type)
    {
        if (_window == null)
            return;

        _window.PartsButton.Parent!.Margin = new Thickness(0, 0, 0, 10);

        _window.Parts.Visible = type == ViewType.Parts;
        _window.PartsButton.Disabled = type == ViewType.Parts;

        _window.Surgeries.Visible = type == ViewType.Surgeries;
        _window.SurgeriesButton.Disabled = type != ViewType.Steps;

        _window.Steps.Visible = type == ViewType.Steps;
        _window.StepsButton.Disabled = type != ViewType.Steps || _previousSurgeries.Count == 0;

        if (_entMan.TryGetComponent(_part, out MetaDataComponent? partMeta) &&
            _entMan.TryGetComponent(_surgery?.Ent, out MetaDataComponent? surgeryMeta))
            _window.Title = $"Surgery - {partMeta.EntityName}, {surgeryMeta.EntityName}";
        else if (partMeta != null)
            _window.Title = $"Surgery - {partMeta.EntityName}";
        else
            _window.Title = "Surgery";
    }

    private enum ViewType
    {
        Parts,
        Surgeries,
        Steps
    }

    private enum StepStatus
    {
        Next,
        Complete,
        Incomplete
    }
}
