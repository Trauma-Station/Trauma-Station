// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.ChemiCompiler;

namespace Content.Trauma.Client.ChemiCompiler.UI;

[GenerateTypedNameReferences]
public sealed partial class ChemiCompilerWindow : FancyWindow
{
    /// <summary>
    /// A slot button lights up green once it holds a program.
    /// </summary>
    private static readonly Color FilledSlotColor = Color.FromHex("#5BA85B");

    /// <summary>
    /// A reservoir button lights up blue once it holds a beaker.
    /// </summary>
    private static readonly Color FilledReservoirColor = Color.FromHex("#4A7AA8");

    public event Action<int, string>? OnSave;
    public event Action<int>? OnRun;
    public event Action<int>? OnReservoir;
    public event Action? OnHalt;

    private readonly Button[] _slots = new Button[ChemiCompilerComponent.CodeSlots];
    private readonly Button[] _reservoirs = new Button[ChemiCompilerComponent.Reservoirs];

    /// <summary>
    /// The code held in each slot, or null for slots that are empty or locked.
    /// Kept so the Load button can fill the editor without another round trip to the server.
    /// </summary>
    private string?[] _programs = new string?[ChemiCompilerComponent.CodeSlots];

    private bool _running;

    public ChemiCompilerWindow()
    {
        RobustXamlLoader.Load(this);

        for (var i = 0; i < ChemiCompilerComponent.CodeSlots; i++)
        {
            var slot = i;
            var button = new Button
            {
                Text = (i + 1).ToString(),
                MinWidth = 40,
            };
            button.OnPressed += _ => SlotPressed(slot);

            _slots[i] = button;
            SlotContainer.AddChild(button);
        }

        for (var i = 0; i < ChemiCompilerComponent.Reservoirs; i++)
        {
            var reservoir = i + 1;
            var button = new Button
            {
                Text = $"r{reservoir}",
                MinWidth = 48,
            };
            button.OnPressed += _ => OnReservoir?.Invoke(reservoir);

            _reservoirs[i] = button;
            // first five on the top row, the rest below, matching the machine's layout
            (i < 5 ? ReservoirTop : ReservoirBottom).AddChild(button);
        }

        // save and load are mutually exclusive, and both are off by default so slots run when pressed
        SaveButton.OnToggled += args =>
        {
            if (args.Pressed)
                LoadButton.Pressed = false;
            UpdateMode();
        };
        LoadButton.OnToggled += args =>
        {
            if (args.Pressed)
                SaveButton.Pressed = false;
            UpdateMode();
        };

        HaltButton.OnPressed += _ => OnHalt?.Invoke();
    }

    /// <summary>
    /// Points the material list at the machine. It reads the storage itself rather than taking it from
    /// the ui state, so this only needs doing once.
    /// </summary>
    public void SetOwner(EntityUid owner)
    {
        MaterialsList.SetOwner(owner);
    }

    public void UpdateState(ChemiCompilerState state)
    {
        _programs = state.Programs;
        _running = state.Running;

        for (var i = 0; i < _slots.Length; i++)
        {
            var filled = i < state.Filled.Length && state.Filled[i];
            _slots[i].ModulateSelfOverride = filled ? FilledSlotColor : null;
            _slots[i].Disabled = _running;
        }

        for (var i = 0; i < _reservoirs.Length; i++)
        {
            var filled = i < state.Reservoirs.Length && state.Reservoirs[i];
            _reservoirs[i].ModulateSelfOverride = filled ? FilledReservoirColor : null;
            _reservoirs[i].Disabled = _running;
        }

        SaveButton.Disabled = _running;
        LoadButton.Disabled = _running;
        HaltButton.Disabled = !_running;

        RegisterLabel.Text = Loc.GetString("chemicompiler-window-registers-values",
            ("source", state.Source),
            ("target", state.Target),
            ("amount", state.Amount));

        StatusLabel.Text = Loc.GetString(_running
            ? "chemicompiler-window-status-running"
            : "chemicompiler-window-status-idle");

        UpdateMode();
    }

    private void SlotPressed(int slot)
    {
        if (_running)
            return;

        if (SaveButton.Pressed)
        {
            OnSave?.Invoke(slot, Rope.Collapse(CodeEdit.TextRope));
            SaveButton.Pressed = false;
            UpdateMode();
            return;
        }

        if (LoadButton.Pressed)
        {
            // a locked program reads back as nothing, which is the whole point of locking it
            CodeEdit.TextRope = new Rope.Leaf(_programs[slot] ?? string.Empty);
            LoadButton.Pressed = false;
            UpdateMode();
            return;
        }

        OnRun?.Invoke(slot);
    }

    private void UpdateMode()
    {
        ModeLabel.Text = Loc.GetString(SaveButton.Pressed
            ? "chemicompiler-window-mode-save"
            : LoadButton.Pressed
                ? "chemicompiler-window-mode-load"
                : "chemicompiler-window-mode-run");
    }
}
