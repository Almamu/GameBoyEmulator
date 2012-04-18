using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nintendo.GB.Utils;

namespace Nintendo.GB
{
    public class Interpreter
    {
        public static GBRom ROMdata = null;
        public static MemoryData Memory = new MemoryData();
        private byte lastSecond = 0;

        public int ticks 
        {
            get
            {
                return Timer.Ticks;
            }
            set
            {
                Timer.Ticks = (byte)value;
            }
        }

        public bool stopped = false;
        public bool halted = false;
        public bool interruptsEnabled = false;
        public bool stopCounting = false;

        public Interpreter(GBRom source)
        {
            ROMdata = source;

            switch (ROMdata.catridgeType)
            {
                // We need to check that the support works correctly
                case GBRom.CatridgeType.ROM_MBC1:
                case GBRom.CatridgeType.ROM_MBC1_RAM:
                case GBRom.CatridgeType.ROM_MBC1_RAM_BATT:
                case GBRom.CatridgeType.ROM:
                case GBRom.CatridgeType.ROM_RAM:
                case GBRom.CatridgeType.ROM_RAM_BATTERY:
                case GBRom.CatridgeType.ROM_MBC2:
                case GBRom.CatridgeType.ROM_MBC2_BATTERY:
                case GBRom.CatridgeType.ROM_MBC3:
                case GBRom.CatridgeType.ROM_MBC3_RAM:
                case GBRom.CatridgeType.ROM_MBC3_RAM_BATT:
                case GBRom.CatridgeType.ROM_MBC3_TIMER_BATT:
                case GBRom.CatridgeType.ROM_MBC3_TIMER_RAM_BATT:
                    break;
                default:
                    throw new NotImplementedException(string.Format("Non-supported MemoryBankController {0}", ROMdata.catridgeType));
            }

            Registers.pc = 0x100;
            ROMdata.SetPosition(0x100);
            Log.Init();
        }

        /* Step()
         * Description: Reads an opcode and calls the handler
         */
        public void Step()
        {
            // Update the RTC registers
            if (Timer.RTCEnabled == true)
            {
                if (lastSecond < DateTime.Now.Second)
                {
                    Timer.Seconds++;
                    if (Timer.Seconds == 60)
                    {
                        Timer.Seconds = 0;
                        Timer.Hours++;

                        if (Timer.Hours == 24)
                        {
                            Timer.Hours = 0;
                            Timer.Days++;

                            if (Timer.Days == 0x1FF)
                            {
                                // Set overflow flag and reset the days counter
                                // Some games use this for count more than 512 days
                                // But this requires the user to play the game
                                // Atleast once every 512 days
                                Memory.rtc[0x04] = (byte)(Memory.rtc[0x04] | 0x80);

                                Timer.Days = 0;
                            }
                        }
                    }
                }
            }

            lastSecond = (byte)(DateTime.Now.Second);

            Memory.rtc[0x00] = Timer.Seconds;
            Memory.rtc[0x01] = Timer.Minutes;
            Memory.rtc[0x02] = Timer.Hours;
            Memory.rtc[0x03] = (byte)(Timer.Days & 0xFF);
            Memory.rtc[0x04] = (byte)(Memory.rtc[0x04] | (Timer.Days >> 8));
            
            Registers.pc = ROMdata.Position();

            // TODO: Check for interrupts
            if (interruptsEnabled == true)
            {
                if ((Screen.VBlankInterruptEnabled) && (Screen.VBlankInterruptRequested))
                {
                    Screen.VBlankInterruptRequested = false;
                    Interrupt(0x0040);
                }
                else if ((Screen.InterruptEnabled) && (Screen.InterruptRequested))
                {
                    Screen.InterruptRequested = false;
                    Interrupt(0x0048);
                }
                else if ((Timer.OverflowInterruptEnabled) && (Timer.OverflowInterruptRequested))
                {
                    Timer.OverflowInterruptRequested = false;
                    Interrupt(0x0050);
                }
                else if ((Memory.IOTransferCompleteInterruptEnabled) && (Memory.IOTransferCompleteInterruptRequested))
                {
                    Memory.IOTransferCompleteInterruptRequested = false;
                    Interrupt(0x0058);
                }
                else if ((Keypad.InterruptEnabled) && (Keypad.InterruptRequested))
                {
                    Keypad.InterruptRequested = false;
                    Interrupt(0x0060);
                }
            }

            byte opcode = 0x00;

            if (halted == false)
            {
                opcode = ROMdata.ReadByte();

                if (stopCounting == true)
                {
                    stopCounting = false;
                    ROMdata.ReadBack();
                }
            }

            HandleOpcode(opcode);

            Registers.pc = ROMdata.Position();

            Log.Logger(opcode);

            if (stopped == true)
            {
                MessageBox.Show("Stop instruction reached...");
                Environment.Exit(0);
            }
        }

        /* HandleOpcode()
         * Description: Handles a normal opcode
         */
        public void HandleOpcode(byte opcode)
        {
            switch (opcode)
            {
                case 0x00: // nop
                case 0xD3:
                case 0xDB:
                case 0xDD:
                case 0xE3:
                case 0xE4:
                case 0xEB:
                case 0xEC:
                case 0xF4:
                case 0xFC:
                case 0xFD:
                    ticks += 4;
                    break;
                case 0x01: // ld bc, nn
                    LoadImmediate(ref Registers.b, ref Registers.c);
                    break;
                case 0x02: // ld (bc), a
                    WriteByte(Registers.b, Registers.c, Registers.a);
                    break;
                case 0x03: // inc bc
                    Increment(ref Registers.b, ref Registers.c);
                    break;
                case 0x04: // inc b
                    Increment(ref Registers.b);
                    break;
                case 0x05: // dec b
                    Decrement(ref Registers.b);
                    break;
                case 0x06: // ld b, n
                    LoadImmediate(ref Registers.b);
                    break;
                case 0x07: // rlca
                    RotateFastLeft(ref Registers.a);
                    break;
                case 0x08: // ld (word), sp
                    WriteWordToImmediateAddress(Registers.sp);
                    break;
                case 0x09: // add hl, bc
                    Add(ref Registers.h, ref Registers.l, Registers.b, Registers.c);
                    break;
                case 0x0A: // ld a, (bc)
                    ReadByte(ref Registers.a, Registers.b, Registers.c);
                    break;
                case 0x0B: // dec bc
                    Decrement(ref Registers.b, ref Registers.c);
                    break;
                case 0x0C: // inc c
                    Increment(ref Registers.c);
                    break;
                case 0x0D: // dec c
                    Decrement(ref Registers.c);
                    break;
                case 0x0E: // ld c, n
                    LoadImmediate(ref Registers.c);
                    break;
                case 0x0F: // rrca
                    RotateFastRight(ref Registers.a);
                    break;
                case 0x10: // stop
                    Halt(); // Basically the same as halt
                    ticks += 4;
                    break;
                case 0x11: // ld de, nn
                    LoadImmediate(ref Registers.d, ref Registers.e);
                    break;
                case 0x12: // ld (de),a
                    WriteByte(Registers.d, Registers.e, Registers.a);
                    break;
                case 0x13: // inc de
                    Increment(ref Registers.d, ref Registers.e);
                    break;
                case 0x14: // inc d
                    Increment(ref Registers.d);
                    break;
                case 0x15: // dec d
                    Decrement(ref Registers.d);
                    break;
                case 0x16: // ld d, n
                    LoadImmediate(ref Registers.d);
                    break;
                case 0x17: // rla
                    RotateFastLeftThroughCarry(ref Registers.a);
                    break;
                case 0x18: // jr n
                    JumpRelative();
                    break;
                case 0x19: // add hl, de
                    Add(ref Registers.h, ref Registers.l, Registers.d, Registers.e);
                    break;
                case 0x1A: // ld a, (de)
                    ReadByte(ref Registers.a, Registers.d, Registers.e);
                    break;
                case 0x1B: // dec de
                    Decrement(ref Registers.d, ref Registers.e);
                    break;
                case 0x1C: // inc e
                    Increment(ref Registers.e);
                    break;
                case 0x1D: // dec e
                    Decrement(ref Registers.e);
                    break;
                case 0x1E: // ld e, n
                    LoadImmediate(ref Registers.e);
                    break;
                case 0x1F: // rra
                    RotateFastRightThroughCarry(ref Registers.a);
                    break;
                case 0x20: // jr nz, n
                    JumpRelativeIfNotZero();
                    break;
                case 0x21: // ld hl, nn
                    LoadImmediate(ref Registers.h, ref Registers.l);
                    break;
                case 0x22: // ld (hl), a
                    WriteByte(Registers.h, Registers.l, Registers.a);
                    Increment(ref Registers.h, ref Registers.l);
                    break;
                case 0x23: // inc hl
                    Increment(ref Registers.h, ref Registers.l);
                    break;
                case 0x24: // inc h
                    Increment(ref Registers.h);
                    break;
                case 0x25: // dec h
                    Decrement(ref Registers.h);
                    break;
                case 0x26: // ld h, n
                    LoadImmediate(ref Registers.h);
                    break;
                case 0x27: // daa
                    DecimallyAdjustA();
                    break;
                case 0x28: // jr z, n
                    JumpRelativeIfZero();
                    break;
                case 0x29: // add hl, hl
                    Add(ref Registers.h, ref Registers.l, Registers.h, Registers.l);
                    break;
                case 0x2A: // ld a, (hli)
                    ReadByte(ref Registers.a, Registers.h, Registers.l);
                    Increment(ref Registers.h, ref Registers.l);
                    break;
                case 0x2B: // dec hl
                    Decrement(ref Registers.h, ref Registers.l);
                    break;
                case 0x2C: // inc l
                    Increment(ref Registers.l);
                    break;
                case 0x2D: // dec l
                    Decrement(ref Registers.l);
                    break;
                case 0x2E: // ld l, n
                    LoadImmediate(ref Registers.l);
                    break;
                case 0x2F: // cpl
                    ComplementA();
                    break;
                case 0x30: // jr nc, n
                    JumpRelativeIfNotCarry();
                    break;
                case 0x31: // ld sp, nn
                    LoadImmediateWord(ref Registers.sp);
                    break;
                case 0x32: // ld (hld), a
                    WriteByte(Registers.h, Registers.l, Registers.a);
                    Decrement(ref Registers.h, ref Registers.l);
                    break;
                case 0x33: // inc sp
                    IncrementWord(ref Registers.sp);
                    break;
                case 0x34: // inc (hl)
                    IncrementMemory(Registers.h, Registers.l);
                    break;
                case 0x35: // dec (hl)
                    DecrementMemory(Registers.h, Registers.l);
                    break;
                case 0x36: // ld (hl), n
                    LoadImmediateIntoMemory(Registers.h, Registers.l);
                    break;
                case 0x37: // scf
                    Registers.SetCarryFlag(1);
                    break;
                case 0x38: // jr c, n
                    JumpRelativeIfCarry();
                    break;
                case 0x39: // add hl, sp
                    AddSPToHL();
                    break;
                case 0x3A: // ld a, (hld)
                    ReadByte(ref Registers.a, Registers.h, Registers.l);
                    Decrement(ref Registers.h, ref Registers.l);
                    break;
                case 0x3B: // dec sp
                    DecrementWord(ref Registers.sp);
                    break;
                case 0x3C: // inc a
                    Increment(ref Registers.a);
                    break;
                case 0x3D: // dec a
                    Decrement(ref Registers.a);
                    break;
                case 0x3E: // ld a, n
                    LoadImmediate(ref Registers.a);
                    break;
                case 0x3F: // ccf
                    ComplementCarryFlag();
                    break;
                case 0x40: // ld b, b
                    Load(ref Registers.b, Registers.b);
                    break;
                case 0x41: // ld b, c
                    Load(ref Registers.b, Registers.c);
                    break;
                case 0x42: // ld b, d
                    Load(ref Registers.b, Registers.d);
                    break;
                case 0x43: // ld b, e
                    Load(ref Registers.b, Registers.e);
                    break;
                case 0x44: // ld b, h
                    Load(ref Registers.b, Registers.e);
                    break;
                case 0x45: // ld b, l
                    Load(ref Registers.b, Registers.l);
                    break;
                case 0x46: // ld b, (hl)
                    ReadByte(ref Registers.b, Registers.h, Registers.l);
                    break;
                case 0x47: // ld b, a
                    Load(ref Registers.b, Registers.a);
                    break;
                case 0x48: // ld c, b
                    Load(ref Registers.c, Registers.b);
                    break;
                case 0x49: // ld c, c
                    Load(ref Registers.c, Registers.c);
                    break;
                case 0x4A: // ld c, d
                    Load(ref Registers.c, Registers.d);
                    break;
                case 0x4B: // ld c, e
                    Load(ref Registers.c, Registers.e);
                    break;
                case 0x4C: // ld c, h
                    Load(ref Registers.c, Registers.h);
                    break;
                case 0x4D: // ld c, l
                    Load(ref Registers.c, Registers.l);
                    break;
                case 0x4E: // ld c, (hl)
                    ReadByte(ref Registers.c, Registers.h, Registers.l);
                    break;
                case 0x4F: // ld c, a
                    Load(ref Registers.c, Registers.a);
                    break;
                case 0x50: // ld d, b
                    Load(ref Registers.d, Registers.b);
                    break;
                case 0x51: // ld d, c
                    Load(ref Registers.d, Registers.c);
                    break;
                case 0x52: // ld d, d
                    Load(ref Registers.d, Registers.d);
                    break;
                case 0x53: // ld d, e
                    Load(ref Registers.d, Registers.e);
                    break;
                case 0x54: // ld d, h
                    Load(ref Registers.d, Registers.h);
                    break;
                case 0x55: // ld d, l
                    Load(ref Registers.d, Registers.l);
                    break;
                case 0x56: // ld d, (hl)
                    ReadByte(ref Registers.d, Registers.h, Registers.l);
                    break;
                case 0x57: // ld d, a
                    Load(ref Registers.d, Registers.a);
                    break;
                case 0x58: // ld e, b
                    Load(ref Registers.e, Registers.b);
                    break;
                case 0x59: // ld e, c
                    Load(ref Registers.e, Registers.c);
                    break;
                case 0x5A: // ld e, d
                    Load(ref Registers.e, Registers.d);
                    break;
                case 0x5B: // ld e, e
                    Load(ref Registers.e, Registers.e);
                    break;
                case 0x5C: // ld e, h
                    Load(ref Registers.e, Registers.h);
                    break;
                case 0x5D: // ld e, l
                    Load(ref Registers.e, Registers.l);
                    break;
                case 0x5E: // ld e, (hl)
                    ReadByte(ref Registers.e, Registers.h, Registers.l);
                    break;
                case 0x5F: // ld e, a
                    Load(ref Registers.e, Registers.a);
                    break;
                case 0x60: // ld h, b
                    Load(ref Registers.h, Registers.b);
                    break;
                case 0x61: // ld h, c
                    Load(ref Registers.h, Registers.c);
                    break;
                case 0x62: // ld h, d
                    Load(ref Registers.h, Registers.d);
                    break;
                case 0x63: // ld h, e
                    Load(ref Registers.h, Registers.e);
                    break;
                case 0x64: // ld h, h
                    Load(ref Registers.h, Registers.h);
                    break;
                case 0x65: // ld h, l
                    Load(ref Registers.h, Registers.l);
                    break;
                case 0x66: // ld h, (hl)
                    ReadByte(ref Registers.h, Registers.h, Registers.l);
                    break;
                case 0x67: // ld, h, a
                    Load(ref Registers.h, Registers.a);
                    break;
                case 0x68: // ld l, b
                    Load(ref Registers.l, Registers.b);
                    break;
                case 0x69: // ld l, c
                    Load(ref Registers.l, Registers.c);
                    break;
                case 0x6A: // ld l, d
                    Load(ref Registers.l, Registers.d);
                    break;
                case 0x6B: // ld l, e
                    Load(ref Registers.l, Registers.e);
                    break;
                case 0x6C: // ld l, h
                    Load(ref Registers.l, Registers.h);
                    break;
                case 0x6D: // ld l, l
                    Load(ref Registers.l, Registers.l);
                    break;
                case 0x6E: // ld l, (hl)
                    ReadByte(ref Registers.l, Registers.h, Registers.l);
                    break;
                case 0x6F: // ld l, a
                    Load(ref Registers.l, Registers.a);
                    break;
                case 0x70: // ld (hl), b
                    WriteByte(Registers.h, Registers.l, Registers.b);
                    break;
                case 0x71: // ld (hl), c
                    WriteByte(Registers.h, Registers.l, Registers.c);
                    break;
                case 0x72: // ld (hl), d
                    WriteByte(Registers.h, Registers.l, Registers.d);
                    break;
                case 0x73: // ld (hl), e
                    WriteByte(Registers.h, Registers.l, Registers.e);
                    break;
                case 0x74: // ld (hl), h
                    WriteByte(Registers.h, Registers.l, Registers.h);
                    break;
                case 0x75: // ld (hl), l
                    WriteByte(Registers.h, Registers.l, Registers.l);
                    break;
                case 0x76: // halt
                    Halt();
                    break;
                case 0x77: // ld (hl), a
                    WriteByte(Registers.h, Registers.l, Registers.a);
                    break;
                case 0x78: // ld a, b
                    Load(ref Registers.a, Registers.b);
                    break;
                case 0x79: // ld a, c
                    Load(ref Registers.a, Registers.c);
                    break;
                case 0x7A: // ld a, d
                    Load(ref Registers.a, Registers.d);
                    break;
                case 0x7B: // ld a, e
                    Load(ref Registers.a, Registers.e);
                    break;
                case 0x7C: // ld a, h
                    Load(ref Registers.a, Registers.h);
                    break;
                case 0x7D: // ld a, l
                    Load(ref Registers.a, Registers.l);
                    break;
                case 0x7E: // ld a, (hl)
                    ReadByte(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0x7F: // ld a, a
                    Load(ref Registers.a, Registers.a);
                    break;
                case 0x80: // add a, b
                    Add(ref Registers.a, Registers.b);
                    break;
                case 0x81: // add a, c
                    Add(ref Registers.a, Registers.c);
                    break;
                case 0x82: // add a, d
                    Add(ref Registers.a, Registers.d);
                    break;
                case 0x83: // add a, e
                    Add(ref Registers.a, Registers.e);
                    break;
                case 0x84: // add a, h
                    Add(ref Registers.a, Registers.h);
                    break;
                case 0x85: // add a, l
                    Add(ref Registers.a, Registers.l);
                    break;
                case 0x86: // add a, (hl)
                    Add(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0x87: // add a, a
                    Add(ref Registers.a, Registers.a);
                    break;
                case 0x88: // adc a, b
                    AddWithCarry(ref Registers.a, Registers.b);
                    break;
                case 0x89: // adc a, c
                    AddWithCarry(ref Registers.a, Registers.c);
                    break;
                case 0x8A: // adc a, d
                    AddWithCarry(ref Registers.a, Registers.d);
                    break;
                case 0x8B: // adc a, e
                    AddWithCarry(ref Registers.a, Registers.e);
                    break;
                case 0x8C: // adc a, h
                    AddWithCarry(ref Registers.a, Registers.h);
                    break;
                case 0x8D: // adc a, l
                    AddWithCarry(ref Registers.a, Registers.l);
                    break;
                case 0x8E: // adc a, (hl)
                    AddWithCarry(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0x8F: // adc a, a
                    AddWithCarry(ref Registers.a, Registers.a);
                    break;
                case 0x90: // sub b
                    Sub(ref Registers.a, Registers.b);
                    break;
                case 0x91: // sub c
                    Sub(ref Registers.a, Registers.c);
                    break;
                case 0x92: // sub d
                    Sub(ref Registers.a, Registers.d);
                    break;
                case 0x93: // sub e
                    Sub(ref Registers.a, Registers.e);
                    break;
                case 0x94: // sub h
                    Sub(ref Registers.a, Registers.h);
                    break;
                case 0x95: // sub l
                    Sub(ref Registers.a, Registers.l);
                    break;
                case 0x96: // sub (hl)
                    Sub(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0x97: // sub a
                    Sub(ref Registers.a, Registers.a);
                    break;
                case 0x98: // sbc b
                    SubWithBorrow(ref Registers.b, Registers.b);
                    break;
                case 0x99: // sbc c
                    SubWithBorrow(ref Registers.a, Registers.c);
                    break;
                case 0x9A: // sbc d
                    SubWithBorrow(ref Registers.a, Registers.d);
                    break;
                case 0x9B: // sbc e
                    SubWithBorrow(ref Registers.a, Registers.e);
                    break;
                case 0x9C: // sbc h
                    SubWithBorrow(ref Registers.a, Registers.h);
                    break;
                case 0x9D: // sbc l
                    SubWithBorrow(ref Registers.a, Registers.l);
                    break;
                case 0x9E: // sbc (hl)
                    SubWithBorrow(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0x9F: // sbc a
                    SubWithBorrow(ref Registers.a, Registers.a);
                    break;
                case 0xA0: // and b
                    And(ref Registers.a, Registers.b);
                    break;
                case 0xA1: // and c
                    And(ref Registers.a, Registers.c);
                    break;
                case 0xA2: // and d
                    And(ref Registers.a, Registers.d);
                    break;
                case 0xA3: // and e
                    And(ref Registers.a, Registers.e);
                    break;
                case 0xA4: // and h
                    And(ref Registers.a, Registers.h);
                    break;
                case 0xA5: // and l
                    And(ref Registers.a, Registers.l);
                    break;
                case 0xA6: // and (hl)
                    And(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0xA7: // and a
                    And(ref Registers.a, Registers.a);
                    break;
                case 0xA8: // xor b
                    Xor(ref Registers.a, Registers.b);
                    break;
                case 0xA9: // xor c
                    Xor(ref Registers.a, Registers.c);
                    break;
                case 0xAA: // xor d
                    Xor(ref Registers.a, Registers.d);
                    break;
                case 0xAB: // xor e
                    Xor(ref Registers.a, Registers.e);
                    break;
                case 0xAC: // xor h
                    Xor(ref Registers.a, Registers.h);
                    break;
                case 0xAD: // xor l
                    Xor(ref Registers.a, Registers.l);
                    break;
                case 0xAE: // xor (hl)
                    Xor(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0xAF: // xor a
                    Xor(ref Registers.a, Registers.a);
                    break;
                case 0xB0: // or b
                    Or(ref Registers.a, Registers.b);
                    break;
                case 0xB1: // or c
                    Or(ref Registers.a, Registers.c);
                    break;
                case 0xB2: // or d
                    Or(ref Registers.a, Registers.d);
                    break;
                case 0xB3: // or e
                    Or(ref Registers.a, Registers.e);
                    break;
                case 0xB4: // or h
                    Or(ref Registers.a, Registers.h);
                    break;
                case 0xB5: // or l
                    Or(ref Registers.a, Registers.l);
                    break;
                case 0xB6: // or (hl)
                    Or(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0xB7: // or a
                    Or(ref Registers.a, Registers.a);
                    break;
                case 0xB8: // cp b
                    Compare(ref Registers.a, Registers.b);
                    break;
                case 0xB9: // cp c
                    Compare(ref Registers.a, Registers.c);
                    break;
                case 0xBA: // cp d
                    Compare(ref Registers.a, Registers.d);
                    break;
                case 0xBB: // cp e
                    Compare(ref Registers.a, Registers.e);
                    break;
                case 0xBC: // cp h
                    Compare(ref Registers.a, Registers.h);
                    break;
                case 0xBD: // cp l
                    Compare(ref Registers.a, Registers.l);
                    break;
                case 0xBE: // cp (hl)
                    Compare(ref Registers.a, Registers.h, Registers.l);
                    break;
                case 0xBF: // cp a
                    Compare(ref Registers.a, Registers.a);
                    break;
                case 0xC0: // ret nz
                    ReturnIfNotZero();
                    break;
                case 0xC1: // pop bc
                    Pop(ref Registers.b, ref Registers.c);
                    break;
                case 0xC2: // jp nz, n
                    JumpIfNotZero();
                    break;
                case 0xC3: // jp n
                    Jump();
                    break;
                case 0xC4: // call nz, nn
                    CallIfNotZero();
                    break;
                case 0xC5: // push bc
                    Push(Registers.b, Registers.c);
                    break;
                case 0xC6: // add a, n
                    AddImmediate(ref Registers.a);
                    break;
                case 0xC7: // rst 0h
                    Restart(0x0);
                    break;
                case 0xC8: // ret z
                    ReturnIfZero();
                    break;
                case 0xC9: //  ret
                    Return();
                    break;
                case 0xCA: // jp z, n
                    JumpIfZero();
                    break;
                case 0xCB:
                    HandleExtendedOpcode();
                    break;
                case 0xCC: // call z, nn
                    CallIfZero();
                    break;
                case 0xCD: // call n
                    Call();
                    break;
                case 0xCE: // adc a, n
                    AddImmediateWithCarry(ref Registers.a);
                    break;
                case 0xCF: // rst 8h
                    Restart(0x0008);
                    break;
                case 0xD0: // ret nc
                    ReturnIfNotCarry();
                    break;
                case 0xD1: // pop de
                    Pop(ref Registers.d, ref Registers.e);
                    break;
                case 0xD2: // jp nc, n
                    JumpIfNotCarry();
                    break;
                case 0xD4: // call nc, nn
                    CallIfNotCarry();
                    break;
                case 0xD5: // push de
                    Push(Registers.d, Registers.e);
                    break;
                case 0xD6: // sub n
                    SubImmediate(ref Registers.a);
                    break;
                case 0xD7: // rst 10h
                    Restart(0x0010);
                    break;
                case 0xD8: // ref c
                    ReturnIfCarry();
                    break;
                case 0xD9: // reti
                    ReturnFromInterrupt();
                    break;
                case 0xDA: // jp c, n
                    JumpIfCarry();
                    break;
                case 0xDC: // call c, nn
                    CallIfCarry();
                    break;
                case 0xDE: // sbc a, n
                    SubImmediateWithBorrow(ref Registers.a);
                    break;
                case 0xDF: // rst 18h
                    Restart(0x0018);
                    break;
                case 0xE0: // ld (FF00 + byte), a
                    SaveTo(Registers.a, ROMdata.ReadByte());
                    break;
                case 0xE1: // pop hl
                    Pop(ref Registers.h, ref Registers.l);
                    break;
                case 0xE2: // ld (FF00 + c), a
                    SaveTo(Registers.a, Registers.c);
                    break;
                case 0xE5: // push hl
                    Push(Registers.h, Registers.l);
                    break;
                case 0xE6: // and n
                    AndImmediate();
                    break;
                case 0xE7: // rst 20h
                    Restart(0x0020);
                    break;
                case 0xE8: // add sp, offset
                    OffsetStackPointer();
                    break;
                case 0xE9: // jp (hl)
                    Jump(Registers.h, Registers.l);
                    break;
                case 0xEA: // ld (word), a
                    Save(Registers.a);
                    break;
                case 0xEE: // xor n
                    XorImmediate();
                    break;
                case 0xEF: // rst 28h
                    Restart(0x0028);
                    break;
                case 0xF0: // ld a, (FF00 + n)
                    LoadAFromImmediate();
                    break;
                case 0xF1: // pop af
                    Pop(ref Registers.a, ref Registers.f);
                    break;
                case 0xF2: // ld a, (FF00 + c)
                    LoadAFromC();
                    break;
                case 0xF3: // di
                    interruptsEnabled = false;
                    break;
                case 0xF5: // push af
                    Push(Registers.a, Registers.f);
                    break;
                case 0xF6: // or n
                    OrImmediate();
                    break;
                case 0xF7: // rst 30h
                    Restart(0x0030);
                    break;
                case 0xF8: // ld hl, sp + dd
                    LoadHLWithSPPlusImmediate();
                    break;
                case 0xF9: // ld sp, hl
                    LoadSPWithHL();
                    break;
                case 0xFA: // ld a, (nn)
                    LoadImmediateFromAddress(ref Registers.a);
                    break;
                case 0xFB: // ei
                    interruptsEnabled = true;
                    break;
                case 0xFE: // cp n
                    CompareImmediate();
                    break;
                case 0xFF: // rst 38h
                    Restart(0x0038);
                    break;
                default:
                    throw new Exception("Unknown opcode " + opcode + "\r\nAddress: " + Registers.pc);
            }
        }

        /* HandleExtendedOpcode()
         * Description: Handles an extended opcode
         */
        public void HandleExtendedOpcode()
        {
            byte opcode = ROMdata.ReadByte();

            Registers.pc = ROMdata.Position();

            switch (opcode)
            {
                case 0x00: // rlc b
                    RotateLeft(ref Registers.b);
                    break;
                case 0x01: // rlc c
                    RotateLeft(ref Registers.c);
                    break;
                case 0x02: // rlc d
                    RotateLeft(ref Registers.d);
                    break;
                case 0x03: // rlc e
                    RotateLeft(ref Registers.e);
                    break;
                case 0x04: // rlc h
                    RotateLeft(ref Registers.h);
                    break;
                case 0x05: // rlc l
                    RotateLeft(ref Registers.l);
                    break;
                case 0x06: // rlc (hl)
                    RotateLeft(Registers.h, Registers.l);
                    break;
                case 0x07: // rlc a
                    RotateLeft(ref Registers.a);
                    break;
                case 0x08: // rrc b
                    RotateRight(ref Registers.b);
                    break;
                case 0x09: // rrc c
                    RotateRight(ref Registers.c);
                    break;
                case 0x0A: // rrc d
                    RotateRight(ref Registers.d);
                    break;
                case 0x0B: // rrc e
                    RotateRight(ref Registers.e);
                    break;
                case 0x0C: // rrc h
                    RotateRight(ref Registers.h);
                    break;
                case 0x0D: // rrc l
                    RotateRight(ref Registers.l);
                    break;
                case 0x0E: // rrc (hl)
                    RotateRight(Registers.h, Registers.l);
                    break;
                case 0x0F: // rrc a
                    RotateRight(ref Registers.a);
                    break;
                case 0x10: // rl b
                    RotateLeftThroughCarry(ref Registers.b);
                    break;
                case 0x11: // rl c
                    RotateLeftThroughCarry(ref Registers.c);
                    break;
                case 0x12: // rl d
                    RotateLeftThroughCarry(ref Registers.d);
                    break;
                case 0x13: // rl e
                    RotateLeftThroughCarry(ref Registers.e);
                    break;
                case 0x14: // rl h
                    RotateLeftThroughCarry(ref Registers.h);
                    break;
                case 0x15: // rl l
                    RotateLeftThroughCarry(ref Registers.l);
                    break;
                case 0x16: // rl (hl)
                    RotateLeftThroughCarry(Registers.h, Registers.l);
                    break;
                case 0x17: // rl a
                    RotateLeftThroughCarry(ref Registers.a);
                    break;
                case 0x18: // rr b
                    RotateRightThroughCarry(ref Registers.b);
                    break;
                case 0x19: // rr c
                    RotateRightThroughCarry(ref Registers.c);
                    break;
                case 0x1A: // rr d
                    RotateRightThroughCarry(ref Registers.d);
                    break;
                case 0x1B: // rr e
                    RotateRightThroughCarry(ref Registers.e);
                    break;
                case 0x1C: // rr h
                    RotateRightThroughCarry(ref Registers.h);
                    break;
                case 0x1D: // rr l
                    RotateRightThroughCarry(ref Registers.l);
                    break;
                case 0x1E: // rr (hl)
                    RotateRightThroughCarry(Registers.h, Registers.l);
                    break;
                case 0x1F: // rr a
                    RotateRightThroughCarry(ref Registers.a);
                    break;
                case 0x20: // sla b
                    ShiftLeft(ref Registers.b);
                    break;
                case 0x21: // sla c
                    ShiftLeft(ref Registers.c);
                    break;
                case 0x22: // sla d
                    ShiftLeft(ref Registers.d);
                    break;
                case 0x23: // sla e
                    ShiftLeft(ref Registers.e);
                    break;
                case 0x24: // sla h
                    ShiftLeft(ref Registers.h);
                    break;
                case 0x25: // sla l
                    ShiftLeft(ref Registers.l);
                    break;
                case 0x26: // sla (hl)
                    ShiftLeft(Registers.h, Registers.l);
                    break;
                case 0x27: // sla a
                    ShiftLeft(ref Registers.a);
                    break;
                case 0x28: // sra b
                    SignedShiftRight(ref Registers.b);
                    break;
                case 0x29: // sra c
                    SignedShiftRight(ref Registers.c);
                    break;
                case 0x2A: // sra d
                    SignedShiftRight(ref Registers.d);
                    break;
                case 0x2B: // sra e
                    SignedShiftRight(ref Registers.e);
                    break;
                case 0x2C: // sra h
                    SignedShiftRight(ref Registers.h);
                    break;
                case 0x2D: // sra l
                    SignedShiftRight(ref Registers.l);
                    break;
                case 0x2E: // sra (hl)
                    SignedShiftRight(Registers.h, Registers.l);
                    break;
                case 0x2F: // sra a
                    SignedShiftRight(ref Registers.a);
                    break;
                case 0x30: // swap b
                    Swap(ref Registers.b);
                    break;
                case 0x31: // swap c
                    Swap(ref Registers.c);
                    break;
                case 0x32: // swap d
                    Swap(ref Registers.d);
                    break;
                case 0x33: // swap e
                    Swap(ref Registers.e);
                    break;
                case 0x34: // swap h
                    Swap(ref Registers.h);
                    break;
                case 0x35: // swap l
                    Swap(ref Registers.l);
                    break;
                case 0x36: // swap (hl)
                    Swap(Registers.h, Registers.l);
                    break;
                case 0x37: // swap a
                    Swap(ref Registers.a);
                    break;
                case 0x38: // srl b
                    UnsignedShiftRight(ref Registers.b);
                    break;
                case 0x39: // srl c
                    UnsignedShiftRight(ref Registers.c);
                    break;
                case 0x3A: // srl d
                    UnsignedShiftRight(ref Registers.d);
                    break;
                case 0x3B: // srl e
                    UnsignedShiftRight(ref Registers.e);
                    break;
                case 0x3C: // srl h
                    UnsignedShiftRight(ref Registers.h);
                    break;
                case 0x3D: // srl l
                    UnsignedShiftRight(ref Registers.l);
                    break;
                case 0x3E: // srl (hl)
                    UnsignedShiftRight(Registers.h, Registers.l);
                    break;
                case 0x3F: // srl a
                    UnsignedShiftRight(ref Registers.a);
                    break;
                case 0x40: // bit 0, b
                    TestBit(0, Registers.b);
                    break;
                case 0x41: // bit 0, c
                    TestBit(0, Registers.c);
                    break;
                case 0x42: // bit 0, d
                    TestBit(0, Registers.d);
                    break;
                case 0x43: // bit 0, e
                    TestBit(0, Registers.e);
                    break;
                case 0x44: // bit 0, h
                    TestBit(0, Registers.h);
                    break;
                case 0x45: // bit 0, l
                    TestBit(0, Registers.l);
                    break;
                case 0x46: // bit 0, (hl)
                    TestBit(0, Registers.h, Registers.l);
                    break;
                case 0x47: // bit 0, a
                    TestBit(0, Registers.a);
                    break;
                case 0x48: // bit 1, b
                    TestBit(1, Registers.b);
                    break;
                case 0x49: // bit 1, c
                    TestBit(1, Registers.c);
                    break;
                case 0x4A: // bit 1, d
                    TestBit(1, Registers.d);
                    break;
                case 0x4B: // bit 1, e
                    TestBit(1, Registers.e);
                    break;
                case 0x4C: // bit 1, h
                    TestBit(1, Registers.h);
                    break;
                case 0x4D: // bit 1, l
                    TestBit(1, Registers.l);
                    break;
                case 0x4E: // bit 1, (hl)
                    TestBit(1, Registers.h, Registers.l);
                    break;
                case 0x4F: // bit 1, a
                    TestBit(1, Registers.a);
                    break;
                case 0x50: // bit 2, b
                    TestBit(2, Registers.b);
                    break;
                case 0x51: // bit 2, c
                    TestBit(2, Registers.c);
                    break;
                case 0x52: // bit 2, d
                    TestBit(2, Registers.d);
                    break;
                case 0x53: // bit 2, e
                    TestBit(2, Registers.e);
                    break;
                case 0x54: // bit 2, h
                    TestBit(2, Registers.h);
                    break;
                case 0x55: // bit 2, l
                    TestBit(2, Registers.l);
                    break;
                case 0x56: // bit 2, (hl)
                    TestBit(2, Registers.h, Registers.l);
                    break;
                case 0x57: // bit 2, a
                    TestBit(2, Registers.a);
                    break;
                case 0x58: // bit 3, b
                    TestBit(2, Registers.b);
                    break;
                case 0x59: // bit 3, c
                    TestBit(3, Registers.c);
                    break;
                case 0x5A: // bit 3, d
                    TestBit(3, Registers.d);
                    break;
                case 0x5B: // bit 3, e
                    TestBit(3, Registers.e);
                    break;
                case 0x5C: // bit 3, h
                    TestBit(3, Registers.h);
                    break;
                case 0x5D: // bit 3, l
                    TestBit(3, Registers.l);
                    break;
                case 0x5E: // bit 3, (hl)
                    TestBit(3, Registers.h, Registers.l);
                    break;
                case 0x5F: // bit 3, a
                    TestBit(3, Registers.a);
                    break;
                case 0x60: // bit 4, b
                    TestBit(4, Registers.b);
                    break;
                case 0x61: // bit 4, c
                    TestBit(4, Registers.c);
                    break;
                case 0x62: // bit 4, d
                    TestBit(4, Registers.d);
                    break;
                case 0x63: // bit 4, e
                    TestBit(4, Registers.e);
                    break;
                case 0x65: // bit 4, l
                    TestBit(4, Registers.l);
                    break;
                case 0x66: // bit 4, (hl)
                    TestBit(4, Registers.h, Registers.l);
                    break;
                case 0x67: // bit 4, a
                    TestBit(4, Registers.a);
                    break;
                case 0x68: // bit 5, b
                    TestBit(5, Registers.b);
                    break;
                case 0x69: // bit 5, c
                    TestBit(5, Registers.c);
                    break;
                case 0x6A: // bit 5, d
                    TestBit(5, Registers.d);
                    break;
                case 0x6B: // bit 5, e
                    TestBit(5, Registers.e);
                    break;
                case 0x6C: // bit 5, h
                    TestBit(5, Registers.h);
                    break;
                case 0x6D: // bit 5, l
                    TestBit(5, Registers.l);
                    break;
                case 0x6E: // bit 5, (hl)
                    TestBit(5, Registers.h, Registers.l);
                    break;
                case 0x6F: // bit 5, a
                    TestBit(5, Registers.a);
                    break;
                case 0x70: // bit 6, b
                    TestBit(6, Registers.b);
                    break;
                case 0x71: // bit 6, c
                    TestBit(6, Registers.c);
                    break;
                case 0x72: // bit 6, d
                    TestBit(6, Registers.d);
                    break;
                case 0x73: // bit 6, e
                    TestBit(6, Registers.e);
                    break;
                case 0x74: // bit 6, h
                    TestBit(6, Registers.h);
                    break;
                case 0x75: // bit 6, l
                    TestBit(6, Registers.l);
                    break;
                case 0x76: // bit 6, (hl)
                    TestBit(6, Registers.h, Registers.l);
                    break;
                case 0x77: // bit 6, a
                    TestBit(6, Registers.a);
                    break;
                case 0x78: // bit 7, b
                    TestBit(7, Registers.b);
                    break;
                case 0x79: // bit 7, c
                    TestBit(7, Registers.c);
                    break;
                case 0x7A: // bit 7, d
                    TestBit(7, Registers.d);
                    break;
                case 0x7B: // bit 7, e
                    TestBit(7, Registers.e);
                    break;
                case 0x7C: // bit 7, h
                    TestBit(7, Registers.h);
                    break;
                case 0x7D: // bit 7, l
                    TestBit(7, Registers.l);
                    break;
                case 0x7E: // bit 7, (hl)
                    TestBit(7, Registers.h, Registers.l);
                    break;
                case 0x7F: // bit 7, a
                    TestBit(7, Registers.a);
                    break;
                case 0x80: // res 0, b
                    ResetBit(0, ref Registers.b);
                    break;
                case 0x81: // res 0, c
                    ResetBit(0, ref Registers.c);
                    break;
                case 0x82: // res 0, d
                    ResetBit(0, ref Registers.d);
                    break;
                case 0x83: // res 0, e
                    ResetBit(0, ref Registers.e);
                    break;
                case 0x84: // res 0, h
                    ResetBit(0, ref Registers.h);
                    break;
                case 0x85: // res 0, l
                    ResetBit(0, ref Registers.l);
                    break;
                case 0x86: // res 0, (hl)
                    ResetBit(0, Registers.h, Registers.l);
                    break;
                case 0x87: // res 0, a
                    ResetBit(0, ref Registers.a);
                    break;
                case 0x88: // res 1, b
                    ResetBit(1, ref Registers.b);
                    break;
                case 0x89: // res 1, c
                    ResetBit(1, ref Registers.c);
                    break;
                case 0x8A: // res 1, d
                    ResetBit(1, ref Registers.d);
                    break;
                case 0x8B: // res 1, e
                    ResetBit(1, ref Registers.e);
                    break;
                case 0x8C: // res 1, h
                    ResetBit(1, ref Registers.h);
                    break;
                case 0x8D: // res 1, l
                    ResetBit(1, ref Registers.l);
                    break;
                case 0x8E: // res 1, (hl)
                    ResetBit(1, Registers.h, Registers.l);
                    break;
                case 0x8F: // res 1, a
                    ResetBit(1, ref Registers.a);
                    break;
                case 0x90: // res 2, b
                    ResetBit(2, ref Registers.b);
                    break;
                case 0x91: // res 2, c
                    ResetBit(2, ref Registers.c);
                    break;
                case 0x92: // res 2, d
                    ResetBit(2, ref Registers.d);
                    break;
                case 0x93: // res 2, e
                    ResetBit(2, ref Registers.e);
                    break;
                case 0x94: // res 2, h
                    ResetBit(2, ref Registers.h);
                    break;
                case 0x95: // res 2, l
                    ResetBit(2, ref Registers.l);
                    break;
                case 0x96: // res 2, (hl)
                    ResetBit(2, Registers.h, Registers.l);
                    break;
                case 0x97: // res 2, a
                    ResetBit(2, ref Registers.a);
                    break;
                case 0x98: // res 3, b
                    ResetBit(3, ref Registers.b);
                    break;
                case 0x99: // res 3, c
                    ResetBit(3, ref Registers.c);
                    break;
                case 0x9A: // res 3, d
                    ResetBit(3, ref Registers.d);
                    break;
                case 0x9B: // res 3, e
                    ResetBit(3, ref Registers.e);
                    break;
                case 0x9C: // res 3, h
                    ResetBit(3, ref Registers.h);
                    break;
                case 0x9D: // res 3, l
                    ResetBit(3, ref Registers.l);
                    break;
                case 0x9E: // res 3, (hl)
                    ResetBit(3, Registers.h, Registers.l);
                    break;
                case 0x9F: // res 3, a
                    ResetBit(3, ref Registers.a);
                    break;
                case 0xA0: // res 4, b
                    ResetBit(4, ref Registers.b);
                    break;
                case 0xA1: // res 4, c
                    ResetBit(4, ref Registers.c);
                    break;
                case 0xA2: // res 4, d
                    ResetBit(4, ref Registers.d);
                    break;
                case 0xA3: // res 4, e
                    ResetBit(4, ref Registers.e);
                    break;
                case 0xA4: // res 4, h
                    ResetBit(4, ref Registers.h);
                    break;
                case 0xA5: // res 4, l
                    ResetBit(4, ref Registers.l);
                    break;
                case 0xA6: // res 4, (hl)
                    ResetBit(4, Registers.h, Registers.l);
                    break;
                case 0xA7: // res 4, a
                    ResetBit(4, ref Registers.a);
                    break;
                case 0xA8: // res 5, b
                    ResetBit(5, ref Registers.b);
                    break;
                case 0xA9: // res 5, c
                    ResetBit(5, ref Registers.c);
                    break;
                case 0xAA: // res 5, d
                    ResetBit(5, ref Registers.d);
                    break;
                case 0xAB: // res 5, e
                    ResetBit(5, ref Registers.e);
                    break;
                case 0xAC: // res 5, h
                    ResetBit(5, ref Registers.h);
                    break;
                case 0xAD: // res 5, l
                    ResetBit(5, ref Registers.l);
                    break;
                case 0xAE: // res 5, (hl)
                    ResetBit(5, Registers.h, Registers.l);
                    break;
                case 0xAF: // res 5, a
                    ResetBit(5, ref Registers.a);
                    break;
                case 0xB0: // res 6, b
                    ResetBit(6, ref Registers.b);
                    break;
                case 0xB1: // res 6, c
                    ResetBit(6, ref Registers.c);
                    break;
                case 0xB2: // res 6, d
                    ResetBit(6, ref Registers.d);
                    break;
                case 0xB3: // res 6, e
                    ResetBit(6, ref Registers.e);
                    break;
                case 0xB4: // res 6, h
                    ResetBit(6, ref Registers.h);
                    break;
                case 0xB5: // res 6, l
                    ResetBit(6, ref Registers.l);
                    break;
                case 0xB6: // res 6, (hl)
                    ResetBit(6, Registers.h, Registers.l);
                    break;
                case 0xB7: // res 6, a
                    ResetBit(6, ref Registers.a);
                    break;
                case 0xB8: // res 7, b
                    ResetBit(7, ref Registers.b);
                    break;
                case 0xB9: // res 7, c
                    ResetBit(7, ref Registers.c);
                    break;
                case 0xBA: // res 7, d
                    ResetBit(7, ref Registers.d);
                    break;
                case 0xBB: // res 7, e
                    ResetBit(7, ref Registers.e);
                    break;
                case 0xBC: // res 7, h
                    ResetBit(7, ref Registers.h);
                    break;
                case 0xBD: // res 7, l
                    ResetBit(7, ref Registers.l);
                    break;
                case 0xBE: // res 7, (hl)
                    ResetBit(7, Registers.h, Registers.l);
                    break;
                case 0xBF: // res 7, a
                    ResetBit(7, ref Registers.a);
                    break;
                case 0xC0: // set 0, b
                    SetBit(0, ref Registers.b);
                    break;
                case 0xC1: // set 0, c
                    SetBit(0, ref Registers.c);
                    break;
                case 0xC2: // set 0, d
                    SetBit(0, ref Registers.d);
                    break;
                case 0xC3: // set 0, e
                    SetBit(0, ref Registers.e);
                    break;
                case 0xC4: // set 0, h
                    SetBit(0, ref Registers.h);
                    break;
                case 0xC5: // set 0, l
                    SetBit(0, ref Registers.l);
                    break;
                case 0xC6: // set 0, (hl)
                    SetBit(0, Registers.h, Registers.l);
                    break;
                case 0xC7: // set 0,A
                    SetBit(0, ref Registers.a);
                    break;
                case 0xC8: // set 1, b
                    SetBit(1, ref Registers.b);
                    break;
                case 0xC9: // set 1, c
                    SetBit(1, ref Registers.c);
                    break;
                case 0xCA: // set 1, d
                    SetBit(1, ref Registers.d);
                    break;
                case 0xCB: // set 1, e
                    SetBit(1, ref Registers.e);
                    break;
                case 0xCC: // set 1, h
                    SetBit(1, ref Registers.h);
                    break;
                case 0xCD: // set 1, l
                    SetBit(1, ref Registers.l);
                    break;
                case 0xCE: // set 1, (hl)
                    SetBit(1, Registers.h, Registers.l);
                    break;
                case 0xCF: // set 1,A
                    SetBit(1, ref Registers.a);
                    break;
                case 0xD0: // set 2, b
                    SetBit(2, ref Registers.b);
                    break;
                case 0xD1: // set 2, c
                    SetBit(2, ref Registers.c);
                    break;
                case 0xD2: // set 2, d
                    SetBit(2, ref Registers.d);
                    break;
                case 0xD3: // set 2, e
                    SetBit(2, ref Registers.e);
                    break;
                case 0xD4: // set 2, h
                    SetBit(2, ref Registers.h);
                    break;
                case 0xD5: // set 2, l
                    SetBit(2, ref Registers.l);
                    break;
                case 0xD6: // set 2, (hl)
                    SetBit(2, Registers.h, Registers.l);
                    break;
                case 0xD7: // set 2,A
                    SetBit(2, ref Registers.a);
                    break;
                case 0xD8: // set 3, b
                    SetBit(3, ref Registers.b);
                    break;
                case 0xD9: // set 3, c
                    SetBit(3, ref Registers.c);
                    break;
                case 0xDA: // set 3, d
                    SetBit(3, ref Registers.d);
                    break;
                case 0xDB: // set 3, e
                    SetBit(3, ref Registers.e);
                    break;
                case 0xDC: // set 3, h
                    SetBit(3, ref Registers.h);
                    break;
                case 0xDD: // set 3, l
                    SetBit(3, ref Registers.l);
                    break;
                case 0xDE: // set 3, (hl)
                    SetBit(3, Registers.h, Registers.l);
                    break;
                case 0xDF: // set 3,A
                    SetBit(3, ref Registers.a);
                    break;
                case 0xE0: // set 4, b
                    SetBit(4, ref Registers.b);
                    break;
                case 0xE1: // set 4, c
                    SetBit(4, ref Registers.c);
                    break;
                case 0xE2: // set 4, d
                    SetBit(4, ref Registers.d);
                    break;
                case 0xE3: // set 4, e
                    SetBit(4, ref Registers.e);
                    break;
                case 0xE4: // set 4, h
                    SetBit(4, ref Registers.h);
                    break;
                case 0xE5: // set 4, l
                    SetBit(4, ref Registers.l);
                    break;
                case 0xE6: // set 4, (hl)
                    SetBit(4, Registers.h, Registers.l);
                    break;
                case 0xE7: // set 4,A
                    SetBit(4, ref Registers.a);
                    break;
                case 0xE8: // set 5, b
                    SetBit(5, ref Registers.b);
                    break;
                case 0xE9: // set 5, c
                    SetBit(5, ref Registers.c);
                    break;
                case 0xEA: // set 5, d
                    SetBit(5, ref Registers.d);
                    break;
                case 0xEB: // set 5, e
                    SetBit(5, ref Registers.e);
                    break;
                case 0xEC: // set 5, h
                    SetBit(5, ref Registers.h);
                    break;
                case 0xED: // set 5, l
                    SetBit(5, ref Registers.l);
                    break;
                case 0xEE: // set 5, (hl)
                    SetBit(5, Registers.h, Registers.l);
                    break;
                case 0xEF: // set 5,A
                    SetBit(5, ref Registers.a);
                    break;
                case 0xF0: // set 6, b
                    SetBit(6, ref Registers.b);
                    break;
                case 0xF1: // set 6, c
                    SetBit(6, ref Registers.c);
                    break;
                case 0xF2: // set 6, d
                    SetBit(6, ref Registers.d);
                    break;
                case 0xF3: // set 6, e
                    SetBit(6, ref Registers.e);
                    break;
                case 0xF4: // set 6, h
                    SetBit(6, ref Registers.h);
                    break;
                case 0xF5: // set 6, l
                    SetBit(6, ref Registers.l);
                    break;
                case 0xF6: // set 6, (hl)
                    SetBit(6, Registers.h, Registers.l);
                    break;
                case 0xF7: // set 6,A
                    SetBit(6, ref Registers.a);
                    break;
                case 0xF8: // set 7, b
                    SetBit(7, ref Registers.b);
                    break;
                case 0xF9: // set 7, c
                    SetBit(7, ref Registers.c);
                    break;
                case 0xFA: // set 7, d
                    SetBit(7, ref Registers.d);
                    break;
                case 0xFB: // set 7, e
                    SetBit(7, ref Registers.e);
                    break;
                case 0xFC: // set 7, h
                    SetBit(7, ref Registers.h);
                    break;
                case 0xFD: // set 7, l
                    SetBit(7, ref Registers.l);
                    break;
                case 0xFE: // set 7, (hl)
                    SetBit(7, Registers.h, Registers.l);
                    break;
                case 0xFF: // set 7,A
                    SetBit(7, ref Registers.a);
                    break;
            }
        }

        /* LoadImmediate
         * Description: Sets a 16bits register
         */
        public void LoadImmediate(ref byte h, ref byte l)
        {
            h = ROMdata.ReadByte();
            l = ROMdata.ReadByte();
            ticks += 8;
        }

        /* LoadImmediate
         * Description: Sets an 8bits register
         */
        public void LoadImmediate(ref byte r)
        {
            r = ROMdata.ReadByte();
            ticks += 8;
        }

        public void LoadImmediateFromAddress(ref byte r)
        {
            ushort addr = ROMdata.ReadWord();
            r = Memory[addr];
            ticks += 16;
        }

        public void LoadSPWithHL()
        {
            Registers.sp = (ushort)((Registers.h << 8) | Registers.l);
            ticks += 8;
        }

        public void LoadHLWithSPPlusImmediate()
        {
            ushort offset = ROMdata.ReadByte();

            if (offset > 0x7F)
            {
                offset -= 256;
            }

            offset += Registers.sp;

            Registers.h = (byte)(offset >> 8);
            Registers.l = (byte)(offset);

            ticks += 12;
        }

        public void LoadAFromC()
        {
            Registers.a = Memory[0xFF00 | Registers.c];
            ticks += 8;
        }

        public void LoadAFromImmediate()
        {
            byte addr = ROMdata.ReadByte();

            Registers.a = Memory[0xFF00 | addr];

            ticks += 12;
        }

        public void Save(byte r)
        {
            ushort addr = ROMdata.ReadWord();

            Memory[addr] = r;

            ticks += 16;
        }

        public void SaveTo(byte r, byte c)
        {
            Memory[0xFF00 | c] = r;

            ticks += 8;
        }

        /* WriteByte
         * Description: Sets a memory address' value
         */
        public void WriteByte(byte high, byte low, byte value)
        {
            ushort addr = (ushort)((high << 8) | low);

            Memory[addr] = (byte)(value & 0xFF);
            ticks += 8;
        }

        public void WriteByte(ushort addr, byte value)
        {
            Memory[addr] = value;
            ticks += 7;
        }

        /* Increment
         * Description: Increments a 16bits register
         */
        public void Increment(ref byte high, ref byte low)
        {
            if (low == 0xFF)
            {
                high = (byte)(0xFF & (high + 1));
                low = 0;
            }
            else
            {
                low++;
            }

            ticks += 4;
        }

        /* Increment
         * Description: Increments an 8bits register
         */
        public void Increment(ref byte r)
        {
            // There should be a better way to do this...
            // But by the moment it should do the trick...
            if ((r & 0x0F) == 0x0F)
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            // Real increment here
            r++;

            if (r == 0)
                Registers.SetZeroFlag(1);
            else
                Registers.SetZeroFlag(0);

            Registers.SetAddSubFlag(0);

            ticks += 4;
        }

        /* Decrement
         * Description: Decrements an 8bits register
         */
        public void Decrement(ref byte r)
        {
            if ((r & 0x0F) == 0x00)
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            r--;

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            Registers.SetAddSubFlag(1);

            ticks += 4;
        }

        /* RotateLeft()
         * Description: Normal shift operation on a register
         */
        public void RotateFastLeft(ref byte r)
        {
            RotateLeft(ref r, true);
        }

        public void RotateLeft(ref byte r)
        {
            RotateLeft(ref r, false);
        }

        public void RotateLeft(ref byte r, bool fast)
        {
            byte highBit = (byte)(r >> 7);

            if (highBit == 1)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)((r << 1) | highBit);
            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            if(fast == true)
            {
                ticks += 4;
            }
            else
            {
                ticks += 8;
            }
        }

        /* RotateLeft()
         * Description: Normal rotate operation on a memory address
         */
        public void RotateLeft(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8 ) | l);
            RotateLeft(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update
            ticks += 8;
        }

        /* WriteWordToImmediateAddress
         * Description: Sets a memory adress' value
         */
        public void WriteWordToImmediateAddress(ushort value)
        {
            ushort addr = ROMdata.ReadWord();

            Memory[addr] = (byte)(value & 0xFF);
            Memory[addr++] = (byte)(value >> 8);

            ticks += 20;
        }

        /* Add
         * Description: Performs an add to high and low from h and l
         */
        public void Add(ref byte high, ref byte low, byte h, byte l)
        {
            byte before_high = high;
            byte before_low = low;

            low += l;

            // We mustn't reset the low value
            // As this would be a common situation
            // As, for example, we can add 1 to 0x00FF
            // So this is just for knowing how many
            // We need to add to high byte
            if (before_low >= low)
            {
                // HalfCarry happened...
                Registers.SetHalfCarryFlag(1);
                high += (byte)(1 + h);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
                high += h;
            }

            if (before_high >= high)
            {
                // Carry happened...
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            Registers.SetAddSubFlag(0);

            ticks += 11;
        }

        public void OffsetStackPointer()
        {
            ushort value = ROMdata.ReadByte();

            if (value > 0x7F)
            {
                value -= 256;
            }

            Registers.sp += value;

            ticks += 16;
        }

        public void AddImmediateWithCarry(ref byte r)
        {
            byte val = ROMdata.ReadByte();

            Add(ref r, val);

            ticks += 4;
        }

        /* ReadByte
         * Description: Reads a byte from memory address
         */
        public void ReadByte(ref byte r, byte high, byte low)
        {
            ushort addr = (ushort)((high << 8) | low);

            r = Memory[addr];

            ticks += 8;
        }

        /* Decrement
         * Description: Decrements a 16bits register
         */
        public void Decrement(ref byte high, ref byte low)
        {
            if (low == 0)
            {
                high -= 1;
                low = 0xFF;
            }
            else
            {
                low--;
            }

            ticks += 6;
        }

        /* RotateRight()
         * Description: Normal shift operation on a register
         */
        public void RotateFastRight(ref byte r)
        {
            RotateRight(ref r, true);
        }

        public void RotateRight(ref byte r)
        {
            RotateRight(ref r, false);
        }

        public void RotateRight(ref byte r, bool fast)
        {
            byte lowBit = (byte)(r & 0x01);

            if (lowBit == 1)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)((r >> 1) | (lowBit << 7));

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            if (fast == true)
            {
                ticks += 4;
            }
            else
            {
                ticks += 8;
            }
        }

        public void RotateRight(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            RotateRight(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 8;
        }

        /* RotateLeftThroughCarry
         * Description: Adds the carry flag to a register shifting it to left
         */
        public void RotateFastLeftThroughCarry(ref byte r)
        {
            RotateLeftThroughCarry(ref r, true);
        }

        public void RotateLeftThroughCarry(ref byte r)
        {
            RotateLeftThroughCarry(ref r, false);
        }

        public void RotateLeftThroughCarry(ref byte r, bool fast)
        {
            byte highBit = Registers.GetCarryFlag();

            if (r > 0x7F)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)((r << 1) | highBit);

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            if (fast == true)
            {
                ticks += 4;
            }
            else
            {
                ticks += 8;
            }
        }

        public void RotateLeftThroughCarry(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            RotateLeftThroughCarry(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 8;
        }

        /* JumpRelative()
         * Description: Just do a relative jump on the catridge code
         */
        public void JumpRelative()
        {
            short relativeAddress = 0;
            relativeAddress = ROMdata.ReadByte();

            if (relativeAddress > 0x7F)
                relativeAddress -= 256;
            
            ROMdata.SetRelativePosition(relativeAddress);

            ticks += 12;
        }

        /* RotateRightThroughCarry()
         * Description: Adds the carry flag to a register shifting it to right
         */
        public void RotateFastRightThroughCarry(ref byte r)
        {
            RotateRightThroughCarry(ref r, true);
        }

        public void RotateRightThroughCarry(ref byte r)
        {
            RotateRightThroughCarry(ref r, false);
        }

        public void RotateRightThroughCarry(ref byte r, bool fast)
        {
            byte highBit = Registers.GetCarryFlag();

            if ((r & 0x01) == 0x01)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)(highBit | (r >> 1));

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            if (fast == true)
            {
                ticks += 4;
            }
            else
            {
                ticks += 8;
            }
        }

        public void RotateRightThroughCarry(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            RotateRightThroughCarry(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* JumpRelativeIfNotZero()
         * Description: Performs a relative jump if not zero
         */
        public void JumpRelativeIfNotZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                ROMdata.ReadByte();
                ticks += 7;
            }
            else
            {
                JumpRelative();
            }
        }

        /* DecimallyAdjustA()
         * Just adjust the a register
         */
        public void DecimallyAdjustA()
        {
            byte highNibble = (byte)(Registers.a >> 4);
            byte lowNibble = (byte)(Registers.a & 0x0F);
            bool fc = true;

            if (Registers.GetAddSubFlag() == 1)
            {
                if (Registers.GetCarryFlag() == 1)
                {
                    if (Registers.GetHalfCarryFlag() == 1)
                    {
                        Registers.a += 0x9A;
                    }
                    else
                    {
                        Registers.a += 0x0A;
                    }
                }
                else
                {
                    fc = false;
                    if (Registers.GetHalfCarryFlag() == 1)
                    {
                        Registers.a += 0xFA;
                    }
                    else
                    {
                        Registers.a += 0x00;
                    }
                }
            }
            else if (Registers.GetCarryFlag() == 1)
            {
                if ((Registers.GetHalfCarryFlag() == 1) || lowNibble > 9)
                {
                    Registers.a += 0x66;
                }
                else
                {
                    Registers.a += 0x60;
                }
            }
            else if (Registers.GetHalfCarryFlag() == 1)
            {
                if (highNibble > 9)
                {
                    Registers.a += 0x66;
                }
                else
                {
                    Registers.a += 0x06;
                    fc = false;
                }
            }
            else if (lowNibble > 9)
            {
                if (highNibble < 9)
                {
                    fc = false;
                    Registers.a += 0x06;
                }
                else
                {
                    Registers.a += 0x66;
                }
            }
            else if (highNibble > 9)
            {
                Registers.a += 0x60;
            }
            else
            {
                fc = false;
            }

            if (fc == false)
            {
                Registers.SetCarryFlag(0);
            }
            else
            {
                Registers.SetCarryFlag(1);
            }

            if (Registers.a == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        /* JumpRelativeIfZero()
         * Description: Just do a relative jump if the last cmp instruction result was zero
         */
        public void JumpRelativeIfZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                JumpRelative();
            }
            else
            {
                ROMdata.ReadByte();
                ticks += 7;
            }
        }

        public void ComplementA()
        {
            Registers.a ^= 0xFF;

            Registers.SetAddSubFlag(1);
            Registers.SetHalfCarryFlag(1);

            ticks += 4;
        }

        public void JumpRelativeIfNotCarry()
        {
            if (Registers.GetCarryFlag() == 1)
            {
                ROMdata.ReadByte();
                ticks += 7;
            }
            else
            {
                JumpRelative();
            }
        }

        /* LoadImmediateWord()
         * Description: Sets a 16bits register
         */
        public void LoadImmediateWord(ref ushort r)
        {
            r = ROMdata.ReadWord();
            ticks += 10;
        }

        /* IncrementWord()
         * Description: Normal increment operation on a 16bits register
         */
        public void IncrementWord(ref ushort r)
        {
            if (r == 0xFFFF)
            {
                r = 0;
                Registers.SetZeroFlag(1);
            }
            else
            {
                r++;
                Registers.SetZeroFlag(0);
            }

            ticks += 6;
        }

        /* IncrementMemory()
         * Description: Normal increment operation on given address
         */
        public void IncrementMemory(byte h, byte l)
        {
            ushort addrs = (ushort)((h << 8) | l);

            Increment(ref Memory.mmap[addrs]);
            Memory[addrs] = Memory.mmap[addrs]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* DecrementMemory()
         * Description: Normal decrement operation on given address
         */
        public void DecrementMemory(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Decrement(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* LoadImmediateIntoMemory()
         * Description: Sets a value into a 16bits address
         */
        public void LoadImmediateIntoMemory(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Memory[addr] = ROMdata.ReadByte();

            ticks += 10;
        }

        /* JumpRelativeIfCarry()
         * Description: Does a relative jump if carry flag is set
         */
        public void JumpRelativeIfCarry()
        {
            if (Registers.GetCarryFlag() == 1)
            {
                JumpRelative();
            }
            else
            {
                ROMdata.ReadByte();
                ticks += 7;
            }
        }

        /* AddSPToHL()
         * Description: Adds SP register to HL register
         */
        public void AddSPToHL()
        {
            byte l = (byte)(Registers.sp & 0xFF);
            byte h = (byte)(Registers.sp >> 8);

            Add(ref Registers.h, ref Registers.l, h, l);
        }

        /* DecrementWord()
         * Description: Normal decrement function for 16bits registers
         */
        public void DecrementWord(ref ushort r)
        {
            if (r == 0)
            {
                r = 0xFFFF;
                Registers.SetZeroFlag(1);
            }
            else
            {
                r--;
                Registers.SetZeroFlag(0);
            }

            ticks += 6;
        }

        /* ComplementCarryFlag()
         * Description: Copy carry to half carry and revert the value
         */
        public void ComplementCarryFlag()
        {
            byte carry = Registers.GetCarryFlag();

            Registers.SetHalfCarryFlag(carry);

            if (carry == 0)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            Registers.SetAddSubFlag(0);

            ticks += 4;
        }

        /* Load()
         * Description: Set the value of a register
         */
        public void Load(ref byte r, byte value)
        {
            r = value;
            ticks += 4;
        }

        /* Halt()
         * Description: Stops the game until the player press a key
         */
        public void Halt()
        {
            if (interruptsEnabled == true)
            {
                halted = true;
            }
            else
            {
                stopCounting = true;
            }

            ticks += 4;
        }

        /* PowerUp()
         * Description: Sets main registers and memory data for game startup
         */
        public void PowerUp()
        {
            // Setup processor registers
            /* Registers.a can indicate the console type
             * 01h -> GameBoy and SuperGameBoy
             * FFh -> GameBoyPocket and SuperGameBoy2
             * 11h -> GameBoyColor and GameBoyAdvance
             * 
             * Registers.a can also indicate the console type
             * 01h -> GameBoyAdvance
             */
            Registers.a = 0x01; // Normal GameBoy
            Registers.b = 0x00;
            Registers.c = 0x13;
            Registers.d = 0x00;
            Registers.e = 0xD8;
            Registers.h = 0x01;
            Registers.l = 0x4D;
            Registers.SetZeroFlag(1);
            Registers.SetCarryFlag(0);
            Registers.SetHalfCarryFlag(1);
            Registers.SetAddSubFlag(1);
            Registers.sp = 0xFFFE;
            Registers.pc = 0x0100;

            // Move the reader
            ROMdata.SetPosition(Registers.pc);

            // Setup memory data
            WriteByte(0xFF05, 0x00); // TIMA
            WriteByte(0xFF06, 0x00); // TMA
            WriteByte(0xFF07, 0x00); // TAC
            WriteByte(0xFF10, 0x80); // NR10
            WriteByte(0xFF11, 0xBF); // NR11
            WriteByte(0xFF12, 0xF3); // NR12
            WriteByte(0xFF14, 0xBF); // NR14
            WriteByte(0xFF16, 0x3F); // NR21
            WriteByte(0xFF17, 0x00); // NR22
            WriteByte(0xFF19, 0xBF); // NR24
            WriteByte(0xFF1A, 0x7F); // NR30
            WriteByte(0xFF1B, 0xFF); // NR31
            WriteByte(0xFF1C, 0x9F); // NR32
            WriteByte(0xFF1E, 0xBF); // NR33
            WriteByte(0xFF20, 0xFF); // NR41
            WriteByte(0xFF21, 0x00); // NR42
            WriteByte(0xFF22, 0x00); // NR43
            WriteByte(0xFF23, 0xBF); // NR30
            WriteByte(0xFF24, 0x77); // NR50
            WriteByte(0xFF25, 0xF3); // NR51
            WriteByte(0xFF26, 0xF1); // NR52
            WriteByte(0xFF40, 0x91); // LCDC
            WriteByte(0xFF42, 0x00); // SCY
            WriteByte(0xFF43, 0x00); // SCX
            WriteByte(0xFF45, 0x00); // LYC
            WriteByte(0xFF47, 0xFC); // BGP
            WriteByte(0xFF48, 0xFF); // OBP0
            WriteByte(0xFF49, 0xFF); // OBP1
            WriteByte(0xFF4A, 0x00); // WY
            WriteByte(0xFF4B, 0x00); // WX
            WriteByte(0xFFFF, 0x00); // IE
        }

        /* Add
         * Description: Adds one register into another one
         */
        public void Add(ref byte r, byte val)
        {
            byte last_value = r;
            if (((r & 0x0F) + (val & 0x0F)) > 0x0F)
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            r += val;

            // If we add the registers and the result is less
            // than the value it was before the add
            // carry has happened
            if (r <= last_value)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            Registers.SetAddSubFlag(0);

            // Carry and also zero if the result is zero
            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        /* Add()
         * Description: Adds one memory value into a register
         */
        public void Add(ref byte r, byte h, byte l)
        {
            ushort addr = (byte)((h << 8) | l);
            Add(ref r, Memory[addr]);
            ticks += 4;
        }

        /* AddWithCarry()
         * Description: Adds the carry flag into a register with an add from a registry
         */
        public void AddWithCarry(ref byte r, byte val)
        {
            byte carry = 0;
            byte last_value = r;

            if (Registers.GetCarryFlag() == 1)
            {
                carry = 1;
            }

            if ((carry + (r & 0x0F) + (val & 0x0F)) > 0x0F)
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            r += (byte)(val + carry);

            if (r <= last_value)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            Registers.SetAddSubFlag(0);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        /* AddWithCarry()
         * Description: Adds the carry flag into a register with an add from memory
         */
        public void AddWithCarry(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            AddWithCarry(ref r, Memory[addr]);

            ticks += 4;
        }

        /* Sub()
         * Description: Substract a value from a Register
         */
        public void Sub(ref byte r, byte val)
        {
            if ((r & 0x0F) < (val & 0x0F))
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            if (val > r)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r -= val;

            Registers.SetAddSubFlag(1);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        /* Sub()
         * Description: Substract a memory value from a Register
         */
        public void Sub(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Sub(ref r, Memory[addr]);

            ticks += 4;
        }

        public void SubImmediate(ref byte r)
        {
            byte val = ROMdata.ReadByte();

            Sub(ref r, val);

            ticks += 4;
        }

        /* SubWithBorrow()
         * Description: I still dont know what this is suppossed to do, ported from GameBoyEmulator
         */
        public void SubWithBorrow(ref byte r, byte v)
        {
            if (Registers.GetZeroFlag() == 1)
            {
                Sub(ref r, (byte)(v + 1));
            }
            else
            {
                Sub(ref r, v);
            }
        }

        public void SubImmediateWithBorrow(ref byte r)
        {
            byte val = ROMdata.ReadByte();

            SubWithBorrow(ref r, val);

            ticks += 4; 
        }
        /* SubWithBorrow()
         * Description: I still dont know what this is suppossed to do, ported from GameBoyEmulator
         */
        public void SubWithBorrow(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            SubWithBorrow(ref r, Memory[addr]);
        }

        /* And()
         * Description: Normal bit-level and operation
         */
        public void And(ref byte r, byte val)
        {
            r = (byte)(0xFF & (r & val));
            Registers.SetHalfCarryFlag(1);
            Registers.SetAddSubFlag(0);
            Registers.SetCarryFlag(0);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        public void AndImmediate()
        {
            byte val = ROMdata.ReadByte();

            And(ref Registers.a, val);

            ticks += 4;
        }

        /* And()
         * Description: Normal bit-level and operation with memory value
         */
        public void And(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            And(ref r, Memory[addr]);

            ticks += 4;
        }

        /* Xor()
         * Description: Normal bit-level xor operation
         */
        public void Xor(ref byte r, byte val)
        {
            r = (byte)(r ^ val);

            Registers.SetHalfCarryFlag(0);
            Registers.SetAddSubFlag(0);
            Registers.SetCarryFlag(0);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        /* Xor()
         * Description: Normal bit-level xor operation with memory value
         */
        public void Xor(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Xor(ref r, Memory[addr]);

            ticks += 4;
        }

        public void XorImmediate()
        {
            byte val = ROMdata.ReadByte();

            Xor(ref Registers.a, val);

            ticks += 4;
        }

        /* Or()
         * Description: Normal bit-level or operation
         */
        public void Or(ref byte r, byte val)
        {
            r = (byte)(0xFF & (r | val));

            Registers.SetHalfCarryFlag(0);
            Registers.SetAddSubFlag(0);
            Registers.SetCarryFlag(0);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        public void OrImmediate()
        {
            byte val = ROMdata.ReadByte();
            Or(ref Registers.a, val);
            ticks += 8;
        }
        /* Or()
         * Description: Normal bit-level or operation with memory value
         */
        public void Or(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Or(ref r, Memory[addr]);

            ticks += 4;
        }

        /* Compare()
         * Description: Normal compare
         */
        public void Compare(ref byte r, byte val)
        {
            if ((r & 0x0F) < (val & 0x0F))
            {
                Registers.SetHalfCarryFlag(1);
            }
            else
            {
                Registers.SetHalfCarryFlag(0);
            }

            if (val > r)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            Registers.SetAddSubFlag(1);

            if (r == val)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            ticks += 4;
        }

        public void CompareImmediate()
        {
            byte val = ROMdata.ReadByte();

            Compare(ref Registers.a, val);

            ticks += 4;
        }

        /* Compare()
         * Description: Normal compare with memory value
         */
        public void Compare(ref byte r, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            Compare(ref r, Memory[addr]);

            ticks += 4;
        }

        /* ReturnIfNotZero
         * Description: Return if not zero flag is set
         */
        public void ReturnIfNotZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                ticks += 5;
            }
            else
            {
                Return();
                ticks += 1;
            }
        }

        /* ReturnIfZero()
         * Description: Return if zero flag is set
         */
        public void ReturnIfZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                Return();
                ticks++;
            }
            else
            {
                ticks += 5;
            }
        }

        /* Return()
         * Description: Helper function for return
         */
        public void Return()
        {
            Pop(ref Registers.pc);
            
            ROMdata.SetPosition(Registers.pc);

            ticks += 16;
        }

        public void ReturnFromInterrupt()
        {
            interruptsEnabled = true;
            halted = false;

            Return();
        }

        public void ReturnIfCarry()
        {
            if (Registers.GetCarryFlag() == 1)
            {
                Return();
                ticks += 4;
            }
            else
            {
                ticks -= 8;
            }
        }

        public void ReturnIfNotCarry()
        {
            if (Registers.GetCarryFlag() == 0)
            {
                Return();
                ticks += 4;
            }
            else
            {
                ticks -= 8;
            }
        }

        /* Pop()
         * Description: Normal pop instruction
         */
        public void Pop(ref byte h, ref byte l)
        {
            l = Memory[Registers.sp++];
            h = Memory[Registers.sp++];

            ticks += 12;
        }

        public void Pop(ref ushort value)
        {
            byte h = 0;
            byte l = 0;

            Pop(ref h, ref l);

            value = (ushort)((h << 8) | l);
        }

        /* Push()
         * Description: Normal push instruction
         */
        public void Push(byte h, byte l)
        {
            Memory[Registers.sp--] = h;
            Memory[Registers.sp--] = l;

            ticks += 16;
        }

        public void Push(ushort value)
        {
            byte h = (byte)(value >> 8);
            byte l = (byte)(value & 0xFF);

            Push(h, l);
        }

        /* JumpIfNotZero()
         * Description: Normal jump if not zero instruction
         */
        public void JumpIfNotZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                ROMdata.ReadWord();
                ticks++;
            }
            else
            {
                Jump();
            }
        }

        /* JumpIfZero()
         * Description: Normal jump if zero instruction
         */
        public void JumpIfZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                Jump();
            }
            else
            {
                ROMdata.ReadWord();
                ticks++;
            }
        }

        /* Jump()
         * Description: Normal jump instruction
         */
        public void Jump()
        {
            ushort addr = ROMdata.ReadWord();

            ROMdata.SetPosition(addr);

            ticks += 10;
        }

        /* Jump()
         * Description: Normal jump instruction with specified address arguments
         */
        public void Jump(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            ROMdata.SetPosition(addr);

            ticks += 4;
        }


        public void JumpIfCarry()
        {
            if (Registers.GetCarryFlag() == 1)
            {
                Jump();
                ticks += 6;
            }
            else
            {
                ROMdata.ReadWord();
                ticks += 12;
            }
        }

        public void JumpIfNotCarry()
        {
            if (Registers.GetCarryFlag() == 0)
            {
                Jump();
                ticks += 16;
            }
            else
            {
                ROMdata.ReadWord();
                ticks += 12;
            }
        }

        /* CallIfNotZero()
         * Description: Normal call if not zero instruction
         */
        public void CallIfNotZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                ROMdata.ReadWord();
                ticks++;
            }
            else
            {
                Call();
            }
        }

        /* CallIfZero()
         * Description: Normal call if zero instruction
         */
        public void CallIfZero()
        {
            if (Registers.GetZeroFlag() == 1)
            {
                Call();
            }
            else
            {
                ROMdata.ReadWord();
                ticks++;
            }
        }

        /* Call()
         * Description: Normal call instruction
         */
        public void Call()
        {
            Push((ushort)(Registers.pc + 2));

            Registers.pc = ROMdata.ReadWord();

            // Update the position
            ROMdata.SetPosition(Registers.pc);

            ticks += 12;
        }

        public void CallIfCarry()
        {
            if (Registers.GetCarryFlag() == 1)
            {
                Call();
                ticks += 12;
            }
            else
            {
                ROMdata.ReadWord();
                ticks += 12;
            }
        }

        public void CallIfNotCarry()
        {
            if (Registers.GetCarryFlag() == 0)
            {
                Call();
                ticks += 12;
            }
            else
            {
                ROMdata.ReadWord();
                ticks += 12;
            }
        }
        /* AddImmediate()
         * Description: Add a value to a register
         */
        public void AddImmediate(ref byte r)
        {
            Add(ref r, ROMdata.ReadByte());

            ticks += 3;
        }

        /* Restart()
         * Description: I still dont know what this is suppossed to do, ported from GameBoyEmulator
         */
        public void Restart(ushort addr)
        {
            Push(Registers.pc);

            Registers.pc = addr;
        }

        /* ShiftLeft()
         * Description: Normal shift operation
         */
        public void ShiftLeft(ref byte r)
        {
            if (r > 0x7F)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)(r << 1);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            ticks += 8;
        }

        public void ShiftLeft(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            ShiftLeft(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* SignedShiftRight()
         * Description: Another shift operation
         */
        public void SignedShiftRight(ref byte r)
        {
            if ((r & 0x01) == 1)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)((r & 0x80) | (r >> 1));

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            ticks += 8;
        }

        public void SignedShiftRight(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);
            
            SignedShiftRight(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* Swap()
         * Description: I dont understand what this does, ported from GameBoyEmulator
         */
        public void Swap(ref byte r)
        {
            r = (byte)((r << 4) | (r >> 5));
            ticks += 8;
        }

        public void Swap(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);
            
            Swap(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* UnsignedShiftRight()
         * Description: Another shift operation
         */
        public void UnsignedShiftRight(ref byte r)
        {
            if ((r & 0x01) == 1)
            {
                Registers.SetCarryFlag(1);
            }
            else
            {
                Registers.SetCarryFlag(0);
            }

            r = (byte)(r >> 1);

            if (r == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            ticks += 8;
        }

        public void UnsignedShiftRight(byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            UnsignedShiftRight(ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 7;
        }

        /* TestBit()
         * Description: Checks if the given bit is 1 or 0
         */
        public void TestBit(byte n, byte r)
        {
            if ((r & (1 << n)) == 0)
            {
                Registers.SetZeroFlag(1);
            }
            else
            {
                Registers.SetZeroFlag(0);
            }

            Registers.SetAddSubFlag(0);
            Registers.SetHalfCarryFlag(0);

            ticks += 8;
        }

        public void TestBit(byte n, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            TestBit(n, Memory[addr]);

            ticks += 4;
        }

        /* ResetBit()
         * Description: Sets the specified bit to 0
         */
        public void ResetBit(byte n, ref byte r)
        {
            byte t = (byte)(~(1 << n));
            r = (byte)(r & t);
            ticks += 8;
        }

        public void ResetBit(byte n, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);

            ResetBit(n, ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 8;
        }

        /* SetBit()
         * Description: Sets the specified bit to 1
         */
        public void SetBit(byte n, ref byte r)
        {
            r = (byte)(r | (1 << n));
            ticks += 8;
        }

        public void SetBit(byte n, byte h, byte l)
        {
            ushort addr = (ushort)((h << 8) | l);
            
            SetBit(n, ref Memory.mmap[addr]);
            Memory[addr] = Memory.mmap[addr]; // Bit stupid, but should do the set update

            ticks += 8;
        }

        /* Interrupt()
         * Description: Interrupts the actual work to handle an interrupt
         */
        public void Interrupt(ushort addr)
        {
            interruptsEnabled = false;
            halted = false;
            Push(Registers.pc);
            Registers.pc = addr;
            ROMdata.SetPosition(Registers.pc);
        }
    }
}
