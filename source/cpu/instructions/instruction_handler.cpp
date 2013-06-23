#include "common.h"

void GB_Z80_InstructionHandler::RegisterHandler(uint8_t opcode, GB_Z80* cpu, GB_Z80_InstructionSet* set, void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*))
{
	if(mHandlers.find(opcode) != mHandlers.end())
	{
		return; // handler already exists, do not overwrite it
	}

	instruction_handler_t* data = new instruction_handler_t;

	data->cpu = cpu;
	data->handler = handler;
	data->opcode = opcode;
	data->set = set;

	mHandlers.insert(std::make_pair(opcode, data));
}

void GB_Z80_InstructionHandler::RegisterExtendedHandler(uint8_t opcode, GB_Z80* cpu, GB_Z80_InstructionSet* set, void (GB_Z80_InstructionSet::*handler)(uint8_t, GB_Z80*))
{
	if(mExtendedHandlers.find(opcode) != mExtendedHandlers.end())
	{
		return; // handler already exists, do not overwrite it
	}

	instruction_handler_t* data = new instruction_handler_t;

	data->cpu = cpu;
	data->handler = handler;
	data->opcode = opcode;
	data->set = set;

	mExtendedHandlers.insert(std::make_pair(opcode, data));
}

void GB_Z80_InstructionHandler::UnregisterHandler(uint8_t opcode)
{
	if(mHandlers.find(opcode) != mHandlers.end())
	{
		mHandlers.erase(opcode);
	}
}

void GB_Z80_InstructionHandler::UnregisterExtendedHandler(uint8_t opcode)
{
	if(mExtendedHandlers.find(opcode) != mExtendedHandlers.end())
	{
		mExtendedHandlers.erase(opcode);
	}
}

bool GB_Z80_InstructionHandler::HandleOpcode(uint8_t opcode)
{
	std::map<uint8_t, instruction_handler_t*>::iterator cur = mHandlers.find(opcode);

	if(cur == mHandlers.end())
	{
		return false;
	}

	instruction_handler_t* handler = (*cur).second;

	handler->set->Call(handler->handler, handler->opcode, handler->cpu);

	return true;
}

bool GB_Z80_InstructionHandler::HandleExtendedOpcode(uint8_t opcode)
{
	std::map<uint8_t, instruction_handler_t*>::iterator cur = mExtendedHandlers.find(opcode);

	if(cur == mExtendedHandlers.end())
	{
		return false;
	}

	instruction_handler_t* handler = (*cur).second;

	handler->set->Call(handler->handler, handler->opcode, handler->cpu);

	return true;
}

std::map<uint8_t, instruction_handler_t*> GB_Z80_InstructionHandler::mHandlers;
std::map<uint8_t, instruction_handler_t*> GB_Z80_InstructionHandler::mExtendedHandlers;