#include "StdAfx.h"
#include "BlobDelimiter.h"
#include <memory>
#include <algorithm>

using namespace std;

#define MANAGED_DEBUG

namespace JapanNUI
{
	namespace ImageProcessing
	{
		namespace SectionsBuilders
		{

#ifndef MANAGED_DEBUG
#pragma managed(push, off)
#endif

#define sum3(a,b,c) (a)+(b)+(c)
#define pixel(line, row) (line) * (rows * stride) + (row) * stride
#define pixelAt(line, row) data[pixel((line), (row))] | data[pixel((line), (row))+1] << 8 | data[pixel((line), (row))+2] << 16
#define pixelSet(line, row, value) data[pixel(line, row)] = (value); data[pixel(line,row)+1] = (value) >> 8; data[pixel(line, row)+2] = (value) >> 16

			void resetState(int * ids, int count)
			{
				memset(ids, 0, count);
			}

			inline int getBlobAt(unsigned char* data, int line, int row, int rows, int stride)
			{
				int prev_address = pixel(line, row);
				return data[prev_address + 0] | data[prev_address + 1] << 8 | data[prev_address + 2] << 16;
			}

			inline int min4NonNull(int a, int b, int c, int d)
			{
				if(a <= 0)
					a = INT_MAX;

				if(b <= 0)
					b = INT_MAX;

				if(c <= 0)
					c = INT_MAX;

				if(d <= 0)
					d = INT_MAX;

				int res = min(a, min(b,min(c, d)));

				if(res == INT_MAX)
					return 0;
				else
					return res;
			}

			void processData(int * blobIdsCorrespondanceData, unsigned char* data, int lines, int rows, int stride, int * nonNullPixels)
			{
				(*nonNullPixels) = 0;
				
				int current_blob_id = 0;
				int label;

				for(int line = 0;line < lines;line++)
				{
					for(int row = 0;row < rows;row++)
					{
						int current = pixelAt(line, row);

						if(current != 0)
						{
							(*nonNullPixels)++;

							int west = row > 0 ? pixelAt(line, row - 1) : 0;
							int north = line > 0 ? pixelAt(line-1, row) : 0;
							int northwest = (line > 0 && row > 0) ? pixelAt(line-1, row-1) : 0;
							int northeast = (line > 0 && row < rows - 1) ? pixelAt(line-1, row+1) : 0;

							
							if(northeast != 0 && north == 0)
							{
								int j = 5;
							}

							int smallestLabel = min4NonNull(west, north, northwest, northeast);

							if(smallestLabel > 0)
							{
								blobIdsCorrespondanceData[label] = min(label, smallestLabel);

								label = smallestLabel;

								blobIdsCorrespondanceData[west] = min(label, blobIdsCorrespondanceData[west]);
								blobIdsCorrespondanceData[north] = min(label, blobIdsCorrespondanceData[north]);
								blobIdsCorrespondanceData[northwest] = min(label, blobIdsCorrespondanceData[northwest]);
								blobIdsCorrespondanceData[northeast] = min(label, blobIdsCorrespondanceData[northeast]);
							}
							else
							{
								current_blob_id++;
								label = current_blob_id;

								blobIdsCorrespondanceData[label] = label;
							}
							

							pixelSet(line, row, label);
						}
					}
				}
			}

			void match(int * blobIdsCorrespondanceData,unsigned char* data, int lines, int rows, int stride)
			{
				
				for(int line = 0;line < lines;line++)
				{
					if(line > 154)
					{
						line++;
						line--;
					}

					for(int row = 0;row < rows;row++)
					{
						int address = pixel(line, row);

						int blob = data[address + 0] | data[address + 1] << 8 | data[address + 2] << 16;
						if(blob > 0)
						{
							int c_blob = blob;

							while(true)
							{
								int test = blobIdsCorrespondanceData[c_blob];
								if(test == c_blob)
									break;
								else
									c_blob = test;
							}

							c_blob *= 40;

							data[address + 0] = (unsigned char)(c_blob);
							data[address + 1] = (unsigned char)(c_blob >> 8);
							data[address + 2] = (unsigned char)(c_blob >> 16);
						}
					}
				}
				
			}

#ifndef MANAGED_DEBUG
#pragma managed(pop)
#endif

			BlobDelimiter::BlobDelimiter(int lines, int rows, int stride)
			{
				this->lines = lines;
				this->rows = rows;
				this->stride = stride;

				blobIdsCorrespondanceData = new int [lines * rows];
			}

			BlobDelimiter::~BlobDelimiter(void)
			{
				delete blobIdsCorrespondanceData;
			}

			void BlobDelimiter::BuildBlobs(unsigned char* data, List<Blob^>^ result)
			{
				int nonNull;

				resetState(blobIdsCorrespondanceData, lines * rows);

				processData(blobIdsCorrespondanceData, data, lines, rows, stride, &nonNull);

				match(blobIdsCorrespondanceData, data, lines, rows, stride);
			}
		}
	}
}
