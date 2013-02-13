#include "common.h"

GB_ROM::GB_ROM(const char* file)
{
	mFileHandler = fopen(file, "wb+");

	if(mFileHandler == NULL)
	{
		throw new GB_ROM_Exception("Cannot open file", file);
	}
}

GB_ROM::~GB_ROM()
{
	if(mFileHandler != NULL)
	{
		fclose(mFileHandler);
	}
}

rom_data_t* GB_ROM::read()
{
	rom_data_t* romData = new rom_data_t;

	fseek(mFileHandler, 0, SEEK_END);
	int end = ftell(mFileHandler);
	fseek(mFileHandler, 0, 0);

	// fill the structure data
	romData->data = new uint8_t[end];
	romData->size = end;

	// now read the file
	fread(&romData->data[0], end, 1, mFileHandler);

	// return the struct with the data
	return romData;
}