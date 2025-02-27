using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Path = System.IO.Path;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing.Printing;

namespace AberrationDistortionExperiment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string? imgFilePath;
        string? imgFileName;
        string? selectedDirectoryPath;
        string? sourceImageDirectoryPath;

        ImageProcessingManagement imageProcessingManagement;

        public MainWindow()
        {
            InitializeComponent();

            //imgFilePath = null;
            //selectedDirectoryPath = null;

            imgFilePath = @"C:\Users\User\Desktop\구면 왜곡 테스트\verification.png";
            selectedDirectoryPath = @"C:\Users\User\Desktop\구면 왜곡 테스트";
        }

        private void MakeOutputImages_Click(object sender, RoutedEventArgs e)
        {
            // ============================================================================================================================== BackgroundWorker_BootingThread_DoWork
            Initialization();
            // ============================================================================================================================== BackgroundWorker_PrintingImageProcessing_DoWork
            /*
            // 구면 왜곡 보정 (0.0은 미적용)
            imageProcessingManagement.CorrectAberrationDistortionImage(selectedDirectoryPath, imgFileName, double.Parse(AberrationDistortionConstant.Text));

            // 이미지를 나눔 (Easy Removal Mask : 적용하지 않음)
            imageProcessingManagement.SplitImage(selectedDirectoryPath, imgFileName, 4240, 3840, 80);

            // Perspective 왜곡 보정
            imageProcessingManagement.WarpImageLeft(Path.Combine(selectedDirectoryPath, "ImageProcessing"), "Left_" + imgFileName);
            imageProcessingManagement.WarpImageRight(Path.Combine(selectedDirectoryPath, "ImageProcessing"), "Right_" + imgFileName);

            // Mask Image 적용
            imageProcessingManagement.MaskImageLeft(Path.Combine(selectedDirectoryPath, "ImageProcessing"), "Left_" + imgFileName, sourceImageDirectoryPath, "MaskL.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(selectedDirectoryPath, "ImageProcessing"), "Right_" + imgFileName, sourceImageDirectoryPath, "MaskR.png");
            */

            if (selectedDirectoryPath == null || imgFileName == null)
            {
                System.Windows.Forms.MessageBox.Show("경로 및 파일 설정 누락", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ImageProcessing(imgFileName);

            // 완료 메시지
            System.Windows.Forms.MessageBox.Show("이미지 처리 완료", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Initialization()
        {
            imageProcessingManagement = new();

            imgFileName = Path.GetFileName(imgFilePath);

            sourceImageDirectoryPath = Path.Combine(selectedDirectoryPath, "Source_Image");

            // ============================================================================================================================== BackgroundWorker_BootingThread_DoWork
            // 전처리
            //imageProcessingManagement.GetImagePerspectiveForm_Scale(
            //    Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_XScale")),
            //    Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_YScale"))
            //);
            imageProcessingManagement.GetImagePerspectiveForm_Scale(1.0, 1.0);

            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "fullImg_4240x3840.png", 4240, 3840, 80);
            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "focusImg_4240x3840.png", 4240, 3840, 80);
            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "gridImg_4240x3840.png", 4240, 3840, 80);
            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "testImg_4240x3840.png", 4240, 3840, 80);
            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "EasyRemovalMask_4240x3840.png", 4240, 3840, 80);
            imageProcessingManagement.SplitImage(sourceImageDirectoryPath, "EasyRemovalMask_4240x3840.png", 4240, 3840, 80);

            //imageProcessingManagement.SetDestinationPoint2f_Left(
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_X0")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_X1")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_X2")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_X3")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_Y0")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_Y1")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_Y2")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineLeft_ImageEdit_DestinationPosition_Y3")));
            imageProcessingManagement.SetDestinationPoint2f_Left(0 + float.Parse(Position_PointX0.Text), 2159 - float.Parse(Position_PointX1.Text), 2159 - float.Parse(Position_PointX2.Text), float.Parse(Position_PointX3.Text), float.Parse(Position_PointY0.Text), float.Parse(Position_PointY1.Text), 3839 - float.Parse(Position_PointY2.Text), 3839 - float.Parse(Position_PointY3.Text));

            imageProcessingManagement.GetImagePerspectiveForm_Left();

            imageProcessingManagement.WarpImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_fullImg_4240x3840.png");
            imageProcessingManagement.MaskImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_fullImg_4240x3840.png", sourceImageDirectoryPath, "MaskL.png");

            imageProcessingManagement.WarpImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_focusImg_4240x3840.png");
            imageProcessingManagement.MaskImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_focusImg_4240x3840.png", sourceImageDirectoryPath, "MaskL.png");

            imageProcessingManagement.WarpImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_gridImg_4240x3840.png");
            imageProcessingManagement.MaskImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_gridImg_4240x3840.png", sourceImageDirectoryPath, "MaskL.png");

            imageProcessingManagement.WarpImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_testImg_4240x3840.png");
            imageProcessingManagement.MaskImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_testImg_4240x3840.png", sourceImageDirectoryPath, "MaskL.png");

            imageProcessingManagement.WarpImageLeft(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Left_EasyRemovalMask_4240x3840.png");

            //imageProcessingManagement.SetDestinationPoint2f_Right(
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_X0")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_X1")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_X2")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_X3")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_Y0")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_Y1")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_Y2")),
            //    (float)Convert.ToDouble(this.dataSaveAndLoad.GetData("LightEngine_EngineRight_ImageEdit_DestinationPosition_Y3")));
            imageProcessingManagement.SetDestinationPoint2f_Right(0 + float.Parse(Position_PointX0.Text), 2159 - float.Parse(Position_PointX1.Text), 2159 - float.Parse(Position_PointX2.Text), float.Parse(Position_PointX3.Text), float.Parse(Position_PointY0.Text), float.Parse(Position_PointY1.Text), 3839 - float.Parse(Position_PointY2.Text), 3839 - float.Parse(Position_PointY3.Text));

            imageProcessingManagement.GetImagePerspectiveForm_Right();

            imageProcessingManagement.WarpImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_fullImg_4240x3840.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_fullImg_4240x3840.png", sourceImageDirectoryPath, "MaskR.png");

            imageProcessingManagement.WarpImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_focusImg_4240x3840.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_focusImg_4240x3840.png", sourceImageDirectoryPath, "MaskR.png");

            imageProcessingManagement.WarpImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_gridImg_4240x3840.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_gridImg_4240x3840.png", sourceImageDirectoryPath, "MaskR.png");

            imageProcessingManagement.WarpImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_testImg_4240x3840.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_testImg_4240x3840.png", sourceImageDirectoryPath, "MaskR.png");

            imageProcessingManagement.WarpImageRight(Path.Combine(sourceImageDirectoryPath, "ImageProcessing"), "Right_EasyRemovalMask_4240x3840.png");
        }

        // 이미지 분할 -> 구면 왜곡 -> Perspective 왜곡 보정 -> Mask Image 적용
        private void ImageProcessing(string targetImageFileName)
        {
            string newDirectory = Path.Combine(selectedDirectoryPath, "Result", Path.GetFileNameWithoutExtension(targetImageFileName));

            if (!Directory.Exists(newDirectory)) Directory.CreateDirectory(newDirectory);

            // newDirectory 안에 생성할 이미지는 다음과 같습니다.
            // 1. 입력 이미지 저장
            Mat image1 = CvInvoke.Imread(Path.Combine(selectedDirectoryPath, targetImageFileName), ImreadModes.Grayscale);
            CvInvoke.Imwrite(Path.Combine(selectedDirectoryPath, newDirectory, "1. 입력 이미지.png"), image1);

            // 2. 이미지 분할 후
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "1. 입력 이미지.png"), Path.Combine(selectedDirectoryPath, newDirectory, "2. 이미지 분할 후.png"), true);
            imageProcessingManagement.SplitImage(Path.Combine(selectedDirectoryPath, newDirectory), "2. 이미지 분할 후.png", 4240, 3840, 80);
            File.Delete(Path.Combine(selectedDirectoryPath, newDirectory, "2. 이미지 분할 후.png"));
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "ImageProcessing", "Left_2. 이미지 분할 후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Left_2. 이미지 분할 후.png"), true);
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "ImageProcessing", "Right_2. 이미지 분할 후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Right_2. 이미지 분할 후.png"), true);

            // 3. 구면 왜곡 + Perspective 보정 이후
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "Left_2. 이미지 분할 후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Left_3. 구면 왜곡 + Perspective 보정 이후.png"), true);
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "Right_2. 이미지 분할 후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Right_3. 구면 왜곡 + Perspective 보정 이후.png"), true);
            imageProcessingManagement.CorrectAberrationAndPerspectiveDistortionImageLeft(Path.Combine(selectedDirectoryPath, newDirectory), "Left_3. 구면 왜곡 + Perspective 보정 이후.png", double.Parse(AberrationDistortionConstant.Text));
            imageProcessingManagement.CorrectAberrationAndPerspectiveDistortionImageRight(Path.Combine(selectedDirectoryPath, newDirectory), "Right_3. 구면 왜곡 + Perspective 보정 이후.png", double.Parse(AberrationDistortionConstant.Text));

            // 4. Mask Image 적용 후
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "Left_3. 구면 왜곡 + Perspective 보정 이후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Left_4. Mask Image 적용 후.png"), true);
            File.Copy(Path.Combine(selectedDirectoryPath, newDirectory, "Right_3. 구면 왜곡 + Perspective 보정 이후.png"), Path.Combine(selectedDirectoryPath, newDirectory, "Right_4. Mask Image 적용 후.png"), true);
            imageProcessingManagement.MaskImageLeft(Path.Combine(selectedDirectoryPath, newDirectory), "Left_4. Mask Image 적용 후.png", Path.Combine(selectedDirectoryPath, "Source_Image"), "MaskL.png");
            imageProcessingManagement.MaskImageRight(Path.Combine(selectedDirectoryPath, newDirectory), "Right_4. Mask Image 적용 후.png", Path.Combine(selectedDirectoryPath, "Source_Image"), "MaskR.png");

            // 필요 없는 디렉토리 삭제
            Directory.Delete(Path.Combine(selectedDirectoryPath, newDirectory, "ImageProcessing"), true);
        }

        private void ButtonOpenImageFileDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 처리를 하고자 하는 파일을 선택하십시오.";
            openFileDialog.Filter = "이미지 파일 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|모든 파일 (*.*)|*.*";
            openFileDialog.Multiselect = false; // 여러 개 선택 방지

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imgFilePath = openFileDialog.FileName;
            }
        }

        private void ButtonTargetDirectoryDialog_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Source_Image 디렉토리와 처리할 이미지가 포함된 디렉토리를 선택하세요.";
                folderDialog.ShowNewFolderButton = true; // 새 폴더 만들기 버튼 활성화

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedDirectoryPath = folderDialog.SelectedPath; // 선택한 폴더 경로
                }
            }
        }
    }
}