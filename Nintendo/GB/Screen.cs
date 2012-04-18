using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintendo.GB
{
    public static class Screen
    {
        public enum ModeType
        {
            HBlank = 0,
            VBlank = 1,
            SearchingOamRam = 2,
            TransferingData = 3
        }

        public const uint White = 0xFFFFFFFF;
        public const uint LightGray = 0xFFAAAAAA;
        public const uint DarkGray = 0xFF555555;
        public const uint Black = 0xFF000000;

        public static uint[] bgPallete = { White, LightGray, DarkGray, Black };
        public static uint[] objPallete0 = { White, LightGray, DarkGray, Black };
        public static uint[] objPallete1 = { White, LightGray, DarkGray, Black };

        public static ModeType Mode;
        public static bool CoincidenceInterruptEnabled;
        public static bool OamInterruptEnabled;
        public static bool VBlankInterruptEnabled;
        public static bool HBlankInterruptEnabled;
        public static bool InterruptRequested;
        public static bool InterruptEnabled;
        public static bool VBlankInterruptRequested;
        public static bool HBlankInterruptRequested;
        public static bool ControlOperationEnabled;
        public static bool sprDisplayed;
        public static bool bgDisplayed;
        public static bool windowTileMapDisplaySelect;
        public static bool windowDisplayed;
        public static bool backgroundAndWindowTileDataSelect;
        public static bool backgroundTileMapDisplaySelect;
        public static bool largeSprites;

        public static uint[,] bgBuffer = new uint[256, 256];
        public static bool[,] bgTileInvalidated = new bool[32, 32];
        public static bool invalidateAllBackgroundTilesRequest;
        public static uint[, , ,] sprTile = new uint[256, 8, 8, 2];
        public static bool[] sprTileInvalidated = new bool[256];
        public static bool invalidateAllSpriteTilesRequest;
        public static uint[,] windowBuffer = new uint[144, 168];

        public static byte scrollX;
        public static byte scrollY;
        public static byte windowX;
        public static byte windowY;
        public static byte ly;
        public static byte lyCompare;
    }
}
