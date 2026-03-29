using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace KeyboardAccessibility;

internal static class ModConfig
{
    private const string ConfigDir = "user://mods/KeyboardAccessibility";
    private const string ConfigPath = ConfigDir + "/config.json";

    internal static bool AutoPlayEnabled { get; private set; } = true;

    internal static void Load()
    {
        if (!FileAccess.FileExists(ConfigPath))
            return;

        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        var json = Json.ParseString(file.GetAsText());
        if (json.VariantType != Variant.Type.Dictionary)
            return;

        var dict = json.AsGodotDictionary();
        if (dict.ContainsKey("autoPlayEnabled"))
            AutoPlayEnabled = dict["autoPlayEnabled"].AsBool();
    }

    internal static void Save()
    {
        DirAccess.MakeDirRecursiveAbsolute(ConfigDir);
        using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        if (file == null)
            return;

        var dict = new Godot.Collections.Dictionary { ["autoPlayEnabled"] = AutoPlayEnabled };
        file.StoreString(Json.Stringify(dict, "  "));
    }

    internal static void SetAutoPlay(bool enabled)
    {
        AutoPlayEnabled = enabled;
        Save();
        Log.Info($"[KeyboardAccessibility] Auto-play: {(enabled ? "ON" : "OFF")}");
    }

    internal static void ToggleAutoPlay()
    {
        SetAutoPlay(!AutoPlayEnabled);
    }
}
