using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nintendo.GB.Utils
{
    public class GBRom
    {
        public static byte[] baseLogo = { 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
                                          0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
                                          0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E };

        public byte[] entryPoint = new byte[0x103 - 0x100];
        public byte[] nintendoLogo = new byte[0x133 - 0x104];
        public byte[] title = new byte[0x143 - 0x134];
        public byte[] manufacturer = new byte[0x142 - 0x13F];
        
        /* Possible values:
         * 0x80 -> Game supports GBC functions, but works on old gameboys also
         * 0xC0 -> Game supports GBC functions only
         */
        public byte gbcFlag = 0x0;
        public byte[] licensee = new byte[0x145 - 0x144];

        /* Possible values:
         * 0x00 -> No SGB functions
         * 0x03 -> Game supports SGB functions
         */
        public byte sgbFlag = 0x0; // 0x146

        public enum CatridgeType
        {
            ROM = 0x00,
            ROM_MBC1 = 0x01,
            ROM_MBC1_RAM = 0x02,
            ROM_MBC1_RAM_BATT = 0x03,
            ROM_MBC2 = 0x05,
            ROM_MBC2_BATTERY = 0x06,
            ROM_RAM = 0x08,
            ROM_RAM_BATTERY = 0x09,
            ROM_MMM01 = 0x0B,
            ROM_MMM01_SRAM = 0x0C,
            ROM_MMM01_SRAM_BATT = 0x0D,
            ROM_MBC3_TIMER_BATT = 0x0F,
            ROM_MBC3_TIMER_RAM_BATT = 0x10,
            ROM_MBC3 = 0x11,
            ROM_MBC3_RAM = 0x12,
            ROM_MBC3_RAM_BATT = 0x13,
            ROM_MBC5 = 0x19,
            ROM_MBC5_RAM = 0x1A,
            ROM_MBC5_RAM_BATT = 0x1B,
            ROM_MBC5_RUMBLE = 0x1C,
            ROM_MBC5_RUMBLE_SRAM = 0x1D,
            ROM_MBC5_RUMBLE_SRAM_BATT = 0x1E,
            PocketCamera = 0x1F,
            BandaiTAMA5 = 0xFD,
            HudsonHuC3 = 0xFE,
            HudsonHuC1 = 0xFF
        };
        /* Catridge Type:
         *   00h  ROM ONLY                 13h  MBC3+RAM+BATTERY
         *   01h  MBC1                     15h  MBC4
         *   02h  MBC1+RAM                 16h  MBC4+RAM
         *   03h  MBC1+RAM+BATTERY         17h  MBC4+RAM+BATTERY
         *   05h  MBC2                     19h  MBC5
         *   06h  MBC2+BATTERY             1Ah  MBC5+RAM
         *   08h  ROM+RAM                  1Bh  MBC5+RAM+BATTERY
         *   09h  ROM+RAM+BATTERY          1Ch  MBC5+RUMBLE
         *   0Bh  MMM01                    1Dh  MBC5+RUMBLE+RAM
         *   0Ch  MMM01+RAM                1Eh  MBC5+RUMBLE+RAM+BATTERY
         *   0Dh  MMM01+RAM+BATTERY        FCh  POCKET CAMERA
         *   0Fh  MBC3+TIMER+BATTERY       FDh  BANDAI TAMA5
         *   10h  MBC3+TIMER+RAM+BATTERY   FEh  HuC3
         *   11h  MBC3                     FFh  HuC1+RAM+BATTERY
         *   12h  MBC3+RAM
         */
        public CatridgeType catridgeType = 0x0; // 0x147

        /* Rom Size:
         *   00h -  32KByte (no ROM banking)
         *   01h -  64KByte (4 banks)
         *   02h - 128KByte (8 banks)
         *   03h - 256KByte (16 banks)
         *   04h - 512KByte (32 banks)
         *   05h -   1MByte (64 banks)  - only 63 banks used by MBC1
         *   06h -   2MByte (128 banks) - only 125 banks used by MBC1
         *   07h -   4MByte (256 banks)
         *   52h - 1.1MByte (72 banks)
         *   53h - 1.2MByte (80 banks)
         *   54h - 1.5MByte (96 banks)
         */
        public byte romSize = 0x0; // 0x148

        /* RAM Size
         *   00h - None
         *   01h - 2 KBytes
         *   02h - 8 Kbytes
         *   03h - 32 KBytes (4 banks of 8KBytes each)
         */
        public byte eramSize = 0x0; // 0x149

        /* Destination Code Values:
         *   00h - Japanese
         *   01h - Non-Japanese
         */
        public byte destinationCode = 0x0; // 0x14A

        /* Old Licensee Code
         * 0x33 -> Use the new code
         */
        public byte oldLicenseeCode = 0x0; // 0x14B
        public byte maskRomVersion = 0x0; // 0x14C
        /* Checksum calculations:
         *   x=0:FOR i=0134h TO 014Ch:x=x-MEM[i]-1:NEXT
         */
        public byte headerChecksum = 0x0; // 0x14D
        public byte[] globalChecksum = new byte[0x14F - 0x14E];

        public byte[] ROM = null;

        private BinaryReader reader = null;

        public bool LoadROM(string filename)
        {
            FileStream rom = null;
            try
            {
                rom = File.Open(filename, FileMode.Open);
            }
            catch (Exception)
            {
                return false;
            }

            reader = new BinaryReader(rom);            

            // Seek to the ROM header
            reader.BaseStream.Seek(0x100, SeekOrigin.Begin);
            entryPoint = reader.ReadBytes(entryPoint.Length);
            nintendoLogo = reader.ReadBytes(nintendoLogo.Length);

            /*if (nintendoLogo != baseLogo)
            {
                try
                {
                    reader.Close();
                    rom.Close();
                }
                catch (Exception)
                {

                }

                return false;
            }*/

            title = reader.ReadBytes(title.Length);
            manufacturer = reader.ReadBytes(manufacturer.Length);
            gbcFlag = reader.ReadByte();
            licensee = reader.ReadBytes(licensee.Length);
            sgbFlag = reader.ReadByte();
            catridgeType = (CatridgeType)reader.ReadByte();
            romSize = reader.ReadByte();
            eramSize = reader.ReadByte();
            destinationCode = reader.ReadByte();
            oldLicenseeCode = reader.ReadByte();
            maskRomVersion = reader.ReadByte();
            headerChecksum = reader.ReadByte();
            globalChecksum = reader.ReadBytes(globalChecksum.Length);

            // Jump to the ROM entry point
            reader.BaseStream.Seek(0x100, SeekOrigin.Begin);
            
            return true;
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public void ReadBack()
        {
            reader.BaseStream.Seek(-1, SeekOrigin.Current);
        }

        public void SetPosition(ushort position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public void SetRelativePosition(short position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Current);
        }

        public ushort ReadWord()
        {
            byte l = ReadByte();
            byte h = ReadByte();

            return (ushort)((h << 8) | l);
        }

        public byte ReadByte(uint address)
        {
            reader.BaseStream.Seek(address, SeekOrigin.Begin);
            return reader.ReadByte();
        }

        public ushort Position()
        {
            return (ushort)reader.BaseStream.Position;
        }
    }
}
