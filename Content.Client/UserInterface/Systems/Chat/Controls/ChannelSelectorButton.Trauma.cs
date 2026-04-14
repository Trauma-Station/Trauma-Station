// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Client.UserInterface.Systems.Chat.Controls;
public sealed partial class ChannelSelectorButton
{
    public void UpdateChannelSelectButton(ChatSelectChannel channel, Shared.Radio.RadioChannelPrototype? radio, CollectiveMindPrototype? collectiveMind = null)
    {
        if (radio != null)
        {
            Text = Loc.GetString(radio.Name);
            Modulate = radio?.Color ?? ChannelSelectColor(channel);
        }
        else if (collectiveMind != null)
        {
            Text = Loc.GetString(collectiveMind.Name);
            Modulate = collectiveMind.Color;
        }
        else
        {
            Text = ChannelSelectorName(channel);
            Modulate = ChannelSelectColor(channel);
        }
    }
}
