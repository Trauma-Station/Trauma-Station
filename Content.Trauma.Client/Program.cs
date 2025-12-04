// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Client;

namespace Content.Trauma.Client;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ContentStart.Start(args);
    }
}
