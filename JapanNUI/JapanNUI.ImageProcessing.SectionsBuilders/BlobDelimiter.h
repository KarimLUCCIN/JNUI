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

				double AvgCenterX;
				double AvgCenterY;

				unsigned long accX;
				unsigned long accY;

				int PixelCount;
			};

			public ref struct ManagedBlob
			{
			public:
				double MinX;
				double MinY;
				double MaxX;
				double MaxY;

				double AvgCenterX;
				double AvgCenterY;

				int PixelCount;
			};

			public ref class BlobDelimiter
			{
			private:
				int lines;
				int rows;
				int stride;

				int currentBlobCount;
				array<ManagedBlob^> ^ m_blobs;

				/* Used when resolving blobs ids after the scan */
				int * blobIdsCorrespondanceData;
				unsigned char * processingIntermediateOutput;
			private:
				Blob * blobs;

				int convertBlobs(int blobCount);
			public:
				array<ManagedBlob^> ^ getBlobs()
				{
					return m_blobs;
				}

				int getCurrentBlobCount()
				{
					return currentBlobCount;
				}

				BlobDelimiter(int lines, int rows, int stride);
				~BlobDelimiter(void);


				/* Return the number of blobs identified, accessed via GetBlobData */
				int BuildBlobs(unsigned char* data);
			};
		}
	}
}

