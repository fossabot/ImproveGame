﻿using ImproveGame.Content.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ImproveGame.Common.Systems
{
    public class UISystem : ModSystem
    {
        public static UserInterface userInterface;
        public static VaultUI vaultUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                vaultUI = new VaultUI();
                vaultUI.Activate();
                userInterface = new UserInterface();
                userInterface.SetState(vaultUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (VaultUI.Visible)
            {
                userInterface.Update(gameTime);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (MouseTextIndex != -1)
            {
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
                    "ImproveGame: VaultUI",
                    delegate
                    {
                        if (VaultUI.Visible)
                        {
                            vaultUI.Draw(Main.spriteBatch);
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }
}