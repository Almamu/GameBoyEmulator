using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintendo.GB
{
    public static class Registers
    {
        /*   CPU Registers and Flags
         *   16bit Hi   Lo   Name/Function
         *   AF    A    -    Accumulator & Flags
         *   BC    B    C    BC
         *   DE    D    E    DE
         *   HL    H    L    HL
         *   SP    -    -    Stack Pointer
         *   PC    -    -    Program Counter/Pointer
         */

        public static byte a = 0x0;
        public static byte f = 0x0;
        public static byte b = 0x0;
        public static byte c = 0x0;
        public static byte d = 0x0;
        public static byte e = 0x0;
        public static byte h = 0x0;
        public static byte l = 0x0;
        public static ushort sp = 0x0;
        public static ushort pc = 0x0;

        public static ushort GetAF()
        {
            return (ushort)((a << 8) | (f));
        }

        public static ushort GetBC()
        {
            return (ushort)((b << 8) | c);
        }

        public static ushort GetDE()
        {
            return (ushort)((d << 8) | e);
        }

        public static ushort GetHL()
        {
            return (ushort)((h << 8) | l);
        }

        public static void SetZeroFlag(byte value)
        {
            value &= 0x01;

            SetBit(ref f, value, 7);
        }

        public static byte GetZeroFlag()
        {
            return (byte)((f & 0x80) >> 7);
        }

        public static void SetAddSubFlag(byte value)
        {
            value &= 0x01;

            SetBit(ref f, value, 6);
        }

        public static byte GetAddSubFlag()
        {
            return (byte)((f & 0x40) >> 6);
        }

        public static void SetHalfCarryFlag(byte value)
        {
            value &= 0x01;

            SetBit(ref f, value, 5);
        }

        public static byte GetHalfCarryFlag()
        {
            return (byte)((f & 0x20) >> 5);
        }

        public static void SetCarryFlag(byte value)
        {
            value &= 0x01;

            SetBit(ref f, value, 4);
        }

        public static byte GetCarryFlag()
        {
            return (byte)((f & 0x10) >> 4);
        }

        // Quick fix for now, but should do the trick
        public static void SetBit(ref byte r, byte v, byte n)
        {
            if (v == 0)
            {
                r = (byte)(r & (~(1 << n)));
            }
            else
            {
                r = (byte)(r | (v << n));
            }
        }
    }
}
