#pragma once

/*
	Image Moments
	http://en.wikipedia.org/wiki/Image_moments
*/

using namespace System::Collections::Generic;

namespace KinectBrowser
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

				double closestPointDepth;
				double closestPointX;
				double closestPointY;

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

				/* Utilisé pour les calculs des moments */
				double Moments[2][2];
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

				double InvertedEstimatedCursorX;
				double InvertedEstimatedCursorY;

				double ClosestPointX;
				double ClosestPointY;

				double AverageDepth;

				double AverageDirection;
				double PrincipalDirection;

				int PixelCount;

				bool Crossed;

				/* Utilisé pour les calculs des moments */
				array<double,2> ^ Moments;
				array<double,2> ^ Mu;

				/* Le blob qui est actuellement croisé avec celui-ci */
				ManagedBlob^ CrossedTarget;

				property double Width
				{
					double get ()
					{
						return MaxX - MinX;
					}
				}				

				property double Height
				{
					double get ()
					{
						return MaxY - MinY;
					}
				}

				bool Equals(ManagedBlob^ other)
				{
					/* on ne compare que certaine propriété, on suppose le reste */
					return 
						other != nullptr &&

						AvgCenterX == other->AvgCenterX &&
						AvgCenterY == other->AvgCenterY &&

						EstimatedCursorX == other->EstimatedCursorX &&
						EstimatedCursorY == other->EstimatedCursorY &&

						InvertedEstimatedCursorX == other->InvertedEstimatedCursorX &&
						InvertedEstimatedCursorY == other->InvertedEstimatedCursorY;
				}

				ManagedBlob^ Clone()
				{
					ManagedBlob^ res = gcnew ManagedBlob();

					res->AverageDepth = AverageDepth;
					res->AverageDirection = AverageDirection;

					res->AvgCenterX = AvgCenterX;
					res->AvgCenterY = AvgCenterY;

					res->EstimatedCursorX = EstimatedCursorX;
					res->EstimatedCursorY = EstimatedCursorY;

					res->InvertedEstimatedCursorX = InvertedEstimatedCursorX;
					res->InvertedEstimatedCursorY = InvertedEstimatedCursorY;

					res->AverageDepth = AverageDepth;

					res->PrincipalDirection = PrincipalDirection;
					res->PixelCount = PixelCount;

					res->MinX = MinX;
					res->MaxX = MaxX;
					res->MinY = MinY;
					res->MaxY = MaxY;

					res->Crossed = Crossed;
					res->CrossedTarget = CrossedTarget;

					res->Moments = gcnew array<double,2>(2, 2);
					res->Mu = gcnew array<double,2>(3, 3);

					for(int i = 0;i<=2;i++)
					{
						for(int j = 0;j<=2;j++)
						{
							if(i < 2 && j < 2)
								res->Moments[i,j] = Moments[i,j];

							res->Mu[i,j] = Mu[i,j];
						}
					}

					return res;
				}
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
				/* 
					Collecte la profondeur minimale pendant la construction des blobs histoire de pouvoir l'utiliser cache
					lors de la réduction
				*/
				double * blobsIdsMinimumDepths;
				unsigned char * processingIntermediateOutput;

				/*
				Les k parmi n, pour k et n variants de 0 à 4 (précalculé pour aller plus vite après)
				*/
				int ** binomialCoeffs;
			private:
				Blob * blobs;

				void convertBlob(ManagedBlob ^ dst, Blob * src, double primaryCenterX, double primaryCenterY, bool crossed);
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

