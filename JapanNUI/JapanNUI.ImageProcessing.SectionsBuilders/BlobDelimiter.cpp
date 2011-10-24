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

#define pixelAt(data, line, row) data[pixel((line), (row))] | data[pixel((line), (row))+1] << 8 | data[pixel((line), (row))+2] << 16
#define pixelSet(data, line, row, value) data[pixel(line, row)] = (value); data[pixel(line,row)+1] = (value) >> 8; data[pixel(line, row)+2] = (value) >> 16
			
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

			void processData(int * blobIdsCorrespondanceData, unsigned char* data, unsigned char* processingIntermediateOutput, int lines, int rows, int stride, int * nonNullPixels, int * maxBlobs)
			{
				(*nonNullPixels) = 0;

				int current_blob_id = 0;
				int label;

				for(int line = 0;line < lines;line++)
				{
					for(int row = 0;row < rows;row++)
					{
						int current = pixelAt(data, line, row);

						if(current != 0)
						{
							(*nonNullPixels)++;

							int west = row > 0 ? pixelAt(processingIntermediateOutput, line, row - 1) : 0;
							int north = line > 0 ? pixelAt(processingIntermediateOutput, line-1, row) : 0;
							int northwest = (line > 0 && row > 0) ? pixelAt(processingIntermediateOutput, line-1, row-1) : 0;
							int northeast = (line > 0 && row < rows - 1) ? pixelAt(processingIntermediateOutput, line-1, row+1) : 0;

							int west_v = west > 0 ? pixelAt(data, line, row - 1) : 0;
							int north_v = north > 0 ? pixelAt(data, line-1, row) : 0;
							int northwest_v = northwest > 0 ? pixelAt(data, line-1,row-1) : 0;
							int northeast_v = northeast > 0 ? pixelAt(data, line-1, row+1) : 0;

							west *= (west_v) == current ? 1 : 0;
							north *= (north_v) == current ? 1 : 0;
							northwest *= (northwest_v) == current ? 1 : 0;
							northeast *= (northeast_v) == current ? 1 : 0;

							int smallestLabel = min4NonNull(west, north, northwest, northeast);

							if(smallestLabel > 0)
							{
								blobIdsCorrespondanceData[label] = min(label, smallestLabel);

								label = smallestLabel;

								//TODO("Je pense qu'il y a une erreur ici, car je dois perdre certaines chaines");
								//TODO("Il faudrait peut être explicitement lister avec un std::set<T> et pas juste faire ça");

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

							pixelSet(processingIntermediateOutput, line, row, label);
						}
						else
							pixelSet(processingIntermediateOutput, line, row, 0);
					}
				}

				(*maxBlobs) = current_blob_id;
			}

#define min2(a,b) ((a) < (b)) ? (a) : (b)
#define max2(a,b) ((a) > (b)) ? (a) : (b)

			void match(Blob * blobs, int * blobIdsCorrespondanceData, unsigned char* data, unsigned char * processingIntermediateOutput, int lines, int rows, int stride, int blobsCount)
			{	
				memset(blobs, 0, sizeof(Blob) * lines * rows);

				for(int line = 0;line < lines;line++)
				{
					for(int row = 0;row < rows;row++)
					{
						int blob = pixelAt(processingIntermediateOutput, line, row);

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

							if(blobs[c_blob].PixelCount <= 0)
							{
								/* init */
								blobs[c_blob].MinX = rows;
								blobs[c_blob].MaxX = 0;
								blobs[c_blob].MinY = lines;
								blobs[c_blob].MaxY = 0;
							}

							blobs[c_blob].PixelCount++;

							blobs[c_blob].MinX = min2(blobs[c_blob].MinX, row);
							blobs[c_blob].MaxX = max2(blobs[c_blob].MaxX, row);

							blobs[c_blob].MinY = min2(blobs[c_blob].MinY, line);
							blobs[c_blob].MaxY = max2(blobs[c_blob].MaxY, line);

							pixelSet(data, line, row, c_blob * 150);
						}
						else
							pixelSet(data, line, row, 0);
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
				processingIntermediateOutput = new unsigned char [lines * rows * stride];
				blobs = new Blob [lines * rows];
			}

			BlobDelimiter::~BlobDelimiter(void)
			{
				delete blobIdsCorrespondanceData;
			}

			int BlobDelimiter::BuildBlobs(unsigned char* data)
			{
				int nonNull, maxBlobs;

				memset(blobIdsCorrespondanceData, 0, lines * rows);

				processData(blobIdsCorrespondanceData, data, processingIntermediateOutput, lines, rows, stride, &nonNull, &maxBlobs);

				match(blobs, blobIdsCorrespondanceData, data, processingIntermediateOutput, lines, rows, stride, maxBlobs);

				return maxBlobs;
			}
		}
	}
}
