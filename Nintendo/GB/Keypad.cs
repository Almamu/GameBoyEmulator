using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nintendo.GB
{
    public static class Keypad
    {
        public static bool Left;
        public static bool Right;
        public static bool Up;
        public static bool Down;
        public static bool A;
        public static bool B;
        public static bool Select;
        public static bool Start;
        public static bool InterruptEnabled;
        public static bool InterruptRequested;

        public static bool p14, p15;
    }
}
