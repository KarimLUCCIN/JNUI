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

				double AvgCenterXleft;
				double AvgCenterYleft;

				double AvgCenterXright;
				double AvgCenterYright;

				double AvgDepth;

				unsigned long accX;
				unsigned long accY;

				unsigned long accXright;
				unsigned long accYright;

				unsigned long pointCountRight;

				unsigned long accXleft;
				unsigned long accYleft;

				unsigned long pointCountLeft;

				unsigned long accDepth;

				double accBorderType[4];

				double averageDirection;
				double principalDirection;

				/* 
				Dans le cas où on croise les mains, on obtient une structure en
				X ou en A avec comme extrémité basses les deux coudes. Alors on recherche
				une telle formation dans les blobs pour voir s'il s'agit a priori de deux mains
				qui se croisent
				*/
				int CrossLeftBottomX;
				int CrossLeftBottomY;
				int CrossRightBottomX;
				int CrossRightBottomY;

				bool haveCrossingPattern;

				double crossFirstAngle;
				double crossSecondAngle;

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
				property array<ManagedBlob^>^ Blobs
				{
					array<ManagedBlob^>^ get()
					{
						return m_blobs;
					}
				}

				property int BlobsValidCount
				{
					int get ()
					{
						return currentBlobCount;
					}
				}

				BlobDelimiter(int lines, int rows, int stride);
				~BlobDelimiter(void);


				/* Return the number of blobs identified, accessed via GetBlobData */
				int BuildBlobs(unsigned char* data, unsigned char * grads);
			};
		}
	}
}

