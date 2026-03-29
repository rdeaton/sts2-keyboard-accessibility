using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace KeyboardAccessibility;

[ModInitializer(nameof(Initialize))]
public static class Plugin
{
    public static void Initialize()
    {
        var harmony = new Harmony("KeyboardAccessibility");
        harmony.PatchAll(typeof(Plugin).Assembly);

        CombatState.InitDelegates();
        ModConfig.Load();

        SceneTree tree = (SceneTree)Engine.GetMainLoop();
        GlobalInputHandler.Register(tree);

        // IsRunningModded is set based on LoadedMods.Count > 0, which routes
        // saves to a "modded/" subdirectory. affects_gameplay only controls
        // multiplayer checks, not save paths. Mark our state as Disabled so
        // GetLoadedMods() (which filters by state == Loaded) excludes us.
        // We can't remove from _mods directly — it's being iterated in a foreach.
        ModManager.OnModDetected += OnModDetected;
    }

    private static void OnModDetected(Mod mod)
    {
        if (mod.manifest?.id != "KeyboardAccessibility")
            return;

        ModManager.OnModDetected -= OnModDetected;
        mod.state = ModLoadState.Disabled;
    }
}
