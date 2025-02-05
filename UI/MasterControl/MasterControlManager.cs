﻿using ImproveGame.Common.ModPlayers;
using ImproveGame.UI.DeathSpectating;
using ImproveGame.UI.ItemContainer;
using ImproveGame.UI.PlayerStats;

namespace ImproveGame.UI.MasterControl;

public class MasterControlManager : ModSystem
{
    public static MasterControlManager Instance => ModContent.GetInstance<MasterControlManager>();

    /// <summary>
    /// 注册
    /// </summary>
    public MasterControlFunction Register(MasterControlFunction item)
    {
        if (!_originalMCFunctions.Contains(item))
        {
            _originalMCFunctions.Add(item);
        }

        return item;
    }

    /// <summary>
    /// 原功能列表
    /// </summary>
    public IReadOnlyList<MasterControlFunction> OriginalMCFuncions => _originalMCFunctions;
    private readonly List<MasterControlFunction> _originalMCFunctions = [];

    /// <summary>
    /// 排序之后的可用功能列表
    /// </summary>
    public IReadOnlyList<MasterControlFunction> AvailableOrderedMCFunctions => _availableOrderedMCFunctions;
    private readonly List<MasterControlFunction> _availableOrderedMCFunctions = [];

    /// <summary>
    /// 排序之后的不可用功能列表
    /// </summary>
    public IReadOnlyList<MasterControlFunction> UnavailableOrderedMCFunctions => _unavailableOrderedMCFunctions;
    private readonly List<MasterControlFunction> _unavailableOrderedMCFunctions = [];

    /// <summary>
    /// 排列功能列表
    /// </summary>
    public void OrderMCFunctions()
    {
        _availableOrderedMCFunctions.Clear();
        _unavailableOrderedMCFunctions.Clear();

        foreach (var item in _originalMCFunctions.OrderBy(i => i))
        {
            if (item.IsAvailable)
            {
                _availableOrderedMCFunctions.Add(item);
            }
            else
            {
                _unavailableOrderedMCFunctions.Add(item);
            }
        }
    }

    public override void PostSetupContent()
    {
        #region 大背包
        var bigBackpack = new MasterControlFunction("SuperVault")
        {
            Icon = ModAsset.BigBackpack.Value,
        }.Register();

        bigBackpack.Available += () => Config.SuperVault;
        bigBackpack.OnMouseDown += tv =>
        {
            if (!Config.SuperVault)
                return;

            if (BigBagGUI.Visible)
                BigBagGUI.Instance.Close();
            else
                BigBagGUI.Instance.Open();
        };
        #endregion

        #region 属性面板
        var playerStats = new MasterControlFunction("PlayerStats")
        {
            Icon = ModAsset.PlayerStats.Value,
        }.Register();

        playerStats.OnMouseDown += tv =>
        {
            var body = PlayerStatsGUI.Instance;

            if (body.HasChild(body.Window))
                body.RemoveChild(body.Window);
            else
            {
                Main.playerInventory = true;
                body.Append(body.Window);
            }
        };
        #endregion

        #region 观战
        var spectating = new MasterControlFunction("Spectating")
        {
            Icon = ModAsset.Spectating.Value,
        }.Register();

        spectating.OnMouseDown += tv =>
        {
            var body = SpectatingGUI.Instance;

            body.Enabled = !body.Enabled;
        };
        #endregion

        #region 旗帜盒
        var bannerChest = new MasterControlFunction("BannerChest")
        {
            Icon = ModAsset.BannerChest.Value,
        }.Register();

        bannerChest.Available += () =>
        {
            if (Main.LocalPlayer.TryGetModPlayer<ImprovePlayer>(out var improvePlayer))
            {
                return improvePlayer.BannerChest != null;
            }

            return false;
        };

        bannerChest.OnMouseDown += _ =>
        {
            if (Main.LocalPlayer.TryGetModPlayer<ImprovePlayer>(out var improvePlayer) &&
                improvePlayer.BannerChest != null)
            {
                if (!ItemContainerGUI.Instace.Enabled ||
                    improvePlayer.BannerChest != ItemContainerGUI.Instace.Container)
                    ItemContainerGUI.Instace.Open(improvePlayer.BannerChest);
                else
                    ItemContainerGUI.Instace.Close();
            }
        };
        #endregion

        #region 药水袋
        var potionBag = new MasterControlFunction("PotionBag")
        {
            Icon = ModAsset.PotionBag.Value,
        }.Register();

        potionBag.Available += () =>
        {
            if (Main.LocalPlayer.TryGetModPlayer<ImprovePlayer>(out var improvePlayer))
            {
                return improvePlayer.PotionBag != null;
            }

            return false;
        };

        potionBag.OnMouseDown += _ =>
        {
            if (Main.LocalPlayer.TryGetModPlayer<ImprovePlayer>(out var improvePlayer) &&
                improvePlayer.PotionBag != null)
            {
                if (!ItemContainerGUI.Instace.Enabled ||
                    improvePlayer.PotionBag != ItemContainerGUI.Instace.Container)
                    ItemContainerGUI.Instace.Open(improvePlayer.PotionBag);
                else
                    ItemContainerGUI.Instace.Close();
            }
        };
        #endregion

        OrderMCFunctions();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        foreach (var function in OriginalMCFuncions)
        {
            function.UpdateAlways();
        }

        OrderMCFunctions();
    }
}
