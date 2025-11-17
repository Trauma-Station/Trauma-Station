// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 FoxxoTrystan <45297731+FoxxoTrystan@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Mnemotechnican <69920617+Mnemotechnician@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._EinsteinEngines.Language.Systems;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._EinsteinEngines.Language.Systems;

/// <summary>
/// Provides API to set current language and action for updating UI when languages change.
/// </summary>
public sealed class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    /// <summary>
    ///     Invoked when the Languages of the local player entity change, for use in UI.
    /// </summary>
    public event Action? OnLanguagesChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageSpeakerComponent, AfterAutoHandleStateEvent>(OnAutoHandleState);

        _player.LocalPlayerAttached += _ => OnLanguagesChanged?.Invoke();
    }

    private void OnAutoHandleState(Entity<LanguageSpeakerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            OnLanguagesChanged?.Invoke();
    }

    /// <summary>
    ///     Returns the LanguageSpeakerComponent of the local player entity.
    ///     Will return null if the player does not have an entity, or if the client has not yet received the component state.
    /// </summary>
    public LanguageSpeakerComponent? GetLocalSpeaker()
        => CompOrNull<LanguageSpeakerComponent>(_player.LocalEntity);

    public void RequestSetLanguage(ProtoId<LanguagePrototype> language)
    {
        if (GetLocalSpeaker()?.CurrentLanguage.Equals(language) == true)
            return;

        RaisePredictiveEvent(new LanguagesSetMessage(language));
    }
}
