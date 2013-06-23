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
	uint8_t nopInstructions[] = {	0x00, 0xD3, 0xD8, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xF4, 0xFC, 0xFD };
	uint8_t ldInstructions[] = {	// TODO: ADD IO OPCODES
									0x01, 0x02, 0x06, 0x08, 0x0A, 0x0E, 0x11, 0x12, 0x16, 0x1A, 0x1E,
									0x21, 0x22, 0x26, 0x2A, 0x2E, 0x31, 0x32, 0x36, 0x3A, 0x3E, 0x40,
									0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B,
									0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56,
									0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F, 0x60, 0x61,
									0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C,
									0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x77, 0x78,
									0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0xEA, 0xFA, 0xF8, 0xF9
								};

	uint8_t incInstructions[] = { 0x03, 0x04, 0x0C, 0x13, 0x14, 0x1C, 0x23, 0x24, 0x2C, 0x33, 0x34, 0x3C };
	uint8_t decInstructions[] = { 0x05, 0x0B, 0x0D, 0x15, 0x1B, 0x1D, 0x25, 0x2B, 0x2D, 0x35, 0x3B, 0x3D };
	uint8_t addInstructions[] = { 0x09, 0x19, 0x29, 0x39, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0xC6 };
	uint8_t pushInstructions[] = { 0xC5, 0xD5, 0xE5, 0xF5 };
	uint8_t popInstructions[] = { 0xC1, 0xD1, 0xE1, 0xF1 };
	uint8_t retInstructions[] = { 0xC0, 0xC8, 0xC9, 0xD0, 0xD8 };
	uint8_t callInstructions[] = { 0xC4, 0xCC, 0xCD, 0xD4, 0xDC };
	uint8_t jpInstructions[] = { 0xC3, 0xE9 };

	// these loops makes our lifes easier
	uint8_t cur;

	for(cur = 0; cur < sizeof(nopInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(nopInstructions[cur], cpu, this, &GB_Z80_InstructionSet::nop);
	}

	for(cur = 0; cur < sizeof(ldInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(ldInstructions[cur], cpu, this, &GB_Z80_InstructionSet::ld);
	}

	for(cur = 0; cur < sizeof(incInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(incInstructions[cur], cpu, this, &GB_Z80_InstructionSet::inc);
	}

	for(cur = 0; cur < sizeof(decInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(decInstructions[cur], cpu, this, &GB_Z80_InstructionSet::dec);
	}

	for(cur = 0; cur < sizeof(addInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(addInstructions[cur], cpu, this, &GB_Z80_InstructionSet::add);
	}

	for(cur = 0; cur < sizeof(pushInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(pushInstructions[cur], cpu, this, &GB_Z80_InstructionSet::push);
	}

	for(cur = 0; cur < sizeof(popInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(popInstructions[cur], cpu, this, &GB_Z80_InstructionSet::pop);
	}

	for(cur = 0; cur < sizeof(retInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(retInstructions[cur], cpu, this, &GB_Z80_InstructionSet::ret);
	}

	for(cur = 0; cur < sizeof(callInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(callInstructions[cur], cpu, this, &GB_Z80_InstructionSet::call);
	}

	for(cur = 0; cur < sizeof(jpInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(jpInstructions[cur], cpu, this, &GB_Z80_InstructionSet::jp);
	}

	// RLCA
	cpu->mInstructionHandler->RegisterHandler(0x07, cpu, this, &GB_Z80_InstructionSet::rlca);

	// RRCA
	cpu->mInstructionHandler->RegisterHandler(0x0F, cpu, this, &GB_Z80_InstructionSet::rrca);

	// STOP
	cpu->mInstructionHandler->RegisterHandler(0x10, cpu, this, &GB_Z80_InstructionSet::stop);

	// RLA
	cpu->mInstructionHandler->RegisterHandler(0x17, cpu, this, &GB_Z80_InstructionSet::rla);

	// RRA
	cpu->mInstructionHandler->RegisterHandler(0x1F, cpu, this, &GB_Z80_InstructionSet::rra);

	// JR
	cpu->mInstructionHandler->RegisterHandler(0x18, cpu, this, &GB_Z80_InstructionSet::jr);
}

void GB_Z80_InstructionSet::nop(uint8_t opcode, GB_Z80* cpu)
{
	// do nothing but increase the ticks
	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::ld(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x01: // ld bc, nn
			SET(cpu->mRegisters.bc, cpu->readFromPC(), cpu->readFromPC());
			cpu->mTicks += 12;
			break;

		case 0x02: // ld (bc), a
			cpu->mMemory[cpu->mRegisters.bc] = cpu->mRegisters.af.a;
			cpu->mTicks += 8;
			break;

		case 0x06: // ld b, n
			SET_HIGH(cpu->mRegisters.bc, cpu->readFromPC());
			cpu->mTicks += 8;
			break;

		case 0x08: // ld (nn), sp
			((uint16_t*)(&cpu->mMemory[cpu->readFromPC() | (cpu->readFromPC() << 8)]))[0] = cpu->mRegisters.sp;
			cpu->mTicks += 20;
			break;

		case 0x0A: // ld a, (bc)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.bc];
			cpu->mTicks += 8;
			break;

		case 0x0E: // ld c, n
			SET_LOW(cpu->mRegisters.bc, cpu->readFromPC());
			cpu->mTicks += 8;
			break;

		case 0x11: // ld de, nn
			SET(cpu->mRegisters.de, cpu->readFromPC(), cpu->readFromPC());
			cpu->mTicks += 12;
			break;

		case 0x12: // ld (de), a
			cpu->mMemory[cpu->mRegisters.de] = cpu->mRegisters.af.a;
			cpu->mTicks += 12;
			break;

		case 0x16: // ld d, n
			SET_HIGH(cpu->mRegisters.de, cpu->readFromPC());
			cpu->mTicks += 8;
			break;

		case 0x1A: // ld a, (de)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.de];
			cpu->mTicks += 8;
			break;

		case 0x1E: // ld e, n
			SET_LOW(cpu->mRegisters.de, cpu->readFromPC());
			cpu->mTicks += 4;
			break;

		case 0x21: // ld hl, nn
			SET(cpu->mRegisters.hl, cpu->readFromPC(), cpu->readFromPC());
			cpu->mTicks += 12;
			break;

		case 0x22: // ldi (hl), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			cpu->mRegisters.hl ++;
			cpu->mTicks += 8;
			break;

		case 0x26: // ld h, n
			SET_HIGH(cpu->mRegisters.hl, cpu->readFromPC());
			cpu->mTicks += 8;
			break;

		case 0x2A: // ldi a, (hl)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			cpu->mRegisters.hl ++;
			cpu->mTicks += 8;
			break;

		case 0x31: // ld sp, nn
			SET(cpu->mRegisters.sp, cpu->readFromPC(), cpu->readFromPC());
			cpu->mTicks += 12;
			break;

		case 0x32: // ldd (hl), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			cpu->mRegisters.hl --;
			cpu->mTicks += 8;
			break;

		case 0x36: // ld (hl), n
			cpu->mMemory[cpu->mRegisters.hl] = cpu->readFromPC();
			cpu->mTicks += 12;
			break;

		case 0x3A: // ldd a, (hl)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			cpu->mRegisters.hl --;
			cpu->mTicks += 8;
			break;

		case 0x3E: // ld a, n
			cpu->mRegisters.af.a = cpu->readFromPC();
			cpu->mTicks += 8;
			break;

		case 0x40: // ld b, b
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;
			
		case 0x41: // ld b, c
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x42: // ld b, d
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x43: // ld b, e
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x44: // ld b, h
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x45: // ld b, l
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x46: // ld b, (hl)
			SET_HIGH(cpu->mRegisters.bc, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x47: // ld b, a
			SET_HIGH(cpu->mRegisters.bc, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x48: // ld c, b
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x49: // ld c, c
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x4A: // ld c, d
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x4B: // ld c, e
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x4C: // ld c, h
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x4D: // ld c, l
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x4E: // ld c, (hl)
			SET_LOW(cpu->mRegisters.bc, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x4F: // ld c, a
			SET_LOW(cpu->mRegisters.bc, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x50: // ld d, b
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x51: // ld d, c
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x52: // ld d, d
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x53: // ld d, e
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x54: // ld d, h
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x55: // ld d, l
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x56: // ld d, (hl)
			SET_HIGH(cpu->mRegisters.de, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x57: // ld d, a
			SET_HIGH(cpu->mRegisters.de, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x58: // ld e, b
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x59: // ld e, c
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x5A: // ld e, d
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x5B: // ld e, e
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x5C: // ld e, h
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x5D: // ld e, l
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x5E: // ld e, (hl)
			SET_LOW(cpu->mRegisters.de, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x5F: // ld e, a
			SET_LOW(cpu->mRegisters.de, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x60: // ld h, b
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x61: // ld h, c
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x62: // ld h, d
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x63: // ld h, e
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x64: // ld h, h
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x65: // ld h, l
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x66: // ld h, (hl)
			SET_HIGH(cpu->mRegisters.hl, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x67: // ld h, a
			SET_HIGH(cpu->mRegisters.hl, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x68: // ld l, b
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x69: // ld l, c
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x6A: // ld l, d
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x6B: // ld l, e
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x6C: // ld l, h
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x6D: // ld l, l
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x6E: // ld l, (hl)
			SET_LOW(cpu->mRegisters.hl, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x6F: // ld l, a
			SET_LOW(cpu->mRegisters.hl, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0x70: // ld (hl), b
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.bc);
			cpu->mTicks += 8;
			break;

		case 0x71: // ld (hl), c
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.bc);
			cpu->mTicks += 8;
			break;

		case 0x72: // ld (hl), d
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.de);
			cpu->mTicks += 8;
			break;

		case 0x73: // ld (hl), e
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.de);
			cpu->mTicks += 8;
			break;

		case 0x74: // ld (hl), h
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.hl);
			cpu->mTicks += 8;
			break;

		case 0x75: // ld (hl), l
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.hl);
			cpu->mTicks += 8;
			break;

		case 0x77: // ld (hl), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			cpu->mTicks += 8;
			break;

		case 0x78: // ld a, b
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.bc);
			cpu->mTicks += 4;
			break;

		case 0x79: // ld a, c
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.bc);
			cpu->mTicks += 4;
			break;

		case 0x7A: // ld a, d
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.de);
			cpu->mTicks += 4;
			break;

		case 0x7B: // ld a, e
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.de);
			cpu->mTicks += 4;
			break;

		case 0x7C: // ld a, h
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.hl);
			cpu->mTicks += 4;
			break;

		case 0x7D: // ld a, l
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.hl);
			cpu->mTicks += 4;
			break;

		case 0x7E: // ld a, (hl)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			cpu->mTicks += 8;
			break;

		case 0x7F: // ld a, a
			cpu->mRegisters.af.a = cpu->mRegisters.af.a;
			cpu->mTicks += 4;
			break;

		case 0xEA: // ld (nn), a
			{
				uint16_t addr = 0;

				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				cpu->mMemory[addr] = cpu->mRegisters.af.a;
				cpu->mTicks += 16;
			}
			break;

		case 0xFA: // ld a, (nn)
			{
				uint16_t addr = 0;

				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				cpu->mRegisters.af.a = cpu->mMemory[addr];
				cpu->mTicks += 16;
			}
			break;

		case 0xF8: // ld hl, sp + dd
			{
				int8_t dif = (int8_t)cpu->readFromPC();

				// check carry and half carry flags first
				if(cpu->mRegisters.sp & 0xF <= dif & 0xF)
				{
					cpu->mRegisters.af.flags.halfCarry = 1;
				}
				else
				{
					cpu->mRegisters.af.flags.halfCarry = 0;
				}

				if(cpu->mRegisters.sp & 0xFF <= dif & 0xFF)
				{
					cpu->mRegisters.af.flags.carry = 1;
				}
				else
				{
					cpu->mRegisters.af.flags.carry = 0;
				}

				cpu->mRegisters.hl = cpu->mRegisters.sp + (int8_t)cpu->readFromPC();
				cpu->mTicks += 12;

				cpu->mRegisters.af.flags.zero = 0;
				cpu->mRegisters.af.flags.addSub = 0;
			}
			break;

		case 0xF9: // ld sp, hl
			cpu->mRegisters.sp = cpu->mRegisters.hl;
			cpu->mTicks += 8;
			break;
	}
}

void GB_Z80_InstructionSet::inc(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x03: // inc bc
			INC16(cpu, cpu->mRegisters.bc);
			cpu->mTicks += 8;
			break;

		case 0x04: // inc b
			INC8(cpu, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x0C: // inc c
			INC8(cpu, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x13: // inc de
			INC16(cpu, cpu->mRegisters.de);
			cpu->mTicks += 8;
			break;

		case 0x14: // inc d
			INC8(cpu, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x1C: // inc e
			INC8(cpu, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x23: // inc hl
			INC16(cpu, cpu->mRegisters.hl);
			cpu->mTicks += 8;
			break;

		case 0x24: // inc h
			INC8(cpu, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x2C: // inc l
			INC8(cpu, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x33: // inc sp
			INC16(cpu, cpu->mRegisters.sp);
			cpu->mTicks += 8;
			break;

		case 0x34: // inc (hl)
			INC8(cpu, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 12;
			break;

		case 0x3C: // inc a
			INC8(cpu, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;
	}

	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::dec(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x05: // dec b
			DEC8(cpu, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x0B: // dec bc
			DEC16(cpu, cpu->mRegisters.bc);
			cpu->mTicks += 8;
			break;

		case 0x0D: // dec c
			DEC8(cpu, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x15: // dec d
			DEC8(cpu, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x1B: // dec de
			DEC16(cpu, cpu->mRegisters.de);
			cpu->mTicks += 8;
			break;

		case 0x1D: // dec e
			DEC8(cpu, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x25: // dec h
			DEC8(cpu, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x2B: // dec hl
			DEC16(cpu, cpu->mRegisters.hl);
			cpu->mTicks += 8;
			break;

		case 0x2D: // dec l
			DEC8(cpu, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x35: // dec (hl)
			DEC8(cpu, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 12;
			break;

		case 0x3B: // dec sp
			DEC16(cpu, cpu->mRegisters.sp);
			cpu->mTicks += 8;
			break;

		case 0x3D: // dec a
			DEC8(cpu, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;
	}

	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::rlca(uint8_t opcode, GB_Z80* cpu)
{
	uint8_t high = cpu->mRegisters.af.a & 0x80;

	cpu->mRegisters.af.a <<= 1;

	if(high == 0)
	{
		cpu->mRegisters.af.flags.carry = 0; // reset carry
	}
	else
	{
		cpu->mRegisters.af.flags.carry = 1; // set carry
		cpu->mRegisters.af.a |= 0x01; // keep old bit
	}

	// set flags to 0
	cpu->mRegisters.af.flags.halfCarry = 0;
	cpu->mRegisters.af.flags.addSub = 0;
	cpu->mRegisters.af.flags.zero = 0;

	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::rrca(uint8_t opcode, GB_Z80* cpu)
{
	uint8_t low = cpu->mRegisters.af.a & 0x01;

	cpu->mRegisters.af.a >>= 1;

	if(low == 0)
	{
		cpu->mRegisters.af.flags.carry = 0; // reset carry
	}
	else
	{
		cpu->mRegisters.af.flags.carry = 1; // set carry
		cpu->mRegisters.af.a |= 0x80; // keep old bit
	}

	// set flags to 0
	cpu->mRegisters.af.flags.halfCarry = 0;
	cpu->mRegisters.af.flags.addSub = 0;
	cpu->mRegisters.af.flags.zero = 0;

	cpu->mTicks += 4;
}


void GB_Z80_InstructionSet::stop(uint8_t opcode, GB_Z80* cpu)
{
	uint8_t mode = cpu->readFromPC();

	if(mode != 0)
	{
		char msg[256];

		sprintf(msg, "Unknown STOP method: 0x%x", mode);

		throw new GB_Z80_InstructionException(msg, cpu->mRegisters.pc - 1);
	}

	throw new GB_Z80_StopRequested("CPU in VERY low power", cpu->mRegisters.pc);
}

void GB_Z80_InstructionSet::rla(uint8_t opcode, GB_Z80* cpu)
{
	uint8_t high = cpu->mRegisters.af.a & 0x80;
	uint8_t carry = cpu->mRegisters.af.flags.carry;

	cpu->mRegisters.af.a <<= 1;

	if(high == 0)
	{
		cpu->mRegisters.af.flags.carry = 0; // reset carry
	}
	else
	{
		cpu->mRegisters.af.flags.carry = 1; // set carry
		cpu->mRegisters.af.a |= carry; // keep old bit
	}

	// set flags to 0
	cpu->mRegisters.af.flags.halfCarry = 0;
	cpu->mRegisters.af.flags.addSub = 0;
	cpu->mRegisters.af.flags.zero = 0;

	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::jr(uint8_t opcode, GB_Z80* cpu)
{
	// this cast will add the sign to relative address
	int8_t dif = (int8_t)cpu->readFromPC();

	cpu->mRegisters.pc += dif;
	cpu->mTicks += 12;
}

void GB_Z80_InstructionSet::add(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x09: // add hl, bc
			ADD16(cpu, cpu->mRegisters.hl, cpu->mRegisters.bc);
			cpu->mTicks += 8;
			break;

		case 0x19: // add hl, de
			ADD16(cpu, cpu->mRegisters.hl, cpu->mRegisters.de);
			cpu->mTicks += 8;
			break;

		case 0x29: // add hl, hl
			ADD16(cpu, cpu->mRegisters.hl, cpu->mRegisters.hl);
			cpu->mTicks += 8;
			break;

		case 0x39: // add hl, sp
			ADD16(cpu, cpu->mRegisters.hl, cpu->mRegisters.sp);
			cpu->mTicks += 8;
			break;

		case 0x80: // add a, b
			ADD8(cpu, cpu->mRegisters.af.a, HIGH(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x81: // add a, c
			ADD8(cpu, cpu->mRegisters.af.a, LOW(cpu->mRegisters.bc));
			cpu->mTicks += 4;
			break;

		case 0x82: // add a, d
			ADD8(cpu, cpu->mRegisters.af.a, HIGH(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x83: // add a, e
			ADD8(cpu, cpu->mRegisters.af.a, LOW(cpu->mRegisters.de));
			cpu->mTicks += 4;
			break;

		case 0x84: // add a, h
			ADD8(cpu, cpu->mRegisters.af.a, HIGH(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x85: // add a, l
			ADD8(cpu, cpu->mRegisters.af.a, LOW(cpu->mRegisters.hl));
			cpu->mTicks += 4;
			break;

		case 0x86: // add a, (hl)
			ADD8(cpu, cpu->mRegisters.af.a, cpu->mMemory[cpu->mRegisters.hl]);
			cpu->mTicks += 8;
			break;

		case 0x87: // add a, a
			ADD8(cpu, cpu->mRegisters.af.a, cpu->mRegisters.af.a);
			cpu->mTicks += 4;
			break;

		case 0xC6: // add a, n
			ADD8(cpu, cpu->mRegisters.af.a, cpu->readFromPC());
			cpu->mTicks += 8;
			break;
	}
}

void GB_Z80_InstructionSet::push(uint8_t opcode, GB_Z80* cpu)
{
	cpu->mRegisters.sp -= 2;

	switch(opcode)
	{
		case 0xC5: // push bc
			((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0] = cpu->mRegisters.bc;
			break;

		case 0xD5: // push de
			((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0] = cpu->mRegisters.de;
			break;

		case 0xE5: // push hl
			((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0] = cpu->mRegisters.hl;
			break;

		case 0xF5: // push af
			memcpy(&((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0], &cpu->mRegisters.af, sizeof(register_t));
			break;
	}

	cpu->mTicks += 16;
}

void GB_Z80_InstructionSet::pop(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0xC1: // pop bc
			cpu->mRegisters.bc = ((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0];
			break;

		case 0xD1: // pop de
			cpu->mRegisters.de = ((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0];
			break;

		case 0xE1: // pop hl
			cpu->mRegisters.hl = ((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0];
			break;

		case 0xF1: // pop af
			memcpy(&cpu->mRegisters.af, &((uint16_t*)(&cpu->mMemory[cpu->mRegisters.sp]))[0], sizeof(uint16_t));
			cpu->mRegisters.af.flags.unused = 0; // make sure this is always zero
			break;
	}

	cpu->mRegisters.sp += 2;
	cpu->mTicks += 12;
}

void GB_Z80_InstructionSet::call(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0xC4: // call nz, nn
			{
				// get call address
				uint16_t addr = 0;
				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				// check zero flag
				if(cpu->mRegisters.af.flags.zero == 0)
				{
					// ok, condition is true, push PC to stack
					cpu->mRegisters.sp -= 2;
					memcpy(&cpu->mMemory[cpu->mRegisters.sp], &cpu->mRegisters.pc, sizeof(register_t));

					// set PC to the readed address
					cpu->mRegisters.pc = addr;

					// ticks count
					cpu->mTicks += 24;
				}
				else // condition not met, continue workflow
				{
					// ticks count
					cpu->mTicks += 12;
				}
			}
			break;

		case 0xCC: // call z, nn
			{
				// get call address
				uint16_t addr = 0;
				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				// check zero flag
				if(cpu->mRegisters.af.flags.zero == 1)
				{
					// ok, condition is true, push PC to stack
					cpu->mRegisters.sp -= 2;
					memcpy(&cpu->mMemory[cpu->mRegisters.sp], &cpu->mRegisters.pc, sizeof(register_t));

					// set PC to the readed address
					cpu->mRegisters.pc = addr;

					// ticks count
					cpu->mTicks += 24;
				}
				else // condition not met, continue workflow
				{
					// ticks count
					cpu->mTicks += 12;
				}
			}
			break;

		case 0xCD:
			{
				// get call address
				uint16_t addr = 0;
				SET(addr, cpu->readFromPC(), cpu->readFromPC());
				
				// push PC to stack
				cpu->mRegisters.sp -= 2;
				memcpy(&cpu->mMemory[cpu->mRegisters.sp], &cpu->mRegisters.pc, sizeof(register_t));

				// set PC to readed address
				cpu->mRegisters.pc = addr;

				// ticks count
				cpu->mTicks += 24;
			}
			break;

		case 0xD4: // call nc, nn
			{
				// get call address
				uint16_t addr = 0;
				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				// check zero flag
				if(cpu->mRegisters.af.flags.carry == 0)
				{
					// ok, condition is true, push PC to stack
					cpu->mRegisters.sp -= 2;
					memcpy(&cpu->mMemory[cpu->mRegisters.sp], &cpu->mRegisters.pc, sizeof(register_t));

					// set PC to the readed address
					cpu->mRegisters.pc = addr;

					// ticks count
					cpu->mTicks += 24;
				}
				else // condition not met, continue workflow
				{
					// ticks count
					cpu->mTicks += 12;
				}
			}
			break;

		case 0xDC: // call c, nn
			{
				// get call address
				uint16_t addr = 0;
				SET(addr, cpu->readFromPC(), cpu->readFromPC());

				// check zero flag
				if(cpu->mRegisters.af.flags.carry == 1)
				{
					// ok, condition is true, push PC to stack
					cpu->mRegisters.sp -= 2;
					memcpy(&cpu->mMemory[cpu->mRegisters.sp], &cpu->mRegisters.pc, sizeof(register_t));

					// set PC to the readed address
					cpu->mRegisters.pc = addr;

					// ticks count
					cpu->mTicks += 24;
				}
				else // condition not met, continue workflow
				{
					// ticks count
					cpu->mTicks += 12;
				}
			}
			break;
	}
}

void GB_Z80_InstructionSet::ret(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0xC0: // ret nz
			if(cpu->mRegisters.af.flags.zero == 0)
			{
				// pop return address from stack and set PC
				memcpy(&cpu->mRegisters.pc, &cpu->mMemory[cpu->mRegisters.sp], sizeof(register_t));
				cpu->mRegisters.sp += 2;

				cpu->mTicks += 20;
			}
			else
			{
				cpu->mTicks += 8;
			}
			break;

		case 0xC8: // ret z
			if(cpu->mRegisters.af.flags.zero == 1)
			{
				// pop return address from stack and set PC
				memcpy(&cpu->mRegisters.pc, &cpu->mMemory[cpu->mRegisters.sp], sizeof(register_t));
				cpu->mRegisters.sp += 2;

				cpu->mTicks += 20;
			}
			else
			{
				cpu->mTicks += 8;
			}
			break;

		case 0xC9:
			// pop return address from stack and set PC
			memcpy(&cpu->mRegisters.pc, &cpu->mMemory[cpu->mRegisters.sp], sizeof(register_t));
			cpu->mRegisters.sp += 2;

			// ticks count
			cpu->mTicks += 16;
			break;

		case 0xD0: // ret nc
			if(cpu->mRegisters.af.flags.carry == 0)
			{
				// pop return address from stack and set PC
				memcpy(&cpu->mRegisters.pc, &cpu->mMemory[cpu->mRegisters.sp], sizeof(register_t));
				cpu->mRegisters.sp += 2;

				cpu->mTicks += 20;
			}
			else
			{
				cpu->mTicks += 8;
			}
			break;

		case 0xD8: // ret c
			if(cpu->mRegisters.af.flags.carry == 1)
			{
				// pop return address from stack and set PC
				memcpy(&cpu->mRegisters.pc, &cpu->mMemory[cpu->mRegisters.sp], sizeof(register_t));
				cpu->mRegisters.sp += 2;

				cpu->mTicks += 20;
			}
			else
			{
				cpu->mTicks += 8;
			}
			break;
	}
}

void GB_Z80_InstructionSet::jp(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0xC3: // jp nn
			SET(cpu->mRegisters.pc, cpu->readFromPC(), cpu->readFromPC());
			cpu->mTicks += 16;
			break;

		case 0xE9: // jp hl
			cpu->mRegisters.pc = cpu->mRegisters.hl;
			cpu->mTicks += 4;
			break;
	}
}

void GB_Z80_InstructionSet::rra(uint8_t opcode, GB_Z80* cpu)
{
	uint8_t low = cpu->mRegisters.af.a & 0x01;
	uint8_t carry = cpu->mRegisters.af.flags.carry;

	cpu->mRegisters.af.a >>= 1;

	if(low == 0)
	{
		cpu->mRegisters.af.flags.carry = 0; // reset carry
	}
	else
	{
		cpu->mRegisters.af.flags.carry = 1; // set carry
		cpu->mRegisters.af.a |= carry << 7; // keep carry bit
	}

	// set flags to 0
	cpu->mRegisters.af.flags.halfCarry = 0;
	cpu->mRegisters.af.flags.addSub = 0;
	cpu->mRegisters.af.flags.zero = 0;

	cpu->mTicks += 4;
}
