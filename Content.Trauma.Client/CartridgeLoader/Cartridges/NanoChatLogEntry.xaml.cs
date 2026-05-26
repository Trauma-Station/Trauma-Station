// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class NanoChatLogEntry : BoxContainer
{
    public NanoChatLogEntry(int number, string time, string message)
    {
        RobustXamlLoader.Load(this);
        NumberLabel.Text = number.ToString();
        TimeLabel.Text = time;
        MessageLabel.Text = message;
    }
}
