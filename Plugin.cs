using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;

namespace KeyboardAccessibility;

[ModInitializer(nameof(Initialize))]
public static class Plugin
{
    public static void Initialize()
    {
        try
        {
            GD.Print("[KeyboardAccessibility] Initializing...");

            var harmony = new Harmony("KeyboardAccessibility");
            harmony.PatchAll(typeof(Plugin).Assembly);
            GD.Print("[KeyboardAccessibility] Harmony patches applied.");

            CombatState.InitDelegates();
            ModConfig.Load();

            SceneTree tree = (SceneTree)Engine.GetMainLoop();
            GlobalInputHandler.Register(tree);
            GD.Print("[KeyboardAccessibility] Initialized successfully.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[KeyboardAccessibility] Failed to initialize: {ex}");
        }
    }
}

// IsRunningModded() gates the "modded/" prefix in save paths
// (UserDataPathProvider.GetProfileDir). affects_gameplay only controls
// multiplayer checks, not save routing. Patch GetProfileDir directly
// so the save path is always the unmodded location, regardless of
// event-handler timing or other mods' load state.
[HarmonyPatch(typeof(UserDataPathProvider), nameof(UserDataPathProvider.GetProfileDir))]
static class SavePathPatch
{
    static bool Prefix(int profileId, ref string __result)
    {
        __result = $"profile{profileId}";
        return false;
    }
}
