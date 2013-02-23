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
	uint8_t ldInstructions[] = {
									0x01, 0x02, 0x06, 0x08, 0x0A, 0x0E, 0x11, 0x12, 0x16, 0x1A, 0x1E,
									0x21, 0x22, 0x26, 0x2A, 0x2E, 0x31, 0x32, 0x36, 0x3A, 0x3E, 0x40,
									0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B,
									0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56,
									0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F, 0x60, 0x61,
									0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C,
									0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x77, 0x78,
									0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F
								};

	uint8_t incInstructions[] = { 0x03, 0x04, 0x0C, 0x13, 0x14, 0x1C, 0x23, 0x24, 0x2C, 0x33, 0x34, 0x3C };
	uint8_t decInstructions[] = { 0x05, 0x0B, 0x0D, 0x15, 0x1B, 0x1D, 0x25, 0x2B, 0x2D, 0x35, 0x3B, 0x3D };
	
	// these loops makes our lifes easier
	for(uint8_t cur = 0; cur < sizeof(nopInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(nopInstructions[cur], cpu, this, &GB_Z80_InstructionSet::nop);
	}

	for(uint8_t cur = 0; cur < sizeof(ldInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(ldInstructions[cur], cpu, this, &GB_Z80_InstructionSet::ld);
	}

	for(uint8_t cur = 0; cur < sizeof(incInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(incInstructions[cur], cpu, this, &GB_Z80_InstructionSet::inc);
	}

	for(uint8_t cur = 0; cur < sizeof(decInstructions); cur ++)
	{
		cpu->mInstructionHandler->RegisterHandler(decInstructions[cur], cpu, this, &GB_Z80_InstructionSet::dec);
	}
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
			break;

		case 0x02: // ld (bc), a
			cpu->mMemory[cpu->mRegisters.bc] = cpu->mRegisters.af.a;
			break;

		case 0x06: // ld b, n
			SET_HIGH(cpu->mRegisters.bc, cpu->readFromPC());
			break;

		case 0x08: // ld (nn), sp
			{
				((uint16_t*)(&cpu->mMemory[cpu->readFromPC() | (cpu->readFromPC() << 8)]))[0] = cpu->mRegisters.sp;
			}
			break;

		case 0x0A: // ld a, (bc)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.bc];
			break;

		case 0x0E: // ld c, n
			SET_LOW(cpu->mRegisters.bc, cpu->readFromPC());
			break;

		case 0x11: // ld de, nn
			SET(cpu->mRegisters.de, cpu->readFromPC(), cpu->readFromPC());
			break;

		case 0x12: // ld (de), a
			cpu->mMemory[cpu->mRegisters.de] = cpu->mRegisters.af.a;
			break;

		case 0x16: // ld d, n
			SET_HIGH(cpu->mRegisters.de, cpu->readFromPC());
			break;

		case 0x1A: // ld a, (de)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.de];
			break;

		case 0x1E: // ld e, n
			SET_LOW(cpu->mRegisters.de, cpu->readFromPC());
			break;

		case 0x21: // ld hl, nn
			SET(cpu->mRegisters.hl, cpu->readFromPC(), cpu->readFromPC());
			break;

		case 0x22: // ld (hl+), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			// TODO: inc hl here
			break;

		case 0x26: // ld h, n
			SET_HIGH(cpu->mRegisters.hl, cpu->readFromPC());
			break;

		case 0x2A: // ld a, (hl+)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			// TODO: inc hl here
			break;

		case 0x31: // ld sp, nn
			SET(cpu->mRegisters.sp, cpu->readFromPC(), cpu->readFromPC());
			break;

		case 0x32: // ld (hl-), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			// TODO: dec hl here
			break;

		case 0x36: // ld (hl), n
			cpu->mMemory[cpu->mRegisters.hl] = cpu->readFromPC();
			break;

		case 0x3A: // ld a, (hl-)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			// TODO: dec hl here
			break;

		case 0x3E: // ld a, n
			cpu->mRegisters.af.a = cpu->readFromPC();
			break;

		case 0x40: // ld b, b
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.bc));
			break;
			
		case 0x41: // ld b, c
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.bc));
			break;

		case 0x42: // ld b, d
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.de));
			break;

		case 0x43: // ld b, e
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.de));
			break;

		case 0x44: // ld b, h
			SET_HIGH(cpu->mRegisters.bc, HIGH(cpu->mRegisters.hl));
			break;

		case 0x45: // ld b, l
			SET_HIGH(cpu->mRegisters.bc, LOW(cpu->mRegisters.hl));
			break;

		case 0x46: // ld b, (hl)
			SET_HIGH(cpu->mRegisters.bc, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x47: // ld b, a
			SET_HIGH(cpu->mRegisters.bc, cpu->mRegisters.af.a);
			break;

		case 0x48: // ld c, b
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.bc));
			break;

		case 0x49: // ld c, c
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.bc));
			break;

		case 0x4A: // ld c, d
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.de));
			break;

		case 0x4B: // ld c, e
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.de));
			break;

		case 0x4C: // ld c, h
			SET_LOW(cpu->mRegisters.bc, HIGH(cpu->mRegisters.hl));
			break;

		case 0x4D: // ld c, l
			SET_LOW(cpu->mRegisters.bc, LOW(cpu->mRegisters.hl));
			break;

		case 0x4E: // ld c, (hl)
			SET_LOW(cpu->mRegisters.bc, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x4F: // ld c, a
			SET_LOW(cpu->mRegisters.bc, cpu->mRegisters.af.a);
			break;

		case 0x50: // ld d, b
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.bc));
			break;

		case 0x51: // ld d, c
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.bc));
			break;

		case 0x52: // ld d, d
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.de));
			break;

		case 0x53: // ld d, e
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.de));
			break;

		case 0x54: // ld d, h
			SET_HIGH(cpu->mRegisters.de, HIGH(cpu->mRegisters.hl));
			break;

		case 0x55: // ld d, l
			SET_HIGH(cpu->mRegisters.de, LOW(cpu->mRegisters.hl));
			break;

		case 0x56: // ld d, (hl)
			SET_HIGH(cpu->mRegisters.de, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x57: // ld d, a
			SET_HIGH(cpu->mRegisters.de, cpu->mRegisters.af.a);
			break;

		case 0x58: // ld e, b
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.bc));
			break;

		case 0x59: // ld e, c
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.bc));
			break;

		case 0x5A: // ld e, d
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.de));
			break;

		case 0x5B: // ld e, e
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.de));
			break;

		case 0x5C: // ld e, h
			SET_LOW(cpu->mRegisters.de, HIGH(cpu->mRegisters.hl));
			break;

		case 0x5D: // ld e, l
			SET_LOW(cpu->mRegisters.de, LOW(cpu->mRegisters.hl));
			break;

		case 0x5E: // ld e, (hl)
			SET_LOW(cpu->mRegisters.de, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x5F: // ld e, a
			SET_LOW(cpu->mRegisters.de, cpu->mRegisters.af.a);
			break;

		case 0x60: // ld h, b
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.bc));
			break;

		case 0x61: // ld h, c
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.bc));
			break;

		case 0x62: // ld h, d
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.de));
			break;

		case 0x63: // ld h, e
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.de));
			break;

		case 0x64: // ld h, h
			SET_HIGH(cpu->mRegisters.hl, HIGH(cpu->mRegisters.hl));
			break;

		case 0x65: // ld h, l
			SET_HIGH(cpu->mRegisters.hl, LOW(cpu->mRegisters.hl));
			break;

		case 0x66: // ld h, (hl)
			SET_HIGH(cpu->mRegisters.hl, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x67: // ld h, a
			SET_HIGH(cpu->mRegisters.hl, cpu->mRegisters.af.a);
			break;

		case 0x68: // ld l, b
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.bc));
			break;

		case 0x69: // ld l, c
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.bc));
			break;

		case 0x6A: // ld l, d
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.de));
			break;

		case 0x6B: // ld l, e
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.de));
			break;

		case 0x6C: // ld l, h
			SET_LOW(cpu->mRegisters.hl, HIGH(cpu->mRegisters.hl));
			break;

		case 0x6D: // ld l, l
			SET_LOW(cpu->mRegisters.hl, LOW(cpu->mRegisters.hl));
			break;

		case 0x6E: // ld l, (hl)
			SET_LOW(cpu->mRegisters.hl, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x6F: // ld l, a
			SET_LOW(cpu->mRegisters.hl, cpu->mRegisters.af.a);
			break;

		case 0x70: // ld (hl), b
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.bc);
			break;

		case 0x71: // ld (hl), c
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.bc);
			break;

		case 0x72: // ld (hl), d
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.de);
			break;

		case 0x73: // ld (hl), e
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.de);
			break;

		case 0x74: // ld (hl), h
			cpu->mMemory[cpu->mRegisters.hl] = HIGH(cpu->mRegisters.hl);
			break;

		case 0x75: // ld (hl), l
			cpu->mMemory[cpu->mRegisters.hl] = LOW(cpu->mRegisters.hl);
			break;

		case 0x77: // ld (hl), a
			cpu->mMemory[cpu->mRegisters.hl] = cpu->mRegisters.af.a;
			break;

		case 0x78: // ld a, b
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.bc);
			break;

		case 0x79: // ld a, c
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.bc);
			break;

		case 0x7A: // ld a, d
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.de);
			break;

		case 0x7B: // ld a, e
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.de);
			break;

		case 0x7C: // ld a, h
			cpu->mRegisters.af.a = HIGH(cpu->mRegisters.hl);
			break;

		case 0x7D: // ld a, l
			cpu->mRegisters.af.a = LOW(cpu->mRegisters.hl);
			break;

		case 0x7E: // ld a, (hl)
			cpu->mRegisters.af.a = cpu->mMemory[cpu->mRegisters.hl];
			break;

		case 0x7F: // ld a, a
			cpu->mRegisters.af.a = cpu->mRegisters.af.a;
			break;

		default:
			cpu->mTicks -= 4;
	}

	cpu->mTicks += 4;
}

void GB_Z80_InstructionSet::inc(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x03: // inc bc
			cpu->mRegisters.bc ++;
			break;

		case 0x04: // inc b
			INC8(cpu, HIGH(cpu->mRegisters.bc));
			break;

		case 0x0C: // inc c
			INC8(cpu, LOW(cpu->mRegisters.bc));
			break;

		case 0x13: // inc de
			cpu->mRegisters.de ++;
			break;

		case 0x14: // inc d
			INC8(cpu, HIGH(cpu->mRegisters.de));
			break;

		case 0x1C: // inc e
			INC8(cpu, LOW(cpu->mRegisters.de));
			break;

		case 0x23: // inc hl
			cpu->mRegisters.hl ++;
			break;

		case 0x24: // inc h
			INC8(cpu, HIGH(cpu->mRegisters.hl));
			break;

		case 0x2C: // inc l
			INC8(cpu, LOW(cpu->mRegisters.hl));
			break;

		case 0x33: // inc sp
			cpu->mRegisters.sp ++;
			break;

		case 0x34: // inc (hl)
			INC8(cpu, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x3C: // inc a
			INC8(cpu, cpu->mRegisters.af.a);
			break;
	}
}

void GB_Z80_InstructionSet::dec(uint8_t opcode, GB_Z80* cpu)
{
	switch(opcode)
	{
		case 0x05: // dec b
			DEC8(cpu, HIGH(cpu->mRegisters.bc));
			break;

		case 0x0B: // dec bc
			cpu->mRegisters.bc --;
			break;

		case 0x0D: // dec c
			DEC8(cpu, LOW(cpu->mRegisters.bc));
			break;

		case 0x15: // dec d
			DEC8(cpu, HIGH(cpu->mRegisters.de));
			break;

		case 0x1B: // dec de
			cpu->mRegisters.de --;
			break;

		case 0x1D: // dec e
			DEC8(cpu, LOW(cpu->mRegisters.de));
			break;

		case 0x25: // dec h
			DEC8(cpu, HIGH(cpu->mRegisters.hl));
			break;

		case 0x2B: // dec hl
			cpu->mRegisters.hl --;
			break;

		case 0x2D: // dec l
			DEC8(cpu, LOW(cpu->mRegisters.hl));
			break;

		case 0x35: // dec (hl)
			DEC8(cpu, cpu->mMemory[cpu->mRegisters.hl]);
			break;

		case 0x3B: // dec sp
			cpu->mRegisters.sp --;
			break;

		case 0x3D: // dec a
			DEC8(cpu, cpu->mRegisters.af.a);
			break;
	}
}