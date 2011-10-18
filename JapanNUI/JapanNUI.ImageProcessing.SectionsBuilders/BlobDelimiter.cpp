#include "StdAfx.h"
#include "BlobDelimiter.h"

namespace JapanNUI
{
	namespace ImageProcessing
	{
		namespace SectionsBuilders
		{

#pragma managed(push, off)

#define sum3(a,b,c) a+b+c

			void processData(unsigned char* data, int lines, int rows, int stride, int * nonNullPixels)
			{
				(*nonNullPixels) = 0;

				unsigned char r,g,b,a;

				for(int line = 0;line < lines;line++)
				{
					for(int row = 0;row < rows;row++)
					{
						int address = line * (rows * stride) + row * stride;

						r = data[address + 0];
						g = data[address + 1];
						b = data[address + 2];
						a = data[address + 3];

						int total = sum3(r,g,b);
						if(total > 0)
							(*nonNullPixels)++;
					}
				}
			}

#pragma managed(pop)

			BlobDelimiter::BlobDelimiter(void)
			{
			}

			void BlobDelimiter::BuildBlobs(unsigned char* data, int lines, int rows, int stride, List<Blob^>^ result)
			{
				int nonNull;
				processData(data, lines, rows, stride, &nonNull);
			}
		}
	}
}
