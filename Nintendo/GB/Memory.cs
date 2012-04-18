using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintendo.GB
{
    public class MemoryData
    {
        /*   General Memory Map
         *   0000-3FFF   16KB ROM Bank 00     (in cartridge, fixed at bank 00)
         *   4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
         *   8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
         *   A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
         *   C000-CFFF   4KB Work RAM Bank 0 (WRAM)
         *   D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
         *   E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
         *   FE00-FE9F   Sprite Attribute Table (OAM)
         *   FEA0-FEFF   Not Usable
         *   FF00-FF7F   I/O Ports
         *   FF80-FFFE   High RAM (HRAM)
         *   FFFF        Interrupt Enable Register
         */
        public byte[] mmap = new byte[0x10000];

        public ushort[] romb0 = { 0x0000, 0x3FFF };
        public ushort[] romb1 = { 0x4000, 0x7FFF };
        public ushort[] vram = { 0x8000, 0x9FFF };
        public ushort[] eram = { 0xA000, 0xBFFF };
        public ushort[] wram1 = { 0xC000, 0xCFFF };
        public ushort[] wram2 = { 0xD000, 0xDFFF };
        public ushort[] echo = { 0xE000, 0xFDFF };
        public ushort[] oam = { 0xFE00, 0xFE9F };
        public ushort[] io = { 0xFF00, 0xFF7F };
        public ushort[] hram = { 0xFF80, 0xFFFE };
        public ushort ier = 0xFFFF;

        public bool IOTransferCompleteInterruptRequested;
        public bool IOTransferCompleteInterruptEnabled;

        // Careful with RAM Banks, MBC1 only needs this when using 32KByte RAM MBC chip
        private byte[,] rambanks = new byte[0x04, 0xBFFF - 0xA000]; // 8KB banks, total of 32KByte
        private byte ramBank = 0;
        private byte romBank = 0;
        public bool eramEnabled = false;
        public bool ramBankingMode = false;
        public bool rtcOrRam = false; // RAM by default
        public byte[] rtc = new byte[0x05];
        private byte rtcSelector = 0x00;

        public byte this [int index]
        {
            get
            {
                if (index > ushort.MaxValue)
                {
                    return 0;
                }

                // Those are common for every MBC chip
                // Not-banked rom read
                if ((index >= 0x0000) && (index <= 0x3FFF))
                {
                    // Move the reader to the desired position
                    Interpreter.ROMdata.SetPosition((ushort)(index));

                    byte result = Interpreter.ROMdata.ReadByte();

                    // Move back to the program counter
                    Interpreter.ROMdata.SetPosition(Registers.pc);

                    // Send back the byte
                    return result;
                }

                // Rom banked read
                if ((index >= 0x4000) && (index <= 0x7FFF))
                {
                    // Move the reader to the desired position:
                    // romBank plus the first 0x3FFF from the memory address
                    ushort addr = (ushort)(((romBank * 0x3FFF) + 0x3FFF) + (index - 0x4000));

                    // Read the byte
                    Interpreter.ROMdata.SetPosition(addr);
                    byte result = Interpreter.ROMdata.ReadByte();

                    // Move back to the program counter
                    Interpreter.ROMdata.SetPosition(Registers.pc);

                    // Send back the byte
                    return result;
                }

                // Specific functions for reading from MBC chips
                switch (Interpreter.ROMdata.catridgeType)
                {
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1_RAM:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1_RAM_BATT:
                        // Ram banked read
                        if ((index >= 0xA000) && (index <= 0xBFFF))
                        {
                            return rambanks[ramBank, index - 0xA000];
                        }
                        break;
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC2:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC2_BATTERY:
                        // RAM read
                        if ((index >= 0xA000) && (index <= 0xA1FF))
                        {
                            return (byte)(mmap[index] & 0xF);
                        }

                        break;

                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_RAM:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_RAM_BATT:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_TIMER_BATT:
                    case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_TIMER_RAM_BATT:
                        if ((index >= 0xA000) && (index <= 0xBFFF))
                        {
                            if (rtcOrRam == false) // RAM
                            {
                                return rambanks[ramBank, index - 0xA000];
                            }
                            else
                            {
                                byte reg = (byte)(rtcSelector - 0x08);
                                return rtc[reg];
                            }
                        }

                        break;
                }

                switch (index)
                {
                    case 0xFF00: // Keypad
                        {
                            if (Keypad.p14)
                            {
                                byte value = 0;
                                if (Keypad.Down)
                                {
                                    value = (byte)(value | 0x08);
                                }

                                if (Keypad.Up)
                                {
                                    value = (byte)(value | 0x04);
                                }

                                if (Keypad.Left)
                                {
                                    value = (byte)(value | 0x02);
                                }

                                if (Keypad.Right)
                                {
                                    value = (byte)(value | 0x01);
                                }

                                mmap[index] = value;
                            }
                            else if (Keypad.p15)
                            {
                                byte value = 0;

                                if (Keypad.Start)
                                {
                                    value = (byte)(value | 0x08);
                                }

                                if (Keypad.Select)
                                {
                                    value = (byte)(value | 0x04);
                                }

                                if (Keypad.B)
                                {
                                    value = (byte)(value | 0x02);
                                }

                                if (Keypad.A)
                                {
                                    value = (byte)(value | 0x01);
                                }

                                mmap[index] = value;
                            }
                        }
                        break;
                    case 0xFF04: // Timer divider
                        mmap[index] = Timer.Ticks;
                        break;
                    case 0xFF05: // Timer counter
                        mmap[index] = Timer.Counter;
                        break;
                    case 0xFF06: // Timer modulo
                        mmap[index] = Timer.Modulo;
                        break;
                    case 0xFF07: // Timer control
                        {
                            byte value = 0;
                            if (Timer.Running)
                            {
                                value = (byte)(value | 0x04);
                            }

                            value = (byte)(value | (byte)Timer.Frequency);
                            mmap[index] = value;
                        }
                        break;
                    case 0xFF0F: // Interrupt Flag(an interrupt requested)
                        {
                            byte value = 0;

                            if (Keypad.InterruptRequested)
                            {
                                value = (byte)(value | 0x10);
                            }

                            if (IOTransferCompleteInterruptRequested)
                            {
                                value = (byte)(value | 0x08);
                            }

                            if (Timer.OverflowInterruptRequested)
                            {
                                value = (byte)(value | 0x04);
                            }

                            if (Screen.InterruptRequested)
                            {
                                value = (byte)(value | 0x02);
                            }

                            if (Screen.VBlankInterruptRequested)
                            {
                                value = (byte)(value | 0x01);
                            }

                            mmap[index] = value;
                        }
                        break;
                    case 0xFF40: // LCDC Control
                        {
                            byte value = 0;

                            if (Screen.ControlOperationEnabled)
                            {
                                value = (byte)(value | 0x80);
                            }

                            if (Screen.windowTileMapDisplaySelect)
                            {
                                value = (byte)(value | 0x40);
                            }

                            if (Screen.windowDisplayed)
                            {
                                value = (byte)(value | 0x20);
                            }

                            if (Screen.backgroundAndWindowTileDataSelect)
                            {
                                value = (byte)(value | 0x10);
                            }

                            if (Screen.backgroundTileMapDisplaySelect)
                            {
                                value = (byte)(value | 0x08);
                            }

                            if (Screen.largeSprites)
                            {
                                value = (byte)(value | 0x04);
                            }

                            if (Screen.sprDisplayed)
                            {
                                value = (byte)(value | 0x02);
                            }

                            if (Screen.bgDisplayed)
                            {
                                value = (byte)(value | 0x01);
                            }

                            mmap[index] = value;
                        }
                        break;
                    case 0xFF41: // LCDC Status
                        {
                            byte value = 0;
                            if (Screen.CoincidenceInterruptEnabled)
                            {
                                value = (byte)(value | 0x40);
                            }

                            if (Screen.OamInterruptEnabled)
                            {
                                value = (byte)(value | 0x20);
                            }

                            if (Screen.VBlankInterruptEnabled)
                            {
                                value = (byte)(value | 0x10);
                            }

                            if (Screen.HBlankInterruptEnabled)
                            {
                                value = (byte)(value | 0x08);
                            }

                            if (Screen.ly == Screen.lyCompare)
                            {
                                value = (byte)(value | 0x04);
                            }

                            value = (byte)(value | (byte)Screen.Mode);
                            mmap[index] = value;
                        }
                        break;
                    case 0xFF42: // Scroll Y
                        mmap[index] = Screen.scrollY;
                        break;
                    case 0xFF43: // Scroll X
                        mmap[index] = Screen.scrollX;
                        break;
                    case 0xFF44: // LY
                        mmap[index] = Screen.ly;
                        break;
                    case 0xFF45: // LYCompare
                        mmap[index] = Screen.lyCompare;
                        break;
                    case 0xFF47: // Background palete
                        {
                            Screen.invalidateAllBackgroundTilesRequest = true;

                            byte value = 0;
                            for (int i = 3; i >= 0; i--)
                            {
                                value <<= 2;
                                switch (Screen.bgPallete[i])
                                {
                                    case Screen.Black:
                                        value = (byte)(value | 0x03);
                                        break;
                                    case Screen.DarkGray:
                                        value = (byte)(value | 0x02);
                                        break;
                                    case Screen.LightGray:
                                        value = (byte)(value | 0x01);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            mmap[index] = value;
                        }
                        break;
                    case 0xFF48: // Object palette 0
                        {
                            Screen.invalidateAllSpriteTilesRequest = true;
                            byte value = 0;

                            for (int i = 3; i >= 0; i--)
                            {
                                value <<= 2;

                                switch (Screen.objPallete0[i])
                                {
                                    case Screen.Black:
                                        value = (byte)(value | 0x03);
                                        break;
                                    case Screen.DarkGray:
                                        value = (byte)(value | 0x02);
                                        break;
                                    case Screen.LightGray:
                                        value = (byte)(value | 0x01);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            mmap[index] = value;
                        }
                        break;
                    case 0xFF49: // Object palette 1
                        {
                            Screen.invalidateAllSpriteTilesRequest = true;
                            byte value = 0;

                            for (int i = 3; i >= 0; i--)
                            {
                                value <<= 2;

                                switch (Screen.objPallete1[i])
                                {
                                    case Screen.Black:
                                        value = (byte)(value | 0x03);
                                        break;
                                    case Screen.DarkGray:
                                        value = (byte)(value | 0x02);
                                        break;
                                    case Screen.LightGray:
                                        value = (byte)(value | 0x01);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            mmap[index] = value;
                        }
                        break;
                    case 0xFF4A: // Window Y
                        mmap[index] = Screen.windowY;
                        break;
                    case 0xFF4B: // Window X
                        mmap[index] = Screen.windowX;
                        break;
                    case 0xFFFF: // Interrupt enable
                        {
                            byte value = 0;
                            if (Keypad.InterruptEnabled)
                            {
                                value = (byte)(value | 0x10);
                            }

                            if (IOTransferCompleteInterruptEnabled)
                            {
                                value = (byte)(value | 0x08);
                            }

                            if (Timer.OverflowInterruptEnabled)
                            {
                                value = (byte)(value | 0x04);
                            }

                            if (Screen.InterruptEnabled)
                            {
                                value = (byte)(value | 0x02);
                            }

                            if (Screen.VBlankInterruptEnabled)
                            {
                                value = (byte)(value | 0x01);
                            }

                            mmap[index] = value;
                        }
                        break;
                }

                return mmap[index];
            }

            set
            {
                if (index <= ushort.MaxValue)
                {
                    switch (Interpreter.ROMdata.catridgeType)
                    {
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1_RAM:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC1_RAM_BATT:
                            if ((index >= 0x0000) && (index <= 0x1FFF))
                            {
                                // External RAM enabler
                                if ((value & 0x0A) == 0x0A)
                                {
                                    eramEnabled = true;
                                }
                                else
                                {
                                    eramEnabled = false;
                                }

                                return;
                            }
                            else if ((index >= 0x2000) && (index <= 0x3FFF))
                            {
                                // ROM Bank number lower 5 bits
                                byte l = (byte)(value & 0x1F);
                                romBank = (byte)(romBank | l);

                                // Check for non-allowed ROM Banks
                                if (romBank == 0x00)
                                    romBank = 0x01;
                                else if (romBank == 0x20)
                                    romBank = 0x21;
                                else if (romBank == 0x40)
                                    romBank = 0x41;
                                else if (romBank == 0x60)
                                    romBank = 0x61;

                                return;
                            }
                            else if ((index >= 0x4000) && (index <= 0x2FFF))
                            {
                                // RAM Bank number or upper bits of ROM bank
                                if (ramBankingMode)
                                {
                                    // The 2 lower bits mean the RAM Bank
                                    ramBank = (byte)(value & 0x03);
                                }
                                else
                                {
                                    // The 2 lower bits mean the 2 upper bits of ROM Bank
                                    romBank = (byte)(romBank | (value << 5));

                                    // Check for non-allowed ROM Banks
                                    if (romBank == 0x00)
                                        romBank = 0x01;
                                    else if (romBank == 0x20)
                                        romBank = 0x21;
                                    else if (romBank == 0x40)
                                        romBank = 0x41;
                                    else if (romBank == 0x60)
                                        romBank = 0x61;
                                }

                                return;
                            }
                            else if ((index >= 0x6000) && (index <= 0x7FFF))
                            {
                                // ROM/RAM mode select
                                ramBankingMode = (value & 0x01) == 0x01;
                                return;
                            }
                            else if ((index >= 0xA000) && (index <= 0xBFFF))
                            {
                                // Different RAM banks, care about them
                                rambanks[ramBank, index - 0xA000] = value;
                                return;
                            }
                            break;
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC2:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC2_BATTERY:
                            // MBC2 Write functions
                            if ((index >= 0xA000) && (index <= 0xA1FF))
                            {
                                // This area only cares about the last 4bits of values
                                mmap[index] = (byte)(value & 0x0F);
                                return;
                            }
                            else if ((index >= 0x000) && (index <= 0x1FFF))
                            {
                                // RAM Enable
                                // This is really the ROM address, but its read-only
                                // So we use the write to enable the extra RAM
                                // The least significant bit of the upper address space
                                byte h = (byte)((index >> 8) & 0xFF);
                                byte lsb = (byte)(h & 0x01);

                                if (lsb == 0x0)
                                {
                                    // We dont handle it, as we dont really need to enable it, just read/write to the memory address
                                }
                                return;
                            }
                            else if ((index >= 0x2000) && (index <= 0x3FFF))
                            {
                                // ROM Bank number
                                byte h = (byte)((index >> 8) & 0xFF);
                                byte lsb = (byte)(h & 0x01);

                                // Least significant bit of the upper address space
                                if (lsb == 0x01)
                                {
                                    romBank = (byte)(value & (0x03 << 1));
                                }

                                return;
                            }
                            break;
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_RAM:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_RAM_BATT:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_TIMER_BATT:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_MBC3_TIMER_RAM_BATT:
                            if ((index >= 0xA000) && (index <= 0xBFFF))
                            {
                                // This can be a problem, used both for RAM and Timer access
                                if (rtcOrRam == false)
                                {
                                    rambanks[ramBank, index - 0xA000] = value;
                                }
                                else
                                {
                                    // Do something here to handle RTC
                                    byte reg = (byte)(rtcSelector - 0x08);
                                    rtc[reg] = value;

                                    if (rtcSelector == 0x0C)
                                    {
                                        if ((value & 0x40) == 0x40)
                                        {
                                            Timer.RTCEnabled = false;
                                        }
                                        else
                                        {
                                            Timer.RTCEnabled = true;
                                        }
                                    }
                                }

                                return;
                            }
                            else if ((index >= 0x0000) && (index <= 0x1FFF))
                            {
                                // RAM and Timer Enable
                                // We should not care about this
                                return;
                            }
                            else if ((index >= 0x4000) && (index <= 0x5FFF))
                            {
                                if ((value & 0x03) != 0x00)
                                {
                                    rtcOrRam = false; // RAM
                                    ramBank = (byte)(value & 0x03);
                                }
                                else
                                {
                                    rtcOrRam = true; // RTC
                                    rtcSelector = value;
                                }

                                return;
                            }

                            break;
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_RAM:
                        case Nintendo.GB.Utils.GBRom.CatridgeType.ROM_RAM_BATTERY:
                            // We can use the plain memory, as its no real problem
                            // The ROM can have a tiny MBC-like circuit connected at 0xA000, 0xBFFF
                            // And our mmap already emulates this behaviour without even using another variable
                            break;
                    }
                    
                    if ((index >= 0x8000) && (index <= 0x9FFF))
                    {
                        ushort vramIndex = (ushort)(index - 0x8000);

                        if (index < 0x9000)
                        {
                            Screen.sprTileInvalidated[vramIndex >> 4] = true;
                        }

                        if (index < 0x9800)
                        {
                            Screen.invalidateAllBackgroundTilesRequest = true;
                        }
                        else if (index >= 0x9C00)
                        {
                            int tileIndex = index - 0x9C00;
                            Screen.bgTileInvalidated[tileIndex >> 5, tileIndex & 0x1F] = true;
                        }
                        else
                        {
                            int tileIndex = index - 0x9800;
                            Screen.bgTileInvalidated[tileIndex >> 5, tileIndex & 0x1F] = true;
                        }
                    }
                    else if (index >= 0xFF00)
                    {
                        switch (index)
                        {
                            case 0xFF00: // Keypad
                                Keypad.p14 = (value & 0x10) != 0x10;
                                Keypad.p15 = (value & 0x20) != 0x20;
                                break;
                            case 0xFF04: // Timer divider
                                break;
                            case 0xFF05: // Timer counter
                                Timer.Counter = value;
                                break;
                            case 0xFF06: // Timer modulo
                                Timer.Modulo = value;
                                break;
                            case 0xFF07: // Timer control
                                Timer.Running = (value & 0x04) == 0x04;
                                Timer.Frequency = (Timer.FrequencyType)(0x03 & value);
                                break;
                            case 0xFF0F: // Interrupt flag (an interrupt request)
                                Keypad.InterruptRequested = (value & 0x10) == 0x10;
                                IOTransferCompleteInterruptRequested = (value & 0x08) == 0x08;
                                Timer.OverflowInterruptRequested = (value & 0x04) == 0x04;
                                Screen.InterruptRequested = (value & 0x02) == 0x02;
                                Screen.VBlankInterruptRequested = (value & 0x01) == 0x01;
                                break;
                            case 0xFF40: // LCDC Control
                                bool bgAndWindowTileDataSelect = Screen.backgroundAndWindowTileDataSelect;
                                bool bgTileMapDisplaySelect = Screen.backgroundTileMapDisplaySelect;
                                bool windowTileMapDisplaySelect = Screen.windowTileMapDisplaySelect;

                                Screen.ControlOperationEnabled = (value & 0x80) == 0x80;
                                Screen.windowTileMapDisplaySelect = (value & 0x40) == 0x40;
                                Screen.windowDisplayed = (value & 0x20) == 0x20;
                                Screen.backgroundAndWindowTileDataSelect = (value & 0x10) == 0x10;
                                Screen.backgroundTileMapDisplaySelect = (value & 0x08) == 0x08;
                                Screen.largeSprites = (value & 0x04) == 0x04;
                                Screen.sprDisplayed = (value & 0x02) == 0x02;
                                Screen.bgDisplayed = (value & 0x01) == 0x01;

                                if (bgAndWindowTileDataSelect != Screen.backgroundAndWindowTileDataSelect
                                    || bgTileMapDisplaySelect != Screen.backgroundTileMapDisplaySelect
                                    || windowTileMapDisplaySelect != Screen.windowTileMapDisplaySelect)
                                {
                                    Screen.invalidateAllBackgroundTilesRequest = true;
                                }
                                break;
                            case 0xFF41: // LCD Status
                                Screen.CoincidenceInterruptEnabled = (value & 0x40) == 0x40;
                                Screen.OamInterruptEnabled = (value & 0x20) == 0x20;
                                Screen.VBlankInterruptEnabled = (value & 0x10) == 0x10;
                                Screen.HBlankInterruptEnabled = (value & 0x08) == 0x08;
                                Screen.Mode = (Screen.ModeType)(value & 0x03);
                                break;
                            case 0xFF42: // Scroll Y
                                Screen.scrollY = value;
                                break;
                            case 0xFF43: // Scroll X
                                Screen.scrollX = value;
                                break;
                            case 0xFF44: // LY
                                Screen.ly = value;
                                break;
                            case 0xFF45: // LY Compare
                                Screen.lyCompare = value;
                                break;
                            case 0xFF46: // Memory Transfer
                                value <<= 8;
                                for (int i = 0; i < 0x8C; i++)
                                {
                                    this[0xFE00 | i] = this[value | i];
                                }
                                break;
                            case 0xFF47: // Background pallete
                                for (int i = 0; i < 4; i++)
                                {
                                    switch (value & 0x03)
                                    {
                                        case 0:
                                            Screen.bgPallete[i] = Screen.White;
                                            break;
                                        case 1:
                                            Screen.bgPallete[i] = Screen.LightGray;
                                            break;
                                        case 2:
                                            Screen.bgPallete[i] = Screen.DarkGray;
                                            break;
                                        case 3:
                                            Screen.bgPallete[i] = Screen.Black;
                                            break;
                                    }

                                    value >>= 2;
                                }
                                Screen.invalidateAllBackgroundTilesRequest = true;
                                break;
                            case 0xFF48: // Object palette 0
                                for (int i = 0; i < 4; i++)
                                {
                                    switch (value & 0x03)
                                    {
                                        case 0:
                                            Screen.objPallete0[i] = Screen.White;
                                            break;
                                        case 1:
                                            Screen.objPallete0[i] = Screen.LightGray;
                                            break;
                                        case 2:
                                            Screen.objPallete0[i] = Screen.DarkGray;
                                            break;
                                        case 3:
                                            Screen.objPallete0[i] = Screen.Black;
                                            break;
                                    }

                                    value >>= 2;
                                }
                                Screen.invalidateAllSpriteTilesRequest = true;
                                break;
                            case 0xFF49: // Object palette 1
                                for (int i = 0; i < 4; i++)
                                {
                                    switch (value & 0x03)
                                    {
                                        case 0:
                                            Screen.objPallete1[i] = Screen.White;
                                            break;
                                        case 1:
                                            Screen.objPallete1[i] = Screen.LightGray;
                                            break;
                                        case 2:
                                            Screen.objPallete1[i] = Screen.DarkGray;
                                            break;
                                        case 3:
                                            Screen.objPallete1[i] = Screen.Black;
                                            break;
                                    }

                                    value >>= 2;
                                }
                                Screen.invalidateAllSpriteTilesRequest = true;
                                break;
                            case 0xFF4A: // Window Y
                                Screen.windowY = value;
                                break;
                            case 0xFF4B: // Window X
                                Screen.windowX = value;
                                break;
                            case 0xFFFF: // Interrupt enable
                                Keypad.InterruptEnabled = (value & 0x10) == 0x10;
                                IOTransferCompleteInterruptEnabled = (value & 0x08) == 0x08;
                                Timer.OverflowInterruptEnabled = (value & 0x04) == 0x04;
                                Screen.InterruptEnabled = (value & 0x02) == 0x02;
                                Screen.VBlankInterruptEnabled = (value & 0x01) == 0x01;
                                break;
                        }
                    }
                    else
                    {
                        mmap[index] = value;
                    }
                }
            }
        }
    }
}
