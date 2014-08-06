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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using WpfAnimatedGif;

using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.ComponentModel;



public class Constants_DIBI
{
    public const int NONE = 0;
    public const int GAMESTATEYELLOW = 1;
    public const int GAMESTATEVISIBLE = 2;
    public const int SKIPHIDDEN = 3;
    public const int SKIPVISIBLE = 4;
    public const int LOSE = 5;
    public const int WIN = 6;
    public const int SCISSOR = 7;
    public const int ROCK = 8;
    public const int PAPER = 9;
    public const int SOONGMAIN = 10;
    public const int FINALWIN = 11;
    public const int FINALLOSE = 12;
    public const int HANDSUP = 13;
    public const int HANDSDOWN = 14;
    public const int REPLAYPRESSED = 15;
    public const int HOMEPRESSED = 16;

}

namespace SungJik_SungHwa
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
  
    //  public delegate void VoidIntDelegate(int i, string path); 얘는 뭐하는 애일까

    public partial class Dibi : Window
    {
        SungJik_SungHwa.PressButton Press = new SungJik_SungHwa.PressButton();

        public Dibi()
        {
            InitializeComponent();
        }

        KinectSensor sensor;
        const int SKELETON_COUNT = 6;
        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];
        Button button = new Button();


        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "dibi\\";

        int gamestate = 111;  // 게임 시작을 알려주는 
        //10 : 대기, 0 : 인트로시작, 1 : 자세인식, 2 : intro, 3 : 디비디비딥 출력, 4 : 자세인식, 5 : 승패 판결후 게임 종료, 6 : 처음으로, 다시하기, 5초대기

        int monkeySate = 0; // 원숭이가 낸거 0 : 대기, 1 : 가위, 2 : 주먹, 3 : 보
        int playerState = 0;// 사람이 낸거 0 : 대기, 1 : 가위, 2 : 주먹, 3 : 보

        int WinCount = 0;       // 이긴 횟수
        int LoseCount = 0;      // 진 횟수

        int Form = 0;

        Random random = new Random();

        System.Media.SoundPlayer sp1 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "dibi\\dibidibi.wav");


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
                sensor = KinectSensor.KinectSensors[0];

            if (sensor.Status == KinectStatus.Connected)
            {
                sensor.ColorStream.Enable();
                sensor.DepthStream.Enable();
                sensor.SkeletonStream.Enable();

                sensor.DepthStream.Range = DepthRange.Near;
                sensor.SkeletonStream.EnableTrackingInNearRange = true;

                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;

                sensor.AllFramesReady += sensor_AllFramesReady;
                sensor.Start();
            }
        }


        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                    return;

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4; //b g r 빈칸 순으로 화면에 배치될꺼 기때문에 4칸이 더 필요????

                //Stream imageStreamSource = new FileStream("movesoong.gif", FileMode.Create);
                //GifBitmapEncoder encoder = new GifBitmapEncoder();

                kinect1.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }
            Skeleton me = null;
            GetSkeleton(e, ref me);

            if (me == null)
                return;
            GetCameraPoint(me, e);
        }

        private void GetSkeleton(AllFramesReadyEventArgs e, ref Skeleton me)
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
                {
                    return;
                }
                    

                // 각 부위 스켈레톤 측정 시작
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint headDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.Head].Position, depth.Format);
                DepthImagePoint neckDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderCenter].Position, depth.Format);
                DepthImagePoint bodyDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.Spine].Position, depth.Format);
                DepthImagePoint hipDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HipCenter].Position, depth.Format);

                DepthImagePoint LShoulderDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderLeft].Position, depth.Format);
                DepthImagePoint RShoulderDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderRight].Position, depth.Format);

                DepthImagePoint LHandDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                DepthImagePoint RHandDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                DepthImagePoint LWristDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.WristLeft].Position, depth.Format);
                DepthImagePoint RWristDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.WristRight].Position, depth.Format);
                DepthImagePoint LElbowDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.ElbowLeft].Position, depth.Format);
                DepthImagePoint RElbowDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.ElbowRight].Position, depth.Format);
                

                // 각 부위 측정 끝

                ColorImagePoint headColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, headDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

                Canvas.SetLeft(Soong, headColorPoint.X - Soong.Width / 2);
                Canvas.SetTop(Soong, headColorPoint.Y - Soong.Width / 2);

                String test = "";

                int HandLength = Math.Abs(RElbowDepthPoint.Y - RWristDepthPoint.Y) * 2;
                int ElboDistance = RElbowDepthPoint.X - LElbowDepthPoint.X;



                //Position.Text = RHandDepthPoint.X + " " + RHandDepthPoint.Y;
                if (gamestate == 111)
                {
                    gamestate = 0;
                    skip.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "skip.png") as ImageSource;
                    skip.Visibility = System.Windows.Visibility.Hidden;

                    Console.WriteLine("Thread Make playstate : " + playerState + " / gamestate : " + gamestate + " / gameCount : " + WinCount);
                    ThreadStart threadStart = new ThreadStart(GameState);
                    Thread newThread = new Thread(threadStart);
                    newThread.Start();
                }
                else if (gamestate == 10)
                {
                    Point replayTopLeft = new Point(Canvas.GetLeft(replay), Canvas.GetTop(replay));

                    Point homeTopLeft = new Point(Canvas.GetLeft(home), Canvas.GetTop(home));

                    //해상도 확장으로 인한 변수 값 조정 
                    //(해상도 줄이면 이곳을 주석처리하고 이미지 크기에 /2 지우면 됨)
                    replayTopLeft.X = replayTopLeft.X / 2;
                    replayTopLeft.Y = replayTopLeft.Y / 2;
                    homeTopLeft.X = homeTopLeft.X / 2;
                    homeTopLeft.Y = homeTopLeft.Y / 2;
                    // 변수값 조정 끝

                         // 리플레이
                    if (RHandDepthPoint.X > replayTopLeft.X && RHandDepthPoint.X < replayTopLeft.X + skip.ActualWidth / 2 && RHandDepthPoint.Y > replayTopLeft.Y && RHandDepthPoint.Y < replayTopLeft.Y + skip.ActualHeight / 2)
                    //if (RHandDepthPoint.X > 400 && RHandDepthPoint.X < 471 && RHandDepthPoint.Y > 100 && RHandDepthPoint.Y < 140)
                    {
                        Menu(Constants_DIBI.REPLAYPRESSED, baseDirectory);
                        /*
                        video.Visibility = System.Windows.Visibility.Visible;
                        video.Source = new Uri(baseDirectory + "sequence02.mov");
                        video.Play();
                        */
                        //MessageBox.Show("start");
                        Press.detectPressure(RHandDepthPoint.Depth);
                        if (Press.isPressed() == true)
                        {
                            Press.reset();
                            gamestate = 0;                 // 마구 들어옴 방지
                            Menu(0, baseDirectory);

                            Console.WriteLine("Thread Make playstate : " + playerState + " / gamestate : " + gamestate + " / gameCount : " + WinCount);
                            ThreadStart threadStart = new ThreadStart(GameState);
                            Thread newThread = new Thread(threadStart);
                            newThread.Start();
                        }
                    }
                        // 홈으로 가기
                    else if (RHandDepthPoint.X > homeTopLeft.X && RHandDepthPoint.X < homeTopLeft.X + home.ActualWidth / 2 && RHandDepthPoint.Y > homeTopLeft.Y && RHandDepthPoint.Y < homeTopLeft.Y + home.ActualHeight / 2)
                    {
                        Menu(Constants_DIBI.HOMEPRESSED, baseDirectory);
                        Press.detectPressure(RHandDepthPoint.Depth);
                        if (Press.isPressed() == true)
                        {
                            Press.reset();

                            MainWindow main = new MainWindow();
                            App.Current.MainWindow = main;
                            this.Close();
                            main.Show();
                            return;
                        }
                    }
                    else
                        Menu(1, baseDirectory);
                }
                else if (gamestate == 11)    //스킵용
                {
                    Point targetTopLeft = new Point(Canvas.GetLeft(skip), Canvas.GetTop(skip));
                    targetTopLeft.X = targetTopLeft.X / 2;
                    targetTopLeft.Y = targetTopLeft.Y / 2;

                    if (RHandDepthPoint.X > targetTopLeft.X && RHandDepthPoint.X < targetTopLeft.X + skip.ActualWidth / 2 && RHandDepthPoint.Y > targetTopLeft.Y && RHandDepthPoint.Y < targetTopLeft.Y + skip.ActualHeight / 2)
                    {
                            Press.reset();
                            gamestate = 3;  // 인트로 스킵
                            Console.WriteLine("gamestate : " + gamestate);
                    }
                }
                else if (gamestate == 1 || gamestate == 5)
                {
                    if (gamestate == 1)
                        gamestate = 2;  // 다른 프로세스 들어옴 방지.
                    else if (gamestate == 5)
                        gamestate = 6;


                    if (LHandDepthPoint.X <= LShoulderDepthPoint.X && RHandDepthPoint.X >= RShoulderDepthPoint.X)  // 양쪽손 > 양쪽어깨 비교(벌릴때 / 보)
                    {
                        //case 보자기
                        if (bodyDepthPoint.Y > LHandDepthPoint.Y && bodyDepthPoint.Y > RHandDepthPoint.Y)   // 몸 < 양손 비교하기
                        {
                            playerState = Constants_DIBI.PAPER;
                        }
                        else
                        {
                            Form = Constants_DIBI.HANDSUP;
                            Alert(Constants_DIBI.HANDSUP, baseDirectory);
                            playerState = 1;
                        }
                    }
                    else if (LHandDepthPoint.X > LShoulderDepthPoint.X && RHandDepthPoint.X < RShoulderDepthPoint.X && LElbowDepthPoint.X < LShoulderDepthPoint.X && RElbowDepthPoint.X > RShoulderDepthPoint.X) 
                        // 양쪽손 in 양쪽어깨 양쪽 팔꿈치 out 어깨  비교 (모을때 / 묵)
                    {
                        // case 묵
                        if (headDepthPoint.Y < LHandDepthPoint.Y && headDepthPoint.Y < RHandDepthPoint.Y)   // 머리 > 양손 비교하기
                            if (bodyDepthPoint.Y > LHandDepthPoint.Y && bodyDepthPoint.Y > RHandDepthPoint.Y)   // 몸 < 양손 비교하기
                            {
                                playerState = Constants_DIBI.ROCK;
                            }
                            else
                            {
                                Form = Constants_DIBI.HANDSUP;
                                Alert(Constants_DIBI.HANDSUP, baseDirectory);
                                playerState = 2;
                            }
                        else
                        {
                            Form = Constants_DIBI.HANDSDOWN;
                            Alert(Constants_DIBI.HANDSDOWN, baseDirectory);
                            playerState = 3;
                        }
                    }

                    else if (RHandDepthPoint.X >= bodyDepthPoint.X && LWristDepthPoint.X > bodyDepthPoint.X)  // 왼쪽어깨 < 왼손 , 머리 < 오른손
                    {
                        //case 가위
                        if (headDepthPoint.Y < LHandDepthPoint.Y && headDepthPoint.Y < RHandDepthPoint.Y)   // 머리 > 양손 비교하기
                            if (hipDepthPoint.Y > LHandDepthPoint.Y && hipDepthPoint.Y > RHandDepthPoint.Y)   // 엉덩이 < 양손 비교하기Form = "가위";
                            {
                                playerState = Constants_DIBI.SCISSOR;
                            }
                            else
                            {
                                Form = Constants_DIBI.HANDSUP;
                                Alert(Constants_DIBI.HANDSUP, baseDirectory);
                                playerState = 4;
                            }
                        else
                        {
                            Form = Constants_DIBI.HANDSDOWN;
                            Alert(Constants_DIBI.HANDSDOWN, baseDirectory);
                            playerState = 5;
                        }
                    }
                    else
                    {
                        Form = 0;
                        playerState = 6;
                    }

                    test = "발꿈치 거리 : " + (ElboDistance).ToString() + " , 손 길이 : " + (HandLength).ToString();
                    /*
                    Position.FontSize = 11;
                    Position.Text = "Y Head : " + headDepthPoint.Y.ToString() + "  /  Neck : " + neckDepthPoint.Y.ToString() + "  /  Body : " + bodyDepthPoint.Y.ToString()
                        + "  /  Hip : " + hipDepthPoint.Y.ToString()
                        + "  /  Left : " + LHandDepthPoint.Y.ToString() + " ,  Left Shoulder : " + LShoulderDepthPoint.Y.ToString() + "  ,  Left Wrist: " + LWristDepthPoint.Y.ToString()
                        + "  ,  Left Elbow : " + LElbowDepthPoint.Y.ToString()
                        + "  /  Right : " + RHandDepthPoint.Y.ToString() + "  ,  Right Shoulder : " + RShoulderDepthPoint.Y.ToString() + "  ,  Right Wrist: " + RWristDepthPoint.Y.ToString()
                        + "  ,  Right Elbow : " + RElbowDepthPoint.Y.ToString()
                        + "\nX Head : " + headDepthPoint.X.ToString() + "  /  Neck : " + neckDepthPoint.X.ToString() + "  /  Body : " + bodyDepthPoint.X.ToString()
                            + "  /  Hip : " + hipDepthPoint.X.ToString()
                        + "  /  Left : " + LHandDepthPoint.X.ToString() + "  ,  Left Shoulder : " + LShoulderDepthPoint.X.ToString() + "  ,  Left Wrist: " + LWristDepthPoint.X.ToString()
                        + "  ,  Left Elbow : " + LElbowDepthPoint.X.ToString()
                        + "  /  Right : " + RHandDepthPoint.X.ToString() + "  ,  Right Shoulder : " + RShoulderDepthPoint.X.ToString() + "  ,  Right Wrist: " + RWristDepthPoint.X.ToString()
                        + "  ,  Right Elbow : " + RElbowDepthPoint.X.ToString()
                        + "\n" + Form
                        + "\n" + test;
                    */
                    if (gamestate == 6)     // 게임 중이므로 판별로 넘어가야함
                    {
                        Thread.Sleep(1200); // 숭익이가 낸거 보여주기 (1.2초동안)
                        checkWin();
                        gamestate = 7;       // 아무런 명령이 없는 7으로 바꿈
                    }
                }
                else if (gamestate == 8)
                {
                    db1.Visibility = System.Windows.Visibility.Hidden;
                    db2.Visibility = System.Windows.Visibility.Hidden;
                    dip.Visibility = System.Windows.Visibility.Hidden;
                    Alert(Constants_DIBI.NONE, baseDirectory);

                    Console.WriteLine("Game Over  : " + WinCount + " : " + LoseCount);

                    if (WinCount == 3 || LoseCount == 3)
                    {
                        if (WinCount == 3)
                        {
                            //doNotice("승리!!");
                            System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "win.wav");
                            sp.Play();
                            SoongOut(Constants_DIBI.FINALWIN, baseDirectory);
                            Alert(Constants_DIBI.FINALWIN, baseDirectory);
                        }
                        else if (LoseCount == 3)
                        {
                            //doNotice("패배!!");
                            System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "loose.wav");
                            sp.Play();
                            SoongOut(Constants_DIBI.FINALLOSE, baseDirectory);
                            Alert(Constants_DIBI.FINALLOSE, baseDirectory);
                        }
                        gamestate = 9;
                    }
                    else
                    {
                        SoongOut(Constants_DIBI.SOONGMAIN, baseDirectory);
                        gamestate = 4;
                    }
                }
                else if (gamestate == 88)
                {
                    gamestate = 881;
                    ImageBehavior.SetAnimatedSource(monkey, null);  // 이미지 없애기
                    ImageBehavior.SetAnimatedSource(Alertimg, null);  // 이미지 없애기
                    Alert(Constants_DIBI.NONE, baseDirectory);
                    SoongOut(Constants_DIBI.SOONGMAIN, baseDirectory);
                    Console.WriteLine("in");
                    score.Visibility = System.Windows.Visibility.Hidden;
                    Menu(1, baseDirectory);

                    WinCount = 0;
                    LoseCount = 0;
                    playerState = 0;
                    monkeySate = 0;
                    gamestate = 10;
                }
            }
        }

        private void GameState()    //게임 상태가 어느상태인지 체크하고 컨트롤
        {
            while (gamestate > -1)
            {
                if (gamestate == 0) // 가이드 시작
                {
                    guide(1, baseDirectory);
                    Thread.Sleep(2000);
                    gamestate = 11;

                    SkipControl(Constants_DIBI.SKIPVISIBLE);

                    guide(2, baseDirectory);
                    Thread.Sleep(2500);
                    if (gamestate == 3)
                        continue;

                    guide(3, baseDirectory);
                    SoongOut(Constants_DIBI.SCISSOR, baseDirectory);
                    gamestate = 1;
                    while (playerState != Constants_DIBI.SCISSOR)
                    {
                        SkipControl(Constants_DIBI.SKIPHIDDEN);
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(Constants_DIBI.NONE, baseDirectory);
                        gamestate = 1;
                    }
                    gamestate = 11;
                    SoongOut(0, baseDirectory);
                    SkipControl(Constants_DIBI.SKIPVISIBLE);

                    if (gamestate == 3)
                        continue;
                    guide(4, baseDirectory);
                    Thread.Sleep(2000);
                    if (gamestate == 3)
                        continue;
                    guide(5, baseDirectory);
                    SoongOut(Constants_DIBI.ROCK, baseDirectory);
                    gamestate = 1;
                    while (playerState != Constants_DIBI.ROCK)
                    {
                        SkipControl(Constants_DIBI.SKIPHIDDEN);
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(Constants_DIBI.NONE, baseDirectory);
                        gamestate = 1;
                    }
                    gamestate = 11;
                    SoongOut(0, baseDirectory);
                    SkipControl(Constants_DIBI.SKIPVISIBLE);
                    if (gamestate == 3)
                        continue;

                    guide(4, baseDirectory);
                    Thread.Sleep(2000);
                    if (gamestate == 3)
                        continue;
                    guide(6, baseDirectory);
                    SoongOut(Constants_DIBI.PAPER, baseDirectory);
                    gamestate = 1;
                    while (playerState != Constants_DIBI.PAPER)
                    {
                        SkipControl(Constants_DIBI.SKIPHIDDEN);
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(Constants_DIBI.NONE, baseDirectory);
                        gamestate = 1;
                    }
                    SoongOut(0, baseDirectory);

                    guide(4, baseDirectory);
                    Thread.Sleep(2000);

                    gamestate = 3;
                }
                else if (gamestate == 3) // 인트로 Song 시작
                {
                    SkipControl(Constants_DIBI.SKIPHIDDEN);
                    guide(7, baseDirectory);
                    Thread.Sleep(2000);
                    guide(8, baseDirectory);
                    Thread.Sleep(2000);
                    guide(0, baseDirectory);

                    Console.WriteLine(baseDirectory + "startsong.wav");
                    Console.WriteLine("gamestate : " + gamestate);
                    System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "startsong.wav");
                    sp.Play();
                    Thread.Sleep(8000);
                    SoongOut(Constants_DIBI.SOONGMAIN, baseDirectory);

                    gamestate = 4;
                }
                else if (gamestate == 4)    // 디비디비딥 출력
                {
                    Console.WriteLine("Begin : " + System.DateTime.Now.ToString("mm:ss"));
                    sp1.Play();
                    while (!sp1.IsLoadCompleted) ;
                    Thread.Sleep(3000);
                    Console.WriteLine("Finish : " + System.DateTime.Now.ToString("mm:ss"));
                    Console.WriteLine("gamestate : " + gamestate);

                    monkeySate = random.Next(0, 100);   // 원숭이 랜덤 출력
                    monkeySate = (monkeySate % 3) + 7;  // 7 : 가위, 8 : 바위, 9 : 보

                    Console.WriteLine("Image Change " + monkeySate);
                    SoongOut(monkeySate, baseDirectory);
                    
                    gamestate = 5;  // 판별 
                }
                else if (gamestate == 7)    // 가위바위보 한번 끝
                {
                    Console.WriteLine("gamestate : " + gamestate);
                    Thread.Sleep(1000);
                    gamestate = 8;  // 가위바위보 준비 초기화 혹은 게임 끝
                }
                else if (gamestate == 9)    // 전체 끝
                {
                    Thread.Sleep(3000);
                    gamestate = 88;  // 가위바위보 준비 초기화 혹은 게임 끝
                }

                Thread.Sleep(300);
            }
        }

        private void Alert(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case Constants_DIBI.NONE:
                        Alertimg.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case Constants_DIBI.HANDSUP:
                        ImagePath += "guide_9.png";
                        Alertimg.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.HANDSDOWN:
                        ImagePath += "guide_10.png";
                        Alertimg.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.FINALWIN:
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(baseDirectory + "YOU-WIN.gif");
                        ImageBehavior.SetRepeatBehavior(Alertimg, new RepeatBehavior(3));
                        image.EndInit();
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        ImageBehavior.SetAnimatedSource(Alertimg, image); // 이미지 띄우기
                        break;
                    case Constants_DIBI.FINALLOSE:
                        var image1 = new BitmapImage();
                        image1.BeginInit();
                        image1.UriSource = new Uri(baseDirectory + "YOU-LOSE.gif");
                        ImageBehavior.SetRepeatBehavior(Alertimg, new RepeatBehavior(3));
                        image1.EndInit();
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        ImageBehavior.SetAnimatedSource(Alertimg, image1); // 이미지 띄우기
                        break;
                }
            }));
        }

        private void checkWin() // 이겼는지 졌는지 판별
        {
            if (monkeySate == playerState)
            {
                Console.WriteLine("Count up 1");
            }
            else if (playerState == Constants_DIBI.SCISSOR)
            {
                if (monkeySate == Constants_DIBI.ROCK)
                {
                    Console.WriteLine("Count up 2");
                    WinLoseCount(Constants_DIBI.LOSE);
                }
                else if (monkeySate == Constants_DIBI.PAPER)
                {
                    Console.WriteLine("Count up 3");
                    WinLoseCount(Constants_DIBI.WIN);
                }
            }
            else if (playerState == Constants_DIBI.ROCK)
            {
                if (monkeySate == Constants_DIBI.SCISSOR)
                {
                    Console.WriteLine("Count up 4");
                    WinLoseCount(Constants_DIBI.WIN);
                }
                else if (monkeySate == Constants_DIBI.PAPER)
                {
                    Console.WriteLine("Count up 5");
                    WinLoseCount(Constants_DIBI.LOSE);
                }
            }
            else if (playerState == Constants_DIBI.PAPER)
            {
                if (monkeySate == Constants_DIBI.SCISSOR)
                {
                    Console.WriteLine("Count up 6");
                    WinLoseCount(Constants_DIBI.LOSE);
                }
                else if (monkeySate == Constants_DIBI.ROCK)
                {
                    Console.WriteLine("Count up 7");
                    WinLoseCount(Constants_DIBI.WIN);
                }
            }

            score.Text = WinCount + " : " + LoseCount;
            score.Visibility = System.Windows.Visibility.Visible;

            Console.WriteLine("Game = " + WinCount);
        }

        private void guide(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case 0:
                        notice.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 1:
                        ImagePath += "guide_1.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 2:
                        ImagePath += "guide_2.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 3:
                        ImagePath += "guide_3.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 4:
                        ImagePath += "guide_4.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 5:
                        ImagePath += "guide_5.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 6:
                        ImagePath += "guide_6.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 7:
                        ImagePath += "guide_7.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 8:
                        ImagePath += "guide_8.png";
                        notice.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        notice.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }
        private void Menu(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case 0:
                        replay.Visibility = System.Windows.Visibility.Hidden;
                        home.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 1:
                        replay.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "re_db.png") as ImageSource;
                        replay.Visibility = System.Windows.Visibility.Visible;
                        home.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "home_db.png") as ImageSource;
                        home.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.REPLAYPRESSED:
                        replay.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "re_db_on.png") as ImageSource;
                        replay.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.HOMEPRESSED:
                        home.Source = new ImageSourceConverter().ConvertFromString(ImagePath + "home_db_on.png") as ImageSource;
                        home.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void startsong()
        {
            System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "startsong.wav");
            sp.Play();
        }
        
        private void SkipControl(int num)  // skip 버튼 컨트롤 하기
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (num)
                {
                    case Constants_DIBI.GAMESTATEVISIBLE:
                        skip.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.SKIPHIDDEN:
                        skip.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case Constants_DIBI.SKIPVISIBLE:
                        skip.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void SoongOut(int num, string ImagePath)    // 숭이의 그림을 바꾸는 것
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (num)
                {
                    case Constants_DIBI.NONE: // 가리기
                        monkey.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case Constants_DIBI.SCISSOR:
                        ImagePath += "sung_scissors.png";
                        monkey.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.ROCK:
                        ImagePath += "sung_rock.png";
                        monkey.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.PAPER:
                        ImagePath += "sung_paper.png";
                        monkey.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.SOONGMAIN:
                        ImagePath += "sung_main.png";
                        monkey.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.FINALWIN:
                        ImagePath += "sung_fail.png";
                        monkey.Source = new ImageSourceConverter().ConvertFromString(ImagePath) as ImageSource;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case Constants_DIBI.FINALLOSE:
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(baseDirectory + "sung_win.gif");
                        ImageBehavior.SetRepeatBehavior(monkey, new RepeatBehavior(3));
                        image.EndInit();
                        ImageBehavior.SetAnimatedSource(monkey, image); // 이미지 띄우기
                        break;
                }
            }));
        }

        

        private void WinLoseCount(int i)
        {
            switch (i)
            {
                case Constants_DIBI.LOSE:
                    monkey.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "sung_win.png") as ImageSource;
                    LoseCount++;
                    break;
                case Constants_DIBI.WIN:
                    monkey.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "sung_fail2.png") as ImageSource;
                    WinCount++;
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {           // sensor.Stop();
        }
    }
}