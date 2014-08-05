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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Windows.Threading;

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
        const int CONFIRM = 20;
        const int FRAME_MAX = 24;

        //14/07/23 : 프로그램 제어를 위함 
        const int SELECTING = 0;
        const int PLAYING = 1;
        const int END = 2;
        //14/07/24 : 스킵버튼을 위한 제어
        const int BEGINNING = 4;

        int frameNum = 0;

        int originalDepth = 0;
        int Press = 0;
        int originalFoot = 0;

        //14/07/24 : 초기 상태는 시작
        int gameState = BEGINNING;

        int time = 0;
        int selectedChar = 0;
        int oppChar = 0;

        private Object thisLock = new Object();
        private Semaphore pool = new Semaphore(0, 1);
        Boolean selected = false;

        Boolean begin = false;
        Boolean start = false;
        Boolean jump = false;
        Boolean oppoJump = false;
        Boolean scoring = false;
        Boolean stop = false;
        Boolean stopPause = true;
        Boolean initial = false;
        Boolean locker = false;
        Skeleton[] allSkeletons = new Skeleton[SKELOTON_COUNT];

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "/줄넘기/";

        Canvas titleCanvas = new Canvas();
        Image titleImage = new Image();
        //14/07/24 : 스킵버튼을 만듬
        TextBlock introText = new TextBlock();
        Button skipButton = new Button();

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
        System.Windows.Media.MediaPlayer sound = new MediaPlayer();
        //System.Windows.Media.MediaPlayer backGround = new MediaPlayer();

        System.Media.SoundPlayer gameover = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/줄넘기/" + "gameOver.wav");
        System.Media.SoundPlayer backGround = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/줄넘기/" + "background.wav");
        System.Media.SoundPlayer win = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/줄넘기/" + "win.wav");


        Character[] characters = new Character[3]{
            new Character("sung_wait.png", "sung_jump.png", "sung_miss.png"), 
            new Character("pig_wait.png", "pig_jump.png", "pig_miss.png"), 
            new Character("mou_wait.png", "mou_jump.png", "mou_miss.png") };

        string[] addresses = new string[FRAME_MAX];

        //메w인 화면
        public SkippingRoper()
        {
            sound.Open(new Uri(baseDirectory + "jump.wav"));
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
            Screen.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "bg.png") as ImageSource;
            //키넥트가 연결되어 있는지 확인한다. 만일 연결되어 있으면 선언한 sensor와 연결된 kinect의 정보를 준다
            if (KinectSensor.KinectSensors.Count > 0)
                sensor = KinectSensor.KinectSensors[0];
            //14/07/24 : 우선 키넥트를 먼저 준비시킨후 실행한다
            prepareKinect();
        }

        private void prepareKinect()
        {
            if (sensor == null)
            {
                CountDown.Text = "\n키넥트를 연결해주세요!";
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
                introduction();
            }
        }

        private void InitUI()
        {
            Canvas.SetZIndex(Me, 1);
            Canvas.SetZIndex(SkippingRope, 0);

            for (int i = 0; i < FRAME_MAX; i++)
            {
                addresses[i] = "rope/rope_" + (i + 1).ToString() + ".png";
            }
            SkippingRope.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + addresses[0]) as ImageSource;
            SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
            Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
            Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;
            Hand.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "hand.png") as ImageSource;

            titleCanvas.Width = 1300;
            titleCanvas.Height = 1000;

            titleImage.Width = 1280;
            titleImage.Height = 960;

            //14/07/24 : skip 버튼의 크기 및 정하기
            skipButton.Height = 100;
            skipButton.Width = 200;
            skipButton.FontSize = 24;
            skipButton.Content = "TOUCH ME\nTO SKIP";
            skipButton.Visibility = System.Windows.Visibility.Hidden;
            Canvas.SetTop(skipButton, 500);
            Canvas.SetLeft(skipButton, 800);


            introText.Width = titleCanvas.Width / 2;
            introText.Height = titleCanvas.Height / 3;
            introText.TextAlignment = TextAlignment.Center;
            introText.FontSize = 35;
            Canvas.SetTop(introText, (titleCanvas.Height / 3) - 100);
            Canvas.SetLeft(introText, (titleCanvas.Width / 4));

            introText.Visibility = System.Windows.Visibility.Hidden;

            titleImage.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "title.png") as ImageSource;
            titleCanvas.Children.Add(titleImage);
            titleCanvas.Children.Add(introText);
            titleCanvas.Children.Add(skipButton);

            mainCanvas.Children.Add(titleCanvas);

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
            introText.Visibility = System.Windows.Visibility.Visible;
            introText.Text = "안녕하세요 \"다함께 줄넘기\" 게임입니다\n 다같이 뛸 준비 됬나요??";

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0.0;
            animation.To = 0.0;
            animation.AccelerationRatio = 0.0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(5));
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
                    Hand.Visibility = System.Windows.Visibility.Visible;
                    guide();

                    break;
                case 3:
                    counter++;
                    introText.Text = "우선 하고픈 캐릭터를 선택합니다.\n선택방법은 오른손으로\n 하고자 하는 캐릭터에 올리신 뒤\n천천히 손을 앞으로 미시면 선택이 됩니다";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(8));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 4:
                    counter++;
                    introText.Text = "그뒤 상대편이 임의적으로 정해집니다.\n 그뒤 줄이 넘어갈때 뛰어주세요! \n 줄에 먼저 걸리는 쪽이 집니다ㅎㅎ";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(8));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 5:
                    counter++;
                    introText.Text = "그럼 시작해 볼까요??";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(2));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    titleCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;

                case 6:
                    counter++;
                    titleCanvas.Visibility = System.Windows.Visibility.Hidden;
                    gameState = SELECTING;
                    break;
                case 7:
                    counter++;
                    CountDown.Text = "4";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 8:
                    counter++;
                    CountDown.Text = "3";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 9:
                    counter++;
                    CountDown.Text = "2";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 10:
                    counter++;
                    CountDown.Text = "1";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(1));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 11:
                    locker = false;
                    Press = 0;
                    originalDepth = 0;

                    counter++;
                    CountDown.Text = "START!";
                    animation.From = 0.0;
                    animation.To = 0.0;
                    animation.AccelerationRatio = 0.0;
                    animation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                    animation.FillBehavior = FillBehavior.Stop;
                    animation.Completed += animation_Completed;
                    mainCanvas.BeginAnimation(Canvas.TopProperty, animation);
                    break;
                case 12:
                    counter = 7;
                    start = true;
                    stop = false;
                    CountDown.Text = "";
                    skipping();
                    break;
                default:
                    break;
            }
        }

        //14/07/24 : skip을 손으로 터치시에 바로 게임을 실행한다

        private void skipIntro(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (me == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                {
                    return;
                }

                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                //손위치 (내 오른쪽 손에 연결한다)
                Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);

                if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(skipButton)) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(skipButton) + skipButton.Width) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(skipButton)) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(skipButton) + skipButton.Height))
                {
                    counter = 6;
                }
            }
        }

        //준비가 됬을때의 이벤트
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            Skeleton me = null;

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
                    CountDown.Visibility = System.Windows.Visibility.Visible;
                    CountDown.Text = "키넥트와 멀리에 서 있어 주세요~";
                    return;
                }

                if (selected == false)
                {
                    //14/07/24 : count down이 보이도록
                    CountDown.Visibility = System.Windows.Visibility.Visible;
                    SungJik.Visibility = System.Windows.Visibility.Visible;
                    Pig.Visibility = System.Windows.Visibility.Visible;
                    Mice.Visibility = System.Windows.Visibility.Visible;
                    Hand.Visibility = System.Windows.Visibility.Visible;
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
                }
            }

            if (gameState == END)
            {
                if (stop == true)
                {

                    result();
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
            if (me == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                CountDown.Text = "\n캐릭터를 선택해 주세요\n(오른손을 이용해주세요)";
                //손목위치 찾기
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                //손위치 (내 오른쪽 손에 연결한다)
                Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);

                if (gameState == SELECTING)
                {
                    //위치 지정시
                    if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(SungJik) + 90) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(SungJik) + 290) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(SungJik) + 80) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(SungJik) + SungJik.Height))
                    {
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].jump) as ImageSource;
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
                        if (originalDepth > HandRightDepthImagePoint.Depth)
                        {
                            //14/07/23 : 1번 실행되도록 제어 
                            Press++;
                            if (Press == CONFIRM)
                            {
                                gameState = PLAYING;
                                Press = 0;

                                selected = true;
                                selectedChar = SUNGJIK;
                                countingDown();
                            }
                        }
                    }

                    else if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Pig) + 90) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Pig) + 290) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Pig) + 80) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Pig) + Pig.Height))
                    {
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].jump) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait) as ImageSource;
                        if (originalDepth > HandRightDepthImagePoint.Depth)
                        {
                            Press++;

                            if (Press == CONFIRM)
                            {
                                gameState = PLAYING;

                                Press = 0;
                                selected = true;
                                selectedChar = PIG;
                                countingDown();
                            }
                        }
                    }
                    else if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Mice) + 90) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Mice) + 290) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Mice) + 80) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Mice) + Mice.Height))
                    {
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].jump) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
                        if (originalDepth > HandRightDepthImagePoint.Depth)
                        {
                            Press++;

                            if (Press == CONFIRM)
                            {
                                gameState = PLAYING;
                                Press = 0;
                                selected = true;
                                selectedChar = MICE;
                                countingDown();
                            }
                        }
                    }
                    else
                    {
                        Mice.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[MICE].wait
                            ) as ImageSource;
                        SungJik.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[SUNGJIK].wait) as ImageSource;
                        Pig.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[PIG].wait) as ImageSource;
                        Press = 0;
                    }
                    originalDepth = HandRightDepthImagePoint.Depth;
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
                        SkippingRope.Visibility = System.Windows.Visibility.Visible;
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
            DoubleAnimation animation = new DoubleAnimation();
            CountDown.Text = "5";
            animation.From = 0.0;
            animation.To = 0.0;
            animation.AccelerationRatio = 0.0;
            animation.Duration = new Duration(TimeSpan.FromSeconds(1));
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += animation_Completed;
            mainCanvas.BeginAnimation(Canvas.TopProperty, animation);

        }

        //발좌표 추적
        private void StartGame(Skeleton me, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (frameNum < addresses.Length / 2)
                {
                    Canvas.SetZIndex(Opponent, 2);
                    Canvas.SetZIndex(Me, 1);
                    Canvas.SetZIndex(SkippingRope, 0);
                }
                else
                {
                    Canvas.SetZIndex(Opponent, 1);
                    Canvas.SetZIndex(SkippingRope, 2);
                    Canvas.SetZIndex(Me, 0);
                }
                if (me == null || depth == null || sensor == null)
                {
                    if (start == false)
                        CountDown.Text = "키넥트와 멀리에 서 있어 주세요~";
                    return;
                }

                if (begin == true && frameNum < 8 && frameNum > 0 && start == true && jump == true)
                {
                    Canvas.SetTop(Me, 410);
                    Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].jump) as ImageSource;
                    if (oppoJump == true)
                    {
                        Canvas.SetTop(Opponent, 410);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;
                    }
                    else
                    {
                        Canvas.SetTop(Opponent, 458);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
                        stop = true;

                    }

                }
                else
                    if (begin == true && frameNum < 8 && frameNum > 0 && start == true && jump == false)
                    {
                        Canvas.SetTop(Me, 458);
                        Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].miss) as ImageSource;
                        if (oppoJump == true)
                        {
                            Canvas.SetTop(Opponent, 410);
                            Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;
                        }
                        else
                        {
                            Canvas.SetTop(Opponent, 458);
                            Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
                        }
                        stop = true;

                    }
                    else
                    {

                        Canvas.SetTop(Me, 458);
                        Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].wait) as ImageSource;
                        Canvas.SetTop(Opponent, 458);
                        Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].wait) as ImageSource;
                    }


                if (frameNum == 1)
                {
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
                    Thread.Sleep(100);
                    if (stop == true)
                        break;
                }
            }

        }

        private void isJumped2(AllFramesReadyEventArgs e, Skeleton me)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                {
                    return;
                }
                if (scoring == true) return;
                if (frameNum < FRAME_MAX - 3 && frameNum > 0) return;
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint footRDepth = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootRight].Position, depth.Format);
                ColorImagePoint footR = coorMap.MapDepthPointToColorPoint(depth.Format, footRDepth, ColorImageFormat.RawBayerResolution1280x960Fps12);
                DepthImagePoint footLDepth = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootRight].Position, depth.Format);
                ColorImagePoint footL = coorMap.MapDepthPointToColorPoint(depth.Format, footLDepth, ColorImageFormat.RawBayerResolution1280x960Fps12);
                int foot = (footR.Y + footL.Y) / 2;
                jumping.Text = originalFoot.ToString() + " " + foot.ToString();

                if (frameNum == FRAME_MAX - 3)
                {
                    originalFoot = foot;
                }
                else if (frameNum == 0 && begin == true)
                {
                    if (Math.Abs(foot - originalFoot) > 10)
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

                if (time <= 10)
                {
                    oppoJump = true;
                }
                else
                {
                    Random rand = new Random();
                    if (rand.Next(10) <= 9 - (int)(time / 5))
                    {
                        oppoJump = true;
                    }
                    else
                    {
                        oppoJump = false;
                        Canvas.SetTop(Opponent, 458);
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

            SkippingRope.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + addresses[frameNum]) as ImageSource;
            this.frameNum = frameNum;
            if (begin == false && frameNum == FRAME_MAX - 2)
            {
                begin = true;
            }
        }

        private void result()
        {
            Canvas.SetTop(Me, 458);
            Canvas.SetTop(Opponent, 458);
            if (jump == false && oppoJump == false)
            {
                CountDown.Text = "비겼네요!";
                Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].miss) as ImageSource;
                Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;
            }
            else if (jump == true && oppoJump == false)
            {
                CountDown.Text = "이겼어요! ^^";
                Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].jump) as ImageSource;
                Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].miss) as ImageSource;

            }
            else
            {
                CountDown.Text = "졌어요 ㅠ";
                Me.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[selectedChar].miss) as ImageSource;
                Opponent.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + characters[oppChar].jump) as ImageSource;

            }
            CountDown.Text += "\n다시할래요??";
        }

        private void replayHome(Skeleton me, AllFramesReadyEventArgs e)
        {
            if (e == null || me == null)
            {
                return;
            }
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                jumping.Text = Press.ToString();
                if (depth == null)
                    return;
                //손목위치 찾기
                CoordinateMapper coorMap = new CoordinateMapper(sensor);
                DepthImagePoint HandRightDepthImagePoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                ColorImagePoint HandRightColorImagePoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthImagePoint, ColorImageFormat.RawBayerResolution1280x960Fps12);
                //손위치 (내 오른쪽 손에 연결한다)
                Canvas.SetLeft(Hand, HandRightColorImagePoint.X - Hand.Width / 2);
                Canvas.SetTop(Hand, HandRightColorImagePoint.Y - Hand.Height / 2);
                if (gameState == END)
                {
                    if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Replay)) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Replay) + Replay.Width) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Replay)) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Replay) + Replay.Height))
                    {
                        Replay.Background = Brushes.Yellow;
                        Home.Background = Brushes.Silver;
                        if (originalDepth > HandRightDepthImagePoint.Depth)
                        {
                            lock (thisLock)
                            {
                                Press++;
                                if (Press == CONFIRM && locker == false)
                                {
                                    gameState = SELECTING;
                                    Press = 0;
                                    locker = true;
                                    stop = false;
                                    restartGame();
                                }
                            }
                        }
                    }
                    else if ((Canvas.GetLeft(Hand) + Hand.Width / 2 > Canvas.GetLeft(Home)) && (Canvas.GetLeft(Hand) + Hand.Width / 2 < Canvas.GetLeft(Home) + Home.Width) && (Canvas.GetTop(Hand) + Hand.Height / 2 > Canvas.GetTop(Home)) && (Canvas.GetTop(Hand) + Hand.Height / 2 < Canvas.GetTop(Home) + Home.Height))
                    {
                        Home.Background = Brushes.Yellow;
                        Replay.Background = Brushes.Silver;
                        if (originalDepth > HandRightDepthImagePoint.Depth)
                        {
                            Press++;
                            if (Press == CONFIRM)
                            {
                                goHome();
                            }
                        }
                    }
                    else
                    {
                        Replay.Background = Brushes.Silver;
                        Home.Background = Brushes.Silver;
                        Press = 0;
                    }
                }
                originalDepth = HandRightDepthImagePoint.Depth;
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

            Replay.Visibility = System.Windows.Visibility.Hidden;
            Home.Visibility = System.Windows.Visibility.Hidden;
            Me.Visibility = System.Windows.Visibility.Hidden;
            Opponent.Visibility = System.Windows.Visibility.Hidden;
            SkippingRope.Visibility = System.Windows.Visibility.Hidden;

            SungJik.Visibility = System.Windows.Visibility.Visible;
            Pig.Visibility = System.Windows.Visibility.Visible;
            Mice.Visibility = System.Windows.Visibility.Visible;
            counter = 7;

        }


        private void goHome()
        {
            MainWindow main = new MainWindow();
            App.Current.MainWindow = main;
            gameover.Stop();
            this.Close();
            main.Show();
            return;
        }

    }

}
