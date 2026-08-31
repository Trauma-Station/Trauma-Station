// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Botany.PlantAnalyzer;

[Serializable, NetSerializable]
public enum PlantAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSetMode(PlantAnalyzerModes modes) : BoundUserInterfaceMessage
{
    public PlantAnalyzerModes Mode { get; } = modes;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerSetGeneIndex(int index, bool isDatabank) : BoundUserInterfaceMessage
{
    public int Index { get; } = index;
    public bool IsDatabank { get; } = isDatabank;
}

[Serializable, NetSerializable]
public sealed class PlantAnalyzerDeleteDatabankEntry : BoundUserInterfaceMessage;
