#include "common.h"

GB_Z80::GB_Z80(cpu_type_t system)
{
	// initialize cpu data
	init(system);

	mInstructionHandler = new GB_Z80_InstructionHandler();
	mInstructionSet = new GB_Z80_InstructionSet();
}

void GB_Z80::init(cpu_type_t system)
{
	/*
		mRegisters.a can be these values on startup:
		- 01h -> GameBoy and SuperGameBoy
		- FFh -> GameBoyPocket and SuperGameBoy2
		- 11h -> GameBoyColor and GameBoyAdvance
	*/

	// initialize cpu registers
	mRegisters.af.a = system; // GameBoy system as of now

	SET_HIGH(mRegisters.bc, 0x00);
	SET_HIGH(mRegisters.de, 0x00);
	SET_HIGH(mRegisters.hl, 0x01);

	SET_LOW(mRegisters.bc, 0x13);
	SET_LOW(mRegisters.de, 0xD8);
	SET_LOW(mRegisters.hl, 0x4D);

	// initialize cpu flags
	mRegisters.af.flags.zero = 0x1;
	mRegisters.af.flags.carry = 0x0;
	mRegisters.af.flags.halfCarry = 0x1;
	mRegisters.af.flags.addSub = 0x1;

	// stack pointer and program counter
	mRegisters.sp = 0xFFFE;
	mRegisters.pc = 0x0100;

	// randomize RAM data
	for(size_t cur = 0; cur < 0xFFFF; cur ++)
	{
		mMemory[cur] = rand() & 0xFF;
	}

	// set basic memory data
	mMemory[0xFF05] = 0x00;
	mMemory[0xFF06] = 0x00;
	mMemory[0xFF07] = 0x00;
	mMemory[0xFF10] = 0x80;
	mMemory[0xFF11] = 0xBF;
	mMemory[0xFF12] = 0xF3;
	mMemory[0xFF14] = 0xBF;
	mMemory[0xFF16] = 0x3F;
	mMemory[0xFF17] = 0x00;
	mMemory[0xFF19] = 0xBF;
	mMemory[0xFF1A] = 0x7F;
	mMemory[0xFF1B] = 0xFF;
	mMemory[0xFF1C] = 0x9F;
	mMemory[0xFF1E] = 0xBF;
	mMemory[0xFF20] = 0xFF;
	mMemory[0xFF21] = 0x00;
	mMemory[0xFF22] = 0x00;
	mMemory[0xFF23] = 0xBF;
	mMemory[0xFF24] = 0x77;
	mMemory[0xFF25] = 0xF3;
	mMemory[0xFF26] = (system == CPUType_SuperGameBoy || system == CPUType_SuperGameBoy2) ? 0xF0 : 0xF1; // F0h for SuperGameBoy
	mMemory[0xFF40] = 0x91;
	mMemory[0xFF42] = 0x00;
	mMemory[0xFF43] = 0x00;
	mMemory[0xFF45] = 0x00;
	mMemory[0xFF47] = 0xFC;
	mMemory[0xFF48] = 0xFF;
	mMemory[0xFF49] = 0xFF;
	mMemory[0xFF4A] = 0x00;
	mMemory[0xFF4B] = 0x00;
	mMemory[0xFFFF] = 0x00;

	// register every handler
	mInstructionSet->RegisterInstructions(this);
}

bool GB_Z80::loadRom(const char* rom_file)
{
	try
	{
		// load file
		GB_ROM* rom = new GB_ROM(rom_file);

		// read rom data
		mRomData = rom->read();

		// free memory and close file
		delete rom;

		// now we should copy the first rom bank to the memory
		memcpy(&mMemory[0], mRomData->data, (mRomData->size > ROM_BANK_SIZE) ? ROM_BANK_SIZE : mRomData->size);

		// copy second bank only if possible
		if(mRomData->size > ROM_BANK_SIZE)
		{
			// we must handle multi-banked games too
			memcpy(&mMemory[ROM_BANK_SIZE], &mRomData->data[ROM_BANK_SIZE], (mRomData->size > (ROM_BANK_SIZE * 2)) ? ROM_BANK_SIZE : (mRomData->size - ROM_BANK_SIZE));
		}

		// perform security checks on roms (TODO: port this to GB assembly and load from a ROM)
		{
			// first check: compare nintendo logo
			for(uint16_t cur = 0; cur < 0x30; cur ++)
			{
				if(mMemory[0x104 + cur] != GB_ROM_baseLogo[cur])
				{
					throw new GB_ROM_Exception("Nintendo logo is not correct", rom_file);
				}
			}

			// second check: 'checksum' 0x19 bytes from 0x134 to 0x14D
			uint8_t checksum = 0;
			
			for(uint16_t cur = 0x134; cur < 0x134 + 0x19; cur ++)
			{
				checksum += mMemory[cur];
			}

			checksum += 0x19;

			if(checksum & 1)
			{
				throw new GB_ROM_Exception("Checksum is not correct", rom_file);
			}

			checksum = 0;

			// third check: header checksum
			for(uint16_t cur = 0x134; cur < 0x134 + 0x18; cur ++)
			{
				checksum -= mMemory[cur] - 1;
			}

			if(checksum != mMemory[0x14D])
			{
				throw new GB_ROM_Exception("Header checksum is not correct", rom_file);
			}
		}

		// finally update the program counter, even when this is called it should be 0x100
		mRegisters.pc = 0x0100;
	}
	catch(GB_ROM_Exception* ex)
	{
		ex;
		return false;
	}

	return true;
}

uint8_t GB_Z80::readFromPC()
{
	return mMemory[mRegisters.pc++];
}