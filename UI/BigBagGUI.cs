﻿using ImproveGame.Common.Configs;
using ImproveGame.Packets;
using ImproveGame.UIFramework;
using ImproveGame.UIFramework.BaseViews;
using ImproveGame.UIFramework.Common;
using ImproveGame.UIFramework.SUIElements;
using ImproveGame.UIFramework.UIElements;
using Terraria.GameInput;

namespace ImproveGame.UI;

[AutoCreateGUI(LayerName.Vanilla.RadialHotbars, "Big Bag")]
public class BigBagGUI : BaseBody
{
    public static BigBagGUI Instance { get; private set; }
    public BigBagGUI() => Instance = this;

    public override bool Enabled { get => Visible; set => Visible = value; }

    public override bool CanSetFocusTarget(UIElement target)
        => target != this && (MainPanel.IsMouseHovering || MainPanel.IsLeftMousePressed);

    public static bool Visible
    {
        get
        {
            if (!Main.playerInventory) /* 关闭背包时设置为 false，使背包再开启后是大背包处于未开启状态 */
            {
                _visible = false;
            }

            return _visible;
        }
        set => _visible = value;
    }
    private static bool _visible = true;

    // 主面板
    public SUIPanel MainPanel;

    // 标题面板
    private SUIPanel TitlePanel;

    // 内容面板
    private View ButtonPanel;

    // 标题
    private SUIText Title;

    // 关闭按钮
    private SUICross Cross;

    // 控制开关
    private SUISwitch RecipesSwitch, SmartGrabSwitch, AutoGrabSwitch;

    // 按钮
    private SUIButton QuickButton, PutButton, ReplenishButton, SortButton;

    // 物品列表
    public ModItemGrid ItemGrid;

    public override void OnInitialize()
    {
        PlayerBigBagSettingPacket.SendMyPlayer();

        UIPlayerSetting setting = Main.LocalPlayer.GetModPlayer<UIPlayerSetting>();
        // 主面板
        MainPanel = new SUIPanel(UIStyle.PanelBorder, UIStyle.PanelBg)
        {
            Shaded = true,
            Draggable = true
        };
        MainPanel.SetPadding(0f);
        MainPanel.JoinParent(this);

        TitlePanel = new SUIPanel(UIStyle.PanelBorder, UIStyle.TitleBg2)
        {
            DragIgnore = true,
            Width = { Pixels = 0f, Precent = 1f },
            Height = { Pixels = 50f, Precent = 0f },
            Rounded = new Vector4(10f, 10f, 0f, 0f),
            RelativeMode = RelativeMode.Vertical
        };
        TitlePanel.SetPadding(0f);
        TitlePanel.JoinParent(MainPanel);

        // 标题
        Title = new SUIText()
        {
            TextOrKey = "Mods.ImproveGame.SuperVault.Name",
            UseKey = true,
            IsLarge = true,
            TextScale = 0.55f,
            VAlign = 0.5f,
        };
        Title.TextBorder = 2f / Title.TextScale;
        Title.PaddingLeft = 20f;
        Title.SetInnerPixels(Title.TextSize * Title.TextScale);
        Title.JoinParent(TitlePanel);

        // Cross
        Cross = new SUICross
        {
            HAlign = 1f,
            VAlign = 0.5f,
            Height = { Pixels = 0f, Precent = 1f },
            Rounded = new Vector4(0f, 10f, 0f, 0f)
        };
        Cross.OnLeftMouseDown += (_, _) => Close();
        Cross.JoinParent(TitlePanel);

        ButtonPanel = new View
        {
            DragIgnore = true,
            RelativeMode = RelativeMode.Vertical
        };
        ButtonPanel.SetPadding(12f, 10f, 11f, 12f);
        ButtonPanel.JoinParent(MainPanel);

        #region 开关按钮
        Vector2 switchSpacing = new Vector2(10, 10);
        RecipesSwitch = new SUISwitch(() => setting.SuperVault_ParticipateSynthesis,
            state =>
            {
                setting.SuperVault_ParticipateSynthesis = state;
                Recipe.FindRecipes();
            }, GetText("SuperVault.Synthesis"), 0.8f)
        {
            ResetAnotherPosition = true,
            RelativeMode = RelativeMode.Vertical,
            Spacing = switchSpacing
        };
        RecipesSwitch.JoinParent(ButtonPanel);

        SmartGrabSwitch = new SUISwitch(() => setting.SuperVault_PrioritizeGrabbing,
            state =>
            {
                setting.SuperVault_PrioritizeGrabbing = state;
                PlayerBigBagSettingPacket.SendMyPlayer();
            },
            GetText("SuperVault.SmartPickup"), 0.8f)
        {
            RelativeMode = RelativeMode.Horizontal,
            Spacing = switchSpacing
        };
        SmartGrabSwitch.JoinParent(ButtonPanel);

        AutoGrabSwitch = new SUISwitch(() => setting.SuperVault_GrabItemsWhenOverflowing,
            state =>
            {
                setting.SuperVault_GrabItemsWhenOverflowing = state;
                PlayerBigBagSettingPacket.SendMyPlayer();
            },
            GetText("SuperVault.OverflowPickup"), 0.8f)
        {
            RelativeMode = RelativeMode.Horizontal,
            Spacing = switchSpacing
        };
        AutoGrabSwitch.JoinParent(ButtonPanel);
        #endregion

        // 按钮
        Vector2 buttonSpacing = new Vector2(10, 8);
        QuickButton = new SUIButton(GetTexture("UI/Quick").Value, Lang.inter[29].Value)
        {
            RelativeMode = RelativeMode.Vertical,
            Spacing = buttonSpacing,
            ResetAnotherPosition = true
        };
        QuickButton.OnLeftMouseDown += (_, _) => QuickTakeOutToPlayerInventory();
        QuickButton.JoinParent(ButtonPanel);

        PutButton = new SUIButton(GetTexture("UI/Put").Value, Lang.inter[30].Value)
        {
            RelativeMode = RelativeMode.Horizontal,
            Spacing = buttonSpacing
        };
        PutButton.OnLeftMouseDown += (_, _) => PutAll();
        PutButton.JoinParent(ButtonPanel);

        ReplenishButton = new SUIButton(GetTexture("UI/Put").Value, Lang.inter[31].Value)
        {
            RelativeMode = RelativeMode.Horizontal,
            Spacing = buttonSpacing
        };
        ReplenishButton.OnLeftMouseDown += (_, _) => Replenish();
        ReplenishButton.JoinParent(ButtonPanel);

        SortButton = new SUIButton(GetTexture("UI/Put").Value, GetText("SuperVault.Sort"))
        {
            RelativeMode = RelativeMode.Horizontal,
            Spacing = buttonSpacing
        };
        SortButton.OnLeftMouseDown += (_, _) => Sort();
        SortButton.JoinParent(ButtonPanel);

        // Inventory 滚动视图
        ItemGrid = new ModItemGrid
        {
            ResetAnotherPosition = true,
            RelativeMode = RelativeMode.Vertical,
            Spacing = new Vector2(10, 15)
        };
        ItemGrid.ItemList.OnMouseDownSlot += NetSyncItem;
        ItemGrid.JoinParent(ButtonPanel);
        ButtonPanel.SetInnerPixels(ItemGrid.Width.Pixels, ItemGrid.Bottom());
        MainPanel.SetInnerPixels(ButtonPanel.Width.Pixels, ButtonPanel.Bottom());
    }

    /// <summary>
    /// 点击操作，将物品发送给服务器（因为像药水袋和旗帜盒这俩左键是不改stack的，所以这来个同步）
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="listeningElement"></param>
    private static void NetSyncItem(UIMouseEvent evt, UIElement listeningElement)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || listeningElement is not BigBagItemSlot itemSlot)
        {
            return;
        }

        var packet = BigBagSlotPacket.Get(itemSlot.Item, Main.myPlayer, itemSlot.Index);
        packet.Send(runLocally: false);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (MainPanel.IsMouseHovering)
        {
            PlayerInput.LockVanillaMouseScroll("ImproveGame: BigBagGUI");
            Main.LocalPlayer.mouseInterface = true;
        }

        if (Math.Abs(ButtonPanel.GetInnerSizePixels().Y - ItemGrid.Bottom()) > 0.000000001)
        {
            ButtonPanel.SetInnerPixels(ItemGrid.Width.Pixels, ItemGrid.Bottom());
            ButtonPanel.Recalculate();
        }

        if (Math.Abs(MainPanel.Height.Pixels - ButtonPanel.Bottom()) < 0.000000001)
        {
            return;
        }

        MainPanel.SetInnerPixels(ButtonPanel.Width.Pixels, ButtonPanel.Bottom());
        MainPanel.Recalculate();
    }

    public void Open()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        Main.playerInventory = true;
        _visible = true;
    }

    public void Close()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);
        _visible = false;
        AdditionalConfig.Save();
    }

    private void Sort()
    {
        SoundEngine.PlaySound(SoundID.Grab);
        Item[] items = ItemGrid.ItemList.items;

        // 拿出来非空非收藏的物品
        List<Item> testSort = new();
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].IsAir || items[i].favorited)
            {
                continue;
            }

            testSort.Add(items[i]);
            items[i] = new Item();
        }

        // 优先级排序
        testSort.Sort((a, b) =>
            -a.rare.CompareTo(b.rare) * 100 + a.type.CompareTo(b.type) * 10 - a.stack.CompareTo(b.stack));

        // 放入背包
        foreach (var item in testSort)
        {
            ItemStackToInventory(items, item, false);
        }

        Recipe.FindRecipes();
    }

    private void Replenish()
    {
        SoundEngine.PlaySound(SoundID.Grab);
        Item[] inventory = Main.LocalPlayer.inventory;
        Item[] bigBag = ItemGrid.ItemList.items;
        for (int i = 10; i < 58; i++)
        {
            if (inventory[i].IsAir || inventory[i].favorited || inventory[i].IsACoin)
            {
                continue;
            }

            if (HasItem(bigBag, -1, inventory[i].type))
            {
                inventory[i] = ItemStackToInventory(bigBag, inventory[i], false);
            }
        }

        Recipe.FindRecipes();
    }

    private void PutAll()
    {
        SoundEngine.PlaySound(SoundID.Grab);
        Item[] inventory = Main.LocalPlayer.inventory;
        Item[] bigBag = ItemGrid.ItemList.items;
        for (int i = 10; i < 50; i++)
        {
            if (!inventory[i].IsAir && !inventory[i].favorited && !inventory[i].IsACoin)
                inventory[i] = ItemStackToInventory(bigBag, inventory[i], false);
        }

        Recipe.FindRecipes();
    }

    private void QuickTakeOutToPlayerInventory()
    {
        SoundEngine.PlaySound(SoundID.Grab);
        Item[] inventory = Main.LocalPlayer.inventory;
        Item[] bigBag = ItemGrid.ItemList.items;
        for (int i = 0; i < bigBag.Length; i++)
        {
            if (!bigBag[i].IsAir && !bigBag[i].favorited && !bigBag[i].IsACoin)
            {
                bigBag[i] = ItemStackToInventory(inventory, bigBag[i], false, 50);
            }
        }

        Recipe.FindRecipes();
    }
}