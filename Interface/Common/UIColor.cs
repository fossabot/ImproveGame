﻿namespace ImproveGame.Interface.Common
{
    public class UIColor
    {
        public static ModColors Default = new();
    }

    public class ModColors
    {
        public Color PanelBorder = new(18, 18, 38);
        public Color PanelBackground = new(44, 57, 105, 160);

        public Color SlotFavoritedBorder = new(233, 176, 0, 200);
        public Color SlotNoFavoritedBorder = new(18, 18, 38, 200);
        public Color SlotFavoritedBackground = new(83, 88, 151, 200);
        public Color SlotNoFavoritedBackground = new(63, 65, 151, 200);

        public Color ButtonBackground = new(54, 56, 130);

        public Color TitleBackground = new(35, 40, 83);

        public Color CheckBackground = new(44, 57, 105);

        // 边框
        public Color CheckBoxBorder = new(21, 15, 56);
        public Color CheckBoxBorderHover = new(233, 176, 0, 200);
        // 开关中的圆形
        public Color CheckBoxRound = new(21, 15, 56);
        public Color CheckBoxRoundHover = new(233, 176, 0, 200);
        // 背景色
        public Color CheckBoxBackground = new(52, 34, 143);
        public Color CheckBoxBackgroundHover = new(52, 34, 143);

        public Color CloseBackground = new(200, 40, 40);
    }
}
