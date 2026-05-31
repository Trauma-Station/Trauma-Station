// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Guidebook.Richtext;
using System.Diagnostics.CodeAnalysis;

namespace Content.Trauma.Client.Guidebook.Controls;

/// <summary>
/// Displays a texture in the guidebook.
/// You can use either <c>Sprite="error.rsi" State="error"</c> or <c>Path="Interface/eject.png"</c> for rsi and raw pngs respectively.
/// Optionally you can override the <see cref="DefaultScale"/> using <c>Scale="1.234"</c>.
/// In either case you don't need to specify the <c>/Textures</c> prefix in the path.
/// The sprite state can be animated.
/// </summary>
public sealed partial class TextureEmbed : Control, IDocumentTag
{
    public static readonly ResPath TextureRoot = new("/Textures");
    public const float DefaultScale = 4f;

    private AnimatedTextureRect _texture = new();

    public TextureEmbed()
    {
        _texture.DisplayRect.TextureScale = new Vector2(DefaultScale, DefaultScale);
        AddChild(_texture);
    }

    bool IDocumentTag.TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        SpriteSpecifier specifier = default!;
        if (args.TryGetValue("Path", out var path))
        {
            specifier = new SpriteSpecifier.Texture(TextureRoot / path);
        }
        else
        {
            if (!args.TryGetValue("Sprite", out var rsiPath))
            {
                Log.Error("Missing 'sprite' xml attribute, the path to the .rsi");
                return false;
            }
            if (!args.TryGetValue("State", out var state))
            {
                Log.Error("Missing 'state' xml attribute, the state of a .rsi");
                return false;
            }
            specifier = new SpriteSpecifier.Rsi(TextureRoot / rsiPath, state);
        }

        var scale = DefaultScale;
        if (args.TryGetValue("Scale", out var scaleStr))
        {
            if (!float.TryParse(scaleStr, out scale))
            {
                Log.Error($"Invalid scale value {scaleStr}");
                return false;
            }
        }
        _texture.DisplayRect.TextureScale = new Vector2(scale, scale);
        Margin = new Thickness(4, 8);

        control = this;
        _texture.SetFromSpriteSpecifier(specifier);
        return true;
    }
}
