// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Guidebook.Controls;

namespace Content.Client.UserInterface.Controls.FancyTree;

/// <summary>
/// Trauma - implement ISearchableControl so you can search for guidebook entries
/// </summary>
public sealed partial class TreeItem
{
    public List<TreeItem> ChildItems = new();

    public bool CheckMatchesSearch(string query)
    {
        // recursive so parents of the desired guidebook are still shown.
        foreach (var child in ChildItems)
        {
            if (child.CheckMatchesSearch(query))
                return true;
        }

        // check this entry itself
        return EntryMatchesSearch(query);
    }

    public void SetHiddenState(bool state, string query)
    {
        // TODO: grey out if descendant matches but not this
        Visible = CheckMatchesSearch(query) == state;
    }

    public bool EntryMatchesSearch(string query)
        => Label.Text?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
}
