#include "common.h"

void GB_Z80_InstructionSet::Call(void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*), uint8_t opcode, GB_Z80* cpu)
{
	// BIG HACK: This allows us to call the handler
	// as we are sure that this is the correct class there should not be any problem
	// but It'll need a better CallDispatcher just like I did with bluelib
	instruction = handler;

	(this->*instruction)(opcode, cpu);
}

void GB_Z80_InstructionSet::RegisterInstructions(GB_Z80* cpu)
{
	uint8_t nopInstructions[11] = { 0x00, 0xD3, 0xD8, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xF4, 0xFC, 0xFD };

	// this loops makes our lives easier
	for(uint8_t cur = 0; cur < 11; cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(nopInstructions[cur], cpu, this, &GB_Z80_InstructionSet::nop);
	}
}

void GB_Z80_InstructionSet::nop(uint8_t opcode, GB_Z80* cpu)
{
	// do nothing but increase the ticks
	cpu->mTicks += 4;
}