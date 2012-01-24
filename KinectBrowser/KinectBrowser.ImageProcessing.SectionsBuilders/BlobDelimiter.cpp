#include "StdAfx.h"
#include "BlobDelimiter.h"
#include <memory>
#include <algorithm>

#define _USE_MATH_DEFINES
#include <math.h>
#include <limits.h>

using namespace std;

#define MANAGED_DEBUG

//#define MATCH_OUTPUT_DEBUG_INFO

namespace KinectBrowser
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

//#define pixelAt(data, line, column) data[pixel((line), (column))] | data[pixel((line), (column))+1] << 8 | data[pixel((line), (column))+2] << 16
#define pixelAt(data, line, column) *((int*)&data[pixel((line), (column))]) & 0x00FFFFFF
//#define pixelSet(data, line, column, value) data[pixel(line, column)] = (value); data[pixel(line,column)+1] = (value) >> 8; data[pixel(line, column)+2] = (value) >> 16
#define pixelSet(data, line, column, value) *((int*)&data[pixel((line), (column))]) = value

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

#define min2(a,b) ((a) < (b)) ? (a) : (b)
#define max2(a,b) ((a) > (b)) ? (a) : (b)

			void processData(int * blobIdsCorrespondanceData, double * blobsIdsMinimumDepths, unsigned char* data, unsigned char* processingIntermediateOutput, int lines, int columns, int stride, int * nonNullPixels, int * maxBlobs)
			{
				(*nonNullPixels) = 0;

				int current_blob_id = 0;
				int label;
				int nonNullCount;

				for(int line = 0;line < lines;line++)
				{
					for(int column = 0;column < columns;column++)
					{
						unsigned char current = data[pixel(line, column)];

						if(current != 0)
						{
							double localDepth = data[pixel(line, column)+1];

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

								blobsIdsMinimumDepths[label] = min2(localDepth, blobsIdsMinimumDepths[label]);
							}
							else
							{
								current_blob_id++;
								label = current_blob_id;

								blobIdsCorrespondanceData[label] = label;
								blobsIdsMinimumDepths[label] = localDepth;
							}

							pixelSet(processingIntermediateOutput, line, column, label);
						}
						else
							pixelSet(processingIntermediateOutput, line, column, 0);
					}
				}

				(*maxBlobs) = current_blob_id;
			}

			void minimizeIds(int * blobIdsCorrespondanceData, double * blobsIdsMinimumDepths, int blobCount)
			{
				int id, newvalue;

				for(int i = 0;i<blobCount;i++)
				{
					id = blobIdsCorrespondanceData[i];

					while(true)
					{
						newvalue = blobIdsCorrespondanceData[id];

						if(newvalue != id)
						{
							blobsIdsMinimumDepths[newvalue] = min2(blobsIdsMinimumDepths[newvalue], blobsIdsMinimumDepths[id]);
							id = newvalue;
						}
						else
							break;
					}

					blobIdsCorrespondanceData[i] = id;
				}
			}

#define BLOB_DIRECTION_HORIZONTAL 0
#define BLOB_DIRECTION_VERTICAL 1
#define BLOB_DIRECTION_DIAG 2
#define BLOB_DIRECTION_INVDIAG 3

#define dot(ax, ay, bx, by) (ax) * (bx) + (ay) * (by)

#define abs2(a) max2((a),-(a))

			inline double p2(double a)
			{
				return a * a;
			}

			int scanLineToPixel(int column, int startY, int endY, unsigned char* data, int lines, int columns, int stride)
			{
				/*
					Scan la ligne et renvoi le premier Y trouvé différent de 0 à partir de startY et jusqu'à endY.
					Renvoi -1 si aucun pixel non nul n'a été trouvé.

					La recherche se fait de bas en haut.
				*/

				for(int i = startY;i>=endY;i--)
				{
					if(data[pixel(i, column)] != 0)
						return i;
				}

				return -1;
			}

			void finalizeBlobs(unsigned char* data, Blob * blobs, int blobCount, int lines, int columns, int stride)
			{
				double globalWeights[] = {1.2,1,1,1};
				double directionsAngles[] = {0, M_PI_2, M_PI_4, M_PI_2 + M_PI_4};
				double directionsAnglesInv[] = {M_PI, M_PI_2, M_PI_4, M_PI_2 + M_PI_4};

				for(int i = 1;i<=blobCount;i++)
				{
					if(blobs[i].PixelCount > 0)
					{
						blobs[i].AvgCenterX = ((double)blobs[i].accX / (double)blobs[i].PixelCount);
						blobs[i].AvgCenterY = ((double)blobs[i].accY / (double)blobs[i].PixelCount);
						blobs[i].AvgDepth = (blobs[i].accDepth / (double)blobs[i].PixelCount) / 255.0;

						blobs[i].AvgCenterXleft = blobs[i].pointCountLeft > 0 ? ((double)blobs[i].accXleft / (double)blobs[i].pointCountLeft) : 0;
						blobs[i].AvgCenterYleft = blobs[i].pointCountLeft > 0 ? ((double)blobs[i].accYleft / (double)blobs[i].pointCountLeft) : 0;

						blobs[i].AvgCenterXright = blobs[i].pointCountRight > 0 ? ((double)blobs[i].accXright / (double)blobs[i].pointCountRight) : 0;
						blobs[i].AvgCenterYright = blobs[i].pointCountRight > 0 ? ((double)blobs[i].accYright / (double)blobs[i].pointCountRight) : 0;

						int maxDirection = BLOB_DIRECTION_HORIZONTAL;
						double score = 0;
						double totalScore = 0;

						double leftOnlyScore = 0;
						double rightOnlyScore = 0;

						for(int j = 0;j<4;j++)
						{
							totalScore += globalWeights[j] * blobs[i].accBorderType[j];

							if(j != BLOB_DIRECTION_DIAG)
								leftOnlyScore += globalWeights[j] * blobs[i].accBorderType[j];
							
							if(j != BLOB_DIRECTION_INVDIAG)
								rightOnlyScore += globalWeights[j] * blobs[i].accBorderType[j];

							if(score < blobs[i].accBorderType[j])
							{
								score = blobs[i].accBorderType[j];
								maxDirection = j;
							}
						}
						
						double avgDiagAngle = 0;
						double avgDiagInvAngle = 0;

						double leftOnlyDirection = 0;
						double rightOnlyDirection = 0;

						for(int j = 0;j<4;j++)
						{
							/* entre 0 et pi/2 */
							avgDiagAngle += globalWeights[j] * directionsAngles[j] * ((double)blobs[i].accBorderType[j] / (double)totalScore);

							/* entre pi/2 et pi */
							avgDiagInvAngle += globalWeights[j] * directionsAnglesInv[j] * ((double)blobs[i].accBorderType[j] / (double)totalScore);

							if(j != BLOB_DIRECTION_INVDIAG)
								rightOnlyDirection += globalWeights[j] * directionsAngles[j] * ((double)blobs[i].accBorderType[j] / rightOnlyScore);

							if(j != BLOB_DIRECTION_DIAG)
								leftOnlyDirection += globalWeights[j] * directionsAnglesInv[j] * ((double)blobs[i].accBorderType[j] / leftOnlyScore);
						}

						double mainAngle;
						
						double avgAngle, avgSecondAngle;

						if(blobs[i].accBorderType[BLOB_DIRECTION_DIAG] > blobs[i].accBorderType[BLOB_DIRECTION_INVDIAG])
						{
							/* entre 0 et pi/2 */
							mainAngle = directionsAngles[maxDirection];
							avgAngle = avgDiagAngle;
						}
						else
						{
							/* entre pi/2 et pi */
							mainAngle = directionsAnglesInv[maxDirection];
							avgAngle = avgDiagInvAngle;
						}

						blobs[i].principalDirection = mainAngle;
						blobs[i].averageDirection = avgAngle;
						
						
						/* Vérifier si deux mains se croisent */
						
						/* Step 1 : Vérifier que BottomLeft & BottomRight sont bien les extrémités de la BBox */
						bool areCornersRepresentativeOfSeparatedArms =
							abs(blobs[i].CrossLeftBottomX - blobs[i].MinX) < 10 &&
							abs(blobs[i].CrossRightBottomX - blobs[i].MaxX) < 10 &&
							abs(max2(blobs[i].CrossLeftBottomY, blobs[i].CrossRightBottomY) - blobs[i].MaxY) < 10;

						int s_borderleft_pixel = scanLineToPixel(blobs[i].MinX + 2, blobs[i].MaxY, (int)blobs[i].AvgCenterY, data, lines, columns, stride);
						int s_borderright_pixel = s_borderleft_pixel < 0 ? -1 : scanLineToPixel(blobs[i].MaxX - 2, blobs[i].MaxY, (int)blobs[i].AvgCenterY, data, lines, columns, stride);
						int s_center_pixel = s_borderright_pixel < 0 ? -1 : scanLineToPixel((int)blobs[i].AvgCenterX, blobs[i].MaxY, (int)blobs[i].AvgCenterY, data, lines, columns, stride);

						double differenceInPixelInRightAndLeftPercentage = (((double)abs2((double)blobs[i].pointCountRight - (double)blobs[i].pointCountLeft)) / ((double)blobs[i].PixelCount));

						bool isEnouphPointsInRightAndLeftStacks =
							differenceInPixelInRightAndLeftPercentage <= 0.02;

						bool areCentersSpacedEnouphForSeparatedArms =
							sqrt(p2(blobs[i].AvgCenterXleft - blobs[i].AvgCenterXright) + p2(blobs[i].AvgCenterYleft - blobs[i].AvgCenterYright)) > 10;

						int t1 = abs2(s_borderleft_pixel - s_borderright_pixel);
						int t2 = abs2(s_center_pixel - min2(s_borderleft_pixel, s_borderright_pixel));
						bool isXpattern = (t1 < 20) && (t2 > 40);

						if(isXpattern)// && (areCornersRepresentativeOfSeparatedArms || (areCentersSpacedEnouphForSeparatedArms && isEnouphPointsInRightAndLeftStacks)))
						{
							/* Step 2 : l'angle principal doit être la verticale */
							//if(maxDirection == BLOB_DIRECTION_VERTICAL)
							{
								double bboxHeight = blobs[i].MaxY - blobs[i].MinY;

								/* Step 3 : vérifier que le centre est à plus de 1% du bord inférieur */
								if(blobs[i].AvgCenterY < blobs[i].MaxY - bboxHeight * 0.01)
								{
									/* Step 4 : vérifier que le centre est vide */
									int minCrossY = -1;
									int crossCenterX = (int)(blobs[i].AvgCenterX);
									int crossCenterY = (int)(blobs[i].AvgCenterY+1);

									for(int j = min2(blobs[i].CrossLeftBottomY, blobs[i].CrossRightBottomY);j > crossCenterY;j--)
									{
										if(data[pixel(j, crossCenterX)] == 0)
											minCrossY = j;
										else
											break;
									}

									double centerToBorderDistance = blobs[i].MaxY - crossCenterY;

									/* On suppose un vide qui va au moins jusqu'à centre + 50% de la distance du centre au bord */
									if(minCrossY > 0 && minCrossY < blobs[i].AvgCenterY + centerToBorderDistance * 0.5)
									{
										blobs[i].haveCrossingPattern = true;

										blobs[i].crossFirstAngle = leftOnlyDirection; //avgDiagAngle;
										blobs[i].crossSecondAngle = rightOnlyDirection; // avgDiagInvAngle;
									}
								}
							}
						}
					}
				}
			}

			inline int indexOfMax(double a, double b, double c, double d)
			{
				double m = max(a, max(b, max(c,d)));

				if(m == a)
					return 0;
				else if(m == b)
					return 1;
				else if(m == c)
					return 2;
				else
					return 3;
			}

			/* de 0 à 10, à la puissance 0 à 10. Sinon ça passe sur le pow classique*/
			double ** powTable = NULL;
#define Q_POW_MAX_TABLE 1024

			inline double q_pow(int nb, int power)
			{
				if(nb < Q_POW_MAX_TABLE && power < Q_POW_MAX_TABLE)
				{
					if(powTable == NULL)
					{
						powTable = new double * [Q_POW_MAX_TABLE];

						for(int p = 0;p<Q_POW_MAX_TABLE;p++)
						{
							powTable[p] = new double [Q_POW_MAX_TABLE];

							for(int q = 0;q<Q_POW_MAX_TABLE;q++)
							{
								powTable[p][q] = pow((double)p, (double)q);
							}
						}
					}

					return powTable[nb][power];
				}
				else
					return pow((double)nb, (double)power);
			}

			inline double momentsIntegrate(int line, int column, int pixel, int p, int q)
			{
				return q_pow((double)column, (double)p) * q_pow((double)line, (double)q) * pixel;
			}

			inline void momentsIntegrateOnBlob( Blob * blobs, int c_blob, int line, int column, int pixelValue, int p, int q) 
			{
				blobs[c_blob].Moments[p][q] += momentsIntegrate(line, column, pixelValue, p, q);
			}

			void match(Blob * blobs, int * blobIdsCorrespondanceData, double * blobsIdsMinimumDepths, unsigned char* data, unsigned char * grads, unsigned char * processingIntermediateOutput, int lines, int columns, int stride, int blobsCount)
			{	
				memset(blobs, 0, sizeof(Blob) * (blobsCount+1)); //lines * columns);

				minimizeIds(blobIdsCorrespondanceData, blobsIdsMinimumDepths, blobsCount);

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

								blobs[c_blob].CrossLeftBottomX = column;
								blobs[c_blob].CrossLeftBottomY = line;

								blobs[c_blob].CrossRightBottomX = column;
								blobs[c_blob].CrossRightBottomY = line;

								blobs[c_blob].haveCrossingPattern = false;

								blobs[c_blob].closestPointDepth = (double)INT_MAX;

								for(int p = 0;p<2;p++)
								{
									for(int q = 0;q<2;q++)
									{
										blobs[c_blob].Moments[p][q] = 0;
									}
								}
							}

							/* Progression du calcul du moment */
							for(int p = 0;p<2;p++)
							{
								for(int q = 0;q<2;q++)
								{
									momentsIntegrateOnBlob(blobs, c_blob, line, column, 1, p, q);
								}
							}
							
							/* Counting the current pixel */
							blobs[c_blob].PixelCount++;

							blobs[c_blob].accX += column;
							blobs[c_blob].accY += line;

							double localDepth = data[pixel(line, column)+1];
							blobs[c_blob].accDepth += localDepth;

							if(localDepth < blobs[c_blob].closestPointDepth)
							{
								blobs[c_blob].closestPointDepth = localDepth;
								blobs[c_blob].closestPointX = column;
								blobs[c_blob].closestPointY = line;
							}

							blobs[c_blob].MinX = min2(blobs[c_blob].MinX, column);
							blobs[c_blob].MaxX = max2(blobs[c_blob].MaxX, column);

							blobs[c_blob].MinY = min2(blobs[c_blob].MinY, line);
							blobs[c_blob].MaxY = max2(blobs[c_blob].MaxY, line);

							/* Bottom right & left edges (to detected crossing arms) */
							if((line > blobs[c_blob].CrossLeftBottomY && (blobs[c_blob].CrossLeftBottomX - column > 0)) ||
								((line > blobs[c_blob].CrossLeftBottomY + 10) && (blobs[c_blob].CrossLeftBottomX - column) > -10 ))
							{
								blobs[c_blob].CrossLeftBottomX = column;
								blobs[c_blob].CrossLeftBottomY = line;
							}

							if((line > blobs[c_blob].CrossRightBottomY && (column - blobs[c_blob].CrossRightBottomX > 0)) ||
								((line > blobs[c_blob].CrossLeftBottomY + 10) && (column - blobs[c_blob].CrossRightBottomX) > -10))
							{
								blobs[c_blob].CrossRightBottomX = column;
								blobs[c_blob].CrossRightBottomY = line;
							}

							/* Direction */
							double d_v = grads[pixel((line), (column))+0] / 255.0;
							double d_h = grads[pixel((line), (column))+1] / 255.0;
							double d_d = grads[pixel((line), (column))+2] / 255.0;
							double d_id = grads[pixel((line), (column))+3] / 255.0;

							int max_i = indexOfMax(d_v, d_h, d_d, d_id);

							if(grads[pixel((line), (column))+max_i] > 0)
							{
								if(max_i == BLOB_DIRECTION_INVDIAG)
								{
									blobs[c_blob].accXleft += column;
									blobs[c_blob].accYleft += line;
									blobs[c_blob].pointCountLeft++;
								}

								if(max_i == BLOB_DIRECTION_DIAG)
								{
									blobs[c_blob].accXright += column;
									blobs[c_blob].accYright += line;
									blobs[c_blob].pointCountRight++;
								}

								blobs[c_blob].accBorderType[BLOB_DIRECTION_VERTICAL] += d_v;
								blobs[c_blob].accBorderType[BLOB_DIRECTION_HORIZONTAL] += d_h;
								blobs[c_blob].accBorderType[BLOB_DIRECTION_DIAG] += d_d;
								blobs[c_blob].accBorderType[BLOB_DIRECTION_INVDIAG] += d_id;
							}

#ifdef MATCH_OUTPUT_DEBUG_INFO
							pixelSet(data, line, column, c_blob * 150);
#endif
						}
						else
						{
#ifdef MATCH_OUTPUT_DEBUG_INFO
							pixelSet(data, line, column, 0);
#endif
						}
					}
				}

				finalizeBlobs(data, blobs, blobsCount, lines, columns, stride);		
			}

#ifndef MANAGED_DEBUG
#pragma managed(pop)
#endif

#pragma region Binomial Coeffs computation

			double Factorial(double nValue)
			{
				if(nValue == 0)
					return 1;
				else if(nValue == 1)
					return 1;
				else if(nValue == 2)
					return 2;
				else if(nValue == 3)
					return 6;
				else
				{
					double result = nValue;
					double result_next;
					double pc = nValue;
					do
					{
						result_next = result*(pc-1);
						result = result_next;
						pc--;
					}while(pc>2);
					nValue = result;
					return nValue;
				}
			}

			double EvaluateBinomialCoefficient(double k, double n)
			{
				if(k < 0 || k > n)
					return 0;
				else
				{
					double sup = 1;
					for(int i = n;i>=n-k+1;i--)
						sup *=i;

					return sup / Factorial(k);
				}
			}

#pragma endregion Binomial Coeffs computation

			BlobDelimiter::BlobDelimiter(int lines, int rows, int stride)
			{
				this->lines = lines;
				this->rows = rows;
				this->stride = stride;

				blobIdsCorrespondanceData = new int [lines * rows];
				blobsIdsMinimumDepths = new double [lines * rows];
				processingIntermediateOutput = new unsigned char [lines * rows * stride];
				blobs = new Blob [lines * rows];
				m_blobs = gcnew array<ManagedBlob^>(lines * rows);

				for(int i = 0;i<lines * rows;i++)
				{
					m_blobs[i] = gcnew ManagedBlob();
					m_blobs[i]->Moments = gcnew array<double,2>(2, 2);
					m_blobs[i]->Mu = gcnew array<double,2>(3, 3);
				}

				binomialCoeffs = new int *[4];

				for(int k = 0;k<4;k++)
				{
					binomialCoeffs[k] = new int[4];
					for(int n = 0;n<4;n++)
					{
						binomialCoeffs[k][n] = EvaluateBinomialCoefficient(k, n);
					}
				}
			}

			BlobDelimiter::~BlobDelimiter(void)
			{
				delete blobIdsCorrespondanceData;
				delete blobsIdsMinimumDepths;
				delete m_blobs;
			}

			inline void normalize(double x, double y, double * d_x, double * d_y)
			{
				double length = sqrt(p2(x) + p2(y));

				if(length <= 0)
				{
					*d_x = 0;
					*d_y = 0;
				}
				else
				{
					*d_x = x / length;
					*d_y = x / length;
				}
			}

			inline float distance(double x1, double y1, double x2, double y2)
			{
				return sqrt(p2(x2-x1)+p2(y2-y1));
			}

			void BlobDelimiter::convertBlob(ManagedBlob ^ dst, Blob * src, double primaryCenterX, double primaryCenterY, bool crossed)
			{
				/* Moments */
				for(int p = 0;p<2;p++)
				{
					for(int q = 0;q<2;q++)
					{
						dst->Moments[p,q] = src->Moments[p][q];
					}
				}

				if(src->PixelCount > 0 && src->Moments[0][0] > 0)
				{
					double x_bar = src->Moments[1][0] / src->Moments[0][0];
					double y_bar = src->Moments[0][1] / src->Moments[0][0];

					for(int p = 0;p<=2;p++)
					{
						for(int q = 0;q<=2;q++)
						{
							dst->Mu[p,q] = 0;

							for(int m = 0;m<=p;m++)
							{
								for(int n = 0;n<=q;n++)
								{
									dst->Mu[p,q] += binomialCoeffs[m][p] * binomialCoeffs[n][q] * pow(-x_bar, p - m) * pow(-y_bar, q - n) * src->Moments[m][n];
								}
							}
						}
					}
				}

				/* Propriétés */
				dst->AvgCenterX = src->AvgCenterX;
				dst->AvgCenterY = src->AvgCenterY;

				dst->MinX = src->MinX;
				dst->MinY = src->MinY;
				dst->MaxX = src->MaxX;
				dst->MaxY = src->MaxY;

				dst->PixelCount = src->PixelCount;

				//if(dst->Mu[0,0] != 0)
				//{
				//	double up20 = dst->Mu[2,0] / dst->Mu[0,0];
				//	double up02 = dst->Mu[0,2] / dst->Mu[0,0];
				//	double up11 = dst->Mu[1,1] / dst->Mu[0,0];

				//	if(up11 != 0)
				//	{
				//		dst->AverageDirection = 0.5 * atan2(2 * up11, up20 - up02) + M_PI_2;
				//	}
				//	else
				//		dst->AverageDirection = src->averageDirection;
				//}
				//else
				//	dst->AverageDirection = src->averageDirection;

				dst->AverageDirection = src->averageDirection;
				dst->PrincipalDirection = src->principalDirection;

				dst->AverageDepth = src->AvgDepth;

				/* estimates the cursor position */
				dst->EstimatedCursorX = min2(dst->MaxX, max2(dst->MinX, src->AvgCenterX + cos(dst->AverageDirection) * abs(primaryCenterX - dst->MinX)));
				dst->EstimatedCursorY = min2(dst->MaxY, max2(dst->MinY, src->AvgCenterY - sin(dst->AverageDirection) * abs(primaryCenterY - dst->MinY)));

				dst->InvertedEstimatedCursorX = min2(dst->MaxX, max2(dst->MinX, src->AvgCenterX - cos(dst->AverageDirection) * abs(primaryCenterX - dst->MinX)));
				dst->InvertedEstimatedCursorY = min2(dst->MaxY, max2(dst->MinY, src->AvgCenterY + sin(dst->AverageDirection) * abs(primaryCenterY - dst->MinY)));

				/* correct the angle, based on the cursor location within the bounding box of the blob */
				double c_x, c_y;
				normalize(dst->EstimatedCursorX - dst->AvgCenterX, dst->EstimatedCursorY - dst->AvgCenterY,
					&c_x, &c_y);

				dst->AverageDirection = acos(dot(c_x, c_y, 1, 0));

				dst->ClosestPointX = src->closestPointX;
				dst->ClosestPointY = src->closestPointY;

				/* Si un des curseurs est très proche du point le plus proche, on remplace */
				/*
				Ne marche pas, au final ça dégrade la qualité des mouvements du point ...
				
				double closeDistance = 20;
				if(distance(dst->ClosestPointX, dst->ClosestPointY, dst->EstimatedCursorX, dst->EstimatedCursorY) < closeDistance)
				{
					dst->EstimatedCursorX = dst->ClosestPointX;
					dst->EstimatedCursorY = dst->ClosestPointY;
				}
				
				if(distance(dst->ClosestPointX, dst->ClosestPointY, dst->InvertedEstimatedCursorX, dst->InvertedEstimatedCursorY) < closeDistance)
				{
					dst->InvertedEstimatedCursorX = dst->ClosestPointX;
					dst->InvertedEstimatedCursorY = dst->ClosestPointY;
				}*/

				dst->Crossed = crossed;
			}

			int BlobDelimiter::convertBlobs(int blobCount)
			{
				int managed_blob_count = 0;

				for(int i = 0;i<blobCount;i++)
				{
					int native_blob_index = i+1;

					if(blobs[native_blob_index].PixelCount > 0)
					{
						double primaryCenterX = blobs[native_blob_index].AvgCenterX;
						double primaryCenterY = blobs[native_blob_index].AvgCenterY;

						if(!blobs[native_blob_index].haveCrossingPattern)
						{
							convertBlob(m_blobs[managed_blob_count], &blobs[native_blob_index], primaryCenterX, primaryCenterY, false);

							m_blobs[managed_blob_count]->CrossedTarget = nullptr;
						}
						else
						{
							/* équivalent à deux blobs avec chacun une des directions */
							blobs[native_blob_index].averageDirection = blobs[native_blob_index].crossFirstAngle;

							blobs[native_blob_index].AvgCenterX = blobs[native_blob_index].AvgCenterXleft;
							blobs[native_blob_index].AvgCenterY = blobs[native_blob_index].AvgCenterYleft;

							convertBlob(m_blobs[managed_blob_count], &blobs[native_blob_index], primaryCenterX, primaryCenterY, true);

							managed_blob_count++;

							blobs[native_blob_index].averageDirection = blobs[native_blob_index].crossSecondAngle;

							blobs[native_blob_index].AvgCenterX = blobs[native_blob_index].AvgCenterXright;
							blobs[native_blob_index].AvgCenterY = blobs[native_blob_index].AvgCenterYright;

							convertBlob(m_blobs[managed_blob_count], &blobs[native_blob_index], primaryCenterX, primaryCenterY, true);

							m_blobs[managed_blob_count-1]->CrossedTarget = m_blobs[managed_blob_count];
							m_blobs[managed_blob_count]->CrossedTarget = m_blobs[managed_blob_count-1];
						}

						managed_blob_count++;
					}
				}

				return currentBlobCount = managed_blob_count;
			}

			int BlobDelimiter::BuildBlobs(unsigned char* data, unsigned char * grads)
			{
				int nonNull, maxBlobs;

				memset(blobIdsCorrespondanceData, 0, sizeof(int) * lines * rows);

				processData(blobIdsCorrespondanceData, blobsIdsMinimumDepths, data, processingIntermediateOutput, lines, rows, stride, &nonNull, &maxBlobs);

				match(blobs, blobIdsCorrespondanceData, blobsIdsMinimumDepths, data, grads, processingIntermediateOutput, lines, rows, stride, maxBlobs);

				return convertBlobs(maxBlobs);
			}
		}
	}
}
