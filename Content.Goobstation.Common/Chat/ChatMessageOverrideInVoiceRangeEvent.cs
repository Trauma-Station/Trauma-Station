// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Chat;

[ByRefEvent]
public record struct ChatMessageOverrideInVoiceRange(bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
