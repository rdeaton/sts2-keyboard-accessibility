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

// Keep our saves in the game's unmodded save location instead of the
// separate "modded/" tree the game routes to when any mod is loaded.
//
// Every profile save path funnels through UserDataPathProvider.GetAccountDir(),
// which returns "modded" vs "" based on the UserDataPathProvider.IsRunningModded
// property (set once at boot from ModManager.IsRunningModded()). Force that
// property's getter to false so all default save reads/writes resolve to the
// unmodded location.
//
// We patch the property rather than GetProfileDir because, as of the game's
// save refactor, GetProfileDir is overloaded (GetProfileDir(int) and
// GetProfileDir(int, bool?)) and can no longer be targeted unambiguously by
// name. The IsRunningModded getter is a single, stable chokepoint. The game's
// own unmodded->modded copy/migration routines pass an explicit forceModState
// argument, so they are unaffected by this override.
[HarmonyPatch(
    typeof(UserDataPathProvider),
    nameof(UserDataPathProvider.IsRunningModded),
    MethodType.Getter
)]
static class SavePathPatch
{
    static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
