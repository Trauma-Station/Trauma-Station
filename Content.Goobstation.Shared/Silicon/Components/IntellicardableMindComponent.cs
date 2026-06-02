// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Silicon.Components;

/// <summary>
/// Declares that this entity's MindContainerComponent can be transferred to/from via an intellicard.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(IntellicardExtrasSystem))]
public sealed partial class IntellicardableMindComponent : Component
{
    /// <summary>
    /// How many times longer it takes to download than it does when downloading from an AI Core.
    /// </summary>
    [DataField]
    public float DownloadTimeFactor = 0.3f;

    /// <summary>
    /// How many times longer it takes to download than it does when uploading to an AI Core.
    /// </summary>
    [DataField]
    public float UploadTimeFactor = 0.8f;
}
