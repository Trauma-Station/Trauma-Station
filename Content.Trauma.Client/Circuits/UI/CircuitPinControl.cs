// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;
using Robust.Client.ResourceManagement;
using Robust.Shared.Input;

namespace Content.Trauma.Client.Circuits.UI;

public sealed class CircuitPinControl : PanelContainer
{
    public event Action<CircuitPinControl>? OnPressed;

    public CircuitIndex Index;
    public int N;

    /// <summary>
    /// Whether this pin belongs to a gate or the circuit's I/O ports.
    /// </summary>
    public bool IsOnGate => Index.IsGate;

    public bool IsOutput => N < 0;

    private static readonly ResPath InputTexture = new("/Textures/_Trauma/Interface/circuits_input_pin.png");
    private static readonly ResPath OutputTexture = new("/Textures/_Trauma/Interface/circuits_output_pin.png");

    public CircuitPinControl(IResourceCache cache, CircuitIndex index, int n = 0)
    {
        Index = index;
        N = n;

        MouseFilter = MouseFilterMode.Pass;
        var path = IsOutput ? OutputTexture : InputTexture;
        PanelOverride = new StyleBoxTexture()
        {
            Texture = cache.GetResource<TextureResource>(path).Texture,
            Mode = StyleBoxTexture.StretchMode.Stretch
        };
        MinSize = new Vector2(32f, 32f);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        args.Handle();
        OnPressed?.Invoke(this);
    }
}
