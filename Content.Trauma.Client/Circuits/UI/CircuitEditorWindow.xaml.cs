// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Circuits;
using Robust.Client.ResourceManagement;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Circuits.UI;

[GenerateTypedNameReferences]
public sealed partial class CircuitEditorWindow : FancyWindow
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IResourceCache _cache = default!;

    public event Action? OnClear;
    public event Action? OnImport;
    public event Action? OnExport;
    public event Action<CircuitGate>? OnAddGate;
    public event Action<int, Vector2>? OnMoveGate;
    public event Action<int>? OnRemoveGate;
    public event Action<CircuitIndex, CircuitIndex, int>? OnLinkGate;
    public event Action<CircuitIndex, int>? OnUnlinkGate;

    private CircuitData? _data;
    public bool HasCircuit => _data != null;
    private CircuitPinControl? _linking;
    private CircuitGateControl? _dragging;
    private Vector2 _dragOffset;

    private Vector2 MousePos => UserInterfaceManager.MousePositionScaled.Position;

    private static readonly ResPath TexturePath = new("/Textures/_Trauma/Interface/circuits_grid.png");
    private static readonly Color LinkingColor = new(0.9f, 0.4f, 0.4f, 0.9f);
    private static readonly Color LinkedColor = new(0.9f, 0.9f, 0.4f, 0.9f);
    private static readonly Vector2 Center = new(500f, 500f);

    public CircuitEditorWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        // center scroll after ui is ready
        UserInterfaceManager.DeferAction(() =>
        {
            GatesScroll.SetScrollValue(new Vector2(280f, 280f)); // nfi why this makes it centered...
        });
        GatesPanel.PanelOverride = new StyleBoxTexture()
        {
            Texture = _cache.GetResource<TextureResource>(TexturePath).Texture,
            Mode = StyleBoxTexture.StretchMode.Tile
        };

        ClearButton.OnPressed += _ =>
        {
            if (HasCircuit)
                OnClear?.Invoke();
        };
        ImportButton.OnPressed += _ =>
        {
            if (HasCircuit)
                OnImport?.Invoke();
        };
        ExportButton.OnPressed += _ =>
        {
            if (HasCircuit)
                OnExport?.Invoke();
        };

        SetupPicker();

        for (var i = 0; i < CircuitComponent.PortsCount; i++)
        {
            var index = CircuitIndex.Port(i);
            // n of -1 indicates output pin, 0 is input pin
            // input ports output a value, output ports take a value so they're seemingly inverted
            var inputPin = new CircuitPinControl(_cache, index, -1);
            inputPin.OnPressed += OnPinPressed;
            InputPortsContainer.AddChild(inputPin);
            var outputPin = new CircuitPinControl(_cache, index, 0);
            outputPin.OnPressed += OnPinPressed;
            OutputPortsContainer.AddChild(outputPin);
        }
    }

    public void UpdateState(CircuitEditorState state)
    {
        _data = state.Data;
        var isEmpty = _data?.Gates?.Count is not { } count || count == 0;

        ClearButton.Disabled = isEmpty;
        ImportButton.Disabled = !HasCircuit;
        ExportButton.Disabled = isEmpty;

        GatesContainer.RemoveAllChildren();
        if (_data is { } data)
        {
            if (_timing.IsFirstTimePredicted) // don't spam update it when adding gates
                GateCountLabel.Text = $"{data.Gates.Count} gates total";
            for (var i = 0; i < data.Gates.Count; i++)
            {
                var index = i; // dogshit language :)
                var gate = data.Gates[i];
                var control = new CircuitGateControl(_cache, gate, i);
                control.OnDrag += () =>
                {
                    _dragging = control;
                    _dragOffset = control.Gate.Pos - MousePos;
                };
                control.OnDrop += () =>
                {
                    _dragging = null;
                    OnMoveGate?.Invoke(index, control.Gate.Pos);
                };
                control.OnPinPressed += OnPinPressed;
                control.OnRemove += () => OnRemoveGate?.Invoke(index);
                LayoutContainer.SetPosition(control, Center + gate.Pos);
                GatesContainer.AddChild(control);
            }
        }
        else
        {
            GateCountLabel.Text = "No circuit inserted!";
            _linking = null;
        }

        if (_linking is { } pin && pin.IsOnGate) // only have to look it up again if gates changed, circuit ports dont
            _linking = FindPinControl(pin.Index, pin.N);
    }

    // gate linking overlay
    protected override void PostRenderChildren(ref ControlRenderArguments args)
    {
        base.PostRenderChildren(ref args);

        if (_data is not { } data)
            return;

        var handle = args.Handle.DrawingHandleScreen;

        // increase bounds by 32 for the I/O pins
        // no axis changes needed since everything is using the same coordinate space
        var clip = GatesScroll.GlobalRect;
        var bounds = new Box2(clip.Left - 32f, clip.Top, clip.Right + 32f, clip.Bottom);

        // draw lines between every input and its output
        for (var i = 0; i < data.Gates.Count; i++)
        {
            var gate = data.Gates[i];
            for (int n = 0; n < gate.Inputs.Count; n++)
            {
                var o = gate.Inputs[n];
                if (FindPinControl(o, -1) is not { } output ||
                    FindPinControl(CircuitIndex.Gate(i), n) is not { } input)
                    continue;

                DrawLink(handle, output, input, bounds);
            }
        }
        for (var o = 0; o < data.OutputIndices.Count; o++)
        {
            var i = data.OutputIndices[o];
            if (FindPinControl(i, -1) is not { } output ||
                FindPinControl(CircuitIndex.Port(o), 0) is not { } input)
                continue;

            DrawLink(handle, output, input, bounds);
        }

        // draw line from linked port to your cursor while you are linking
        if (_linking is { } pin)
        {
            var center = pin.GlobalPosition + pin.Size * 0.5f;
            var mousePos = MousePos;
            if (ClipEnd(center, ref mousePos, bounds))
                handle.DrawLine(center, mousePos, LinkingColor);
        }
    }

    private void DrawLink(DrawingHandleScreen handle, Control output, Control input, Box2 bounds)
    {
        var start = output.GlobalPosition + output.Size * 0.5f;
        var end = input.GlobalPosition + input.Size * 0.5f;
        if (ClipBoth(ref start, ref end, bounds))
            handle.DrawLine(start, end, LinkedColor);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_dragging is not {} control)
            return;

        // handle gate dragging
        var pos = CircuitGate.ClampPosition(MousePos + _dragOffset);
        control.Gate.Pos = pos;
        LayoutContainer.SetPosition(control, Center + pos);
    }

    // not drawing inside of the scroll container since it needs to go to I/O ports which do not scroll
    // instead this solves line bounds to manually clip them.
    private bool ClipEnd(Vector2 start, ref Vector2 end, Box2 bounds)
    {
        if (bounds.Contains(start) && bounds.Contains(end))
            return true; // both points are inside, already good

        // find where they intersect the bounds
        var dir = (start - end).Normalized();
        var ray = new Ray(end, dir);
        return ray.Intersects(bounds, out _, out end);
    }

    private bool ClipBoth(ref Vector2 start, ref Vector2 end, Box2 bounds)
        => ClipEnd(start, ref end, bounds) && ClipEnd(end, ref start, bounds);

    private void OnPinPressed(CircuitPinControl pin)
    {
        if (_linking is not { } output)
        {
            // start linking
            _linking = pin;
            return;
        }

        if (pin == output)
        {
            _linking = null; // stop linking when you click the same pin again
            return;
        }

        if (output.IsOutput == pin.IsOutput)
            return; // have to link input and output

        // correct linking order if user selected the "wrong" way
        // linking must be the output pin, pin must be the input
        if (!output.IsOutput)
        {
            var swap = pin;
            pin = output;
            output = swap;
        }

        // toggle the link
        if (IsLinked(output.Index, pin))
            OnUnlinkGate?.Invoke(pin.Index, pin.N);
        else
            OnLinkGate?.Invoke(output.Index, pin.Index, pin.N);

        _linking = null; // done now
    }

    private bool IsLinked(CircuitIndex output, CircuitPinControl input)
    {
        if (_data is not { } data)
            return false;

        if (input.Index.GateIndex is { } g)
            return data.Gates.TryGetValue(g, out var gate) && gate.Inputs[input.N] == output;

        return input.Index.PortIndex is { } p &&
            data.OutputIndices.TryGetValue(p, out var linked) &&
            linked == output;
    }

    private CircuitPinControl? FindPinControl(CircuitIndex i, int n)
    {
        if (!i.Valid)
            return null;

        // negative n is a 1-based output port, positive is 0-based input port
        var output = n < 0;
        if (output)
            n = -n - 1;

        if (i.GateIndex is { } g)
        {
            if (g >= GatesContainer.ChildCount ||
                GatesContainer.Children[g] is not CircuitGateControl gate ||
                gate.GetPin(n, output) is not { } pin)
                return null;

            return pin;
        }

        if (i.PortIndex is not { } p)
            return null;

        // looks funny since input ports have output pins and vice versa
        var ports = output ? InputPortsContainer : OutputPortsContainer;
        return p < ports.ChildCount
            ? (CircuitPinControl) ports.Children[p]
            : null;
    }
}
