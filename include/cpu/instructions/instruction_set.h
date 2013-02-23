#ifndef __INSTRUCTION_SET_H__
#define __INSTRUCTION_SET_H__

#include "common.h"

class GB_Z80;

class GB_Z80_InstructionSet
{
public:
	void Call(void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*), uint8_t opcode, GB_Z80* cpu);
	void RegisterInstructions(GB_Z80* cpu);

	void nop(uint8_t opcode, GB_Z80* cpu);
	void ld(uint8_t opcode, GB_Z80* cpu);
private:
	void (GB_Z80_InstructionSet::*instruction)(uint8_t, GB_Z80*);
};

#endif /* __INSTRUCTION_SET_H__ */