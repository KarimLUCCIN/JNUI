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

				double AvgDepth;

				unsigned long accX;
				unsigned long accY;

				unsigned long accDepth;

				unsigned long accBorderType[4];

				double averageDirection;
				double principalDirection;

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

				double EstimatedCursorX;
				double EstimatedCursorY;

				double AverageDepth;

				double AverageDirection;
				double PrincipalDirection;

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
				int BuildBlobs(unsigned char* data, unsigned char * grads);
			};
		}
	}
}

