using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace KeyboardAccessibility;

public static class GlobalInputHandler
{
    internal static readonly StringName[] SelectActions =
    {
        MegaInput.selectCard1,
        MegaInput.selectCard2,
        MegaInput.selectCard3,
        MegaInput.selectCard4,
        MegaInput.selectCard5,
        MegaInput.selectCard6,
        MegaInput.selectCard7,
        MegaInput.selectCard8,
        MegaInput.selectCard9,
    };

    private static readonly StringName ToggleAutoPlayAction = "ka_toggle_autoplay";
    private static readonly StringName ConfirmAction = "ka_confirm";
    private static readonly StringName NextRowAction = "ka_next_row";
    private static readonly StringName PrevRowAction = "ka_prev_row";

    private static readonly FieldInfo ChestButtonField = typeof(NTreasureRoom).GetField(
        "_chestButton",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo RewardButtonsField = typeof(NRewardsScreen).GetField(
        "_rewardButtons",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo CardRowField = typeof(NCardRewardSelectionScreen).GetField(
        "_cardRow",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo RewardAlternativesField = typeof(NCardRewardSelectionScreen).GetField(
        "_rewardAlternativesContainer",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo MapPointDictField = typeof(NMapScreen).GetField(
        "_mapPointDictionary",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo RestChoicesField = typeof(NRestSiteRoom).GetField(
        "_choicesContainer",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo GridField = typeof(NCardGridSelectionScreen).GetField(
        "_grid",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static readonly FieldInfo CardRowsField = typeof(NCardGrid).GetField(
        "_cardRows",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private static NRewardsScreen? _lastRewardsScreen;
    private static int _lastButtonCount;

    private static List<NMapPoint> _lastTravelablePoints = new();
    private static NMapScreen? _lastMapScreen;

    private static NCardGridSelectionScreen? _lastGridScreen;
    private static int _gridSelectedRow;

    private static void RegisterAction(StringName action, Key key, bool shift = false)
    {
        if (InputMap.HasAction(action))
            return;
        InputMap.AddAction(action);
        var ev = new InputEventKey();
        ev.Keycode = key;
        ev.ShiftPressed = shift;
        InputMap.ActionAddEvent(action, ev);
    }

    private static void LabelItems<T>(
        IReadOnlyList<T> items,
        int fontSize,
        Vector2 size,
        Func<T, Vector2> offset,
        int outlineSize = 5,
        int startIndex = 0
    )
        where T : Node
    {
        for (int i = 0; i < items.Count && startIndex + i < SelectActions.Length; i++)
            NumberLabels.AddOrUpdate(items[i], startIndex + i + 1, fontSize, size, offset(items[i]), outlineSize);
    }

    private static int? GetPressedIndex(int count, int startIndex = 0)
    {
        for (int i = 0; i < count && startIndex + i < SelectActions.Length; i++)
        {
            if (Input.IsActionJustPressed(SelectActions[startIndex + i]))
                return i;
        }
        return null;
    }

    public static void Register(SceneTree tree)
    {
        tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));

        RegisterAction(ToggleAutoPlayAction, Key.F7);
        RegisterAction(ConfirmAction, Key.Space);
        RegisterAction(NextRowAction, Key.Tab);
        RegisterAction(PrevRowAction, Key.Tab, shift: true);
    }

    private static void OnProcessFrame()
    {
        if (Input.IsActionJustPressed(ToggleAutoPlayAction))
            ModConfig.ToggleAutoPlay();

        var currentScreen = ActiveScreenContext.Instance.GetCurrentScreen();
        TryHandleTreasureRoom(currentScreen);
        TryHandleCardRewardScreen(currentScreen);
        TryHandleRewardsScreen(currentScreen);
        TryHandleMapScreen(currentScreen);
        TryHandleRestSite(currentScreen);
        TryHandleCardGridSelection(currentScreen);
        TryHandleEventScreen(currentScreen);
    }

    private static void TryHandleTreasureRoom(IScreenContext? currentScreen)
    {
        if (currentScreen is not NTreasureRoom treasureRoom)
            return;

        if (!Input.IsActionJustPressed(SelectActions[0]) && !Input.IsActionJustPressed(ConfirmAction))
            return;

        var chestButton = (NButton)ChestButtonField.GetValue(treasureRoom)!;
        if (chestButton.IsEnabled)
            chestButton.ForceClick();
    }

    private static void TryHandleCardRewardScreen(IScreenContext? currentScreen)
    {
        if (currentScreen is not NCardRewardSelectionScreen cardRewardScreen)
            return;

        if (!cardRewardScreen.IsVisibleInTree())
            return;

        var cardRow = (Control)CardRowField.GetValue(cardRewardScreen)!;
        // GetChildren() order matches AddChild() order, which is left-to-right.
        // Do NOT sort by Position.X — positions are animated with a 0.5s tween.
        var holders = cardRow.GetChildren().OfType<NGridCardHolder>().Where(h => !h.IsQueuedForDeletion()).ToList();
        if (holders.Count == 0)
            return;

        var altContainer = (Control)RewardAlternativesField.GetValue(cardRewardScreen)!;
        var altButtons = altContainer
            .GetChildren()
            .OfType<NCardRewardAlternativeButton>()
            .Where(b => !b.IsQueuedForDeletion())
            .ToList();

        LabelItems(holders, 28, new Vector2(40, 40), _ => new Vector2(-20, -236));
        LabelItems(
            altButtons,
            24,
            new Vector2(32, 32),
            b => new Vector2(8, (b.Size.Y - 32) / 2),
            startIndex: holders.Count
        );

        var idx = GetPressedIndex(holders.Count);
        if (idx is int i)
        {
            holders[i].EmitSignal(NCardHolder.SignalName.Pressed, holders[i]);
            return;
        }

        var altIdx = GetPressedIndex(altButtons.Count, startIndex: holders.Count);
        if (altIdx is int j)
            altButtons[j].ForceClick();
    }

    private static void TryHandleMapScreen(IScreenContext? currentScreen)
    {
        if (currentScreen is not NMapScreen mapScreen)
        {
            if (_lastMapScreen != null)
            {
                ClearMapLabels();
                _lastMapScreen = null;
            }
            return;
        }

        if (!mapScreen.IsTravelEnabled)
            return;

        var dict = (Dictionary<MapCoord, NMapPoint>)MapPointDictField.GetValue(mapScreen)!;
        var travelable = dict
            .Values.Where(p => p.State == MapPointState.Travelable)
            .OrderBy(p => p.Point.coord.col)
            .ToList();

        if (!travelable.SequenceEqual(_lastTravelablePoints))
        {
            ClearMapLabels();
            _lastTravelablePoints = travelable;
            _lastMapScreen = mapScreen;
            LabelItems(travelable, 24, new Vector2(32, 32), _ => new Vector2(-4, -40));
        }

        var idx = GetPressedIndex(travelable.Count);
        if (idx is int i)
            mapScreen.OnMapPointSelectedLocally(travelable[i]);
    }

    private static void ClearMapLabels()
    {
        foreach (var point in _lastTravelablePoints)
        {
            if (GodotObject.IsInstanceValid(point))
                NumberLabels.Remove(point);
        }
        _lastTravelablePoints = new List<NMapPoint>();
    }

    private static void TryHandleRestSite(IScreenContext? currentScreen)
    {
        if (currentScreen is not NRestSiteRoom restSiteRoom)
            return;

        if (!restSiteRoom.IsVisibleInTree())
            return;

        var choicesContainer = (Control)RestChoicesField.GetValue(restSiteRoom)!;
        var buttons = choicesContainer
            .GetChildren()
            .OfType<NRestSiteButton>()
            .Where(b => !b.IsQueuedForDeletion())
            .ToList();

        if (buttons.Count == 0)
            return;

        LabelItems(buttons, 24, new Vector2(32, 32), b => new Vector2(8, (b.Size.Y - 32) / 2));

        var idx = GetPressedIndex(buttons.Count);
        if (idx is int i && buttons[i].IsEnabled)
            buttons[i].ForceClick();
    }

    private static void TryHandleEventScreen(IScreenContext? currentScreen)
    {
        if (currentScreen is not NEventRoom eventRoom)
            return;

        var layout = eventRoom.Layout;
        if (layout == null)
            return;

        var buttons = layout.OptionButtons.ToList();
        if (buttons.Count == 0)
            return;

        LabelItems(buttons, 24, new Vector2(32, 32), b => new Vector2(8, (b.Size.Y - 32) / 2));

        var idx = GetPressedIndex(buttons.Count);
        if (idx is int i && !buttons[i].Option.IsLocked)
            buttons[i].ForceClick();
    }

    private static void TryHandleCardGridSelection(IScreenContext? currentScreen)
    {
        if (currentScreen is not NCardGridSelectionScreen gridScreen)
        {
            if (_lastGridScreen != null)
            {
                _lastGridScreen = null;
                _gridSelectedRow = 0;
            }
            return;
        }

        if (!gridScreen.IsVisibleInTree())
            return;

        var grid = (NCardGrid)GridField.GetValue(gridScreen)!;
        var cardRows = (List<List<NGridCardHolder>>)CardRowsField.GetValue(grid)!;
        if (cardRows.Count == 0)
            return;

        bool rowChanged = false;
        if (gridScreen != _lastGridScreen)
        {
            _lastGridScreen = gridScreen;
            _gridSelectedRow = 0;
            rowChanged = true;
        }

        if (Input.IsActionJustPressed(NextRowAction))
        {
            ClearGridRowLabels(cardRows, _gridSelectedRow);
            _gridSelectedRow = (_gridSelectedRow + 1) % cardRows.Count;
            rowChanged = true;
        }
        else if (Input.IsActionJustPressed(PrevRowAction))
        {
            ClearGridRowLabels(cardRows, _gridSelectedRow);
            _gridSelectedRow = (_gridSelectedRow - 1 + cardRows.Count) % cardRows.Count;
            rowChanged = true;
        }

        if (_gridSelectedRow >= cardRows.Count)
        {
            _gridSelectedRow = 0;
            rowChanged = true;
        }

        if (rowChanged)
            LabelItems(cardRows[_gridSelectedRow], 28, new Vector2(40, 40), _ => new Vector2(-20, -236));

        var activeRow = cardRows[_gridSelectedRow];
        var idx = GetPressedIndex(activeRow.Count);
        if (idx is int i)
            activeRow[i].EmitSignal(NCardHolder.SignalName.Pressed, activeRow[i]);
    }

    private static void ClearGridRowLabels(List<List<NGridCardHolder>> cardRows, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= cardRows.Count)
            return;
        foreach (var holder in cardRows[rowIndex])
        {
            if (GodotObject.IsInstanceValid(holder))
                NumberLabels.Remove(holder);
        }
    }

    private static void TryHandleRewardsScreen(IScreenContext? currentScreen)
    {
        if (currentScreen is not NRewardsScreen rewardsScreen)
        {
            _lastRewardsScreen = null;
            _lastButtonCount = 0;
            return;
        }

        if (rewardsScreen.IsComplete || !rewardsScreen.IsVisibleInTree())
            return;

        var buttons = (List<Control>)RewardButtonsField.GetValue(rewardsScreen)!;
        if (buttons.Count == 0)
            return;

        if (rewardsScreen != _lastRewardsScreen || buttons.Count != _lastButtonCount)
        {
            _lastRewardsScreen = rewardsScreen;
            _lastButtonCount = buttons.Count;
            LabelItems(buttons, 24, new Vector2(32, 32), b => new Vector2(2, (b.Size.Y - 32) / 2), outlineSize: 4);
        }

        var idx = GetPressedIndex(buttons.Count);
        if (idx is int i)
            ClickRewardButton(buttons[i]);
    }

    private static void ClickRewardButton(Control button)
    {
        if (button is NRewardButton rewardButton && rewardButton.IsEnabled)
            rewardButton.ForceClick();
        else if (button is NLinkedRewardSet linkedSet)
            linkedSet.GrabFocus();
    }
}
