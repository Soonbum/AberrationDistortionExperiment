using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace AberrationDistortionExperiment
{
    public class ImageProcessingManagement
    {
        private Mat homographyMatrixScale;
        private Mat homographyMatrixLeft;
        private Mat homographyMatrixRight;

        private PointF[] src_pt_Scale;
        private PointF[] dst_pt_Scale;

        private PointF[] src_pt;

        private PointF[] dst_ptL;
        private PointF[] dst_ptR;

        private double EasyRemoval_RPX;
        private double EasyRemoval_RPY;
        private double EasyRemoval_HIX;
        private double EasyRemoval_HIY;
        private double EasyRemoval_VIX;
        private double EasyRemoval_VIY;
        private double EasyRemoval_R;

        private int EasyRemovalhorizontalN;
        private int EasyRemovalverticalN;
        private bool EasyRemoval_ZigZag;

        public ImageProcessingManagement()
        {
            homographyMatrixLeft = new Mat();
            homographyMatrixRight = new Mat();
            homographyMatrixScale = new Mat();

            src_pt = new PointF[4];
            src_pt_Scale = new PointF[4];

            dst_pt_Scale = new PointF[4];

            dst_ptL = new PointF[4];
            dst_ptR = new PointF[4];

            SetSourcePoint2f(0, 2159, 2159, 0,
                             0, 0, 3839, 3839);                                                      // UV 엔진 하나의 조사면 해상도: 2160 * 3840 (0: 좌상단, 1: 우상단, 2: 우하단, 3: 좌하단)
            SetSourcePoint2f_Scale(-0.5f, 4239.5f, 4239.5f, -0.5f, -0.5f, -0.5f, 3839.5f, 3839.5f);     // 전체 조사면 해상도: 4240 * 3840 (마진이 80이므로 2160 * 2 - 80 = 4240) (0: 좌상단, 1: 우상단, 2: 우하단, 3: 좌하단)
                                                                                                        // 좌상단은 (-0.5, -0.5), 우상단은 (0.5, -0.5), 우하단은 (0.5, 0.5), 좌하단은 (-0.5, 0.5)만큼 Shift되어 있음
                                                                                                        // 바깥쪽으로 0.5픽셀만큼 넓은 이유는 픽셀이 정사각형이기 때문에 논리적 픽셀의 센터 포인트 정보가 물리적 픽셀의 중심을 가리키려면 -0.5 픽셀만큼 밖으로 확장시켜야 함
                                                                                                        //this.setDestinationPoint2f(20.50f, 2135.27f, 2142.83f, 54.24f, 80.51f, 18.42f, 3827.32f, 3815.83f);
        }

        public void Dispose()
        {
            homographyMatrixLeft?.Dispose();
            homographyMatrixRight?.Dispose();
            homographyMatrixScale?.Dispose();
        }

        public PointF[] GetSourcePointF() { return src_pt; }

        public void SetSourcePoint2f(float x0, float x1, float x2, float x3, float y0, float y1, float y2, float y3)
        {
            src_pt[0] = new PointF(x0, y0);
            src_pt[1] = new PointF(x1, y1);
            src_pt[2] = new PointF(x2, y2);
            src_pt[3] = new PointF(x3, y3);
        }

        public void SetSourcePoint2f_Scale(float x0, float x1, float x2, float x3, float y0, float y1, float y2, float y3)
        {
            src_pt_Scale[0] = new PointF(x0, y0);
            src_pt_Scale[1] = new PointF(x1, y1);
            src_pt_Scale[2] = new PointF(x2, y2);
            src_pt_Scale[3] = new PointF(x3, y3);
        }

        public void SetDestinationPoint2f_Scale(float x0, float x1, float x2, float x3, float y0, float y1, float y2, float y3)
        {
            dst_pt_Scale[0] = new PointF(x0, y0);
            dst_pt_Scale[1] = new PointF(x1, y1);
            dst_pt_Scale[2] = new PointF(x2, y2);
            dst_pt_Scale[3] = new PointF(x3, y3);
        }

        public void SetDestinationPoint2f_Left(float x0, float x1, float x2, float x3, float y0, float y1, float y2, float y3)
        {
            dst_ptL[0] = new PointF(x0, y0);
            dst_ptL[1] = new PointF(x1, y1);
            dst_ptL[2] = new PointF(x2, y2);
            dst_ptL[3] = new PointF(x3, y3);
        }

        public void SetDestinationPoint2f_Right(float x0, float x1, float x2, float x3, float y0, float y1, float y2, float y3)
        {
            dst_ptR[0] = new PointF(x0, y0);
            dst_ptR[1] = new PointF(x1, y1);
            dst_ptR[2] = new PointF(x2, y2);
            dst_ptR[3] = new PointF(x3, y3);
        }

        public void SetEasyRemoval_Parameters(double RPX, double RPY, double HIX, double HIY, double VIX, double VIY, double R, int HorizontalN, int VerticalN, bool ZigZag)
        {
            EasyRemoval_RPX = RPX;
            EasyRemoval_RPY = RPY;
            EasyRemoval_HIX = HIX;
            EasyRemoval_HIY = HIY;
            EasyRemoval_VIX = VIX;
            EasyRemoval_VIY = VIY;
            EasyRemoval_R = R;

            EasyRemovalhorizontalN = HorizontalN;
            EasyRemovalverticalN = VerticalN;
            EasyRemoval_ZigZag = ZigZag;
        }

        // 최종적으로 얻은 이미지 homographyMatrixScale 변수는 (1 미만인 경우) XScale, YScale에 의해 축소도 되고 좌표값도 안쪽으로 조정됨
        public void GetImagePerspectiveForm_Scale(double XScale, double YScale)
        {
            float x0, x1, x2, x3, y0, y1, y2, y3;

            // x0 ~ y3 구하기 (각각 좌상단, 우상단, 우하단, 좌하단 좌표)
            x0 = 4240 * (1 - (float)XScale) / 2 - 0.5f;
            y0 = 3840 * (1 - (float)YScale) / 2 - 0.5f;
            x1 = 4240 * (1 + (float)XScale) / 2 - 0.5f;
            y1 = 3840 * (1 - (float)YScale) / 2 - 0.5f;
            x2 = 4240 * (1 + (float)XScale) / 2 - 0.5f;
            y2 = 3840 * (1 + (float)YScale) / 2 - 0.5f;
            x3 = 4240 * (1 - (float)XScale) / 2 - 0.5f;
            y3 = 3840 * (1 + (float)YScale) / 2 - 0.5f;

            SetDestinationPoint2f_Scale(x0, x1, x2, x3, y0, y1, y2, y3);

            homographyMatrixScale = CvInvoke.GetPerspectiveTransform(src_pt_Scale, dst_pt_Scale);
        }

        public void GetImagePerspectiveForm_Left() { homographyMatrixLeft = CvInvoke.GetPerspectiveTransform(src_pt, dst_ptL); }

        public void GetImagePerspectiveForm_Right() { homographyMatrixRight = CvInvoke.GetPerspectiveTransform(src_pt, dst_ptR); }

        // 원본 슬라이스 이미지에서 Perspective 왜곡 보정을 수행하고 Left/Right 이미지로 분리함
        public string SplitImage(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            Mat resultImage = originalImage.Clone();

            CvInvoke.WarpPerspective(originalImage, resultImage, homographyMatrixScale, originalImage.Size);
            originalImage = resultImage.Clone();

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(originalImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(originalImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            //for (int i = 0; i < vertical; i++)
            //{
            //    for (int j = (horizontal / 2) - (margin / 2); j < (horizontal / 2) + (margin / 2); j++)
            //    {
            //        //splitImageLeft.at<unsigned char>(i, j) = int(double(splitImageLeft.at<unsigned char>(i, j)) * double((horizontal / 2) + (margin / 2) - j) / double(margin));
            //        image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((horizontal / 2) + (margin / 2) - j) / (double)margin), 0, 255);
            //    }
            //}
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_" + fileName), splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_" + fileName), ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            //for (int i = 0; i < vertical; i++)
            //{
            //    for (int j = 0; j < margin; j++)
            //    {
            //        //splitImageRight.at<unsigned char>(i, j) = int(double(splitImageRight.at<unsigned char>(i, j)) * double(j) / double(margin));
            //        image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
            //    }
            //}
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_" + fileName), splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_" + fileName) + "," + Path.Combine(imageProcessingPath, "Right_" + fileName));
        }

        public string SplitImageLeft(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(originalImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = (horizontal / 2) - (margin / 2); j < (horizontal / 2) + (margin / 2); j++)
                {
                    //splitImageLeft.at<unsigned char>(i, j) = int(double(splitImageLeft.at<unsigned char>(i, j)) * double((horizontal / 2) + (margin / 2) - j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((horizontal / 2) + (margin / 2) - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageLeft?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName);
        }

        public string SplitImageRight(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(originalImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //splitImageRight.at<unsigned char>(i, j) = int(double(splitImageRight.at<unsigned char>(i, j)) * double(j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageRight?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Right_") + fileName);
        }

        public string SplitImageForPrinting(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            Mat dstImage;

            dstImage = originalImage.Clone();

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = (horizontal / 2) - (margin / 2); j < (horizontal / 2) + (margin / 2); j++)
                {
                    //splitImageLeft.at<unsigned char>(i, j) = int(double(splitImageLeft.at<unsigned char>(i, j)) * double((horizontal / 2) + (margin / 2) - j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((horizontal / 2) + (margin / 2) - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //splitImageRight.at<unsigned char>(i, j) = int(double(splitImageRight.at<unsigned char>(i, j)) * double(j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            dstImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName + "," + Path.Combine(imageProcessingPath, "Right_") + fileName);
        }

        public string SplitImageForPrinting_New(string filePath, string fileName, int horizontal, int vertical, int margin, string removalPath, string removalName, bool isEasyRemoval)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixScale, originalImage.Size);
            Mat easyRemovalImage = CvInvoke.Imread(Path.Combine(removalPath, removalName), ImreadModes.Grayscale);
            Mat dstImage;

            dstImage = originalImage.Clone();

            if (isEasyRemoval)
                dstImage -= easyRemovalImage;

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = (horizontal / 2) - (margin / 2); j < (horizontal / 2) + (margin / 2); j++)
                {
                    //splitImageLeft.at<unsigned char>(i, j) = int(double(splitImageLeft.at<unsigned char>(i, j)) * double((horizontal / 2) + (margin / 2) - j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((horizontal / 2) + (margin / 2) - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //splitImageRight.at<unsigned char>(i, j) = int(double(splitImageRight.at<unsigned char>(i, j)) * double(j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            easyRemovalImage?.Dispose();
            dstImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName + "," + Path.Combine(imageProcessingPath, "Right_") + fileName);
        }

        public string SplitImageForPrinting_NewReverse(string filePath, string fileName, int horizontal, int vertical, int margin, string removalPath, string removalName, bool isEasyremoval)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixScale, originalImage.Size);
            Mat easyRemovalImage = CvInvoke.Imread(Path.Combine(removalPath, removalName), ImreadModes.Grayscale);
            Mat dstImage = originalImage.Clone();

            //cv::flip(originalImage, dstImage, 1);
            CvInvoke.Flip(originalImage, dstImage, FlipType.Horizontal);

            if (isEasyremoval)
                dstImage -= easyRemovalImage;

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = (horizontal / 2) - (margin / 2); j < (horizontal / 2) + (margin / 2); j++)
                {
                    //splitImageLeft.at<unsigned char>(i, j) = int(double(splitImageLeft.at<unsigned char>(i, j)) * double((horizontal / 2) + (margin / 2) - j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((horizontal / 2) + (margin / 2) - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < vertical; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //splitImageRight.at<unsigned char>(i, j) = int(double(splitImageRight.at<unsigned char>(i, j)) * double(j) / double(margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            easyRemovalImage?.Dispose();
            dstImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName + "," + Path.Combine(imageProcessingPath, "Right_") + fileName);
        }

        public string SplitImageExceptGradation(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(originalImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "LeftNoGrad_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(originalImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "RightNoGrad_") + fileName, splitImageRight);

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "LeftNoGrad_") + fileName + "," + Path.Combine(imageProcessingPath, "RightNoGrad_") + fileName);
        }

        public string SplitImageLeftExceptGradation(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Rectangle rectLeft = new(new Point(0, 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageLeft = new(originalImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "LeftNoGrad_") + fileName, splitImageLeft);

            splitImageLeft?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "LeftNoGrad_") + fileName);
        }

        public string SplitImageRightExceptGradation(string filePath, string fileName, int horizontal, int vertical, int margin)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Rectangle rectRight = new(new Point((horizontal / 2) - (margin / 2), 0), new Size((horizontal / 2) + (margin / 2), vertical));
            Mat splitImageRight = new(originalImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "RightNoGrad_") + fileName, splitImageRight);

            splitImageRight?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "RightNoGrad_") + fileName);
        }

        // Perspective 왜곡 보정을 수행함 (Left 이미지 전용)
        public string WarpImageLeft(string filePath, string fileName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixLeft, originalImage.Size);
            CvInvoke.Imwrite(Path.Combine(filePath, fileName), originalImage);

            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        // Perspective 왜곡 보정을 수행함 (Right 이미지 전용)
        public string WarpImageRight(string filePath, string fileName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixRight, originalImage.Size);
            CvInvoke.Imwrite(Path.Combine(filePath, fileName), originalImage);

            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        // 구면 왜곡 + Perspective 왜곡 보정 (Left 전용)
        public string CorrectAberrationAndPerspectiveDistortionImageLeft(string filePath, string fileName, double DistortionValue)
        {
            Mat homographyMatrix = homographyMatrixLeft;

            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            int rows = originalImage.Rows;
            int cols = originalImage.Cols;
            double k1 = DistortionValue;

            Mat MapX = new(rows, cols, DepthType.Cv32F, 1);
            Mat MapY = new(rows, cols, DepthType.Cv32F, 1);

            float[] MapXData = new float[rows * cols];
            float[] MapYData = new float[rows * cols];

            // 좌상단이 (0, 0)인 이미지의 중심 좌표
            double centerX = (cols - 1) / 2.0;
            double centerY = (rows - 1) / 2.0;

            // 중심에서 꼭지점까지의 거리
            double maxDistance = Math.Sqrt(centerX * centerX + centerY * centerY);

            // 새로운 보정된 k1 적용
            double adjustedK1 = k1 / (1 + k1 * maxDistance * maxDistance);

            // 원본 이미지의 네 개의 꼭지점
            PointF[] originalCorners = new PointF[]
            {
                    new PointF(0, 0),                  // 좌상단
                    new PointF(cols - 1, 0),           // 우상단
                    new PointF(0, rows - 1),           // 좌하단
                    new PointF(cols - 1, rows - 1)     // 우하단
            };

            // 왜곡 후 꼭지점 위치 저장용
            PointF[] distortedCorners = new PointF[4];

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    int index = j * cols + i;

                    // 좌상단이 (0, 0)인 이미지의 중심을 (0, 0)으로 이동시킴
                    double pixelForDistortionX = i - centerX;
                    double pixelForDistortionY = j - centerY;

                    // 중심으로부터 해당 픽셀까지의 거리를 계산함
                    double pixelDistanceBefore = Math.Sqrt(pixelForDistortionX * pixelForDistortionX + pixelForDistortionY * pixelForDistortionY);

                    // 각 픽셀의 거리를 기반으로 왜곡 보정 값을 구함
                    double distanceAdjustment = 1 + adjustedK1 * Math.Pow(pixelDistanceBefore, 2.0);

                    // 왜곡 보정 값이 적용된 새로운 픽셀 위치 값을 계산함
                    double pixelDistortedX = distanceAdjustment * pixelForDistortionX;
                    double pixelDistortedY = distanceAdjustment * pixelForDistortionY;

                    // 새로운 픽셀 위치 값을 좌상단이 (0, 0)인 기준으로 이동시킴
                    double distortedX = pixelDistortedX + centerX;
                    double distortedY = pixelDistortedY + centerY;

                    // Map 데이터를 만듦
                    MapXData[index] = (float)distortedX;
                    MapYData[index] = (float)distortedY;

                    // 꼭지점 좌표 변환 확인
                    if (i == 0 && j == 0)
                        distortedCorners[0] = new PointF((float)distortedX, (float)distortedY);
                    if (i == cols - 1 && j == 0)
                        distortedCorners[1] = new PointF((float)distortedX, (float)distortedY);
                    if (i == 0 && j == rows - 1)
                        distortedCorners[2] = new PointF((float)distortedX, (float)distortedY);
                    if (i == cols - 1 && j == rows - 1)
                        distortedCorners[3] = new PointF((float)distortedX, (float)distortedY);
                }
            }

            Marshal.Copy(MapXData, 0, MapX.DataPointer, MapXData.Length);
            Marshal.Copy(MapYData, 0, MapY.DataPointer, MapYData.Length);

            Mat distortedImage;
            Mat resizeImage;
            Mat resultImage;

            // 핀쿠션 왜곡 보정을 위해 배럴 왜곡 발생시킬 경우
            if (adjustedK1 > 0)
            {
                // 왜곡된 네 모서리의 최소, 최대 좌표를 구하여 새로운 출력 이미지 크기를 결정 (추가 연산)
                float minX = distortedCorners.Min(pt => pt.X);
                float minY = distortedCorners.Min(pt => pt.Y);
                float maxX = distortedCorners.Max(pt => pt.X);
                float maxY = distortedCorners.Max(pt => pt.Y);
                int newWidth = (int)Math.Ceiling(maxX - minX);
                int newHeight = (int)Math.Ceiling(maxY - minY);

                // 출력 이미지 크기가 newWidth x newHeight일 때 출력 이미지의 중심
                float newCenterX = newWidth / 2.0f;
                float newCenterY = newHeight / 2.0f;

                // 원본 이미지의 중심
                float originalCenterX = cols / 2.0f;
                float originalCenterY = rows / 2.0f;

                // 기존 distortedCorners가 원래 이미지 기준이라면, 이들을 출력 이미지의 중심에 맞추기 위해 오프셋 계산
                float offsetX = newCenterX - originalCenterX;
                float offsetY = newCenterY - originalCenterY;

                // 목적 좌표 shiftedDistortedCorners를 재정의: 기존 distortedCorners에 오프셋을 추가
                PointF[] shiftedDistortedCorners = distortedCorners
                    .Select(pt => new PointF(pt.X + offsetX, pt.Y + offsetY))
                    .ToArray();

                distortedImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resizeImage = new(newHeight, newWidth, originalImage.Depth, originalImage.NumberOfChannels);
                resultImage = new(newHeight, newWidth, originalImage.Depth, originalImage.NumberOfChannels);

                // 픽셀 이동
                CvInvoke.Remap(originalImage, distortedImage, MapX, MapY, Inter.Linear);

                // 이미지 확장
                Mat warpMatrix = CvInvoke.GetPerspectiveTransform(originalCorners, shiftedDistortedCorners);
                CvInvoke.WarpPerspective(distortedImage, resizeImage, warpMatrix, new Size(newWidth, newHeight));

                // Perspective 왜곡 보정
                CvInvoke.WarpPerspective(resizeImage, resizeImage, homographyMatrix, resizeImage.Size);

                // 원본 이미지의 영역 (예: 좌상단 기준, 크기는 Size(cols, rows))
                Rectangle centerCrop = new Rectangle((resizeImage.Width - cols) / 2, (resizeImage.Height - rows) / 2, cols, rows);

                Mat innerImage = new Mat(resizeImage, centerCrop);

                CvInvoke.Imwrite(Path.Combine(filePath, fileName), innerImage);

                return new string(Path.Combine(filePath, fileName));
            }
            // 배럴 왜곡 보정을 위해 핀쿠션 왜곡 발생시킬 경우
            else
            {
                distortedImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resizeImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resultImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);

                // 이미지 축소
                Mat warpMatrix = CvInvoke.GetPerspectiveTransform(originalCorners, distortedCorners);
                CvInvoke.WarpPerspective(originalImage, resizeImage, warpMatrix, new Size(cols, rows));

                // 픽셀 이동
                CvInvoke.Remap(resizeImage, distortedImage, MapX, MapY, Inter.Linear);

                // Perspective 왜곡 보정
                CvInvoke.WarpPerspective(distortedImage, resultImage, homographyMatrix, originalImage.Size);

                CvInvoke.Imwrite(Path.Combine(filePath, fileName), resultImage);

                return new string(Path.Combine(filePath, fileName));
            }
        }

        // 구면 왜곡 + Perspective 왜곡 보정 (Right 전용)
        public string CorrectAberrationAndPerspectiveDistortionImageRight(string filePath, string fileName, double DistortionValue)
        {
            Mat homographyMatrix = homographyMatrixRight;

            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            int rows = originalImage.Rows;
            int cols = originalImage.Cols;
            double k1 = DistortionValue;

            Mat MapX = new(rows, cols, DepthType.Cv32F, 1);
            Mat MapY = new(rows, cols, DepthType.Cv32F, 1);

            float[] MapXData = new float[rows * cols];
            float[] MapYData = new float[rows * cols];

            // 좌상단이 (0, 0)인 이미지의 중심 좌표
            double centerX = (cols - 1) / 2.0;
            double centerY = (rows - 1) / 2.0;

            // 중심에서 꼭지점까지의 거리
            double maxDistance = Math.Sqrt(centerX * centerX + centerY * centerY);

            // 새로운 보정된 k1 적용
            double adjustedK1 = k1 / (1 + k1 * maxDistance * maxDistance);

            // 원본 이미지의 네 개의 꼭지점
            PointF[] originalCorners = new PointF[]
            {
                    new PointF(0, 0),                  // 좌상단
                    new PointF(cols - 1, 0),           // 우상단
                    new PointF(0, rows - 1),           // 좌하단
                    new PointF(cols - 1, rows - 1)     // 우하단
            };

            // 왜곡 후 꼭지점 위치 저장용
            PointF[] distortedCorners = new PointF[4];

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    int index = j * cols + i;

                    // 좌상단이 (0, 0)인 이미지의 중심을 (0, 0)으로 이동시킴
                    double pixelForDistortionX = i - centerX;
                    double pixelForDistortionY = j - centerY;

                    // 중심으로부터 해당 픽셀까지의 거리를 계산함
                    double pixelDistanceBefore = Math.Sqrt(pixelForDistortionX * pixelForDistortionX + pixelForDistortionY * pixelForDistortionY);

                    // 각 픽셀의 거리를 기반으로 왜곡 보정 값을 구함
                    double distanceAdjustment = 1 + adjustedK1 * Math.Pow(pixelDistanceBefore, 2.0);

                    // 왜곡 보정 값이 적용된 새로운 픽셀 위치 값을 계산함
                    double pixelDistortedX = distanceAdjustment * pixelForDistortionX;
                    double pixelDistortedY = distanceAdjustment * pixelForDistortionY;

                    // 새로운 픽셀 위치 값을 좌상단이 (0, 0)인 기준으로 이동시킴
                    double distortedX = pixelDistortedX + centerX;
                    double distortedY = pixelDistortedY + centerY;

                    // Map 데이터를 만듦
                    MapXData[index] = (float)distortedX;
                    MapYData[index] = (float)distortedY;

                    // 꼭지점 좌표 변환 확인
                    if (i == 0 && j == 0)
                        distortedCorners[0] = new PointF((float)distortedX, (float)distortedY);
                    if (i == cols - 1 && j == 0)
                        distortedCorners[1] = new PointF((float)distortedX, (float)distortedY);
                    if (i == 0 && j == rows - 1)
                        distortedCorners[2] = new PointF((float)distortedX, (float)distortedY);
                    if (i == cols - 1 && j == rows - 1)
                        distortedCorners[3] = new PointF((float)distortedX, (float)distortedY);
                }
            }

            Marshal.Copy(MapXData, 0, MapX.DataPointer, MapXData.Length);
            Marshal.Copy(MapYData, 0, MapY.DataPointer, MapYData.Length);

            Mat distortedImage;
            Mat resizeImage;
            Mat resultImage;

            // 핀쿠션 왜곡 보정을 위해 배럴 왜곡 발생시킬 경우
            if (adjustedK1 > 0)
            {
                // 왜곡된 네 모서리의 최소, 최대 좌표를 구하여 새로운 출력 이미지 크기를 결정 (추가 연산)
                float minX = distortedCorners.Min(pt => pt.X);
                float minY = distortedCorners.Min(pt => pt.Y);
                float maxX = distortedCorners.Max(pt => pt.X);
                float maxY = distortedCorners.Max(pt => pt.Y);
                int newWidth = (int)Math.Ceiling(maxX - minX);
                int newHeight = (int)Math.Ceiling(maxY - minY);

                // 출력 이미지 크기가 newWidth x newHeight일 때 출력 이미지의 중심
                float newCenterX = newWidth / 2.0f;
                float newCenterY = newHeight / 2.0f;

                // 원본 이미지의 중심
                float originalCenterX = cols / 2.0f;
                float originalCenterY = rows / 2.0f;

                // 기존 distortedCorners가 원래 이미지 기준이라면, 이들을 출력 이미지의 중심에 맞추기 위해 오프셋 계산
                float offsetX = newCenterX - originalCenterX;
                float offsetY = newCenterY - originalCenterY;

                // 목적 좌표 shiftedDistortedCorners를 재정의: 기존 distortedCorners에 오프셋을 추가
                PointF[] shiftedDistortedCorners = distortedCorners
                    .Select(pt => new PointF(pt.X + offsetX, pt.Y + offsetY))
                    .ToArray();

                distortedImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resizeImage = new(newHeight, newWidth, originalImage.Depth, originalImage.NumberOfChannels);
                resultImage = new(newHeight, newWidth, originalImage.Depth, originalImage.NumberOfChannels);

                // 픽셀 이동
                CvInvoke.Remap(originalImage, distortedImage, MapX, MapY, Inter.Linear);

                // 이미지 확장
                Mat warpMatrix = CvInvoke.GetPerspectiveTransform(originalCorners, shiftedDistortedCorners);
                CvInvoke.WarpPerspective(distortedImage, resizeImage, warpMatrix, new Size(newWidth, newHeight));

                // Perspective 왜곡 보정
                CvInvoke.WarpPerspective(resizeImage, resizeImage, homographyMatrix, resizeImage.Size);

                // 원본 이미지의 영역 (예: 좌상단 기준, 크기는 Size(cols, rows))
                Rectangle centerCrop = new Rectangle((resizeImage.Width - cols) / 2, (resizeImage.Height - rows) / 2, cols, rows);

                Mat innerImage = new Mat(resizeImage, centerCrop);

                CvInvoke.Imwrite(Path.Combine(filePath, fileName), innerImage);

                return new string(Path.Combine(filePath, fileName));
            }
            // 배럴 왜곡 보정을 위해 핀쿠션 왜곡 발생시킬 경우
            else
            {
                distortedImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resizeImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);
                resultImage = new(rows, cols, originalImage.Depth, originalImage.NumberOfChannels);

                // 이미지 축소
                Mat warpMatrix = CvInvoke.GetPerspectiveTransform(originalCorners, distortedCorners);
                CvInvoke.WarpPerspective(originalImage, resizeImage, warpMatrix, new Size(cols, rows));

                // 픽셀 이동
                CvInvoke.Remap(resizeImage, distortedImage, MapX, MapY, Inter.Linear);

                // Perspective 왜곡 보정
                CvInvoke.WarpPerspective(distortedImage, resultImage, homographyMatrix, originalImage.Size);

                CvInvoke.Imwrite(Path.Combine(filePath, fileName), resultImage);

                return new string(Path.Combine(filePath, fileName));
            }
        }

        // Double Exposure 이미지 생성
        public string DoubleExposureImage(string filePath, string fileName, int doubleExposureThreshold)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            Mat analysisImage = originalImage.Clone();

            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));   // Mat::ones(cv::Size(3, 3), CV_8UC1)

            CvInvoke.Erode(analysisImage, analysisImage, kernel, new Point(-1, -1), doubleExposureThreshold, BorderType.Default, default);
            CvInvoke.Dilate(analysisImage, analysisImage, kernel, new Point(-1, -1), doubleExposureThreshold + 1, BorderType.Default, default);

            Mat resultImage = originalImage - analysisImage;

            MCvScalar ForMatMean;
            double MatMean;

            ForMatMean = CvInvoke.Mean(resultImage);
            MatMean = ForMatMean.V0;
            //printf("Mean : %f \n", MatMean);

            //if(MatMean != 0)
            CvInvoke.Imwrite(Path.Combine(filePath, "DoubleExposure_") + fileName, resultImage);

            resultImage?.Dispose();
            kernel?.Dispose();
            analysisImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        public string WarpImageLeftForEdit(string filePath, string fileName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixLeft, originalImage.Size);
            CvInvoke.Imwrite(Path.Combine(filePath, "ImageProcessing", fileName), originalImage);

            originalImage?.Dispose();

            return new string(Path.Combine(filePath, "ImageProcessing", fileName));
        }

        public string WarpImageRightForEdit(string filePath, string fileName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixRight, originalImage.Size);
            CvInvoke.Imwrite(Path.Combine(filePath, "ImageProcessing", fileName), originalImage);

            originalImage?.Dispose();

            return new string(Path.Combine(filePath, "ImageProcessing", fileName));
        }

        // 그리드 별로 일정하지 않은 광도를 보정하기 위한 Mask 이미지를 만듦 (Left 전용)
        public string MaskImageLeft(string filePath, string fileName, string maskPath, string maskName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            Mat maskImageL = CvInvoke.Imread(Path.Combine(maskPath, maskName), ImreadModes.Grayscale);

            Image<Gray, byte> img_originalImage = originalImage.ToImage<Gray, byte>();
            Image<Gray, byte> img_maskImageL = maskImageL.ToImage<Gray, byte>();
            for (int i = 0; i < originalImage.Cols; i++)
            {
                for (int j = 0; j < originalImage.Rows; j++)
                {
                    //originalImage.at<unsigned char>(j, i) = originalImage.at<unsigned char>(j, i) * _maskImageL.at<unsigned char>(j, i) / 255;
                    img_originalImage.Data[j, i, 0] = (byte)(img_originalImage.Data[j, i, 0] * img_maskImageL.Data[j, i, 0] / 255);
                }
            }
            originalImage = img_originalImage.Mat;
            maskImageL = img_maskImageL.Mat;

            CvInvoke.Imwrite(Path.Combine(filePath, fileName), originalImage);

            img_originalImage?.Dispose();
            img_maskImageL?.Dispose();

            maskImageL?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        // 그리드 별로 일정하지 않은 광도를 보정하기 위한 Mask 이미지를 만듦 (Right 전용)
        public string MaskImageRight(string filePath, string fileName, string maskPath, string maskName)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);
            Mat maskImageR = CvInvoke.Imread(Path.Combine(maskPath, maskName), ImreadModes.Grayscale);

            Image<Gray, byte> img_originalImage = originalImage.ToImage<Gray, byte>();
            Image<Gray, byte> img_maskImageR = maskImageR.ToImage<Gray, byte>();
            for (int i = 0; i < originalImage.Cols; i++)
            {
                for (int j = 0; j < originalImage.Rows; j++)
                {
                    //originalImage.at<unsigned char>(j, i) = originalImage.at<unsigned char>(j, i) * _maskImageR.at<unsigned char>(j, i) / 255;
                    img_originalImage.Data[j, i, 0] = (byte)(img_originalImage.Data[j, i, 0] * img_maskImageR.Data[j, i, 0] / 255);
                }
            }
            originalImage = img_originalImage.Mat;
            maskImageR = img_maskImageR.Mat;

            CvInvoke.Imwrite(Path.Combine(filePath, fileName), originalImage);

            img_originalImage?.Dispose();
            img_maskImageR?.Dispose();

            maskImageR?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        // Easy Removal 이미지 생성
        public string MakeEasyRemovalImage(string filePath, string fileName)
        {
            Mat EasyRemovalImage = new(3840, 4240, DepthType.Cv8U, 1);
            EasyRemovalImage.SetTo(new MCvScalar(0)); // Set the image to black

            for (int VerticalN = 0; VerticalN < EasyRemovalverticalN; VerticalN++)
            {
                if (EasyRemoval_ZigZag)
                {
                    for (int HorizontalN = 0 - (VerticalN / 2); HorizontalN < (EasyRemovalhorizontalN - VerticalN / 2); HorizontalN++)
                    {
                        Point center = new(
                            (int)(EasyRemoval_RPX + EasyRemoval_HIX * HorizontalN + EasyRemoval_VIX * VerticalN),
                            (int)(EasyRemoval_RPY + EasyRemoval_HIY * HorizontalN + EasyRemoval_VIY * VerticalN)
                        );
                        CvInvoke.Circle(EasyRemovalImage, center, (int)EasyRemoval_R, new MCvScalar(255, 255, 255), -1);
                    }
                }
                else
                {
                    for (int HorizontalN = 0; HorizontalN < (EasyRemovalhorizontalN / 2); HorizontalN++)
                    {
                        Point center = new(
                            (int)(EasyRemoval_RPX + EasyRemoval_HIX * HorizontalN + EasyRemoval_VIX * VerticalN),
                            (int)(EasyRemoval_RPY + EasyRemoval_HIY * HorizontalN + EasyRemoval_VIY * VerticalN)
                        );
                        CvInvoke.Circle(EasyRemovalImage, center, (int)EasyRemoval_R, new MCvScalar(255, 255, 255), -1);
                    }
                }
            }

            CvInvoke.Imwrite(Path.Combine(filePath, fileName), EasyRemovalImage);

            EasyRemovalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        public string ApplyEasyRemovalToImage(string srcFilePath, string srcFileName, string easyRemovalFilePath, string easyRemovalFileName, int horizontal, int vertical)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(srcFilePath, srcFileName), ImreadModes.Grayscale);
            Mat removalImage = CvInvoke.Imread(Path.Combine(easyRemovalFilePath, easyRemovalFileName), ImreadModes.Grayscale);

            originalImage -= removalImage;

            CvInvoke.Imwrite(Path.Combine(srcFilePath, srcFileName), originalImage);

            removalImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(srcFilePath, srcFileName));
        }

        public string FlipImage(string filePath, string fileName, int flipMode)
        {
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            //cv::flip(originalImage, originalImage, flipMode);
            if (flipMode == 0)
                CvInvoke.Flip(originalImage, originalImage, FlipType.Vertical);
            else if (flipMode == 1)
                CvInvoke.Flip(originalImage, originalImage, FlipType.Horizontal);
            else if (flipMode == -1)
                CvInvoke.Flip(originalImage, originalImage, FlipType.Both);

            CvInvoke.Imwrite(Path.Combine(filePath, fileName), originalImage);

            originalImage?.Dispose();

            return new string(Path.Combine(filePath, fileName));
        }

        public string SplitImageGeneralized(string filePath, string fileName, int oriX, int oriY, int margin, int black)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            // 이미지 배율 조정
            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixScale, originalImage.Size);

            // margin 폭에 따른 image 좌우 끝 black image 추가
            if (black > 0)
            {
                Mat originalBlackImage = CvInvoke.Imread(Path.Combine(filePath, "blackImage.png"), ImreadModes.Grayscale);

                Rectangle rectBlack = new(new Point(0, 0), new Size(black, oriY));
                Mat newBlackImage = new(originalBlackImage, rectBlack);

                CvInvoke.HConcat(newBlackImage, originalImage, originalImage);
                CvInvoke.HConcat(originalImage, newBlackImage, originalImage);

                originalBlackImage?.Dispose();
            }

            Mat dstImage = originalImage.Clone();

            // 이미지 좌우 분할
            Rectangle rectLeft = new(new Point(0, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_" + fileName), splitImageLeft);

            Rectangle rectRight = new(new Point((oriX / 2) - (margin / 2) + black, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_" + fileName), splitImageRight);

            //좌우 이미지 그라데이션 적용
            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_" + fileName), ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = (oriX / 2) - (margin / 2) + black; j < (oriX / 2) + (margin / 2) + black; j++)
                {
                    //_splitImageLeft.at<unsigned char>(i, j) = int(double(_splitImageLeft.at<unsigned char>(i, j)) * double((_oriX / 2) + (_margin / 2) + _black - j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((oriX / 2) + (margin / 2) + black - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_" + fileName), splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_" + fileName), ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //_splitImageRight.at<unsigned char>(i, j) = int(double(_splitImageRight.at<unsigned char>(i, j)) * double(j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_" + fileName), splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_" + fileName + "," + imageProcessingPath, "Right_" + fileName));
        }

        public string SplitImageGeneralizedForPrinting_crmaslice(string filePath, string fileName, int oriX, int oriY, int margin, int black, string removalPath, string removalName, bool isEasyRemoval)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            // 이미지 배율 조정
            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixScale, originalImage.Size);

            // easyremoval 적용
            Mat easyRemovalImage = CvInvoke.Imread(Path.Combine(removalPath, removalName), ImreadModes.Grayscale);
            Mat dstImage;

            dstImage = originalImage.Clone();

            if (isEasyRemoval)
                dstImage -= easyRemovalImage;

            // margin 폭에 따른 image 좌우 끝 black image 추가
            if (black > 0)
            {
                Mat originalBlackImage = CvInvoke.Imread(Path.Combine(filePath, "blackImage.png"), ImreadModes.Grayscale);

                Rectangle rectBlack = new(new Point(0, 0), new Size(black, oriY));
                Mat newBlackImage = new(originalBlackImage, rectBlack);

                CvInvoke.HConcat(newBlackImage, dstImage, dstImage);
                CvInvoke.HConcat(dstImage, newBlackImage, dstImage);

                originalBlackImage?.Dispose();
            }

            // 이미지 좌우 분할
            Rectangle rectLeft = new(new Point(0, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((oriX / 2) - (margin / 2) + black, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            // 좌우 이미지 그라데이션 적용
            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = (oriX / 2) - (margin / 2) + black; j < (oriX / 2) + (margin / 2) + black; j++)
                {
                    //_splitImageLeft.at<unsigned char>(i, j) = int(double(_splitImageLeft.at<unsigned char>(i, j)) * double((_oriX / 2) + (_margin / 2) + _black - j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((oriX / 2) + (margin / 2) + black - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //_splitImageRight.at<unsigned char>(i, j) = int(double(_splitImageRight.at<unsigned char>(i, j)) * double(j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            easyRemovalImage?.Dispose();
            dstImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName + "," + Path.Combine(imageProcessingPath, "Right_") + fileName);
        }

        public string SplitImageGeneralizedForPrinting_slice(string filePath, string fileName, int oriX, int oriY, int margin, int black, string removalPath, string removalName, bool isEasyRemoval)
        {
            string imageProcessingPath = Path.Combine(filePath, "ImageProcessing");
            DirectoryInfo directoryInfo = new(imageProcessingPath);

            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }
            Mat originalImage = CvInvoke.Imread(Path.Combine(filePath, fileName), ImreadModes.Grayscale);

            // 이미지 배율 조정
            CvInvoke.WarpPerspective(originalImage, originalImage, homographyMatrixScale, originalImage.Size);

            // easyremoval 적용
            Mat easyRemovalImage = CvInvoke.Imread(Path.Combine(removalPath, removalName), ImreadModes.Grayscale);
            Mat dstImage;

            dstImage = originalImage.Clone();

            CvInvoke.Flip(originalImage, dstImage, FlipType.Horizontal);

            if (isEasyRemoval)
                dstImage -= easyRemovalImage;

            // margin 폭에 따른 image 좌우 끝 black image 추가 
            if (black > 0)
            {
                Mat originalBlackImage = CvInvoke.Imread(Path.Combine(filePath, "blackImage.png"), ImreadModes.Grayscale);

                Rectangle rectBlack = new(new Point(0, 0), new Size(black, oriY));
                Mat newBlackImage = new(originalBlackImage, rectBlack);

                CvInvoke.HConcat(newBlackImage, dstImage, dstImage);
                CvInvoke.HConcat(dstImage, newBlackImage, dstImage);

                originalBlackImage?.Dispose();
            }

            // 이미지 좌우 분할 
            Rectangle rectLeft = new(new Point(0, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageLeft = new(dstImage, rectLeft);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);

            Rectangle rectRight = new(new Point((oriX / 2) - (margin / 2) + black, 0), new Size((oriX / 2) + (margin / 2) + black, oriY));
            Mat splitImageRight = new(dstImage, rectRight);
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);

            // 좌우 이미지 그라데이션 적용
            splitImageLeft = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Left_") + fileName, ImreadModes.Grayscale);
            Image<Gray, byte> image = splitImageLeft.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = (oriX / 2) - (margin / 2) + black; j < (oriX / 2) + (margin / 2) + black; j++)
                {
                    //_splitImageLeft.at<unsigned char>(i, j) = int(double(_splitImageLeft.at<unsigned char>(i, j)) * double((_oriX / 2) + (_margin / 2) + _black - j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp(((double)image.Data[i, j, 0] * (double)((oriX / 2) + (margin / 2) + black - j) / (double)margin), 0, 255);
                }
            }
            splitImageLeft = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Left_") + fileName, splitImageLeft);
            image?.Dispose();

            splitImageRight = CvInvoke.Imread(Path.Combine(imageProcessingPath, "Right_") + fileName, ImreadModes.Grayscale);
            image = splitImageRight.ToImage<Gray, byte>();
            for (int i = 0; i < oriY; i++)
            {
                for (int j = 0; j < margin; j++)
                {
                    //_splitImageRight.at<unsigned char>(i, j) = int(double(_splitImageRight.at<unsigned char>(i, j)) * double(j) / double(_margin));
                    image.Data[i, j, 0] = (byte)Math.Clamp((image.Data[i, j, 0] * (double)j / (double)margin), 0, 255);
                }
            }
            splitImageRight = image.Mat;
            CvInvoke.Imwrite(Path.Combine(imageProcessingPath, "Right_") + fileName, splitImageRight);
            image?.Dispose();

            splitImageLeft?.Dispose();
            splitImageRight?.Dispose();
            easyRemovalImage?.Dispose();
            dstImage?.Dispose();
            originalImage?.Dispose();

            return new string(Path.Combine(imageProcessingPath, "Left_") + fileName + "," + Path.Combine(imageProcessingPath, "Right_") + fileName);
        }
    }
}
