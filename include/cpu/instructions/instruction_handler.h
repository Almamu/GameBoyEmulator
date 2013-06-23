#ifndef __INSTRUCTION_HANDLER_H__
#define __INSTRUCTION_HANDLER_H__

#include "common.h"

class GB_Z80;

struct instruction_handler_t
{
	GB_Z80* cpu;
	GB_Z80_InstructionSet* set;
	uint8_t opcode;
	void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*);
};

class GB_Z80_InstructionException
{
public:
	GB_Z80_InstructionException(std::string message, uint16_t address) : mMessage(message), mAddress(address) {}
	~GB_Z80_InstructionException() {}

	std::string mMessage;
	uint16_t mAddress;
};

class GB_Z80_InstructionHandler
{
public:
	static void RegisterHandler(uint8_t opcode, GB_Z80* cpu, GB_Z80_InstructionSet* set, void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*));
	static void UnregisterHandler(uint8_t opcode);

	static void RegisterExtendedHandler(uint8_t opcode, GB_Z80* cpu, GB_Z80_InstructionSet* set, void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*));
	static void UnregisterExtendedHandler(uint8_t opcode);

	static bool HandleOpcode(uint8_t opcode);
	static bool HandleExtendedOpcode(uint8_t opcode);
private:
	static std::map<uint8_t, instruction_handler_t*> mHandlers;
	static std::map<uint8_t, instruction_handler_t*> mExtendedHandlers;
};

#endif /* __INSTRUCTION_HANDLER_H__ */