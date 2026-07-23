// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Reactor;

/// <summary>
/// Appearance keys for the reactor.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorVisuals : byte
{
    Melted,
    Status,
    Input,
    Output,
    Lights,
    Smoke,
    Fire,
}

/// <summary>
/// Visual sprite layers for the reactor.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorVisualLayers : byte
{
    Base,
    Status,
    Input,
    Output,
    Lights,
    Smoke,
    Fire,
}

/// <summary>
/// Status screens.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorStatusLights : byte
{
    Off,
    Active,
    Overheat,
    Meltdown,
}

/// <summary>
/// Warning lights settings.
/// </summary>
[Serializable, NetSerializable]
public enum ReactorWarningLights : byte
{
    LightsOff,
    LightsWarning,
    LightsMeltdown,
}
