// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface.RichText;

namespace Content.Goobstation.UIKit.UserInterface.RichText;

public sealed class ExamineBorderTag : IMarkupTagHandler
{
    public const string TagName = "examineborder";

    public string Name => TagName;
}
