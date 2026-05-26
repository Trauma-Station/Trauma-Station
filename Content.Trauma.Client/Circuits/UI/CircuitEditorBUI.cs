// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Circuits;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace Content.Trauma.Client.Circuits.UI;

public sealed partial class CircuitEditorBUI : BoundUserInterface
{
    [Dependency] private IFileDialogManager _dialog = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private ISerializationManager _serMan = default!;
    private CircuitEditorSystem _editor;

    private ISawmill _sawmill;
    private CircuitEditorWindow? _window;
    private CircuitData? _data;

    public CircuitEditorBUI(EntityUid owner, Enum key) : base(owner, key)
    {
        _sawmill = _log.GetSawmill("circuits");
        _editor = EntMan.System<CircuitEditorSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CircuitEditorWindow>();
        _window.OnClear += () => SendPredictedMessage(new CircuitEditorClearMessage());
        _window.OnImport += () =>
        {
            ImportCircuit();
        };
        _window.OnExport += () =>
        {
            ExportCircuit();
        };
        _window.OnAddGate += gate => SendPredictedMessage(new CircuitEditorAddGateMessage(gate));
        _window.OnMoveGate += (index, pos) => SendPredictedMessage(new CircuitEditorMoveGateMessage(index, pos));
        _window.OnRemoveGate += index => SendPredictedMessage(new CircuitEditorRemoveGateMessage(index));
        _window.OnLinkGate += (input, index, n) => SendPredictedMessage(new CircuitEditorLinkMessage(input, index, n));
        _window.OnUnlinkGate += (index, n) => SendPredictedMessage(new CircuitEditorUnlinkMessage(index, n));
    }

    private async void ImportCircuit()
    {
        var filters = new FileDialogFilters(new FileDialogFilters.Group("yml"));
        if (await _dialog.OpenFile(filters) is not { } file)
            return;

        try
        {
            using var reader = new StreamReader(file, EncodingHelpers.UTF8);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);
            var root = yamlStream.Documents[0].RootNode;
            var data = _serMan.Read<CircuitData>(root.ToDataNode(), notNullableOverride: true);
            _sawmill.Info($"Sending import message for {data.Gates.Count} gates");
            SendPredictedMessage(new CircuitEditorImportMessage(data));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error when importing circuit: {e}");
        }
    }

    private async void ExportCircuit()
    {
        if (_data is not { } data)
            return;

        var filters = new FileDialogFilters(new FileDialogFilters.Group("yml"));
        if (await _dialog.SaveFile(filters) is not { } file)
            return;

        try
        {
            var node = _serMan.WriteValue(data.GetType(), data);
            await using var writer = new StreamWriter(file.fileStream);
            node.Write(writer);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error when exporting circuit: {e}");
        }
        finally
        {
            await file.fileStream.DisposeAsync();
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CircuitEditorState cast)
            return;

        _data = cast.Data;
        if (_data is { } data &&
            EntMan.GetEntity(cast.Circuit) is { } circuit &&
            EntMan.TryGetComponent<CircuitComponent>(circuit, out var comp))
        {
            comp.Data = data; // update it so messages can be predicted, basically per-client networked field
        }

        _window?.UpdateState(cast);
    }
}
