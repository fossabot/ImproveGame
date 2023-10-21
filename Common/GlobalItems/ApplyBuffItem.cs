﻿using ImproveGame.Common.ModSystems;
using ImproveGame.Content.Patches;
using ImproveGame.Core;
using ImproveGame.Interface.Common;
using ImproveGame.Interface.GUI;
using Microsoft.Xna.Framework.Input;
using System.Collections.ObjectModel;
using Terraria.GameInput;

namespace ImproveGame.Common.GlobalItems
{
    public class ApplyBuffItem : GlobalItem
    {
        // 不能用同一个List，因为要求的堆叠量不一样，否则会串台
        private static Dictionary<int, List<int>> _potionToBuffs = new();
        private static Dictionary<int, List<int>> _stationToBuffs = new();

        public static void UpdateBuffTypesShouldHide(Item item)
        {

            var buffTypes = GetItemBuffType(item);
            buffTypes.ForEach(buffType =>
            {
                if (buffType is -1)
                    return;

                HideBuffSystem.BuffTypesShouldHide[buffType] = true;
            });
        }
        
        public static bool IsItemAvailable(Item item)
        {
            var buffTypes = GetItemBuffType(item);
            if (buffTypes.Count > 0)
                return true;

            // 非增益药剂
            if (Config.NoConsume_Potion && item.stack >= Config.NoConsume_PotionRequirement &&
                Lookups.SpecialPotions.Contains(item.type))
                return true;

            // 红药水扩展
            if (item.IsAvailableRedPotionExtension())
                return true;

            // 随身增益站：旗帜
            if (Config.NoPlace_BUFFTile_Banner && ItemToBanner(item) != -1)
                return true;

            // 弹药
            if (Config.NoConsume_Ammo && item.stack >= 3996 && item.ammo > 0)
                return true;

            // 花园侏儒
            if (item.type == ItemID.GardenGnome)
                return true;
 
            return false;
        }

        public static List<int> GetItemBuffType(Item item)
        {
            if (ModIntegrationsSystem.ModdedInfBuffsIgnore.Contains(item.type))
                return new List<int>();
            if (Config.NoConsume_Potion)
            {
                // 普通药水
                if (item.stack >= Config.NoConsume_PotionRequirement)
                {
                    if (_potionToBuffs.TryGetValue(item.type, out var buffsInTable))
                        return buffsInTable;

                    var buffs = new List<int>();
                    if (item.buffType > 0)
                        buffs.Add(item.buffType);
                    // 其他Mod的，自行添加了引用
                    if (ModIntegrationsSystem.ModdedPotionBuffs.TryGetValue(item.type, out List<int> buffTypes))
                        buffs.AddRange(buffTypes);
                    _potionToBuffs[item.type] = buffs;
                    return buffs;
                }
            }

            // 随身增益站：普通
            if (Config.NoPlace_BUFFTile)
            {
                if (_stationToBuffs.TryGetValue(item.type, out var buffsInTable))
                    return buffsInTable;

                IsBuffTileItem(item, out List<int> buffTypes);
                if (item.type is ItemID.HoneyBucket)
                    buffTypes.Add(BuffID.Honey);
                if (buffTypes.Count > 0)
                {
                    _stationToBuffs[item.type] = buffTypes;
                    return buffTypes;
                }
            }

            return new List<int>();
        }

        public static bool IsBuffTileItem(Item item, out List<int> buffTypes)
        {
            // 会给玩家buff的雕像
            buffTypes = (from t in Lookups.BuffTiles where item.createTile == t.TileID && (item.placeStyle == t.Style || t.Style == -1) select t.BuffID).ToList();

            // 其他Mod的，自行添加了引用
            if (ModIntegrationsSystem.ModdedPlaceableItemBuffs.TryGetValue(item.type, out var moddedBuffs))
                buffTypes.AddRange(moddedBuffs);
            return buffTypes.Count > 0;
        }

        // 物品消耗
        public override bool ConsumeItem(Item item, Player player)
        {
            if (item.ModItem?.Mod.Name is "Everglow") return base.ConsumeItem(item, player);

            if (Config.NoConsume_Potion && item.stack >= Config.NoConsume_PotionRequirement &&
                (item.buffType > 0 || Lookups.SpecialPotions.Contains(item.type)))
            {
                return false;
            }

            return base.ConsumeItem(item, player);
        }

        private static bool _oldMiddlePressed;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!IsItemAvailable(item))
                return;

            if (IsBuffTileItem(item, out _) || item.type is ItemID.HoneyBucket or ItemID.GardenGnome ||
                (item.stack >= Config.NoConsume_PotionRequirement && item.buffType > 0 && item.active))
            {
                var buffTypes = GetItemBuffType(item);
                buffTypes.ForEach(buffType =>
                {
                    if (buffType is -1 && item.type != ItemID.GardenGnome) return;

                    MouseState mouseState = Mouse.GetState();
                    if (_oldMiddlePressed)
                    {
                        _oldMiddlePressed = mouseState.MiddleButton == ButtonState.Pressed;
                    }

                    if (mouseState.MiddleButton == ButtonState.Pressed && !_oldMiddlePressed)
                    {
                        _oldMiddlePressed = true;
                        if (BuffTrackerGUI.Visible)
                            UISystem.Instance.BuffTrackerGUI.Close();
                        else
                            UISystem.Instance.BuffTrackerGUI.Open();
                    }

                    TagItem.ModifyBuffTooltips(Mod, item.type, buffType, tooltips);
                });
            }

            // 红药水扩展
            if (item.IsAvailableRedPotionExtension())
            {
                tooltips.Add(new TooltipLine(Mod, "TagDetailed.RedPotion", GetText("Tips.TagDetailed.RedPotion"))
                {
                    OverrideColor = Color.SkyBlue
                });
                TagItem.AddShiftForMoreTooltip(tooltips);
            }
        }

        public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
        {
            if (!IsItemAvailable(item))
                return base.PreDrawTooltip(item, lines, ref x, ref y);

            if ((item.IsAvailableRedPotionExtension() || item.type is ItemID.GardenGnome) && ItemSlot.ShiftInUse)
            {
                TagItem.DrawTagTooltips(lines, TagItem.GenerateDetailedTags(Mod, lines), x, y);
                return base.PreDrawTooltip(item, lines, ref x, ref y);
            }

            if (IsBuffTileItem(item, out _) || item.type == ItemID.HoneyBucket ||
                (item.stack >= Config.NoConsume_PotionRequirement && item.buffType > 0 && item.active))
            {
                var buffTypes = GetItemBuffType(item);

                foreach (int buffType in buffTypes)
                {
                    if (buffType is -1)
                        return base.PreDrawTooltip(item, lines, ref x, ref y);

                    object arg = new
                    {
                        BuffName = Lang.GetBuffName(buffType),
                        MaxSpawn = Config.SpawnRateMaxValue
                    };
                    if (ItemSlot.ShiftInUse)
                        TagItem.DrawTagTooltips(lines, TagItem.GenerateDetailedTags(Mod, lines, arg), x, y);
                }
            }

            return base.PreDrawTooltip(item, lines, ref x, ref y);
        }
    }
}