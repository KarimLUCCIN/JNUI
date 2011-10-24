#pragma once

using namespace System::Collections::Generic;

namespace JapanNUI
{
	namespace ImageProcessing
	{
		namespace SectionsBuilders
		{
			public struct Blob
			{
			public:
				double MinX;
				double MinY;
				double MaxX;
				double MaxY;

				int PixelCount;
			};

			public ref struct ManagedBlob
			{
			public:
				double MinX;
				double MinY;
				double MaxX;
				double MaxY;

				int PixelCount;
			};

			public ref class BlobDelimiter
			{
			private:
				int lines;
				int rows;
				int stride;

				/* Used when resolving blobs ids after the scan */
				int * blobIdsCorrespondanceData;
				unsigned char * processingIntermediateOutput;
			public:
				Blob * blobs;

				void GetBlobData(int index, double& minX, double& maxX, double& minY, double& maxY, int& pixelCount)
				{
					Blob b = blobs[index+1];

					minX = b.MinX;
					minY = b.MinY;
					maxX = b.MaxX;
					maxY = b.MaxY;
					pixelCount = b.PixelCount;
				}
			public:
				BlobDelimiter(int lines, int rows, int stride);
				~BlobDelimiter(void);


				/* Return the number of blobs identified, accessed via GetBlobData */
				int BuildBlobs(unsigned char* data);
			};
		}
	}
}

