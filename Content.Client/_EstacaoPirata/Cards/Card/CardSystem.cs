// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 RadsammyT <32146976+RadsammyT@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Card;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._EstacaoPirata.Cards.Card;

public sealed class CardSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardComponent, ComponentStartup>(OnComponentStartupEvent);
        SubscribeNetworkEvent<CardFlipUpdatedEvent>(OnFlip);
    }

    private void OnComponentStartupEvent(EntityUid uid, CardComponent comp, ComponentStartup args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            if (!sprite.TryGetLayer(i, out var layer) || layer.State.Name == null)
                continue;

            var rsi = layer.RSI ?? sprite.BaseRSI;
            if (rsi == null)
                continue;

            comp.FrontSprite.Add(new SpriteSpecifier.Rsi(rsi.Path, layer.State.Name));
        }

        comp.BackSprite ??= comp.FrontSprite;
        DirtyEntity(uid);
        UpdateSprite(uid, comp);
    }

    private void OnFlip(CardFlipUpdatedEvent args)
    {
        if (!TryComp(GetEntity(args.Card), out CardComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Card), comp);
    }

    private void UpdateSprite(EntityUid uid, CardComponent comp)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var newSprite = comp.Flipped ? comp.BackSprite : comp.FrontSprite;
        var layerCount = newSprite.Count();

        //inserts Missing Layers
        if (sprite.AllLayers.Count() < layerCount)
        {
            for (var i = sprite.AllLayers.Count(); i < layerCount; i++)
            {
                sprite.AddBlankLayer(i);
            }
        }
        //Removes extra layers
        else if (sprite.AllLayers.Count() > layerCount)
        {
            for (var i = sprite.AllLayers.Count() - 1; i >= layerCount; i--)
            {
                sprite.RemoveLayer(i);
            }
        }

        for (var i = 0; i < newSprite.Count(); i++)
        {
            var layer = newSprite[i];
            sprite.LayerSetSprite(i, layer);
        }
    }
}
