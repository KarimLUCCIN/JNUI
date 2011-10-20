#pragma once

using namespace System::Collections::Generic;

namespace JapanNUI
{
	namespace ImageProcessing
	{
		namespace SectionsBuilders
		{
			public ref struct Blob
			{
			public:
				double centerX;
				double centerY;

				double X;
				double Y;
				double Width;
				double Height;
			};

			public ref class BlobDelimiter
			{
			private:
				int lines;
				int rows;
				int stride;

				/* Used when resolving blobs ids after the scan */
				int * blobIdsCorrespondanceData;
			public:
				BlobDelimiter(int lines, int rows, int stride);
				~BlobDelimiter(void);


				void BuildBlobs(unsigned char* data, List<Blob^>^ result);
			};
		}
	}
}

