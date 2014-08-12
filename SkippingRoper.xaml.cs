﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using WpfAnimatedGif;
//

//추가해주어야 할 부분 using을 쓰면 kinect library를 불러올 수 있다.
using Microsoft.Kinect;
//프로젝트 이름이다.
namespace SungJik_SungHwa
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public delegate void VoidIntDelegate(int num);

    public partial class SkippingRoper : Window
    {
        //kinect sensor를 선언함
        KinectSensor sensor = null;

        const int SKELOTON_COUNT = 6;
        const int SUNGJIK = 0;
        const int PIG = 1;
        const int MICE = 2;
        const int CONFIRM = 15;
        const int FRAME_MAX = 24;

        //14/07/23 : 프로그램 제어를 위함 
        const int SELECTING = 0;
        const int PLAYING = 1;
        const int END = 2;
        //14/07/24 : 스킵버튼을 위한 제어
        const int BEGINNING = 4;
        //14/08/08
        const int RIGHT = 1;
        const int LEFT = 0;

        int PressingHand = RIGHT;

        int frameNum = 0;

        //14/07/24 : 초기 상태는 시작
        int gameState = BEGINNING;

        int time = 0;
        int selectedChar = 0;
        int oppChar = 0;

        private Object thisLock = new Object();
        Boolean selected = false;

        Boolean begin = false;
        Boolean start = false;
        Boolean jump = false;
        Boolean oppoJump = false;
        Boolean scoring = false;
        Boolean stop = false;
        Boolean stopPause = true;
        Boolean initial = false;
        Skeleton[] allSkeletons = new Skeleton[SKELOTON_COUNT];

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "줄넘기\\";

        Canvas titleCanvas = new Canvas();
        Canvas ropeCanvas = new Canvas();
        //14/07/24 : 스킵버튼을 만듬
        Image introText = new Image();
        Image go = new Image();
        Image ready = new Image();
        Image WinLose = new Image();
        Image skipButton = new Image();
        Image Hand = new Image();
        Image[] rope = new Image[FRAME_MAX];

        struct Character
        {
            public String wait;
            public String jump;
            public String miss;

            public Character(String wait, String jump, String miss)
            {
                this.wait = wait;
                this.jump = jump;
                this.miss = miss;
            }
        }
        MediaPlayer sound = new MediaPlayer();

        MediaPlayer gameover = new MediaPlayer();
        MediaPlayer backGround = new MediaPlayer();
        MediaPlayer[] cheers = new MediaPlayer[7];
        System.Media.SoundPlayer win = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/줄넘기/" + "monkey.wav");


        Character[] characters = new Character[3]{
            new Character("sung_wait.png", "sung_jump.png", "sung_miss.png"), 
            new Character("pig_wait.png", "pig_jump.png", "pig_miss.png"), 
            new Character("mou_wait.png", "mou_jump.png", "mou_miss.png") };

        string[] addresses = new string[FRAME_MAX];

        PressButton Pressing;



        //메w인 화면
        public SkippingRoper()
        {
            Pressing = new PressButton(baseDirectory + "mouse.png", baseDirectory + "mouse_pull.png");

            //backGround.Open(new Uri(baseDirectory + "background.wav"));

            InitializeComponent();

            InitUI();

        }

        //윈도우가 닫히는 이벤트: 아무것도 하지 않는다
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        //윈도우가 불러오면 하는 이벤트
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = (desktopWorkingArea.Right - this.Width) / 2;
            this.Top = (desktopWorkingArea.Bottom - this.Height) / 2;
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(baseDirectory + "bg.gif");
            image.EndInit();
            ImageBehavior.SetAnimatedSource(Screen, image);
            //키넥트가 연결되어 있는지 확인한다. 만일 연결되어 있으면 선언한 sensor와 연결된 kinect의 정보를 준다
            if (KinectSensor.KinectSensors.Count > 0)
                sensor = KinectSensor.KinectSensors[0];
            //14/07/24 : 우선 키넥트를 먼저 준비시킨후 실행한다
            guide();

        }

        private void prepareKinect()
        {
            if (sensor == null)
            {
                return;
            }
            if (sensor.Status == KinectStatus.Connected)
            {
                //색깔 정보
                sensor.ColorStream.Enable();
                //깊이 정보
                sensor.DepthStream.Enable();
                //사람 인체 인식 정보
                sensor.SkeletonStream.Enable();

                //kinect가 준비되면 이벤트를 발생시키라는 명령문

                sensor.AllFramesReady += sensor_AllFramesReady;
                //sensor를 시작한다 (thread와 같다고 보면 된다)
                sensor.Start();
                //14/07/24 : intro를 kinect 준비 시킨뒤 실행산다
            }
        }

        private void InitUI()
        {
            for (int j = 0; j < 7; j++)
            {
                cheers[j] = new MediaPlayer();
                cheers[j].Open(new Uri(baseDirectory + "cheer\\cheer_" + (j + 1).ToString() + ".wav"));
            }

            sound.Open(new Uri(baseDirectory + "jump.wav"));
            gameover.Open(new Uri(baseDirectory + "gameOver.mp3"));
            backGround.Open(new Uri(baseDirectory + "background.mp3"));


            for (int i = 0; i < FRAME_MAX; i++)
            {
                rope[i] = new Image();
                rope[i].Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "rope\\rope_" + (i).ToString() + ".png") as ImageSource;
                addresses[i] = "rope\\rope_" + (i).ToString() + ".png";
            }
            SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
            Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
            Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;

            Hand.Visibility = System.Windows.Visibility.Visible;
            Hand.Width = 45;
            Hand.Height = 45;
            Hand.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "mouse.png") as ImageSource;

            Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_jump.png") as ImageSource;
            Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_jump.png") as ImageSource;
            go.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "go.png") as ImageSource;
            ready.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "ready.gif") as ImageSource;
            WinLose.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "YOU-LOSE.gif") as ImageSource;

            go.Visibility = System.Windows.Visibility.Hidden;
            Canvas.SetLeft(go, 520);
            Canvas.SetTop(go, 200);

            ready.Visibility = System.Windows.Visibility.Hidden;
            Canvas.SetLeft(ready, 450);
            Canvas.SetTop(ready, 200);


            WinLose.Visibility = System.Windows.Visibility.Hidden;
            Canvas.SetLeft(WinLose, 460);
            Canvas.SetTop(WinLose, 200);

            titleCanvas.Width = 1300;
            titleCanvas.Height = 1000;
            ropeCanvas.Width = 1300;
            ropeCanvas.Height = 1000;

            //14/07/24 : skip 버튼의 크기 및 정하기
            skipButton.Visibility = System.Windows.Visibility.Visible;
            skipButton.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "skip.png") as ImageSource;
            Canvas.SetTop(skipButton, 500);
            Canvas.SetLeft(skipButton, 800);

            Canvas.SetTop(GuideText, (titleCanvas.Height / 3) - 100);
            Canvas.SetLeft(GuideText, (titleCanvas.Width / 3));

            introText.Visibility = System.Windows.Visibility.Hidden;

            titleCanvas.Children.Add(introText);
            titleCanvas.Children.Add(skipButton);

            foreach (Image ropef in rope)
            {
                ropeCanvas.Children.Add(ropef);
                ropef.Visibility = System.Windows.Visibility.Hidden;
            }

            mainCanvas.Children.Add(titleCanvas);
            mainCanvas.Children.Add(ropeCanvas);
            mainCanvas.Children.Add(go);
            mainCanvas.Children.Add(ready);
            mainCanvas.Children.Add(WinLose);
            mainCanvas.Children.Add(Hand);

            if (initial == false)
            {
                backGround.Play();
                initial = true;
            }

        }

        private void introduction()
        {
            DoubleAnimation animation = new DoubleAnimation();

            //14/07/24 : introduction의 초기를 늘렸다 (loading이 너무 길어서 intro가 안보인다)
            animation.From = 800.0;
            animation.To = 0.0;

            animation.AccelerationRatio = 1;
            animation.Duration = new Duration(TimeSpan.FromSeconds(5));

            animation.FillBehavior = FillBehavior.Stop;

            animation.Completed += animation_Completed;

            titleCanvas.BeginAnimation(Canvas.TopProperty, animation);

        }

        private void guide()
        {
            prepareKinect();
            introText.Visibility = System.Windows.Visibility.Visible;
            Canvas.SetTop(introText, (this.Height / 2) - 137);
            Canvas.SetLeft(introText, (this.Width / 2) - 226);
            introText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_1.png") as ImageSource;


            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0.0;
            animation.To = 0.0;
            animation.AccelerationRatio = 0.0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(7));
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += animation_Completed;
            titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
        }
        //본래 0
        int counter = 0;
        void animation_Completed(object sender, EventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation();

            switch (counter)
            {
                /*
            case 0:
                counter++;
                animation.From = 0.0;
                animation.To = 0.0;
                animation.AccelerationRatio = 0.0;
                animation.Duration = new Duration(TimeSpan.FromSeconds(5));
                animation.FillBehavior = FillBehavior.HoldEnd;
                animation.Completed += animation_Completed;
                titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                break;
            case 1:
                counter++;
                animation.From = 0.0;
                animation.To = -800.0;
                animation.AccelerationRatio = 1.0;
                animation.Duration = new Duration(TimeSpan.FromSeconds(5));
                animation.FillBehavior = FillBehavior.Stop;
                animation.Completed += animation_Completed;
                titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                break;
            case 2:
                counter++;
                titleImage.Visibility = System.Windows.Visibility.Hidden;
                //14/07/24 : skip 버튼이 제목이 지나간뒤 보여진다
                skipButton.Visibility = System.Windows.Visibility.Visible;
                HandR.Visibility = System.Windows.Visibility.Visible;
                //14/08/0 hand left 추가
                HandL.Visibility = System.Windows.Visibility.Visible;
                guide();
                prepareKinect();
                break;*/
                case 0:
                    counter++;
                    introText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_2.png") as ImageSource;
                    Canvas.SetTop(introText, (titleCanvas.Height / 2) - 111);
                    Canvas.SetLeft(introText, (titleCanvas.Width / 2) - 230);
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(4));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 1:
                    counter++;
                    introText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_3.png") as ImageSource;
                    Canvas.SetTop(introText, (titleCanvas.Height / 2) - 111);
                    Canvas.SetLeft(introText, (titleCanvas.Width / 2) - 349);
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(4));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 2:
                    counter++;
                    introText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_4.png") as ImageSource;
                    Canvas.SetTop(introText, (titleCanvas.Height / 2) - 112);
                    Canvas.SetLeft(introText, (titleCanvas.Width / 2) - 240);
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(3));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 3:
                    counter++;
                    introText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_5.png") as ImageSource;
                    Canvas.SetTop(introText, (titleCanvas.Height / 2) - 111);
                    Canvas.SetLeft(introText, (titleCanvas.Width / 2) - 161);
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(3));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 4:
                    counter++;
                    titleCanvas.Visibility = System.Windows.Visibility.Hidden;
                    gameState = SELECTING;
                    break;
                case 5:
                    counter++;
                    ready.Visibility = System.Windows.Visibility.Hidden;
                    go.Visibility = System.Windows.Visibility.Visible;
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 6:
                    counter = 5;
                    start = true;
                    stop = false;
                    go.Visibility = System.Windows.Visibility.Hidden;
                    skipping();
                    break;
                default:
                    break;
            }
        }

        //14/07/24 : skip을 손으로 터치시에 바로 게임을 실행한다
        int handdepth = 0;
        private void skipIntro(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (me == null || e == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null) return;
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                DepthImagePoint HandLeftDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                ColorImagePoint HandLeftColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandLeftDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);

                //손위치 (내 오른쪽 손에 연결한다)

                if (HandRightDepthImagePoint.Depth <= HandLeftDepthImagePoint.Depth)
                {
                    Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                    Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);
                    handdepth = HandRightDepthImagePoint.Depth;
                }
                else
                {
                    Canvas.SetLeft(Hand, HandLeftColorImagePoint.X - Hand.Width / 2);
                    Canvas.SetTop(Hand, HandLeftColorImagePoint.Y - Hand.Height / 2);
                    handdepth = HandLeftDepthImagePoint.Depth;
                }
                if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > 800) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < 880) && (Canvas.GetTop(Hand) + Hand.Height / 2 > 500) && (Canvas.GetTop(Hand) + Hand.Height / 2 < 645))
                {
                    skipButton.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "skip_on.png") as ImageSource;
                    counter = 4;
                }
            }
        }

        //준비가 됬을때의 이벤트
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            Skeleton me = null;
            Console.WriteLine("Hello");
            GetSkeleton(e, ref me);
            if (me == null)
                return;
            if (gameState == BEGINNING)
            {
                skipIntro(me, e);
            }

            if (gameState == SELECTING)
            {
                if (me == null)
                {
                    GuideText.Visibility = System.Windows.Visibility.Visible;
                    return;
                }

                if (selected == false)
                {
                    //14/07/24 : count down이 보이도록
                    GuideText.Visibility = System.Windows.Visibility.Visible;
                    SungJik.Visibility = System.Windows.Visibility.Visible;
                    Pig.Visibility = System.Windows.Visibility.Visible;
                    Mice.Visibility = System.Windows.Visibility.Visible;
                    SelectCharacter(me, e);
                }
            }
            if (gameState == PLAYING)
            {
                if (selected == true)
                {
                    StartGame(me, e);

                }

                if (stop == false && start == true)
                {
                    isJumped2(e, me);
                }

                if (stop == true && stopPause == true)
                {
                    gameState = END;
                    stopPause = false;
                    backGround.Stop();
                    if (jump == false)
                        gameover.Play();
                    else
                        win.Play();
                    Hand.Visibility = System.Windows.Visibility.Visible;
                    Replay.Visibility = System.Windows.Visibility.Visible;
                    Home.Visibility = System.Windows.Visibility.Visible;
                    WinLose.Visibility = System.Windows.Visibility.Visible;
                    result();
                }
            }

            if (gameState == END)
            {
                if (stop == true)
                {
                    replayHome(me, e);
                }
            }
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

        //캐릭터 선택창
        private void SelectCharacter(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (me == null || e == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null) return;
                GuideText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide_2.png") as ImageSource;
                //손목위치 찾기
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);

                DepthImagePoint HandLeftDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                ColorImagePoint HandLeftColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandLeftDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                //손위치 (내 오른쪽 손에 연결한다)
                if ((PressingHand == RIGHT && Pressing.isPressed() == true) || (HandRightDepthImagePoint.Depth <= HandLeftDepthImagePoint.Depth))
                {
                    if (PressingHand == LEFT && Pressing.isPressed() == true) { }
                    else
                    {
                        Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                        Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);
                        handdepth = HandRightDepthImagePoint.Depth;
                        PressingHand = RIGHT;
                    }
                }
                if ((PressingHand == LEFT && Pressing.isPressed() == true) || (HandRightDepthImagePoint.Depth > HandLeftDepthImagePoint.Depth))
                {
                    Canvas.SetLeft(Hand, HandLeftColorImagePoint.X - Hand.Width / 2);
                    Canvas.SetTop(Hand, HandLeftColorImagePoint.Y - Hand.Height / 2);
                    handdepth = HandLeftDepthImagePoint.Depth;
                    PressingHand = LEFT;
                }
                if (gameState == SELECTING)
                {
                    //위치 지정시
                    if ((
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(SungJik) + 90 - Pressing.wideRange()) &&
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(SungJik) + 290 + Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(SungJik) + 80 - Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(SungJik) + SungJik.Height + Pressing.wideRange())))
                    {
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].jump) as ImageSource;
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;


                        Pressing.detectPressure(handdepth, ref Hand);

                        if (Pressing.isConfirmed() == true)
                        {
                            gameState = PLAYING;
                            Pressing.reset(ref Hand);

                            selected = true;
                            selectedChar = SUNGJIK;
                            countingDown();
                        }

                    }

                    else if ((
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Pig) + 90 - Pressing.wideRange()) &&
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Pig) + 290 + Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Pig) + 80 - Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Pig) + Pig.Height + Pressing.wideRange())))
                    {
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].jump) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;

                        Pressing.detectPressure(handdepth, ref Hand);

                        if (Pressing.isConfirmed() == true)
                        {
                            gameState = PLAYING;

                            Pressing.reset(ref Hand);
                            selected = true;
                            selectedChar = PIG;
                            countingDown();
                        }
                    }
                    else if ((
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Mice) + 90 - Pressing.wideRange()) &&
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Mice) + 290 + Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Mice) + 80 - Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Mice) + Mice.Height + Pressing.wideRange())))
                    {
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].jump) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;

                        Pressing.detectPressure(HandRightDepthImagePoint.Depth, ref Hand);

                        if (Pressing.isConfirmed() == true)
                        {
                            gameState = PLAYING;
                            Pressing.reset(ref Hand);

                            selected = true;
                            selectedChar = MICE;
                            countingDown();
                        }

                    }
                    else
                    {
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait
                            ) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
                        Pressing.reset(ref Hand);
                    }
                }

                if (gameState == PLAYING)
                {
                    if (selected == true)
                    {
                        Random rand = new Random();
                        do
                        {
                            oppChar = rand.Next(3);
                        } while (oppChar == selectedChar);
                        Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].wait) as ImageSource;
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].wait) as ImageSource;
                        rope[0].Visibility = System.Windows.Visibility.Visible;
                        Me.Visibility = System.Windows.Visibility.Visible;
                        Opponent.Visibility = System.Windows.Visibility.Visible;
                        SungJik.Visibility = System.Windows.Visibility.Hidden;
                        Pig.Visibility = System.Windows.Visibility.Hidden;
                        Mice.Visibility = System.Windows.Visibility.Hidden;
                        Hand.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
            }
        }

        private void countingDown()
        {
            GuideText.Visibility = System.Windows.Visibility.Hidden;
            ready.Visibility = System.Windows.Visibility.Visible;

            DoubleAnimation animation = new DoubleAnimation();
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(baseDirectory + "ready.gif");
            ImageBehavior.SetRepeatBehavior(ready, new RepeatBehavior(3));
            image.EndInit();
            ImageBehavior.SetAnimatedSource(ready, image);
            //GuideText.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "ready.gif") as ImageSource;
            animation.From = 0.0;
            animation.To = 0.0;
            animation.AccelerationRatio = 0.0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(2));
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += animation_Completed;
            mainCanvas.BeginAnimation(Canvas.TopProperty, animation);

        }

        //발좌표 추적
        private void StartGame(Skeleton me, AllFramesReadyEventArgs e)
        {
            Random random = new Random();
            if (me == null || e == null) return;
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null) return;
                if (frameNum < rope.Length / 2)
                {
                    Canvas.SetZIndex(Opponent, 2);
                    Canvas.SetZIndex(Me, 1);
                    Canvas.SetZIndex(rope[frameNum], 0);
                }
                else
                {
                    Canvas.SetZIndex(Opponent, 1);
                    Canvas.SetZIndex(rope[frameNum], 2);
                    Canvas.SetZIndex(Me, 0);
                }
                if (me == null || depth == null || sensor == null)
                {
                    if (start == false)
                        return;
                }

                if (begin == true && frameNum < 8 && frameNum > 0 && start == true && jump == true)
                {
                    Canvas.SetTop(Me, 400);
                    Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].jump) as ImageSource;
                    if (oppoJump == true)
                    {
                        Canvas.SetTop(Opponent, 400);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;
                    }
                    else
                    {
                        Canvas.SetTop(Opponent, 410);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
                        stop = true;

                    }

                }
                else
                    if (begin == true && frameNum < 8 && frameNum > 0 && start == true && jump == false)
                    {
                        Canvas.SetTop(Me, 410);
                        Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].miss) as ImageSource;
                        if (oppoJump == true)
                        {
                            Canvas.SetTop(Opponent, 400);
                            Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;
                        }
                        else
                        {
                            Canvas.SetTop(Opponent, 410);
                            Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
                        }
                        stop = true;

                    }
                    else
                    {

                        Canvas.SetTop(Me, 410);
                        Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].wait) as ImageSource;
                        Canvas.SetTop(Opponent, 410);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].wait) as ImageSource;
                    }


                if (frameNum == 1)
                {
                    if (scoring == true && stop == false && time % 2 == 0)
                    {
                        cheers[random.Next(7)].Play();
                    }
                    scoring = false;
                }



            }
        }

        //줄넘기 
        private void skipping()
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                ThreadStart threadStart = new ThreadStart(skipping);
                Thread newThread = new Thread(threadStart);
                newThread.Start();
                return;
            }
            //while (true)
            //{               
            while (stop == false)
            {
                for (int i = 0; i < FRAME_MAX; i++)
                {
                    skippingRopeConvert(i);
                    Thread.Sleep(80);
                    if (stop == true)
                        break;
                }
            }

        }
        int originalRFoot = 0;
        int originalLFoot = 0;
        int originalHead = 0;
        private void isJumped2(AllFramesReadyEventArgs e, Skeleton me)
        {
            if (e == null || me == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null) return;
                if (scoring == true) return;
                if (frameNum < FRAME_MAX - 4 && frameNum > 0) return;
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint footRDepth = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootRight].Position, depth.Format);
                ColorImagePoint footR = coorMap.MapDepthPointToColorPoint(depth.Format, footRDepth, ColorImageFormat.RawBayerResolution1280x960Fps12);
                DepthImagePoint footLDepth = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootLeft].Position, depth.Format);
                ColorImagePoint footL = coorMap.MapDepthPointToColorPoint(depth.Format, footLDepth, ColorImageFormat.RawBayerResolution1280x960Fps12);
                DepthImagePoint headDepth = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.Head].Position, depth.Format);
                ColorImagePoint head = coorMap.MapDepthPointToColorPoint(depth.Format, headDepth, ColorImageFormat.RawBayerResolution1280x960Fps12);
                if (frameNum == FRAME_MAX - 4)
                {
                    originalRFoot = footR.Y;
                    originalLFoot = footL.Y;
                    originalHead = head.Y;
                }
                else if (frameNum == 0 && begin == true)
                {
                    if ((Math.Abs(footR.Y - originalRFoot) >= 10 || Math.Abs(footL.Y - originalLFoot) >= 10) && (Math.Abs(head.Y - originalHead) >= 10))
                    {
                        time++;
                        jump = true;
                        scoring = true;
                        sound.Play();

                    }
                    else
                        jump = false;
                }
                else return;

                if (time <= 5)
                {
                    oppoJump = true;
                }
                else
                {
                    oppoJump = false;

                    Random rand = new Random();
                    if (rand.Next(10) <= 9 - (int)(time / 3))
                    {
                        oppoJump = true;
                    }
                    else
                    {
                        oppoJump = false;
                        Canvas.SetTop(Opponent, 410);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
                    }
                }
            }
        }

        private void skippingRopeConvert(int frameNum)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new VoidIntDelegate(skippingRopeConvert), new object[] { frameNum });
                return;
            }
            if (frameNum == 0)
            {
                rope[FRAME_MAX - 1].Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                rope[frameNum - 1].Visibility = System.Windows.Visibility.Hidden;
            }
            rope[frameNum].Visibility = System.Windows.Visibility.Visible;
            this.frameNum = frameNum;
            if (begin == false && frameNum == FRAME_MAX - 2)
            {
                begin = true;
            }
        }

        private void result()
        {
            Canvas.SetTop(Me, 410);
            Canvas.SetTop(Opponent, 410);

            DoubleAnimation animation = new DoubleAnimation();


            if (jump == true && oppoJump == false)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(baseDirectory + "YOU-WIN.gif");
                ImageBehavior.SetRepeatBehavior(WinLose, new RepeatBehavior(3));
                image.EndInit();
                ImageBehavior.SetAnimatedSource(WinLose, image);
                Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].jump) as ImageSource;
                Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;

            }
            else
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(baseDirectory + "YOU-LOSE.gif");
                ImageBehavior.SetRepeatBehavior(WinLose, new RepeatBehavior(3));
                image.EndInit();
                ImageBehavior.SetAnimatedSource(WinLose, image);
                Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].miss) as ImageSource;
                Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;
            }
            animation.From = 0.0;
            animation.To = 0.0;
            animation.AccelerationRatio = 0.0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(4));
            animation.FillBehavior = FillBehavior.Stop;
            titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
        }

        private void replayHome(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (e == null || me == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null)
                    return;
                //손목위치 찾기
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);

                DepthImagePoint HandLeftDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                ColorImagePoint HandLeftColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandLeftDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                //손위치 (내 양손에 연결한다)
                if ((PressingHand == RIGHT && Pressing.isPressed() == true) || HandRightDepthImagePoint.Depth <= HandLeftDepthImagePoint.Depth)
                {
                    if (PressingHand == LEFT && Pressing.isPressed() == true) { }
                    else
                    {
                        Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                        Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);
                        handdepth = HandRightDepthImagePoint.Depth;
                        PressingHand = RIGHT;
                    }
                }
                if ((PressingHand == LEFT && Pressing.isPressed() == true) || (HandRightDepthImagePoint.Depth > HandLeftDepthImagePoint.Depth))
                {
                    Canvas.SetLeft(Hand, HandLeftColorImagePoint.X - Hand.Width / 2);
                    Canvas.SetTop(Hand, HandLeftColorImagePoint.Y - Hand.Height / 2);
                    PressingHand = LEFT;
                }
                if (gameState == END)
                {
                    if ((
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Replay) - Pressing.wideRange()) &&
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Replay) + Replay.Width + Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Replay) - Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Replay) + Replay.Height + Pressing.wideRange())))
                    {
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_jump_on.png") as ImageSource;
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_jump.png") as ImageSource;
                        lock (thisLock)
                        {
                            Pressing.detectPressure(HandRightDepthImagePoint.Depth, ref Hand);

                            if (Pressing.isConfirmed() == true)
                            {
                                gameState = SELECTING;
                                Pressing.reset(ref Hand);
                                stop = false;
                                restartGame();
                            }
                        }

                    }
                    else if ((
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Home) - Pressing.wideRange()) &&
                        (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Home) + Home.Width + Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Home) - Pressing.wideRange()) &&
                        (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Home) + Home.Height + Pressing.wideRange())))
                    {
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_jump.png") as ImageSource;
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_jump_on.png") as ImageSource;
                        Pressing.detectPressure(HandRightDepthImagePoint.Depth, ref Hand);

                        if (Pressing.isConfirmed() == true)
                        {
                            Pressing.reset(ref Hand);
                            stop = false;
                            goHome();
                        }

                    }
                    else
                    {
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_jump.png") as ImageSource;
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_jump.png") as ImageSource;
                        Pressing.reset(ref Hand);
                    }
                }
            }
        }





        private void restartGame()
        {
            selected = false;

            begin = false;
            start = false;
            jump = false;
            oppoJump = false;
            scoring = false;
            stopPause = true;
            initial = false;

            gameover.Stop();
            win.Stop();
            backGround.Play();
            time = 0;

            Replay.Visibility = System.Windows.Visibility.Hidden;
            Home.Visibility = System.Windows.Visibility.Hidden;
            Me.Visibility = System.Windows.Visibility.Hidden;
            Opponent.Visibility = System.Windows.Visibility.Hidden;
            rope[1].Visibility = System.Windows.Visibility.Hidden;
            rope[2].Visibility = System.Windows.Visibility.Hidden;
            WinLose.Visibility = System.Windows.Visibility.Hidden;

            SungJik.Visibility = System.Windows.Visibility.Visible;
            Pig.Visibility = System.Windows.Visibility.Visible;
            Mice.Visibility = System.Windows.Visibility.Visible;
            counter = 5;

        }


        private void goHome()
        {
            MainWindow main = new MainWindow();
            App.Current.MainWindow = main;
            gameover.Stop();
            main.Show();
            return;
        }

    }
}