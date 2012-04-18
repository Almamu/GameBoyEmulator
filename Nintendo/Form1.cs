using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nintendo.GB.Utils;
using Nintendo.GB;

namespace Nintendo
{
    public partial class Form1 : Form
    {
        const int FPS = 60;
        public int MAXSKIPPED = 10;
        int WIDTH = 160;
        int HEIGHT = 144;
        public long FREQUENCY = Stopwatch.Frequency;
        public long TICKS_PER_FRAME = Stopwatch.Frequency / FPS;
        private Bitmap bitmap;
        public Graphics graphics;
        public Stopwatch stopwatch = new Stopwatch();
        public long nextFrameStart;
        public static Thread emu = new Thread(Run);
        public static string file = "";
        private Rectangle rect;
        private double scanLineTicks;
        private uint[] pixels = new uint[160 * 144];

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Roms Game Boy Color(*.gb)|*.gb|All types(*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                file = openFileDialog1.FileName;
                Start();
            }
        }

        private void RenderFrame()
        {
            graphics.DrawImage(bitmap, 0, 0, WIDTH, HEIGHT);
        }

        private void InitFrame()
        {
            rect = new Rectangle(0, 0, WIDTH, HEIGHT);

            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        private void SetImageSize(int scale)
        {
            WIDTH = scale * 160;
            HEIGHT = scale * 144;
            ClientSize = new Size(WIDTH, HEIGHT);
        }

        private void InitGraphics()
        {
            if (graphics != null)
            {
                graphics.Dispose();
            }

            graphics = CreateGraphics();

            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        }

        private void InitImage()
        {
            InitGraphics();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0xFF000000;
            }

            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            IntPtr pointer = Marshal.UnsafeAddrOfPinnedArrayElement(pixels, 0);
            bitmap = new Bitmap(160, 144, 160 * 4, PixelFormat.Format32bppPArgb, pointer);
        }

        // Emulator code
        public static void Start()
        {
            emu.Start();
        }

        public static void Run()
        {
            GBRom rom = new GBRom();
            bool res = rom.LoadROM(file);

            if (res == false)
            {
                MessageBox.Show("No se puede abrir el ROM especificado", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try
                {
                    emu.Abort();
                }
                catch (Exception)
                {

                }
            }

            Interpreter i = new Interpreter(rom);
            i.PowerUp();

            while (true)
            {
                Thread.Sleep(1);

                try
                {
                    i.Step();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
