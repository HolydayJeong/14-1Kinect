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

namespace SungJik_SungHwa
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 

    //  public delegate void VoidIntDelegate(int i, string path); 얘는 뭐하는 애일까

    public partial class Dibi : Window
    {
        const int NONE = 0;
        const int SkipSource = 1;
        const int GAMESTATEVISIBLE = 2;
        const int UFO = 3;
        const int HANDHIDDEN = 4;
        //const int SKIPHIDDEN = 3;
        //const int SKIPVISIBLE = 4;
        const int LOSE = 5;
        const int WIN = 6;
        const int SCISSOR = 7;
        const int ROCK = 8;
        const int PAPER = 9;
        const int SOONGMAIN = 10;
        const int FINALWIN = 11;
        const int FINALLOSE = 12;
        const int HANDSUP = 13;
        const int HANDSDOWN = 14;
        const int REPLAYPRESSED = 15;
        const int HOMEPRESSED = 16;
        const int NOTRECOGNIZED = 17;
        const int HIDDEN = 18;
        const int VISIBLE = 19;
        const int NOBODY = 120;

        SungJik_SungHwa.PressButton Press = new SungJik_SungHwa.PressButton(AppDomain.CurrentDomain.BaseDirectory + "Main\\mouse.png", AppDomain.CurrentDomain.BaseDirectory + "Main\\mouse_pull.png");

        public MainWindow Main;

        public Dibi(MainWindow Main)
        {
            InitializeComponent();
            this.Main = Main;
        }

        KinectSensor sensor;
        const int SKELETON_COUNT = 6;
        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];
        Button button = new Button();


        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "dibi\\";

        int gamestate = 0;  // 게임 시작을 알려주는 
        // 01이 뒤에 붙으면 중간단계

        bool guideOn = false;
        int guideNo = 0; // 가이드 번호 프레임에서 제어해야 렌더링 크기를 알수 있기 때문에 그 제어를 위해서 변수 추가.

        int monkeySate = 0; // 원숭이가 낸거 0 : 대기, 1 : 가위, 2 : 주먹, 3 : 보
        int playerState = 0;// 사람이 낸거 0 : 대기, 1 : 가위, 2 : 주먹, 3 : 보

        int GameCount = 0;       // 게임 횟수
        int WinCount = 0;       // 이긴 횟수
        int LoseCount = 0;      // 진 횟수

        int Form = 0;

        int Nobody = 0;

        DepthImagePoint UserHand;

        Random random = new Random();

        System.Media.SoundPlayer sp1 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "dibi\\dibidibi.wav");
        System.Media.SoundPlayer bgm1 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "dibi\\No Spam Polka-wav.wav");    // bgm 틀기
        // 각 이미지 리스트 선언 //
        public List<Image> guideList = new List<Image>();
        public List<Image> backgroundList = new List<Image>();
        public List<Image> scoreList = new List<Image>();
        public List<Image> menuList = new List<Image>();
        public List<Image> soongList = new List<Image>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = (desktopWorkingArea.Right - this.Width) / 2;
            this.Top = (desktopWorkingArea.Bottom - this.Height) / 2;

            /////   이미지 세팅    /////
            for (int i = 1; i < 12; i++)
            {
                guideList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "guide_" + i + ".png")) });
            }
            for (int i = 0; i < 6; i++)
            {
                scoreList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "nc" + i + ".png")) });
            }

            //  원숭이 이미지 리스트  //

            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_scissors.png")) });
            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_rock.png")) });
            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_paper.png")) });
            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_main.png")) });
            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_win.png")) });
            soongList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "sung_fail2.png")) });

            //  배경 이미지 리스트 (skip, 배경, :) //
            backgroundList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "skip.png")) });
            backgroundList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "skip_on.png")) });
            backgroundList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "bg3.png")) });
            backgroundList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "colon.png")) });

            // 끝나고 메뉴 버튼 리스트 //
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "home_db.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "home_db_on.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "re_db.png")) });
            menuList.Add(new Image() { Source = new BitmapImage(new Uri(baseDirectory + "re_db_on.png")) });

            bg.Source = backgroundList[2].Source;
            colon.Source = backgroundList[3].Source;
            hand.Source = new ImageSourceConverter().ConvertFromString(AppDomain.CurrentDomain.BaseDirectory + "Main\\mouse.png") as ImageSource;

            /////   이미지 세팅 끝    /////

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

            if (Nobody == NOBODY)
            {
                Nobody = 121;
                Console.WriteLine("Nobody 121");
                goHome();
                return;
            }

            if (me == null)
            {
                Nobody++;
                return;
            }
            else
                Nobody = 0;

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
            if (SungJik_SungHwa.GLOBAL.SelectedGame == 1)
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

                    int HandLength = Math.Abs(RElbowDepthPoint.Y - RWristDepthPoint.Y) * 2;
                    int ElboDistance = RElbowDepthPoint.X - LElbowDepthPoint.X;

                    if (guideOn) // 가이드 출력용
                    {
                        guide(guideNo - 1, baseDirectory);
                    }
                    if (gamestate == 0)
                    {
                        gamestate = 101;

                        SkipControl(HIDDEN);
                        BgControl(HIDDEN, baseDirectory);

                        ScoreControl(0, baseDirectory, score1);
                        ScoreControl(0, baseDirectory, score2);
                        ScoreControl(HIDDEN, baseDirectory, null);

                        ThreadStart threadStart = new ThreadStart(GameState);
                        Thread newThread = new Thread(threadStart);
                        newThread.Start();
                        gamestate = 1;
                    }
                    else if (gamestate == 2)    //스킵용
                    {
                        Point targetTopLeft = new Point(Canvas.GetLeft(skip), Canvas.GetTop(skip));
                        targetTopLeft.X = targetTopLeft.X / 2;
                        targetTopLeft.Y = targetTopLeft.Y / 2;

                        if (RHandDepthPoint.X > targetTopLeft.X && RHandDepthPoint.X < targetTopLeft.X + skip.ActualWidth / 2 && RHandDepthPoint.Y > targetTopLeft.Y && RHandDepthPoint.Y < targetTopLeft.Y + skip.ActualHeight / 2)
                        {
                            Press.reset();
                            skip.Source = backgroundList[1].Source;
                            gamestate = 4;  // 인트로 스킵
                            Console.WriteLine("gamestate : " + gamestate);
                        }
                    }
                    else if (gamestate == 3 || gamestate == 6)
                    {
                        if (gamestate == 3)
                            gamestate = 301;  // 다른 프로세스 들어옴 방지.
                        else if (gamestate == 6)
                            gamestate = 601;    // 프로세스 난입 방지


                        if (LHandDepthPoint.X <= LShoulderDepthPoint.X && RHandDepthPoint.X >= RShoulderDepthPoint.X)  // 양쪽손 > 양쪽어깨 비교(벌릴때 / 보)
                        {
                            //case 보자기
                            if (bodyDepthPoint.Y > LHandDepthPoint.Y && bodyDepthPoint.Y > RHandDepthPoint.Y)   // 몸 < 양손 비교하기
                            {
                                playerState = PAPER;
                            }
                            else
                            {
                                Form = HANDSUP;
                                Alert(HANDSUP, baseDirectory);
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
                                    playerState = ROCK;
                                }
                                else
                                {
                                    Form = HANDSUP;
                                    Alert(HANDSUP, baseDirectory);
                                    playerState = 2;
                                }
                            else
                            {
                                Form = HANDSDOWN;
                                Alert(HANDSDOWN, baseDirectory);
                                playerState = 3;
                            }
                        }

                        else if (RHandDepthPoint.X >= bodyDepthPoint.X && LWristDepthPoint.X > LShoulderDepthPoint.X)  // 왼쪽어깨 < 왼손 , 머리 < 오른손
                        {
                            //case 가위
                            if (headDepthPoint.Y < LHandDepthPoint.Y && headDepthPoint.Y < RHandDepthPoint.Y)   // 머리 > 양손 비교하기
                                if (hipDepthPoint.Y > LHandDepthPoint.Y && hipDepthPoint.Y > RHandDepthPoint.Y)   // 엉덩이 < 양손 비교하기Form = "가위";
                                {
                                    playerState = SCISSOR;
                                }
                                else
                                {
                                    Form = HANDSUP;
                                    Alert(HANDSUP, baseDirectory);
                                    playerState = 4;
                                }
                            else
                            {
                                Form = HANDSDOWN;
                                Alert(HANDSDOWN, baseDirectory);
                                playerState = 5;
                            }
                        }
                        else
                        {
                            Form = NOTRECOGNIZED;
                            playerState = 6;
                        }

                        if (gamestate == 601)     // 게임 중이므로 판별로 넘어가야함
                        {
                            gamestate = 7;       // 대기 및 판별인 7로 넘어감
                        }
                    }
                    else if (gamestate == 9)
                    {
                        gamestate = 901;    // 프로세스 난입 방지

                        Alert(NONE, baseDirectory);

                        Console.WriteLine("Game Over  : " + WinCount + " : " + LoseCount);

                        if (GameCount == 5)
                        {
                            if (WinCount == 3)
                            {
                                //doNotice("승리!!");
                                SoongOut(FINALWIN, baseDirectory);
                                Alert(FINALWIN, baseDirectory);
                                System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "win.wav");
                                sp.Play();

                            }
                            else if (LoseCount == 3)
                            {
                                //doNotice("패배!!");
                                SoongOut(FINALLOSE, baseDirectory);
                                Alert(FINALLOSE, baseDirectory);
                                System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "loose.wav");
                                sp.Play();
                            }
                            gamestate = 10;
                        }
                        else
                        {
                            SoongOut(SOONGMAIN, baseDirectory);
                            gamestate = 5;  // 디비디비딥 출력
                        }
                    }
                    else if (gamestate == 11)   // 끝 정리.
                    {
                        gamestate = 1101;
                        ImageBehavior.SetAnimatedSource(monkey, null);  // 이미지 없애기
                        ImageBehavior.SetAnimatedSource(Alertimg, null);  // 이미지 없애기
                        ImageBehavior.SetAnimatedSource(ufo, null);  // 이미지 없애기
                        Alert(NONE, baseDirectory);
                        BgControl(HIDDEN, baseDirectory);
                        SoongOut(NONE, baseDirectory);
                        Menu(1, baseDirectory);
                        ScoreControl(HIDDEN, null, null);

                        GameCount = 0;
                        WinCount = 0;
                        LoseCount = 0;
                        playerState = 0;
                        monkeySate = 0;
                        gamestate = 12;
                    }
                    else if (gamestate == 12)
                    {
                        hand.Visibility = System.Windows.Visibility.Visible;
                        Point replayTopLeft = new Point(Canvas.GetLeft(replay), Canvas.GetTop(replay));
                        Point homeTopLeft = new Point(Canvas.GetLeft(home), Canvas.GetTop(home));

                        //해상도 확장으로 인한 변수 값 조정 
                        //(해상도 줄이면 이곳을 주석처리하고 이미지 크기에 /2 지우면 됨)
                        replayTopLeft.X = replayTopLeft.X / 2;
                        replayTopLeft.Y = replayTopLeft.Y / 2;
                        homeTopLeft.X = homeTopLeft.X / 2;
                        homeTopLeft.Y = homeTopLeft.Y / 2;
                        // 변수값 조정 끝

                        if (RHandDepthPoint.Depth <= LHandDepthPoint.Depth) // 유저의 손이 오른손일 경우
                            UserHand = RHandDepthPoint;
                        else
                            UserHand = LHandDepthPoint;

                        Canvas.SetLeft(hand, UserHand.X * 2 - hand.Width / 2);  // *2 는 해상도 확장으로 인한 변수 값 조정
                        Canvas.SetTop(hand, UserHand.Y * 2 - hand.Width / 2);   // *2 는 해상도 확장으로 인한 변수 값 조정

                        if (Nobody > NOBODY)
                        {
                            goHome();
                            return;
                        }
                        // 리플레이
                        else if (UserHand.X > replayTopLeft.X && UserHand.X < replayTopLeft.X + replay.ActualWidth / 2 && UserHand.Y > replayTopLeft.Y && UserHand.Y < replayTopLeft.Y + replay.ActualHeight / 2)
                        {
                            Menu(REPLAYPRESSED, baseDirectory);
                            Press.detectPressure(UserHand.Depth, ref hand);
                            if (Press.isConfirmed() == true)
                            {
                                gamestate = 1201;
                                hand.Visibility = System.Windows.Visibility.Hidden;
                                Press.reset(ref hand);
                                Menu(0, baseDirectory);

                                gamestate = 1;      // 다시 초기화
                            }
                        }
                        // 홈으로 가기
                        else if (UserHand.X > homeTopLeft.X && UserHand.X < homeTopLeft.X + home.ActualWidth / 2 && UserHand.Y > homeTopLeft.Y && UserHand.Y < homeTopLeft.Y + home.ActualHeight / 2)
                        {
                            Menu(HOMEPRESSED, baseDirectory);
                            Press.detectPressure(UserHand.Depth, ref hand);
                            if (Press.isConfirmed() == true)
                            {
                                gamestate = 1201;   // 프로세스 난입 방지
                                hand.Visibility = System.Windows.Visibility.Hidden;
                                Press.reset(ref hand);
                                goHome();
                                return;
                            }
                        }
                        else
                        {
                            Press.reset(ref hand);
                            Menu(1, baseDirectory);
                        }

                    }
                }
            }
        }

        private void GameState()    //게임 상태가 어느상태인지 체크하고 컨트롤
        {
            while (gamestate > -1)
            {
                if (Nobody > NOBODY)
                {
                    bgm1.Stop();
                    goHome();
                }
                else if (SungJik_SungHwa.GLOBAL.SelectedGame != 1)
                    break;
                Console.WriteLine("gamestate in gamestate : " + gamestate);
                if (gamestate == 1) // 가이드 시작
                {
                    gamestate = 101; // 프로세스 난입 방지.
                    BgControl(HANDHIDDEN, null);        // 손 감추기
                    bgm1.Play();
                    guideControl(1, 300);
                    Thread.Sleep(2000);
                    gamestate = 2;

                    SkipControl(SkipSource);
                    SkipControl(VISIBLE);

                    guideControl(2, 100);
                    Thread.Sleep(2500);
                    if (gamestate == 4) /// 인트로 확정 다 넘기자
                        continue;

                    guideControl(3, 100);
                    SoongOut(SCISSOR, baseDirectory);
                    gamestate = 3;
                    SkipControl(HIDDEN);
                    while (playerState != SCISSOR)
                    {
                        if (Nobody > NOBODY)
                            break;
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(NONE, baseDirectory);
                        gamestate = 3;
                    }
                    if (Nobody > NOBODY)
                        continue;
                    gamestate = 2;
                    SoongOut(0, baseDirectory);
                    SkipControl(VISIBLE);

                    guideControl(4, 100);
                    Thread.Sleep(2000);
                    if (gamestate == 4)
                        continue;

                    guideControl(5, 100);
                    SoongOut(ROCK, baseDirectory);
                    gamestate = 3;
                    SkipControl(HIDDEN);
                    while (playerState != ROCK)
                    {
                        if (Nobody > NOBODY)
                            break;
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(NONE, baseDirectory);
                        gamestate = 3;
                    }
                    gamestate = 2;
                    SoongOut(0, baseDirectory);
                    SkipControl(VISIBLE);

                    guideControl(4, 100);
                    Thread.Sleep(2000);
                    if (gamestate == 4)
                        continue;

                    guideControl(6, 100);
                    SoongOut(PAPER, baseDirectory);
                    gamestate = 3;
                    SkipControl(HIDDEN);
                    while (playerState != PAPER)
                    {
                        if (Nobody > NOBODY)
                            break;
                        Console.WriteLine("playerstate : " + playerState);
                        Alert(Form, baseDirectory);
                        Thread.Sleep(800);
                        Alert(NONE, baseDirectory);
                        gamestate = 3;
                    }
                    SoongOut(0, baseDirectory);

                    guideControl(4, 100);
                    Thread.Sleep(2000);
                    bgm1.Stop();
                    gamestate = 4;
                }
                else if (gamestate == 4) // 인트로 Song 시작
                {
                    gamestate = 401;    // 프로세스 난입 방지
                    SkipControl(HIDDEN);

                    guideControl(7, 100);
                    Thread.Sleep(2000);

                    guideControl(8, 100);
                    Thread.Sleep(2000);

                    guideOn = false;
                    guide(HIDDEN, null);


                    Console.WriteLine(baseDirectory + "startsong.wav");
                    Console.WriteLine("gamestate : " + gamestate);
                    BgControl(VISIBLE, baseDirectory); //화면 세팅
                    System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "startsong.wav");
                    sp.Play();

                    BgControl(UFO, baseDirectory);

                    Thread.Sleep(8000);

                    ScoreControl(VISIBLE, baseDirectory, null);
                    SoongOut(SOONGMAIN, baseDirectory);

                    gamestate = 5;
                }
                else if (gamestate == 5)    // 디비디비딥 출력
                {
                    gamestate = 501;
                    //Console.WriteLine("Begin : " + System.DateTime.Now.ToString("mm:ss"));
                    sp1.PlaySync(); // 플레이가 끝날때까지 대기함.
                    Console.WriteLine("gamestate : " + gamestate);

                    monkeySate = random.Next(0, 100);   // 원숭이 랜덤 출력
                    monkeySate = (monkeySate % 3) + 7;  // 7 : 가위, 8 : 바위, 9 : 보

                    //Console.WriteLine("Image Change " + monkeySate);
                    SoongOut(monkeySate, baseDirectory);

                    gamestate = 6;  // 판별 
                }
                else if (gamestate == 7)
                {
                    gamestate = 701;    // 프로세스 난입 방지
                    Thread.Sleep(800);
                    checkWin();
                    gamestate = 8;
                }
                else if (gamestate == 8)    // 가위바위보 한번 끝
                {
                    gamestate = 801;
                    Console.WriteLine("gamestate : " + gamestate);
                    Thread.Sleep(1000); // 한판 이김, 짐 숭익이 보여주기
                    gamestate = 9;  // 가위바위보 준비 초기화 혹은 게임 끝
                }
                else if (gamestate == 10)
                {
                    gamestate = 1001;
                    Thread.Sleep(3000);
                    gamestate = 11;
                }
                else
                    Thread.Sleep(300);
            }
        }

        private void Alert(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case NONE:
                        Alertimg.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case HANDSUP:
                        Alertimg.Source = guideList[8].Source;
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case HANDSDOWN:
                        Alertimg.Source = guideList[9].Source;
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case NOTRECOGNIZED:
                        Alertimg.Source = guideList[10].Source;
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case FINALWIN:
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(baseDirectory + "YOU-WIN.gif");
                        ImageBehavior.SetRepeatBehavior(Alertimg, new RepeatBehavior(3));
                        image.EndInit();
                        Alertimg.Visibility = System.Windows.Visibility.Visible;
                        ImageBehavior.SetAnimatedSource(Alertimg, image); // 이미지 띄우기
                        break;
                    case FINALLOSE:
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

        private void BgControl(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case HANDHIDDEN:
                        hand.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case HIDDEN:
                        bg.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case VISIBLE:
                        bg.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case UFO:
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(baseDirectory + "ufo.gif");
                        ImageBehavior.SetRepeatBehavior(ufo, new RepeatBehavior(1));
                        image.EndInit();
                        ufo.Visibility = System.Windows.Visibility.Visible;
                        ImageBehavior.SetAnimatedSource(ufo, image); // 이미지 띄우기
                        break;
                }
            }));
        }

        private void checkWin() // 이겼는지 졌는지 판별
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (monkeySate == playerState)
                {
                    WinLoseCount(LOSE);
                }
                else if (playerState > 6)
                    WinLoseCount(WIN);

            }));
        }
        private void goHome()
        {
            Dispatcher.Invoke(DispatcherPriority.Loaded, new Action(delegate
            {
                gamestate = 1201;
                Main.Show();
                SungJik_SungHwa.GLOBAL.SelectedGame = 0;
                this.Hide();
                gamestate = 0;
                return;
            }));
        }

        private void guide(int i, string ImagePath)
        {
            Dispatcher.Invoke(DispatcherPriority.Loaded, new Action(delegate
            {
                if (i == HIDDEN)
                    notice.Visibility = System.Windows.Visibility.Hidden;
                else if (i == VISIBLE)
                    notice.Visibility = System.Windows.Visibility.Visible;
                else
                    notice.Source = guideList[i].Source;

                noticePosition();
            }));
        }

        private void guideControl(int i, int time)
        {
            if (i == 1)
            {
                guideNo = i;
                guideOn = true;
                Thread.Sleep(time);
                guide(VISIBLE, null);

            }
            else
            {
                guide(HIDDEN, null);
                guideNo = i;
                Thread.Sleep(time);
                guide(VISIBLE, null);
            }

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
                        home.Source = menuList[0].Source;
                        home.Visibility = System.Windows.Visibility.Visible;
                        replay.Source = menuList[2].Source;
                        replay.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case REPLAYPRESSED:
                        replay.Source = menuList[3].Source;
                        replay.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case HOMEPRESSED:
                        home.Source = menuList[1].Source;
                        home.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void noticePosition()
        {

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                if (notice.ActualWidth == 0)
                {
                    Canvas.SetLeft(notice, kinect1.ActualWidth / 2 - 200);
                }

                Canvas.SetLeft(notice, kinect1.ActualWidth / 2 - notice.ActualWidth / 2);
                Canvas.SetTop(notice, Canvas.GetTop(kinect1) + 30);
            }));
        }

        private void ScoreControl(int i, string ImagePath, Image image)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (i)
                {
                    case 0:
                        image.Source = scoreList[0].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 1:
                        image.Source = scoreList[1].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 2:
                        image.Source = scoreList[2].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 3:
                        image.Source = scoreList[3].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 4:
                        image.Source = scoreList[4].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 5:
                        image.Source = scoreList[5].Source;
                        image.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case HIDDEN:
                        score1.Visibility = System.Windows.Visibility.Hidden;
                        colon.Visibility = System.Windows.Visibility.Hidden;
                        score2.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case VISIBLE:
                        score1.Visibility = System.Windows.Visibility.Visible;
                        colon.Visibility = System.Windows.Visibility.Visible;
                        score2.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));

        }

        private void SkipControl(int num)  // skip 버튼 컨트롤 하기
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (num)
                {
                    case SkipSource:
                        skip.Source = backgroundList[0].Source;
                        break;
                    case HIDDEN:
                        skip.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case VISIBLE:
                        skip.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
            }));
        }

        private void startsong()
        {
            System.Media.SoundPlayer sp = new System.Media.SoundPlayer(baseDirectory + "startsong.wav");
            sp.Play();
        }



        private void SoongOut(int num, string ImagePath)    // 숭이의 그림을 바꾸는 것
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                switch (num)
                {
                    case NONE: // 가리기
                        monkey.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case SCISSOR:
                        monkey.Source = soongList[0].Source;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case ROCK:
                        monkey.Source = soongList[1].Source;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case PAPER:
                        monkey.Source = soongList[2].Source;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case SOONGMAIN:
                        monkey.Source = soongList[3].Source;
                        monkey.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case FINALWIN:
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(baseDirectory + "sung_cry.gif");
                        ImageBehavior.SetRepeatBehavior(monkey, new RepeatBehavior(3));
                        image.EndInit();
                        ImageBehavior.SetAnimatedSource(monkey, image); // 이미지 띄우기
                        break;
                    case FINALLOSE:
                        var image1 = new BitmapImage();
                        image1.BeginInit();
                        image1.UriSource = new Uri(baseDirectory + "sung_win.gif");
                        ImageBehavior.SetRepeatBehavior(monkey, new RepeatBehavior(3));
                        image1.EndInit();
                        ImageBehavior.SetAnimatedSource(monkey, image1); // 이미지 띄우기
                        break;
                }
            }));
        }

        private void WinLoseCount(int i)
        {
            switch (i)
            {
                case LOSE:
                    monkey.Source = soongList[4].Source;
                    LoseCount++;
                    GameCount++;
                    ScoreControl(LoseCount, baseDirectory, score2);
                    break;
                case WIN:
                    monkey.Source = soongList[5].Source;
                    WinCount++;
                    GameCount++;
                    ScoreControl(WinCount, baseDirectory, score1);
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {           // sensor.Stop();
        }
    }
}