﻿using ImproveGame.UI.MasterControl.Components;
using ImproveGame.UIFramework;
using ImproveGame.UIFramework.BaseViews;
using ImproveGame.UIFramework.Common;
using ImproveGame.UIFramework.SUIElements;
using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;

namespace ImproveGame.UI.MasterControl;

[AutoCreateGUI(LayerName.Vanilla.RadialHotbars, "Master Control GUI", 100)]
public class MasterControlGUI : BaseBody
{
    public readonly AnimationTimer OpacityTimer = new();
    public override bool Enabled
    {
        get => Main.keyState.IsKeyDown(Keys.OemTilde) || OpacityTimer.AnyOpen || OpacityTimer.Closing;
        set { }
    }
    public override bool CanSetFocusTarget(UIElement target) => Window.IsMouseHovering;
    public override bool IsNotSelectable => OpacityTimer.AnyClose;

    public SUIPanel Window { get; private set; }
    public SUIScrollView2 AvailableFunctions { get; private set; }
    public SUIScrollView2 UnavailableFunctions { get; private set; }

    #region HeadBar and TailBar
    /// <summary>
    /// 面板头部
    /// </summary>
    public View HeadBar { get; private set; }
    /// <summary>
    /// 标题
    /// </summary>
    public SUIText Title { get; private set; }

    /// <summary>
    /// 面板尾部
    /// </summary>
    public View TailBar { get; private set; }
    /// <summary>
    /// 版本号文本框
    /// </summary>
    public SUIText VersionText { get; private set; }
    #endregion

    public override void OnInitialize()
    {
        Opacity.Type = OpacityType.Own;

        #region Window
        Window = new SUIPanel(Color.Transparent, Color.Transparent)
        {
            Padding = 0f,
            FinallyDrawBorder = true,
            IsAdaptiveWidth = true,
            IsAdaptiveHeight = true,
            HAlign = 0.5f,
            VAlign = 0.5f,
        };
        Window.SetRoundedRectProperties(UIStyle.PanelBg, 2f, UIStyle.PanelBorder, 12);
        Window.JoinParent(this);
        #endregion

        #region HeadBar
        HeadBar = ViewHelper.CreateHead(Color.Black * 0.3f, 45f, 12f);
        HeadBar.JoinParent(Window);

        Title = new SUIText
        {
            TextOrKey = $"{ImproveGame.Instance.DisplayName}",
            TextAlign = new Vector2(0.5f),
            TextBorder = 1.5f,
        };
        Title.SetSizePercent(1f, 1f);
        Title.JoinParent(HeadBar);
        #endregion

        var availableText = new SUIText
        {
            RelativeMode = RelativeMode.Vertical,
            Spacing = new Vector2(4f),
            TextScale = 0.8f,
            TextOrKey = "可用功能",
            TextOffset = new Vector2(12f, 0f)
        };
        availableText.SetSize(0f, availableText.TextSize.Y * availableText.TextScale, 1f, 0f);
        availableText.JoinParent(Window);

        AvailableFunctions = new SUIScrollView2(Orientation.Vertical)
        {
            RelativeMode = RelativeMode.Vertical,
            Spacing = new Vector2(4f),
        };
        AvailableFunctions.SetPadding(6f, 0f);
        AvailableFunctions.SetSizePixels(500, 120);
        AvailableFunctions.JoinParent(Window);

        ReloadAvailableFunctionsElement();

        var unavailableText = new SUIText
        {
            RelativeMode = RelativeMode.Vertical,
            Spacing = new Vector2(4f),
            TextScale = 0.8f,
            TextOrKey = "不可用功能",
            TextOffset = new Vector2(12f, 0f)
        };
        unavailableText.SetSize(0f, unavailableText.TextSize.Y * unavailableText.TextScale, 1f, 0f);
        unavailableText.JoinParent(Window);

        UnavailableFunctions = new SUIScrollView2(Orientation.Vertical)
        {
            RelativeMode = RelativeMode.Vertical,
            Spacing = new Vector2(4f)
        };
        UnavailableFunctions.SetPadding(6f, 0f);
        UnavailableFunctions.SetSizePixels(500, 120);
        UnavailableFunctions.JoinParent(Window);

        ReloadUnavailableFunctionsElement();

        #region TailBar
        TailBar = ViewHelper.CreateTail(Color.Black * 0.3f, 35f, 12f);
        TailBar.JoinParent(Window);

        VersionText = new SUIText
        {
            TextScale = 0.9f,
            TextOrKey = $"版本号: {ImproveGame.Instance.Version}",
            TextAlign = new Vector2(0.5f),
            TextBorder = 1.5f,
            HAlign = 1f,
        };
        VersionText.Width.Pixels = VersionText.TextSize.X * VersionText.TextScale;
        VersionText.Height.Percent = 1f;
        VersionText.JoinParent(TailBar);
        #endregion
    }

    public const float HNumber = 5f;
    public const float ChildrenSpacing = 4f;
    public const float ItemWidthPercent = 1f / HNumber;

    /// <summary>
    /// 重新加载 可用的 功能列表视图
    /// </summary>
    public void ReloadAvailableFunctionsElement()
    {
        AvailableFunctions.ListView.RemoveAllChildren();

        for (int i = 0; i < MasterControlManager.Instance.AvailableOrderedMCFunctions.Count; i++)
        {
            var functionComponent = new MCFIconComponent(MasterControlManager.Instance.AvailableOrderedMCFunctions, i)
            {
                RelativeMode = RelativeMode.Horizontal,
                Spacing = new Vector2(ChildrenSpacing),
            };

            if (i % HNumber == 0)
                functionComponent.DirectLineBreak = true;

            functionComponent.SetSize(-ChildrenSpacing * (HNumber - 1) / HNumber, 52f, ItemWidthPercent);
            functionComponent.JoinParent(AvailableFunctions.ListView);
        }
    }

    /// <summary>
    /// 重新加载 不可用的 功能列表视图
    /// </summary>
    public void ReloadUnavailableFunctionsElement()
    {
        UnavailableFunctions.ListView.RemoveAllChildren();

        for (int i = 0; i < MasterControlManager.Instance.UnavailableOrderedMCFunctions.Count; i++)
        {
            var functionComponent = new MCFIconComponent(MasterControlManager.Instance.UnavailableOrderedMCFunctions, i)
            {
                RelativeMode = RelativeMode.Horizontal,
                Spacing = new Vector2(ChildrenSpacing),
            };

            if (i % HNumber == 0)
                functionComponent.DirectLineBreak = true;

            functionComponent.SetSize(-ChildrenSpacing * (HNumber - 1) / HNumber, 52f, ItemWidthPercent);
            functionComponent.JoinParent(UnavailableFunctions.ListView);
        }
    }

    public override void CheckWhetherRecalculate(out bool recalculate)
    {
        base.CheckWhetherRecalculate(out recalculate);

        if (AvailableFunctions.ListView.Children.Count() != MasterControlManager.Instance.AvailableOrderedMCFunctions.Count &&
            UnavailableFunctions.ListView.Children.Count() != MasterControlManager.Instance.UnavailableOrderedMCFunctions.Count)
        {
            ReloadAvailableFunctionsElement();
            ReloadUnavailableFunctionsElement();

            recalculate = true;
        }

        if (OpacityTimer.Lerp(0, 1) != Opacity.Value)
        {
            Opacity.SetValue(OpacityTimer.Lerp(0, 1));
            recalculate = true;
        }
    }

    public override void Update(GameTime gameTime)
    {
        OpacityTimer.Update();
        base.Update(gameTime);

        if (Window.IsMouseHovering)
            PlayerInput.LockVanillaMouseScroll($"{ImproveGame.Instance.Name}: Control Center GUI");

        if (Main.keyState.IsKeyDown(Keys.OemTilde))
        {
            if (!OpacityTimer.AnyOpen)
                OpacityTimer.OpenAndResetTimer();
        }
        else
        {
            if (!OpacityTimer.AnyClose)
                OpacityTimer.CloseAndResetTimer();
        }
    }
}
