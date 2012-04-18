using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nintendo.GB.Utils
{
    public static class Log
    {
        public static void Init()
        {
            if (File.Exists("emulator.log") == false)
            {
                File.Create("emulator.log").Close();
            }

            file = File.AppendText("emulator.log");
        }

        public static void Logger(byte opcode)
        {
            string message = "A: " + Registers.a.ToString("X2")
            + " B: " + Registers.b.ToString("X2") + " C: " + Registers.c.ToString("X2")
            + " D: " + Registers.d.ToString("X2") + " E: " + Registers.e.ToString("X2")
            + " H: " + Registers.h.ToString("X2") + " L: " + Registers.l.ToString("X2")
            + " PC: " + Registers.pc.ToString("X4") + " SP: " + Registers.sp.ToString("X4")
            + " HC: " + Registers.GetHalfCarryFlag() + " CF: " + Registers.GetCarryFlag()
            + " ASF: " + Registers.GetAddSubFlag() + " Z: " + Registers.GetZeroFlag()
            + " Opcode: " + opcode.ToString("X2") + "\r\n";

            file.Write(message);
            file.Flush();
        }

        private static StreamWriter file = null;
    }
}
