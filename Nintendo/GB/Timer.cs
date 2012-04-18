using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintendo.GB
{
    public static class Timer
    {
        public enum FrequencyType
        {
            hz4096 = 0,
            hz262144 = 1,
            hz65536 = 2,
            hz16384 = 3
        }

        public static bool Running;
        public static byte Counter;
        public static byte Modulo;
        public static byte Ticks;
        public static FrequencyType Frequency;

        public static bool OverflowInterruptRequested;
        public static bool OverflowInterruptEnabled;

        public static byte Seconds = 0;
        public static byte Minutes = 0;
        public static byte Hours = 0;
        public static ushort Days = 0;

        public static bool RTCEnabled = false;
    }
}
