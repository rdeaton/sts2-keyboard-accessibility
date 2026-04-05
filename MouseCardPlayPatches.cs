using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KeyboardAccessibility.Patches;

[HarmonyPatch(typeof(NMouseCardPlay))]
internal static class MouseCardPlayPatches
{
    // The game remaps "ui_accept" to E via NInputManager, so we check raw keys
    // since _Input fires before the game's input translation layer.
    private static bool IsConfirmKey(InputEvent inputEvent)
    {
        return inputEvent is InputEventKey key
            && (key.Keycode == Key.Space || key.Keycode == Key.Enter)
            && key.IsPressed()
            && !key.IsEcho();
    }

    [HarmonyPrefix]
    [HarmonyPatch("_Input")]
    static bool InputPrefix(
        NMouseCardPlay __instance,
        InputEvent inputEvent,
        bool ____skipStartCardDrag,
        CancellationTokenSource ____cancellationTokenSource
    )
    {
        if (____cancellationTokenSource.IsCancellationRequested)
            return true;

        // Block all input during multi-target selection
        if (CombatState.HasMultipleTargets && NTargetManager.Instance.IsInSelection)
            return false;

        if (____skipStartCardDrag)
        {
            var targetType = __instance.Holder?.CardModel?.TargetType;
            bool isNonTargeted =
                targetType.HasValue && targetType != TargetType.AnyEnemy && targetType != TargetType.AnyAlly;

            if (isNonTargeted)
            {
                bool shouldPlay = ModConfig.AutoPlayEnabled || IsConfirmKey(inputEvent);
                if (shouldPlay)
                {
                    CombatState.TryPlayCard!(__instance, null);
                    ____cancellationTokenSource.Cancel();
                    __instance.GetViewport()?.SetInputAsHandled();
                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("IsCardInPlayZone")]
    static void IsCardInPlayZonePostfix(bool ____skipStartCardDrag, ref bool __result)
    {
        if (____skipStartCardDrag)
            __result = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("MultiCreatureTargeting")]
    static bool MultiCreatureTargetingPrefix(
        NMouseCardPlay __instance,
        bool ____skipStartCardDrag,
        CancellationTokenSource ____cancellationTokenSource,
        ref Task __result
    )
    {
        if (!____skipStartCardDrag || !ModConfig.AutoPlayEnabled)
            return true;

        CombatState.TryPlayCard!(__instance, null);

        ____cancellationTokenSource.Cancel();

        __result = Task.CompletedTask;
        return false;
    }
}
