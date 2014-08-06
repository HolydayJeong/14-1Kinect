using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

// gif 하려면 이게 필요해
using WpfAnimatedGif;
using EatingFruit;
using System.Windows.Threading;

namespace SungJik_SungHwa
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        const int NONE = 0;
        const int DIBIPRESSED = 1;
        const int JUMPPRESSED = 2;
        const int FRUITPRESSED = 3;
        const int ALL = 4;

        //kinect sensor를 선언함 
        KinectSensor sensor;
        // 항상 6여야 한다. 
        const int SKELETON_COUNT = 6;

        int gamestate = 0;

        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];

        List<Image> images;
        //private Window1 ReadyWindow;

        Boolean locker = false;
        PressButton Press = new PressButton();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "Main\\";

        public MainWindow()
        {
            InitializeComponent();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            images = new List<Image> { Dibi, Jump, Fruit };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //키넥트가 연결되어 있는지 확인한다. 만일 연결되어 있으면 선언한 sensor와 연결된 kinect 정보를 준다. 

            if (KinectSensor.KinectSensors.Count > 0)
                sensor = KinectSensor.KinectSensors[0];

            //연결에 성공하면.. 
            if (sensor.Status == KinectStatus.Connected)
            {
                //색깔정보
                sensor.ColorStream.Enable();
                //깊이정보
                sensor.DepthStream.Enable();
                //사람 인체 인식 정보 
                sensor.SkeletonStream.Enable();

                //if using window kinect only!
                //Detph stream이 가까우면 할 수 있다. ( 참고로 xbox kinect는 nearmode가 없다.) 

                sensor.DepthStream.Range = DepthRange.Near;
                sensor.SkeletonStream.EnableTrackingInNearRange = true;
                //sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

                Menu(NONE, baseDirectory);

                // 시작 화면 스타트
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(baseDirectory + "main.gif");
                ImageBehavior.SetRepeatBehavior(Begin, System.Windows.Media.Animation.RepeatBehavior.Forever);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(Begin, image); // 이미지 띄우기

                //kinect 가 준비하면 이벤트를 발생시키라는 명령문 
                //sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
                sensor.AllFramesReady += sensor_AllFramesReady;

                //sensor를 시작한다. thread와 같다고 보면 된다. 
                sensor.Start();

                //User_ViewGrid.Visibility = Visibility.Visible; //화면 보여줌 
                //User_ViewGrid.Visibility = Visibility.Collapsed; // 화면 감춤 

            }
        }
        //준비가 되었을 때, 이벤트 
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // 받아오는 정보를 colorFrame에 받아온다. 
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;
                //pixel의 크기 초기화 
                byte[] pixels = new byte[colorFrame.PixelDataLength];

                //pixel 의 정보 담아오기 
                colorFrame.CopyPixelDataTo(pixels);
                //stride = r, g,b, none 의 정보가 하나의 pixel에 있기에 *4를 한다.
                int stride = colorFrame.Width * 4;

                //screen image의 source를 결정해준다. 
                Screen.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);

            }
            Skeleton me = null;
            GetSkelton(e, ref me);

            if (me == null)
                return;
            GetCameraPoint(me, e);
        }

        private void GetSkelton(AllFramesReadyEventArgs e, ref Skeleton me)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                    return;

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);
                me = (from s in allSkeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();

            }
        }
        private void GetCameraPoint(Skeleton me, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                    return;
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint handDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                DepthImagePoint bodyDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.Spine].Position, depth.Format);

                ColorImagePoint handColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, handDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);


                Canvas.SetLeft(Hand, handColorPoint.X - Hand.Width / 2);
                Canvas.SetTop(Hand, handColorPoint.Y - Hand.Height / 2);

                // 14/07/25 lock을 걸어서 창이 복수생산이 되지 않도록한다

                if (gamestate == 0)
                {
                    gamestate = 1;  // lock 역할

                    Point targetTopLeft = new Point(Canvas.GetLeft(Jump), Canvas.GetTop(Jump));
                    targetTopLeft.X /= 2;
                    targetTopLeft.Y /= 2;
                    box.Text = "TopLeft X : " + targetTopLeft.X + "Width : " + Jump.ActualWidth + " TopLeft Y : " + targetTopLeft.Y + " Height : " + Jump.ActualHeight + "Body X: " + bodyDepthPoint.X + "Body Y: " + bodyDepthPoint.Y;
                    //if (bodyDepthPoint.X > targetTopLeft.X && bodyDepthPoint.X < targetTopLeft.X + Jump.ActualWidth/2 && bodyDepthPoint.Y > targetTopLeft.Y && bodyDepthPoint.Y < targetTopLeft.Y + Jump.ActualHeight)
                    if (bodyDepthPoint.X > 310 && bodyDepthPoint.X < 350 && bodyDepthPoint.Y > 240 && bodyDepthPoint.Y < 280)
                    {
                        gamestate = 2;
                    }
                    else
                        gamestate = 0;
                }

                else if (gamestate == 2)
                {
                    Begin.Visibility = System.Windows.Visibility.Hidden;

                    Menu(ALL, baseDirectory);

                    int i = 0;
                    foreach (Image target in images)
                    {
                        Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                        targetTopLeft.X /= 2;
                        targetTopLeft.Y /= 2;

                        box.Text = "TopLeft X : " + targetTopLeft.X + " TopLeft Y : " + targetTopLeft.Y + " Hand X : " + handDepthPoint.X * 2 + " Hand Y : " + handDepthPoint.Y * 2 + "Body X: " + bodyDepthPoint.X * 2 + "Body Y: " + bodyDepthPoint.Y * 2 + "gamestate : " + gamestate;
                        if (locker == false)
                        {
                            if (handDepthPoint.X > targetTopLeft.X &&
                                   handDepthPoint.X < targetTopLeft.X + target.ActualWidth / 2 &&
                                   handDepthPoint.Y > targetTopLeft.Y &&
                                   handDepthPoint.Y < targetTopLeft.Y + target.ActualHeight / 2)
                            {
                                box.Text = "Pressing";
                                if (target.Name == "Dibi")
                                    i = DIBIPRESSED;
                                else if (target.Name == "Jump")
                                    i = JUMPPRESSED;
                                else if (target.Name == "Fruit")
                                    i = FRUITPRESSED;
                                else
                                    i = NONE;
                                box.Text += " I : " + i;
                                Menu(i, baseDirectory);

                                Press.detectPressure(handDepthPoint.Depth);
                                if (Press.isPressed() == true)
                                {
                                    locker = true;
                                    Menu_Click(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Menu_Click(int i)
        {
            switch (i)
            {
                case DIBIPRESSED:
                    Dibi Dibi = new Dibi();
                    App.Current.MainWindow = Dibi;
                    this.Close();
                    Dibi.Show();
                    return;
                case JUMPPRESSED:
                    SkippingRoper skippingRope = new SkippingRoper();
                    App.Current.MainWindow = skippingRope;
                    this.Close();
                    skippingRope.Show();
                    return;
                case FRUITPRESSED:
                    Fruit fruit = new Fruit();
                    App.Current.MainWindow = fruit;
                    this.Close();
                    fruit.Show();
                    return;
            }
        }

        private void Menu(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case NONE:
                        Dibi.Visibility = System.Windows.Visibility.Hidden;
                        Jump.Visibility = System.Windows.Visibility.Hidden;
                        Fruit.Visibility = System.Windows.Visibility.Hidden;
                        Hand.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case DIBIPRESSED:
                        Dibi.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu1_on.png") as ImageSource;
                        Dibi.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case JUMPPRESSED:
                        Jump.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu2_on.png") as ImageSource;
                        Jump.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case FRUITPRESSED:
                        Fruit.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu3_on.png") as ImageSource;
                        Fruit.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case ALL:
                        Dibi.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu1.png") as ImageSource;
                        Dibi.Visibility = System.Windows.Visibility.Visible;
                        Jump.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu2.png") as ImageSource;
                        Jump.Visibility = System.Windows.Visibility.Visible;
                        Fruit.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "menu3.png") as ImageSource;
                        Fruit.Visibility = System.Windows.Visibility.Visible;
                        Hand.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}

