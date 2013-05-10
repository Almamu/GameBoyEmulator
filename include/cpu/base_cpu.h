#ifndef __BASE_CPU_H__
#define __BASE_CPU_H__

#include "common.h"

/*
	Game Boy Technical Data
	  CPU          - 8-bit (Similar to the Z80 processor)
	  Clock Speed  - 4.194304MHz (4.295454MHz for SGB, max. 8.4MHz for CGB)
	  Work RAM     - 8K Byte (32K Byte for CGB)
	  Video RAM    - 8K Byte (16K Byte for CGB)
	  Screen Size  - 2.6"
	  Resolution   - 160x144 (20x18 tiles)
	  Max sprites  - Max 40 per screen, 10 per line
	  Sprite sizes - 8x8 or 8x16
	  Palettes     - 1x4 BG, 2x3 OBJ (for CGB: 8x4 BG, 8x3 OBJ)
	  Colors       - 4 grayshades (32768 colors for CGB)
	  Horiz Sync   - 9198 KHz (9420 KHz for SGB)
	  Vert Sync    - 59.73 Hz (61.17 Hz for SGB)
	  Sound        - 4 channels with stereo sound
	  Power        - DC6V 0.7W (DC3V 0.7W for GB Pocket, DC3V 0.6W for CGB)
*/

#define ROM_BANK_SIZE	0x4000
#define RAM_SIZE		0x10000
#define LOW(x)			((uint8_t*)(&x))[1]
#define HIGH(x)			((uint8_t*)(&x))[0]
#define SET_LOW(x,y)	LOW(x) = y
#define SET_HIGH(x,y)	HIGH(x) = y
#define SET(x,y,z)		SET_HIGH(x,z); SET_LOW(x,y)

typedef uint16_t register_t;
typedef uint8_t smallRegister_t;
typedef uint8_t* memoryPointer_t;

// values for A register
enum cpu_type_t
{
	CPUType_GameBoy				= 0x1,
	CPUType_SuperGameBoy		= 0x1,
	CPUType_GameBoyPocket		= 0xFF,
	CPUType_SuperGameBoy2		= 0xFF,
	CPUType_GameBoyColor		= 0x11,
	CPUType_GameBoyAdvance		= 0x11
};

enum interrupt_flags_t
{
	IF_VerticalBlank	= 0x01,
	IF_LCDStart			= 0x02,
	IF_Timer			= 0x04,
	IF_Serial			= 0x08,
	IF_Joypad			= 0x10
};

union flags_t
{
	smallRegister_t zero : 1;
	smallRegister_t addSub : 1;
	smallRegister_t halfCarry : 1;
	smallRegister_t carry : 1;
	smallRegister_t unused : 4;
};

struct registers_t
{
	union
	{
		smallRegister_t a;
		flags_t flags;
	}af;

	register_t bc;
	register_t de;
	register_t hl;
	register_t sp;
	register_t pc;
};

class GB_Z80_StopRequested
{
public:
	GB_Z80_StopRequested(std::string message, uint16_t address) : mMessage(message), mAddress(address) {}
	~GB_Z80_StopRequested() {}

	std::string mMessage;
	uint16_t mAddress;
};

class GB_Z80
{
public:
	GB_Z80(cpu_type_t system);
	~GB_Z80();
	
	void init(cpu_type_t system);

	registers_t mRegisters;
	rom_data_t* mRomData;

	/*
		Memory Map
		  0000-3FFF   16KB ROM Bank 00     (in cartridge, fixed at bank 00)
		  4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
		  8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
		  A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
		  C000-CFFF   4KB Work RAM Bank 0 (WRAM)
		  D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
		  E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
		  FE00-FE9F   Sprite Attribute Table (OAM)
		  FEA0-FEFF   Not Usable
		  FF00-FF7F   I/O Ports
		  FF80-FFFE   High RAM (HRAM)
		  FFFF        Interrupt Enable Register
	*/
	static const uint16_t BANK0		= 0x0000;			// ROM bank 00
	static const uint16_t BANKN		= ROM_BANK_SIZE;	// ROM bank 00..NN
	static const uint16_t VRAM		= 0x8000;			// Video RAM
	static const uint16_t ERAM		= 0xA000;			// External RAM
	static const uint16_t WRAM0		= 0xC000;			// Work RAM bank 0
	static const uint16_t WRAM1		= 0xD000;			// Work RAM bank 1
	static const uint16_t WRAME		= 0xE000;			// Work RAM bank 0 ECHO
	static const uint16_t OAM		= 0xFE00;			// Sprite Attribute Table
	static const uint16_t UNUSED	= 0xFEA0;			// Not usable
	static const uint16_t IOPORTS	= 0xFF00;			// I/O Ports
	static const uint16_t IF		= 0xFF0F;			// Interrupt flag
	static const uint16_t HRAM		= 0xFF80;			// High RAM
	static const uint16_t IE		= 0xFFFF;			// Interrupt Enable Register

	uint8_t mMemory[RAM_SIZE];							// 0x10000 just to make sure we do not overflow
	uint8_t mTicks;										// TODO: Move it to proper place

	bool loadRom(const char* rom_file);

	GB_Z80_InstructionHandler* mInstructionHandler;
	GB_Z80_InstructionSet* mInstructionSet;

	uint8_t readFromPC();
private:
	
};

#endif /* __BASE_CPU_H__ */