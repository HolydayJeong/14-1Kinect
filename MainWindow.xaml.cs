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
using System.Diagnostics;

namespace SungJik_SungHwa
{
    class GLOBAL
    {
        public static int SelectedGame = 0;
        public static bool StartGame = false;
        public static int FruitCounter = 0;
        public static BitmapSource kinectScreen;

    }
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

        const int LEFT = 0;
        const int RIGHT = 1;

        double resolution = 0.8;

        int pressingHand = RIGHT;

        //kinect sensor를 선언함 
        KinectSensor sensor;
        // 항상 6여야 한다. 
        const int SKELETON_COUNT = 6;

        int gamestate = 0;

        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];

        List<Image> images;
        List<Image> menuList = new List<Image>();
        //private Window1 ReadyWindow;

        Boolean locker = false;
        PressButton Press;

        Dibi DIBI;
        SkippingRoper JUMP;
        Fruit FRUIT;

        MediaPlayer monkeySound = new MediaPlayer();

        static WriteableBitmap writeableBitmap;


        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "Main\\";

        public MainWindow()
        {
            Press = new PressButton(baseDirectory + "mouse.png", baseDirectory + "mouse_pull.png");
            InitializeComponent();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            images = new List<Image> { Dibi, Jump, Fruit };
            Console.WriteLine(baseDirectory+"menu1.png");
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu1.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu1_on.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu2.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu2_on.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu3.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "menu3_on.png")) });
            Console.WriteLine(baseDirectory);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hand.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "mouse.png") as ImageSource;
            Guide.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_0.png") as ImageSource;

            //창 가운데로 배치
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = (desktopWorkingArea.Right - this.Width) / 2;
            this.Top = (desktopWorkingArea.Bottom - this.Height) / 2;

            //키넥트가 연결되어 있는지 확인한다. 만일 연결되어 있으면 선언한 sensor와 연결된 kinect 정보를 준다. 

            

            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
                Console.WriteLine("Main in1");
            }

            Console.WriteLine(sensor.Status.ToString());
            while (sensor.Status != KinectStatus.Connected) ;
            //연결에 성공하면.. 
            if (sensor.Status == KinectStatus.Connected)
            {
                Console.WriteLine("Main in2");
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
            DIBI = new Dibi(this);
            JUMP = new SkippingRoper(this);
            FRUIT = new Fruit(this);
            monkeySound.Open(new Uri(baseDirectory + "main_monkey.mp3"));

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

                    SungJik_SungHwa.GLOBAL.kinectScreen = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
                    //writeableBitmap = new WriteableBitmap(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
                    //screen image의 source를 결정해준다. 
                    Screen.Source = SungJik_SungHwa.GLOBAL.kinectScreen;
                    
                    pixels = null;

                }
                Skeleton me = null;
                GetSkelton(e, ref me);

                if (me == null)
                {
                    monkeySound.Pause();
                    return;
                }
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
        private int handDepth;
        private void GetCameraPoint(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (SungJik_SungHwa.GLOBAL.SelectedGame == 0)
            {
                using (DepthImageFrame depth = e.OpenDepthImageFrame())
                {
                    if (depth == null || sensor == null)
                        return;
                    CoordinateMapper coorMap = new CoordinateMapper(sensor);
                    DepthImagePoint handRightDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                    DepthImagePoint handLeftDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                    DepthImagePoint bodyDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.Spine].Position, depth.Format);

                    ColorImagePoint handRightColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, handRightDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);
                    ColorImagePoint handLeftColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, handLeftDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

                    if ((Press.isPressed() == true && pressingHand == RIGHT) || (handRightDepthPoint.Depth <= handLeftDepthPoint.Depth))
                    {
                        if (Press.isPressed() == true && pressingHand == LEFT) { }
                        else
                        {
                            Canvas.SetLeft(Hand, (handRightColorPoint.X - Hand.Width / 2 )*resolution);  // resolution 추가된 이유 : 해상도를 몇배 떨어트려서 다 보일수 있도록함( 밑에 잘렸었음)
                            Canvas.SetTop(Hand, (handRightColorPoint.Y - Hand.Height / 2) * resolution);
                            handDepth = handRightDepthPoint.Depth;
                            pressingHand = RIGHT;
                        }
                    }
                    if ((Press.isPressed() == true && pressingHand == LEFT) || (handRightDepthPoint.Depth > handLeftDepthPoint.Depth))
                    {
                        Canvas.SetLeft(Hand, (handLeftColorPoint.X - Hand.Width / 2)*resolution);
                        Canvas.SetTop(Hand, (handLeftColorPoint.Y - Hand.Height / 2) * resolution);
                        handDepth = handLeftDepthPoint.Depth;
                        pressingHand = LEFT;
                    }


                    // 14/07/25 lock을 걸어서 창이 복수생산이 되지 않도록한다

                    if (gamestate == 0)
                    {
                        gamestate = 1;  // lock 역할

                        monkeySound.Play();

                        Point targetTopLeft = new Point(Canvas.GetLeft(Jump), Canvas.GetTop(Jump));
                        targetTopLeft.X = targetTopLeft.X / 2;   // 이미 xaml에서 줄여진 상태이기때문에 따로 resolution 안곱해도 되
                        targetTopLeft.Y = targetTopLeft.Y / 2;
                        box.Text = "TopLeft X : " + targetTopLeft.X + "Width : " + Jump.ActualWidth + " TopLeft Y : " + targetTopLeft.Y + " Height : " + Jump.ActualHeight + "Body X: " + bodyDepthPoint.X + "Body Y: " + bodyDepthPoint.Y;
                        //if (bodyDepthPoint.X > targetTopLeft.X && bodyDepthPoint.X < targetTopLeft.X + Jump.ActualWidth/2 && bodyDepthPoint.Y > targetTopLeft.Y && bodyDepthPoint.Y < targetTopLeft.Y + Jump.ActualHeight)
                        if (bodyDepthPoint.X > 310 && bodyDepthPoint.X < 350 && bodyDepthPoint.Y > 230 && bodyDepthPoint.Y < 280)
                        {
                            gamestate = 2;
                        }
                        else
                            gamestate = 0;
                    }

                    else if (gamestate == 2)
                    {
                        monkeySound.Pause();
                        Begin.Visibility = System.Windows.Visibility.Hidden;

                        Menu(ALL, baseDirectory);

                        int i = 0;
                        int clicked = 0;
                        foreach (Image target in images)
                        {
                            Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                            //targetTopLeft.X /= 2;
                            //targetTopLeft.Y /= 2;

                            box.Text = "Target Name " + target.Name + "TopLeft X : " + targetTopLeft.X + " TopLeft Y : " + targetTopLeft.Y + " Hand X : " + (Canvas.GetLeft(Hand) + Hand.Width / 2) + " Hand Y : " + (Canvas.GetTop(Hand) + Hand.Width / 2) + "Body X: " + bodyDepthPoint.X * 2 + "Body Y: " + bodyDepthPoint.Y * 2 + "gamestate : " + gamestate + " I : " + i + "locker : " + locker;
                            if (locker == false)
                            {
                                if ((Canvas.GetLeft(Hand) + Hand.Width / 2) > targetTopLeft.X &&      // resolution 안곱하는 이유 -> 아마도 둘다 상대적인 위치라서??  곱하면 이상하게 나온당
                                       (Canvas.GetLeft(Hand) + Hand.Width / 2) < targetTopLeft.X + target.ActualWidth &&
                                       (Canvas.GetTop(Hand) + Hand.Height / 2) > targetTopLeft.Y &&
                                       (Canvas.GetTop(Hand) + Hand.Height / 2) < targetTopLeft.Y + target.ActualHeight)
                                {
                                    clicked = 1;
                                    box.Text = "Pressing";
                                    if (target.Name == "Dibi")
                                        i = DIBIPRESSED;
                                    else if (target.Name == "Jump")
                                        i = JUMPPRESSED;
                                    else if (target.Name == "Fruit")
                                        i = FRUITPRESSED;
                                    else
                                    {
                                        i = NONE;
                                    }

                                    Menu(i, baseDirectory);

                                    Press.detectPressure(handDepth, ref Hand);
                                    if (Press.isConfirmed() == true)
                                    {
                                        locker = true;
                                        Press.reset(ref Hand);
                                        Menu_Click(i);
                                        locker = false;
                                    }
                                }
                            }
                        }
                        if (clicked == 0)
                            Press.reset(ref Hand);
                    }
                }
            }
        }

        private void Menu_Click(int i)
        {
            Screen.Source = null;
            switch (i)
            {
                case DIBIPRESSED:
                    DIBI.Show();
                    SungJik_SungHwa.GLOBAL.SelectedGame = 1;
                    this.Hide();
                    return;
                case JUMPPRESSED:
                    JUMP.Show();
                    SungJik_SungHwa.GLOBAL.SelectedGame = 2;
                    SungJik_SungHwa.GLOBAL.StartGame = true;
                    this.Hide();
                    return;
                case FRUITPRESSED:
                    FRUIT.Show();
                    SungJik_SungHwa.GLOBAL.SelectedGame = 3;
                    SungJik_SungHwa.GLOBAL.FruitCounter++;
                    SungJik_SungHwa.GLOBAL.StartGame = true;
                    this.Hide();
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
                        Guide.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case DIBIPRESSED:
                        Dibi.Source = menuList[1].Source;
                        Dibi.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case JUMPPRESSED:
                        Jump.Source = menuList[3].Source;
                        Jump.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case FRUITPRESSED:
                        Fruit.Source = menuList[5].Source;
                        Fruit.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case ALL:
                        Dibi.Source = menuList[0].Source;
                        Dibi.Visibility = System.Windows.Visibility.Visible;
                        Jump.Source = menuList[2].Source;
                        Jump.Visibility = System.Windows.Visibility.Visible;
                        Fruit.Source = menuList[4].Source;
                        Fruit.Visibility = System.Windows.Visibility.Visible;
                        Hand.Visibility = System.Windows.Visibility.Visible;
                        Guide.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sensor.Stop();
        }
    }
}

