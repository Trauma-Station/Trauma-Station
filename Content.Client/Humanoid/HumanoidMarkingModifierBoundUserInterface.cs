// SPDX-FileCopyrightText: 2022 Flipp Syder <76629141+vulppine@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Morb <14136326+Morb0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2025 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid;
using Robust.Client.UserInterface;

namespace Content.Client.Humanoid;

// Marking BUI.
// Do not use this in any non-privileged instance. This just replaces an entire marking set
// with the set sent over.

public sealed class HumanoidMarkingModifierBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HumanoidMarkingModifierWindow? _window;

    private readonly MarkingsViewModel _markingsModel = new();

    public HumanoidMarkingModifierBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<HumanoidMarkingModifierWindow>();
        _window.MarkingPickerWidget.SetModel(_markingsModel);
        _window.RespectLimits.OnPressed += args => _markingsModel.EnforceLimits = args.Button.Pressed;
        _window.RespectGroupSex.OnPressed += args => _markingsModel.EnforceGroupAndSexRestrictions = args.Button.Pressed;

        _markingsModel.MarkingsChanged += (_, _) => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HumanoidMarkingModifierState cast)
            return;

        _markingsModel.OrganData = cast.OrganData;
        _markingsModel.OrganProfileData = cast.OrganProfileData;
        _markingsModel.Markings = cast.Markings;
    }

    private void SendMarkingSet()
    {
        SendMessage(new HumanoidMarkingModifierMarkingSetMessage(_markingsModel.Markings));
    }
}
