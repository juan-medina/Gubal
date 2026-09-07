using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace Gubal;

public record SearchCommand(string Url, string HelpMessage, bool Enabled = true);

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public Dictionary<string, SearchCommand> SearchCommands { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/wiki", new SearchCommand("https://ffxiv.consolegameswiki.com/mediawiki/index.php?search={text}", "Search in wiki") },
        { "/lodestone", new SearchCommand("https://www.google.com/search?q=site:eu.finalfantasyxiv.com/lodestone+{text}", "Search in Lodestone") },
    };

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
