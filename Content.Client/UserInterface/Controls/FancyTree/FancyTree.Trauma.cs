namespace Content.Client.UserInterface.Controls.FancyTree;

/// <summary>
/// Trauma - handle searching for guidebook entries
/// </summary>
public sealed partial class FancyTree
{
    private void InitTrauma()
    {
        SearchBar.OnTextChanged += _ => UpdateFilter();
    }

    private void UpdateFilter()
    {
        var query = SearchBar.Text.Trim();
        foreach (var item in Items)
        {
            item.SetHiddenState(true, query);
        }
    }
}
