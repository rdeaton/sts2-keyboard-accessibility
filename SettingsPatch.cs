using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace KeyboardAccessibility.Patches;

[HarmonyPatch]
internal static class SettingsPatch
{
    private const string TickboxName = "KA_AutoPlayTickbox";
    private const string RowName = "KA_AutoPlay";

    [HarmonyPatch(typeof(NSettingsScreen), "_Ready")]
    [HarmonyPostfix]
    static void SettingsScreenReadyPostfix(NSettingsScreen __instance)
    {
        var generalPanel = __instance.GetNode<NSettingsPanel>("%GeneralSettings");
        var content = generalPanel.Content;

        if (content.HasNode(RowName))
            return;

        var templateRow = content.GetNode<Node>("SkipIntroLogo");
        var newRow = templateRow.Duplicate();
        newRow.Name = RowName;

        var label = newRow.GetNode<MegaRichTextLabel>("Label");
        label.Text = "Auto-Play Cards";

        foreach (var child in newRow.GetChildren())
        {
            if (child is NIntroLogoTickbox tickbox)
            {
                tickbox.Name = TickboxName;
                break;
            }
        }

        content.AddChild(newRow);

        // Set initial state after the node enters the tree (_Ready has run)
        var tb = newRow.GetNodeOrNull<NIntroLogoTickbox>(TickboxName);
        if (tb != null)
            tb.IsTicked = ModConfig.AutoPlayEnabled;
    }

    [HarmonyPatch(typeof(NIntroLogoTickbox), "OnTick")]
    [HarmonyPrefix]
    static bool OnTickPrefix(NIntroLogoTickbox __instance)
    {
        if (__instance.Name != TickboxName)
            return true;

        ModConfig.SetAutoPlay(true);
        return false;
    }

    [HarmonyPatch(typeof(NIntroLogoTickbox), "OnUntick")]
    [HarmonyPrefix]
    static bool OnUntickPrefix(NIntroLogoTickbox __instance)
    {
        if (__instance.Name != TickboxName)
            return true;

        ModConfig.SetAutoPlay(false);
        return false;
    }

    [HarmonyPatch(typeof(NIntroLogoTickbox), "SetFromSettings")]
    [HarmonyPrefix]
    static bool SetFromSettingsPrefix(NIntroLogoTickbox __instance)
    {
        if (__instance.Name != TickboxName)
            return true;

        __instance.IsTicked = ModConfig.AutoPlayEnabled;
        return false;
    }
}
