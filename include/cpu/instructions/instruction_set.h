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

#define INC16(cpu, reg)							\
	cpu->mRegisters.af.flags.addSub = 0;		\
												\
	if(reg >= 0xFF)								\
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

#define DEC16(cpu, reg)							\
	cpu->mRegisters.af.flags.addSub = 1;		\
												\
	if(reg > 0xFF)								\
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

#define RL(cpu, reg)							\
	uint8_t high = reg & 0x80;					\
												\
	reg <<= 1;									\
												\
	if(cpu->mRegisters.af.flags.carry)			\
	{											\
		reg |= 0x01;							\
	}											\
												\
	if(high)									\
	{											\
		cpu->mRegisters.af.flags.carry = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.carry = 0;		\
	}											\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\
												\
	cpu->mRegisters.af.flags.addSub = 0;		\
	cpu->mRegisters.af.flags.halfCarry = 0;		\

#define RR(cpu, reg)							\
	uint8_t low = reg & 0x01;					\
												\
	reg >>= 1;									\
												\
	if(cpu->mRegisters.af.flags.carry)			\
	{											\
		reg |= 0x80;							\
	}											\
												\
	if(low)										\
	{											\
		cpu->mRegisters.af.flags.carry = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.carry = 0;		\
	}											\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\
												\
	cpu->mRegisters.af.flags.addSub = 0;		\
	cpu->mRegisters.af.flags.halfCarry = 0;		\

#define ADD16(cpu, reg, val)					\
	if( (reg & 0xFFF) + (val & 0xFFF) > 4095)	\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 1;	\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 0;	\
	}											\
												\
	if( (reg + val) > 0xFFFF)					\
	{											\
		cpu->mRegisters.af.flags.carry = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.carry = 0;		\
	}											\
												\
	reg += val;									\
	cpu->mRegisters.af.flags.addSub = 0;		\

#define ADD8(cpu, reg, val)						\
	if( (reg & 0x0F) + (val & 0x0F) > 4095)		\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 1;	\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 0;	\
	}											\
												\
	if( (reg + val) > 0xFF)						\
	{											\
		cpu->mRegisters.af.flags.carry = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.carry = 0;		\
	}											\
												\
	reg += val;									\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\
												\
	cpu->mRegisters.af.flags.addSub = 0;		\

#define ADC(cpu, reg, val)						\
	if( (reg & 0x0F) + (val & 0x0F) > 4095)		\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 1;	\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.halfCarry = 0;	\
	}											\
												\
	reg += val + cpu->mRegisters.af.flags.carry;\
												\
	if( (reg + val) > 0xFF)						\
	{											\
		cpu->mRegisters.af.flags.carry = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.carry = 0;		\
	}											\
												\
	if(reg == 0)								\
	{											\
		cpu->mRegisters.af.flags.zero = 1;		\
	}											\
	else										\
	{											\
		cpu->mRegisters.af.flags.zero = 0;		\
	}											\
												\
	cpu->mRegisters.af.flags.addSub = 0;		\

class GB_Z80_InstructionSet
{
public:
	void Call(void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*), uint8_t opcode, GB_Z80* cpu);
	void RegisterInstructions(GB_Z80* cpu);
	void RegisterExtendedInstructions(GB_Z80* cpu);

	void nop(uint8_t opcode, GB_Z80* cpu);
	void ld(uint8_t opcode, GB_Z80* cpu);
	void inc(uint8_t opcode, GB_Z80* cpu);
	void dec(uint8_t opcode, GB_Z80* cpu);
	void rlca(uint8_t opcode, GB_Z80* cpu);
	void rrca(uint8_t opcode, GB_Z80* cpu);
	void stop(uint8_t opcode, GB_Z80* cpu);
	void rla(uint8_t opcode, GB_Z80* cpu);
	void jr(uint8_t opcode, GB_Z80* cpu);
	void add(uint8_t opcode, GB_Z80* cpu);
	void push(uint8_t opcode, GB_Z80* cpu);
	void pop(uint8_t opcode, GB_Z80* cpu);
	void call(uint8_t opcode, GB_Z80* cpu);
	void ret(uint8_t opcode, GB_Z80* cpu);
	void jp(uint8_t opcode, GB_Z80* cpu);
	void rra(uint8_t opcode, GB_Z80* cpu);
	void rl(uint8_t opcode, GB_Z80* cpu);
	void rr(uint8_t opcode, GB_Z80* cpu);
	void ExtendedOpcodeHandler(uint8_t opcode, GB_Z80* cpu);

private:
	void (GB_Z80_InstructionSet::*instruction)(uint8_t, GB_Z80*);
};

#endif /* __INSTRUCTION_SET_H__ */