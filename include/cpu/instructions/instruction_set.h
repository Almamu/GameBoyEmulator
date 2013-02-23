#ifndef __INSTRUCTION_SET_H__
#define __INSTRUCTION_SET_H__

#include "common.h"

class GB_Z80;

#define INC8(cpu, reg)							\
	cpu->mRegisters.af.flags.addSub = 0;		\
												\
	if(reg >= 0xF)								\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 1;	\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 0;	\
	}											\
												\
	reg ++;										\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\
												
#define DEC8(cpu, reg)							\
	cpu->mRegisters.af.flags.addSub = 1;		\
												\
	if(reg > 0xF)								\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 1;	\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 0;	\
	}											\
												\
	reg--;										\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\

class GB_Z80_InstructionSet
{
public:
	void Call(void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*), uint8_t opcode, GB_Z80* cpu);
	void RegisterInstructions(GB_Z80* cpu);

	void nop(uint8_t opcode, GB_Z80* cpu);
	void ld(uint8_t opcode, GB_Z80* cpu);
	void inc(uint8_t opcode, GB_Z80* cpu);
	void dec(uint8_t opcode, GB_Z80* cpu);

private:
	void (GB_Z80_InstructionSet::*instruction)(uint8_t, GB_Z80*);
};

#endif /* __INSTRUCTION_SET_H__ */