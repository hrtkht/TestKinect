using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectTest001
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                // Kinectが接続されているか確認する
                if (KinectSensor.KinectSensors.Count == 0)
                {
                    throw new Exception("Kinectを接続してください");
                }

                // Kinectの動作を開始する
                StartKinect(KinectSensor.KinectSensors[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        private void StartKinect(KinectSensor Kinect)
        {
            // RGBカメラを有効にしてフレーム更新イベントを登録する
            Kinect.ColorStream.Enable();
            Kinect.ColorFrameReady +=
                new EventHandler<ColorImageFrameReadyEventArgs>(Kinect_ColorFrameReady);

            // 距離カメラを有効にして、フレーム更新イベントを登録する
            Kinect.DepthStream.Enable();
            Kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(Kinect_DepthFrameReady);

            // プレイヤーを取得するために、スケルトンを有効にする
            Kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(Kinect_SkeletonFrameReady);
            Kinect.SkeletonStream.Enable();      

            // Kinectの動作を開始する
            Kinect.Start();

            // Defaultモードとnearモードの切り替え
            comboBoxRange.Items.Clear();
            foreach (var range in Enum.GetValues(typeof(DepthRange)))
            {
                comboBoxRange.Items.Add(range.ToString());
            }

            comboBoxRange.SelectedIndex = 0;
            {

            }
            
        }

        void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            try
            {
                // RGBカメラのフレームデータを取得する
                using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
                {
                    if (colorFrame != null)
                    {
                        // RGBカメラのピクセルデータを取得する
                        byte[] colorPixel = new byte[colorFrame.PixelDataLength];
                        colorFrame.CopyPixelDataTo(colorPixel);

                        // ピクセルデータをビットマップに変換する
                        imageRgb.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, colorPixel,
                            colorFrame.Width * colorFrame.BytesPerPixel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        readonly int Bgr32BytesPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;
        void Kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            try
            {
                // Kinectのインスタンスを取得する
                KinectSensor Kinect = sender as KinectSensor;
                if (Kinect == null)
                {
                    return;
                }

                // 距離カメラのフレームデータを取得する
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame != null)
                    {
                        // 距離データを画像化して表示
                        imageDepth.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null,
                            ConvertDepthColor(Kinect, depthFrame),depthFrame.Width * Bgr32BytesPerPixel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            try
            {
                // Kinectのインスタンスを取得する
                KinectSensor Kinect = sender as KinectSensor;
                if (Kinect == null)
                    return;
            
                // スケルトンおフレームを取得する
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                    {
                        DrawSkeleton(Kinect, skeletonFrame);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DrawSkeleton(KinectSensor Kinect, SkeletonFrame skeletonFrame)
        {
            // スケルトンのデータを取得する
            Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(skeletons);

            canvasSkeleton.Children.Clear();

            // トラッキングされているスケルトンのジョイントを描写する
            foreach (Skeleton skeleton in skeletons)
            {
                // スケルトンが（Dafaultモード）トラッキング状態の場合は、
                // ジョイントを描画する
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // ジョイントを描画する
                    foreach (Joint joint in skeleton.Joints)
                    {
                        // ジョイントがトラッキングされていなければ次へ
                        if (joint.TrackingState == JointTrackingState.NotTracked)
                        {
                            continue;
                        }

                        // ジョイントの座標を描く
                        DrawEllipse(Kinect, joint.Position);
                    }
                }
                // スケルトンが位置追跡の（Nearモード）の場合は、
                // スケルトン位置（Center hip）を描写する
                else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
                {
                    // スケルトンの座標を描く
                    DrawEllipse(Kinect, skeleton.Position);
                }

            }
        }

        private void DrawEllipse(KinectSensor Kinect, SkeletonPoint position)
        {
            const int R = 5;

            // スケルトンの座標を、RGBカメラの座標に変換する
            ColorImagePoint point = 
                // Kinect.CoordinateMapper.MapSkeletonPointToColorPoint(position, Kinect.ColorStream.Format);
                    Kinect.CoordinateMapper.MapSkeletonPointToColorPoint(position, Kinect.ColorStream.Format);

            // 座標を画面のサイズに変換する
            point.X = (int)ScaleTo(point.X, Kinect.ColorStream.FrameWidth,
                                   canvasSkeleton.Width);
            point.Y = (int)ScaleTo(point.Y, Kinect.ColorStream.FrameHeight,
                                   canvasSkeleton.Height);

            // 円を描く
            canvasSkeleton.Children.Add(new Ellipse()
            {
                Fill = new SolidColorBrush(Colors.Red),
                Margin = new Thickness(point.X - R, point.Y - R, 0, 0),
                Width = R * 2,
                Height = R * 2,
            });

            canvasSkeleton.Children.Add(new Ellipse()
            {
                Fill = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(point.X - (R * 0.5), point.Y - (R * 0.5), 0, 0),
                Width = (R - (R * 0.5)) * 2,
                Height = (R - (R * 0.5)) * 2,
            });
        }

        double ScaleTo(double value, double source, double dest)
        {
            return (value * dest) / source;
        }

        private byte[] ConvertDepthColor(KinectSensor Kinect, DepthImageFrame depthFrame)
        {
            ColorImageStream colorStream = Kinect.ColorStream;
            DepthImageStream depthStream = Kinect.DepthStream;

            // 距離カメラのピクセルごとのデータを取得する
            short[] depthPixel = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(depthPixel);

            // 距離カメラの座標に対応するRGBカメラの座標を取得する（座標合わせ）
            ColorImagePoint[] colorPoint = new ColorImagePoint[depthFrame.PixelDataLength];
            Kinect.MapDepthFrameToColorFrame(depthStream.Format, depthPixel, colorStream.Format, colorPoint);
            

            byte[] DepthColor = new byte[depthFrame.PixelDataLength * Bgr32BytesPerPixel];


            for (int index = 0; index < depthPixel.Length; index++ )
            {

                // 距離カメラのデータから、プレイヤーIDと距離を取得する
                int player = depthPixel[index] & DepthImageFrame.PlayerIndexBitmask;
                int distance = depthPixel[index] >> DepthImageFrame.PlayerIndexBitmaskWidth;
 
                // 変換した結果が、フレームサイズを超えることがあるため、小さい保を使う
                int x = Math.Min(colorPoint[index].X, colorStream.FrameWidth - 1);
                int y = Math.Min(colorPoint[index].Y, colorStream.FrameHeight - 1);


                int colorIndex = ((y * depthFrame.Width) + x) * Bgr32BytesPerPixel;

                //　プレイヤーがいるピクセルの場合
                if (player != 0)
                {
                    
                    DepthColor[colorIndex] = 255;
                    DepthColor[colorIndex + 1] = 255;
                    DepthColor[colorIndex + 2] = 255;
                }
                else
                {
                    // サポート外　0-40cm
                    if (distance == depthStream.UnknownDepth)
                    {
                        DepthColor[colorIndex] = 0;
                        DepthColor[colorIndex + 1] = 0;
                        DepthColor[colorIndex + 2] = 255;
                    }

                    // 近すぎ　40-80cm(Default)
                    else if (distance == depthStream.TooNearDepth)
                    {
                        DepthColor[colorIndex] = 0;
                        DepthColor[colorIndex + 1] = 255;
                        DepthColor[colorIndex + 2] = 0;
                    }

                    // 遠すぎ　3m(Near),4m(Default)-8m
                    else if (distance == depthStream.TooFarDepth)
                    {
                        DepthColor[colorIndex] = 255;
                        DepthColor[colorIndex + 1] = 0;
                        DepthColor[colorIndex + 2] = 0;
                    }

                    // 有効な距離データ
                    else
                    {
                        DepthColor[colorIndex] = 0;
                        DepthColor[colorIndex + 1] = 255;
                        DepthColor[colorIndex + 2] = 255;
                    }
                }
                
            }

            return DepthColor;


        }



        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(KinectSensor.KinectSensors[0]);
        }

        private void StopKinect(KinectSensor Kinect)
        {
            if (Kinect != null)
            {
                if (Kinect.IsRunning)
                {
                    // フレーム更新イベントを削除する
                    Kinect.ColorFrameReady -= Kinect_ColorFrameReady;
                    Kinect.DepthFrameReady -= Kinect_DepthFrameReady;
                    Kinect.SkeletonFrameReady -= Kinect_SkeletonFrameReady;

                    // Kinectの停止と、ネイティブリソースのを解放する
                    Kinect.Stop();
                    Kinect.Dispose();

                    imageRgb.Source = null;
                    imageDepth.Source = null;
                }
            }
        }

        private void cmboBoxRange_SelectionChanged(object sender, MouseWheelEventArgs e)
        {
            try
            {
                KinectSensor.KinectSensors[0].DepthStream.Range = (DepthRange)comboBoxRange.SelectedIndex;
            }
            catch (Exception)
            {
                comboBoxRange.SelectedIndex = 0;
            }

        }

        private void comboBoxRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    }

}
