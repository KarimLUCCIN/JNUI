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
			public:
				BlobDelimiter(void);
				void BuildBlobs(unsigned char* data, int lines, int rows, int stride, List<Blob^>^ result);
			};
		}
	}
}

