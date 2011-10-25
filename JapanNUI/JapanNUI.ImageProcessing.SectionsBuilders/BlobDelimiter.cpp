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

#define pixel(line, column) (line) * (columns * stride) + (column) * stride

#define pixelAt(data, line, column) data[pixel((line), (column))] | data[pixel((line), (column))+1] << 8 | data[pixel((line), (column))+2] << 16
#define pixelSet(data, line, column, value) data[pixel(line, column)] = (value); data[pixel(line,column)+1] = (value) >> 8; data[pixel(line, column)+2] = (value) >> 16

			inline int getBlobAt(unsigned char* data, int line, int column, int columns, int stride)
			{
				int prev_address = pixel(line, column);
				return data[prev_address + 0] | data[prev_address + 1] << 8 | data[prev_address + 2] << 16;
			}

			inline int min4NonNull(int a, int b, int c, int d, int * nonNullCount)
			{
				(*nonNullCount) = 4;

				if(a <= 0)
				{
					a = INT_MAX;
					(*nonNullCount)--;
				}

				if(b <= 0)
				{
					b = INT_MAX;
					(*nonNullCount)--;
				}

				if(c <= 0)
				{
					c = INT_MAX;
					(*nonNullCount)--;
				}

				if(d <= 0)
				{
					d = INT_MAX;
					(*nonNullCount)--;
				}

				int res = min(a, min(b,min(c, d)));

				if(res == INT_MAX)
					return 0;
				else
					return res;
			}

			void processData(int * blobIdsCorrespondanceData, unsigned char* data, unsigned char* processingIntermediateOutput, int lines, int columns, int stride, int * nonNullPixels, int * maxBlobs)
			{
				(*nonNullPixels) = 0;

				int current_blob_id = 0;
				int label;
				int nonNullCount;

				for(int line = 0;line < lines;line++)
				{
					for(int column = 0;column < columns;column++)
					{
						int current = pixelAt(data, line, column);

						if(current != 0)
						{
							(*nonNullPixels)++;

							int west = column > 0 ? pixelAt(processingIntermediateOutput, line, column - 1) : 0;
							int north = line > 0 ? pixelAt(processingIntermediateOutput, line-1, column) : 0;
							int northwest = (line > 0 && column > 0) ? pixelAt(processingIntermediateOutput, line-1, column-1) : 0;
							int northeast = (line > 0 && column < columns - 1) ? pixelAt(processingIntermediateOutput, line-1, column+1) : 0;

							nonNullCount = 0;

							int smallestLabel = min4NonNull(west, north, northwest, northeast, &nonNullCount);

							if(smallestLabel > 0)
							{
								if(nonNullCount > 0)
								{
									blobIdsCorrespondanceData[west] = west > 0 ? min(blobIdsCorrespondanceData[west], smallestLabel) : 0;
									blobIdsCorrespondanceData[north] = north > 0 ? min(blobIdsCorrespondanceData[north], smallestLabel) : 0;
									blobIdsCorrespondanceData[northwest] = northwest > 0 ? min(blobIdsCorrespondanceData[northwest], smallestLabel) : 0;
									blobIdsCorrespondanceData[northeast] = northeast > 0 ? min(blobIdsCorrespondanceData[northeast], smallestLabel) : 0;
								}

								label = smallestLabel;
							}
							else
							{
								current_blob_id++;
								label = current_blob_id;

								blobIdsCorrespondanceData[label] = label;
							}

							pixelSet(processingIntermediateOutput, line, column, label);
						}
						else
							pixelSet(processingIntermediateOutput, line, column, 0);
					}
				}

				(*maxBlobs) = current_blob_id;
			}

#define min2(a,b) ((a) < (b)) ? (a) : (b)
#define max2(a,b) ((a) > (b)) ? (a) : (b)

			void minimizeIds(int * blobIdsCorrespondanceData, int blobCount)
			{
				int id, newvalue;

				for(int i = 0;i<blobCount;i++)
				{
					id = blobIdsCorrespondanceData[i];

					while(true)
					{
						newvalue = blobIdsCorrespondanceData[id];

						if(newvalue != id)
							id = newvalue;
						else
							break;
					}

					blobIdsCorrespondanceData[i] = id;
				}
			}

			void finalizeBlobs(Blob * blobs, int blobCount)
			{
				for(int i = 0;i<blobCount;i++)
				{
					if(blobs[i].PixelCount > 0)
					{
						blobs[i].AvgCenterX = ((double)blobs[i].accX / (double)blobs[i].PixelCount);
						blobs[i].AvgCenterY = ((double)blobs[i].accY / (double)blobs[i].PixelCount);
					}
				}
			}

			void match(Blob * blobs, int * blobIdsCorrespondanceData, unsigned char* data, unsigned char * processingIntermediateOutput, int lines, int columns, int stride, int blobsCount)
			{	
				memset(blobs, 0, sizeof(Blob) * lines * columns);

				minimizeIds(blobIdsCorrespondanceData, blobsCount);

				for(int line = 0;line < lines;line++)
				{
					for(int column = 0;column < columns;column++)
					{
						int blob = pixelAt(processingIntermediateOutput, line, column);

						if(blob > 0)
						{
							int c_blob = blobIdsCorrespondanceData[blob];

							if(blobs[c_blob].PixelCount <= 0)
							{
								/* init */
								blobs[c_blob].MinX = columns;
								blobs[c_blob].MaxX = 0;
								blobs[c_blob].MinY = lines;
								blobs[c_blob].MaxY = 0;

								blobs[c_blob].accX = 0;
								blobs[c_blob].accY = 0;
							}

							blobs[c_blob].PixelCount++;

							blobs[c_blob].accX += column;
							blobs[c_blob].accY += line;

							blobs[c_blob].MinX = min2(blobs[c_blob].MinX, column);
							blobs[c_blob].MaxX = max2(blobs[c_blob].MaxX, column);

							blobs[c_blob].MinY = min2(blobs[c_blob].MinY, line);
							blobs[c_blob].MaxY = max2(blobs[c_blob].MaxY, line);

							pixelSet(data, line, column, c_blob * 150);
						}
						else
							pixelSet(data, line, column, 0);
					}
				}

				finalizeBlobs(blobs, blobsCount);		
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
				m_blobs = gcnew array<ManagedBlob^>(lines * rows);

				for(int i = 0;i<lines * rows;i++)
					m_blobs[i] = gcnew ManagedBlob();
			}

			BlobDelimiter::~BlobDelimiter(void)
			{
				delete blobIdsCorrespondanceData;
				delete m_blobs;
			}

			int BlobDelimiter::convertBlobs(int blobCount)
			{
				int managed_blob_count = 0;

				for(int i = 0;i<blobCount;i++)
				{
					int native_blob_index = i+1;

					if(blobs[native_blob_index].PixelCount > 0)
					{
						m_blobs[managed_blob_count]->AvgCenterX = blobs[native_blob_index].AvgCenterX;
						m_blobs[managed_blob_count]->AvgCenterY = blobs[native_blob_index].AvgCenterY;
						m_blobs[managed_blob_count]->MinX = blobs[native_blob_index].MinX;
						m_blobs[managed_blob_count]->MinY = blobs[native_blob_index].MinY;
						m_blobs[managed_blob_count]->MaxX = blobs[native_blob_index].MaxX;
						m_blobs[managed_blob_count]->MaxY = blobs[native_blob_index].MaxY;
						m_blobs[managed_blob_count]->PixelCount = blobs[native_blob_index].PixelCount;

						managed_blob_count++;
					}
				}

				return currentBlobCount = managed_blob_count;
			}

			int BlobDelimiter::BuildBlobs(unsigned char* data)
			{
				int nonNull, maxBlobs;

				memset(blobIdsCorrespondanceData, 0, lines * rows);

				processData(blobIdsCorrespondanceData, data, processingIntermediateOutput, lines, rows, stride, &nonNull, &maxBlobs);

				match(blobs, blobIdsCorrespondanceData, data, processingIntermediateOutput, lines, rows, stride, maxBlobs);

				return convertBlobs(maxBlobs);
			}
		}
	}
}