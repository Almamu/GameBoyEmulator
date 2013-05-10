#ifndef __BASE_ROM_H__
#define __BASE_ROM_H__

#include "common.h"

static uint8_t GB_ROM_baseLogo[] = { 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
									 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
									 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E };

static uint16_t GB_ROM_RST[] = { 0x0000, 0x0008, 0x0010, 0x0018, 0x0020, 0x0028, 0x0030, 0x0038 };
static uint16_t GB_ROM_IRQ[] = { 0x0040, 0x0048, 0x0050, 0x0058, 0x0060 };

struct rom_data_t
{
	uint8_t* data;
	size_t size;
};

class GB_ROM_Exception
{
public:
	GB_ROM_Exception(std::string message, std::string filename) : mMessage(message), mFilename(filename) {}
	~GB_ROM_Exception() {}

	std::string mMessage;
	std::string mFilename;
};

class GB_ROM
{
public:
	GB_ROM(const char* file);
	~GB_ROM();

	// this will read the whole rom to memory
	// probably not the best idea
	// but we need to do it this way
	// so we can later load it in memory
	// just memcpy'ing it
	rom_data_t* read();

private:

	FILE* mFileHandler;
};

#endif /* __BASE_ROM_H__ */
