﻿using ImproveGame.Common.Configs;
using ImproveGame.Common.GlobalItems;
using ImproveGame.Content.Items;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using Terraria.Enums;

namespace ImproveGame.Common.ModSystems
{
    /// <summary>
    /// 为了方便管理，这里主要放一些不成体系的小修改，比如一些单独的On, IL
    /// </summary>
    public class MinorModifySystem : ModSystem
    {
        public override void Load()
        {
            // 死亡是否掉落墓碑
            On_Player.DropTombstone += DisableDropTombstone;
            // 修改空间法杖显示平台剩余数量
            IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += TweakDrawCountInventory;
            // 伤害波动
            On_Main.DamageVar_float_int_float += DisableDamageVar;
            // 使存钱罐中物品生效，如同放入背包一样
            On_Player.UpdateEquips += TweakExtraUpdateInventory;
            // 摇树总是掉落水果
            IL_WorldGen.ShakeTree += TweakShakeTree;
            // “草药” 生长速度
            IL_WorldGen.GrowAlch += WorldGen_GrowAlch;
            // “草药” 绘制的是否是开花图案
            Terraria.GameContent.Drawing.On_TileDrawing.IsAlchemyPlantHarvestable += TileDrawing_IsAlchemyPlantHarvestable;
            // “草药” 是否可以被 “再生法杖” 收割
            IL_Player.PlaceThing_Tiles_BlockPlacementForAssortedThings += Player_PlaceThing_Tiles_BlockPlacementForAssortedThings;
            // “草药” 是否掉落成熟时候物品
            // IL.WorldGen.KillTile_GetItemDrops += WorldGen_KillTile_GetItemDrops;
            On_WorldGen.IsHarvestableHerbWithSeed += WorldGen_IsHarvestableHerbWithSeed;
            // 旅商永远不离开
            On_WorldGen.UnspawnTravelNPC += TravelNPCStay;
            // 修改旗帜需求
            On_NPC.CountKillForBannersAndDropThem += NPC_CountKillForBannersAndDropThem;
            // 熔岩史莱姆不生成熔岩
            IL_NPC.VanillaHitEffect += LavalessLavaSlime;
            // 死后保存Buff
            IL_Player.UpdateDead += KeepBuffOnUpdateDead;
            // 禁止腐化蔓延
            IL_WorldGen.UpdateWorld_Inner += DisableBiomeSpread;
            // NPC住在腐化
            IL_WorldGen.ScoreRoom += LiveInCorrupt;

            // 移除Social和Favorite提示
            // 现在没空修，先保证能运行。
            /*IL_Main.MouseText_DrawItemTooltip_GetLinesInfo += il =>
            {
                var c = new ILCursor(il);

                QuickModify(nameof(Item.favorited));
                QuickModify(nameof(Item.social));

                void QuickModify(string name)
                {
                    if (!c.TryGotoNext(
                            MoveType.After,
                            i => i.Match(OpCodes.Ldarg_0),
                            i => i.MatchLdfld<Item>(name)))
                        return;
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldc_I4_0);
                }
            };*/

            // 大背包内弹药可直接被使用
            On_Player.ChooseAmmo += (orig, player, weapon) =>
                orig.Invoke(player, weapon) ??
                GetAllInventoryItemsList(player, "inv portable")
                    .FirstOrDefault(i => i.stack > 0 && ItemLoader.CanChooseAmmo(weapon, i, player), null);
            // 大背包内弹药在UI的数值显示
            IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += il =>
            {
                var c = new ILCursor(il);
                if (!c.TryGotoNext(MoveType.After,
                        i => i.Match(OpCodes.Ldloc_1),
                        i => i.MatchLdfld<Item>(nameof(Item.useAmmo)),
                        i => i.Match(OpCodes.Ldc_I4_0),
                        i => i.Match(OpCodes.Ble_S),
                        i => i.Match(OpCodes.Ldloc_1),
                        i => i.MatchLdfld<Item>(nameof(Item.useAmmo)),
                        i => i.Match(OpCodes.Pop),
                        i => i.Match(OpCodes.Ldc_I4_0)))
                    return;
                c.Emit(OpCodes.Ldloc_1); // 将weapon读入
                c.EmitDelegate<Func<int, Item, int>>((_, weapon) =>
                {
                    ItemCount(GetAllInventoryItemsList(Main.LocalPlayer, "inv portable").ToArray(),
                        i => i.stack > 0 && ItemLoader.CanChooseAmmo(weapon, i, Main.LocalPlayer), out int count);
                    return count;
                });
            };
        }

        private void LiveInCorrupt(ILContext il)
        {
            // int num3 = -WorldGen.GetTileTypeCountByCategory(tileTypeCounts, TileScanGroup.TotalGoodEvil);
            // if (num3 < 50) { ... }
            // IL_005F: call      int32 Terraria.WorldGen::GetTileTypeCountByCategory(int32[], valuetype Terraria.Enums.TileScanGroup)
            // IL_0064: neg
            // IL_0065: stloc.s   num3
            // IL_0067: ldloc.s   num3
            // IL_0069: ldc.i4.s  50
            // IL_006B: bge.s     IL_0070
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                i => i.MatchCall(typeof(WorldGen).GetMethod("GetTileTypeCountByCategory")),
                i => i.Match(OpCodes.Neg),
                i => i.Match(OpCodes.Stloc_S),
                i => i.Match(OpCodes.Ldloc_S),
                i => i.Match(OpCodes.Ldc_I4_S)))
                return;
            // < 50则会设置为0，开选项的时候把这个设置成114514就行了
            c.EmitDelegate<Func<int, int>>((returnValue) => Config.NPCLiveInEvil ? 114514 : returnValue);
        }

        private void DisableBiomeSpread(ILContext il)
        {
            var c = new ILCursor(il);
            /* IL_0022: ldc.i4.0
             * IL_0023: ceq
             * IL_0025: stsfld    bool Terraria.WorldGen::AllowedToSpreadInfections
             * (原版设置的后面 插入)
             * IL_002A: ldc.i4.3
             * IL_002B: stloc.1
             */
            if (!c.TryGotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Ceq),
                i => i.MatchStsfld<WorldGen>(nameof(WorldGen.AllowedToSpreadInfections))
            ))
                return;

            var label = c.DefineLabel();
            c.Emit<MyUtils>(OpCodes.Ldsfld, nameof(Config));
            c.Emit<ImproveConfigs>(OpCodes.Ldfld, nameof(Config.NoBiomeSpread));
            c.Emit(OpCodes.Brfalse, label); // 为False，跳走
            c.Emit(OpCodes.Ldc_I4_0); // 推一个0，也就是False，设置到AllowedToSpreadInfections
            c.Emit<WorldGen>(OpCodes.Stsfld, nameof(WorldGen.AllowedToSpreadInfections));
            c.MarkLabel(label);
        }

        // 只想要保存增益，不要减益，于是复杂了起来
        private void KeepBuffOnUpdateDead(ILContext il)
        {
            var c = new ILCursor(il);

            /* IL_01C0: ldsfld    bool[] Terraria.Main::persistentBuff
             * IL_01C5: ldarg.0
             * IL_01C6: ldfld     int32[] Terraria.Player::buffType
             * IL_01CB: ldloc.2
             * IL_01CC: ldelem.i4
             * IL_01CD: ldelem.u1
             * IL_01CE: brtrue.s  IL_01E2
             */
            if (!c.TryGotoNext(
                MoveType.After,
                i => i.MatchLdsfld<Main>(nameof(Main.persistentBuff)),
                i => i.Match(OpCodes.Ldarg_0),
                i => i.MatchLdfld<Player>(nameof(Player.buffType)),
                i => i.Match(OpCodes.Ldloc_2),
                i => i.Match(OpCodes.Ldelem_I4),
                i => i.Match(OpCodes.Ldelem_U1)
            ))
                return;

            c.Emit(OpCodes.Ldarg_0); // Player实例
            c.Emit(OpCodes.Ldfld, typeof(Player).GetField(nameof(Player.buffType), BindingFlags.Instance | BindingFlags.Public)); // buffType数组
            c.Emit(OpCodes.Ldloc_2); // 索引 i
            c.Emit(OpCodes.Ldelem_I4); // 结合出int32
            c.EmitDelegate<Func<bool, int, bool>>((returnValue, buffType) =>
            {
                if (Config.DontDeleteBuff)
                {
                    // 返回false就会进入删除
                    return !Main.debuff[buffType] && !Main.buffNoSave[buffType] && !Main.lightPet[buffType] && !Main.vanityPet[buffType];
                }
                return returnValue;
            });
        }

        private void LavalessLavaSlime(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(
                MoveType.After,
                i => i.MatchCall(typeof(Main), "get_expertMode"),
                i => i.Match(OpCodes.Brfalse),
                i => i.Match(OpCodes.Ldarg_0),
                i => i.MatchLdfld(typeof(NPC), nameof(NPC.type)),
                i => i.MatchLdcI4(NPCID.LavaSlime)
            ))
                return;

            c.EmitDelegate<Func<int, int>>(returnValue =>
            {
                // 把if (type == 59) 的59换掉，NPC.type不可能为NPCLoader.NPCCount
                return Config.LavalessLavaSlime ? NPCLoader.NPCCount : returnValue;
            });
        }

        private void NPC_CountKillForBannersAndDropThem(Terraria.On_NPC.orig_CountKillForBannersAndDropThem orig, NPC npc)
        {
            int bannerID = Item.NPCtoBanner(npc.BannerID());
            int itemID = Item.BannerToItem(bannerID);
            int originalRequirement = ItemID.Sets.KillsToBanner[itemID];
            ItemID.Sets.KillsToBanner[itemID] = (int)(ItemID.Sets.KillsToBanner[itemID] * Config.BannerRequirement);
            orig.Invoke(npc);
            ItemID.Sets.KillsToBanner[itemID] = originalRequirement;
        }

        private void TravelNPCStay(Terraria.On_WorldGen.orig_UnspawnTravelNPC orig)
        {
            if (!Config.TravellingMerchantStay)
                orig.Invoke();
        }

        private bool TileDrawing_IsAlchemyPlantHarvestable(Terraria.GameContent.Drawing.On_TileDrawing.orig_IsAlchemyPlantHarvestable orig, Terraria.GameContent.Drawing.TileDrawing self, int style)
        {
            return Config.AlchemyGrassAlwaysBlooms || orig.Invoke(self, style);
        }

        private bool WorldGen_IsHarvestableHerbWithSeed(Terraria.On_WorldGen.orig_IsHarvestableHerbWithSeed orig, int type, int style)
        {
            return Config.AlchemyGrassAlwaysBlooms || orig.Invoke(type, style);
        }

        // “草药” 是否掉落成熟时候物品
        /*private void WorldGen_KillTile_GetItemDrops(ILContext il) {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ret),
                i => i.Match(OpCodes.Ldloc_S)))
                return;
            c.EmitDelegate<Func<bool, bool>>(flag => MyUtils.Config.AlchemyGrassAlwaysBlooms || flag);
        }*/

        // “草药” 是否可以被 “再生法杖” 收割
        private static int style = 0;
        private void Player_PlaceThing_Tiles_BlockPlacementForAssortedThings(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_I4_S, (sbyte)84),
                i => i.Match(OpCodes.Bne_Un_S)))
                return;
            c.EmitDelegate(() =>
            {
                if (Config.StaffOfRegenerationAutomaticPlanting)
                {
                    style = Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX / 18;
                }
            });

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Call)))
                return;
            c.EmitDelegate(() =>
            {
                if (Config.StaffOfRegenerationAutomaticPlanting)
                {
                    WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, 82, false, false, -1, style);
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, Player.tileTargetX, Player.tileTargetY, 82, style);
                }
            });

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_R8, 40500d),
                i => i.Match(OpCodes.Ble_Un_S),
                i => i.Match(OpCodes.Ldc_I4_1),
                i => i.Match(OpCodes.Stloc_S),
                i => i.Match(OpCodes.Ldloc_S)))
                return;
            c.EmitDelegate<Func<bool, bool>>(flag =>
            {
                if (Config.StaffOfRegenerationAutomaticPlanting)
                {
                    style = Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX / 18;
                }
                return Config.AlchemyGrassAlwaysBlooms || flag;
            });

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldc_R4),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Ldc_I4_0),
                i => i.Match(OpCodes.Call)))
                return;
            c.EmitDelegate(() =>
            {
                if (Config.StaffOfRegenerationAutomaticPlanting)
                {
                    WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, 82, false, false, -1, style);
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, Player.tileTargetX, Player.tileTargetY, 82, style);
                }
            });
        }

        // 提升草药生长速度
        private void WorldGen_GrowAlch(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Call),
                i => i.Match(OpCodes.Ldc_I4_S)))
                return;
            c.EmitDelegate<Func<int, int>>(num => Config.AlchemyGrassGrowsFaster ? 1 : num);

            /*if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ret),
                i => i.Match(OpCodes.Ldsfld)))
                return;
            c.EmitDelegate<Func<bool, bool>>(flag => MyUtils.Config.DisableAlchemyPlantRipeCondition ? true : flag); // “太阳花”

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ret),
                i => i.Match(OpCodes.Ldsfld)))
                return;
            c.EmitDelegate<Func<bool, bool>>(flag => MyUtils.Config.DisableAlchemyPlantRipeCondition ? false : flag); // “月光草”

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ret),
                i => i.Match(OpCodes.Ldsfld)))
                return;
            c.EmitDelegate<Func<bool, bool>>(flag => MyUtils.Config.DisableAlchemyPlantRipeCondition ? true : flag); // “幌菊”

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Conv_R8),
                i => i.Match(OpCodes.Ldsfld)))
                return;
            c.EmitDelegate<Func<double, double>>(x => MyUtils.Config.DisableAlchemyPlantRipeCondition ? 0 : x); // “闪耀根”

            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Ldsfld),
                i => i.Match(OpCodes.Ldc_I4),
                i => i.Match(OpCodes.Sub)))
                return;
            c.EmitDelegate<Func<int, int>>(x => MyUtils.Config.DisableAlchemyPlantRipeCondition ? 0 : x); // “火焰花”*/
        }

        /// <summary>
        /// 旗帜BUFF在背包生效
        /// </summary>
        private static void AddBannerBuff(Item item)
        {
            if (item is null)
                return;

            bool globalItemNotNull = item.TryGetGlobalItem<GlobalItemData>(out var itemData);
            if (globalItemNotNull)
                itemData.ShouldHaveInvGlowForBanner = false;

            int bannerID = ItemToBanner(item);
            if (bannerID != -1)
            {
                Main.SceneMetrics.NPCBannerBuff[bannerID] = true;
                Main.SceneMetrics.hasBanner = true;
                if (globalItemNotNull)
                    itemData.ShouldHaveInvGlowForBanner = true;
            }
        }

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            // 不要模拟
            if (TileCounter.Simulating)
                return;
            // 随身旗帜（增益站）
            if (Config.NoPlace_BUFFTile_Banner && Main.netMode is not NetmodeID.Server)
            {
                TryAddBuff(Main.LocalPlayer);
                if (Config.ShareInfBuffs)
                    CheckTeamPlayers(Main.myPlayer, TryAddBuff);
            }
        }

        private static void TryAddBuff(Player player)
        {
            var items = GetAllInventoryItemsList(player);
            foreach (var item in items)
            {
                AddBannerBuff(item);
                if (item is not null && !item.IsAir && item.ModItem is not null && item.ModItem is BannerChest bannerChest && bannerChest.StoredBanners.Count > 0)
                {
                    foreach (var p in bannerChest.StoredBanners)
                    {
                        AddBannerBuff(p);
                    }
                }
            }
        }

        /// <summary>
        /// 使存钱罐中物品如同放在背包
        /// </summary>
        private void TweakExtraUpdateInventory(On_Player.orig_UpdateEquips orig, Player self, int i)
        {
            orig(self, i);

            if (Main.myPlayer != self.whoAmI)
                return;

            var items = GetAllInventoryItemsList(self, "inv");
            foreach (var item in items)
            {
                if (item.type != ItemID.EncumberingStone)
                {
                    ItemLoader.UpdateInventory(item, self);

                    self.RefreshInfoAccsFromItemType(item.type);
                    self.RefreshMechanicalAccsFromItemType(item.type);
                }
            }
        }

        /// <summary>
        /// 伤害波动
        /// </summary>
        private int DisableDamageVar(On_Main.orig_DamageVar_float_int_float orig, float dmg, int percent, float luck)
        {
            if (Config.BanDamageVar)
                return (int)Math.Round(dmg);
            else
                return orig(dmg, percent, luck);
        }

        /// <summary>
        /// 空间法杖计算剩余平台数
        /// </summary>
        private void TweakDrawCountInventory(ILContext il)
        {
            // 计算剩余平台
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                i => i.Match(OpCodes.Pop),
                i => i.Match(OpCodes.Ldc_I4_M1)))
                return;
            c.Emit(OpCodes.Ldarg_1); // 玩家物品槽
            c.Emit(OpCodes.Ldarg_2); // content
            c.Emit(OpCodes.Ldarg_3); // 物品在物品槽的位置
            c.EmitDelegate<Func<int, Item[], int, int, int>>((num11, inv, content, slot) =>
            {
                if (content == 13)
                {
                    if (inv[slot].ModItem is SpaceWand)
                    {
                        SpaceWand spaceWand = inv[slot].ModItem as SpaceWand;
                        ItemCount(inv, spaceWand.GetConditions(), out int count);
                        return count;
                    }
                    else if (inv[slot].ModItem is WallPlace)
                    {
                        ItemCount(inv, (item) => item.createWall > -1, out int count);
                        return count;
                    }
                    return -1;
                }
                else
                {
                    return -1;
                }
            });
        }

        /// <summary>
        /// 墓碑掉落
        /// </summary>

        private void DisableDropTombstone(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            if (!Config.BanTombstone)
            {
                orig(self, coinsOwned, deathText, hitDirection);
            }
        }

        /// <summary>
        /// 摇树总掉水果
        /// </summary>
        private void TweakShakeTree(ILContext il)
        {
            try
            {
                // 源码，在最后：
                // if (flag) {
                //     [摇树有物品出现，执行一些特效]
                // }
                // 搞到这个flag, 如果为false(没东西)就加水果, 然后让他读到true
                // IL_0DAF: ldloc.s   flag
                // IL_0DB1: brfalse.s IL_0E12
                // 这两行就可以精确找到, 因为其他地方没有相同的
                // 值得注意的是，代码开始之前有这个：
                // treeShakeX[numTreeShakes] = x;
                // treeShakeY[numTreeShakes] = y;
                // numTreeShakes++;
                // 所以我们可以直接用了，都不需要委托获得x, y

                ILCursor c = new(il);

                if (!c.TryGotoNext(MoveType.Before,
                                   i => i.Match(OpCodes.Ldloc_S),
                                   i => i.Match(OpCodes.Brfalse_S)))
                {
                    ErrorTweak();
                    return;
                }

                c.Index++;
                c.EmitDelegate<Func<bool, bool>>((shackSucceed) =>
                {
                    if (!shackSucceed && Config.ShakeTreeFruit)
                    {
                        int x = WorldGen.treeShakeX[WorldGen.numTreeShakes - 1];
                        int y = WorldGen.treeShakeY[WorldGen.numTreeShakes - 1];
                        int tileType = Main.tile[x, y].TileType;
                        TreeTypes treeType = WorldGen.GetTreeType(tileType);

                        // 获取到顶部
                        y--;
                        while (y > 10 && Main.tile[x, y].HasTile && TileID.Sets.IsShakeable[Main.tile[x, y].TileType])
                        {
                            y--;
                        }
                        y++;

                        int fruit = CollectHelper.GetShakeTreeFruit(treeType);
                        if (fruit > -1)
                        {
                            Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), x * 16, y * 16, 16, 16, fruit);
                            shackSucceed = true;
                        }
                    }
                    return shackSucceed;
                });

            }
            catch
            {
                ErrorTweak();
                return;
            }
        }

        private static void ErrorTweak()
        {
            string exception = "Something went wrong in TweakShakeTree(), please contact with the mod developers.\nYou can still use the mod, but the \"Always drop fruit when shaking the tree\" option will not work";
            if (GameCulture.FromCultureName(GameCulture.CultureName.Chinese).IsActive)
                exception = "TweakShakeTree()发生错误，请联系Mod制作者\n你仍然可以使用Mod，但是“摇树总掉水果”选项不会起作用";
            ImproveGame.Instance.Logger.Warn(exception);
        }
    }
}