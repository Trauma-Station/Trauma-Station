// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.UIKit.UserInterface.Chat;

public sealed class ShaderLabel : Label
{
    private readonly ShaderInstance? _shader;

    public ShaderLabel()
    {
        RobustXamlLoader.Load(this);
    }

    public ShaderLabel(ShaderInstance shader)
    {
        RobustXamlLoader.Load(this);

        _shader = shader;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_shader == null)
            return;

        handle.UseShader(_shader);
        base.Draw(handle);
        handle.UseShader(null);
    }
}
