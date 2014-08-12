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
using System.Windows.Media.Animation;
using System.Threading;
using System.Windows.Threading;
using SungJik_SungHwa;

namespace EatingFruit
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Fruit : Window
    {
        PressButton Press = new PressButton(AppDomain.CurrentDomain.BaseDirectory + "/과일/mouse.png", AppDomain.CurrentDomain.BaseDirectory + "/과일/mouse_pull.png");

        //kinect sensor를 선언함 
        KinectSensor sensor;
        const int SKELETON_COUNT = 6;
        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];

        //과일을 담는 캔버스 
        Canvas[] canvasPool1 = new Canvas[80];
        Canvas FruitScoreCanvas = new Canvas();
        Canvas GuideCanvas = new Canvas();
        Canvas MouCanvas = new Canvas();

        List<Image> Color = new List<Image>();

        int counter1 = 0;
        int SungScore = 0;
        int MouScore = 0;
        int start = 0;
        int end = 0;

        //게임상태 설정 
        const int begin = 0;
        const int playing = 1;
        const int option = 2;
        int gameState = begin;
        //Debug 폴더의 위치 
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory + "/과일/";

        //음악 
        System.Media.SoundPlayer bgm1 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/과일/" + "fruitbgm-wav.wav");
        System.Media.SoundPlayer bgm2 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/과일/" + "Pixel Peeker Polka - faster-wav.wav");
        System.Media.SoundPlayer gameover = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/과일/" + "Run Amok-wav.wav");

        DepthImagePoint UserHand;
        
        public Fruit()
        {
           InitializeComponent();
           ReadyGame();
           InitUI();
        }

        
        //Initialization
        void InitUI()
        {
            Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit.png") as ImageSource;
            Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit.png") as ImageSource;

            Background1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "bg.png") as ImageSource;
            Background2.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "bg_cloud.png") as ImageSource;
            SungHwa.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "sung_basket.png") as ImageSource;
            Mou.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "mou_basket.png") as ImageSource;
            hand.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "mouse.png") as ImageSource;
            hand.Width=45;
            hand.Height=45;


            Replay.Visibility = Visibility.Hidden;
            Home.Visibility = Visibility.Hidden;
            //  hand.Visibility = Visibility.Hidden;

        }

        void ReadyGame()
        {
            var FruitScore = new System.Windows.Controls.Image();
            FruitScore.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "score.png") as ImageSource;

            var Guide = new System.Windows.Controls.Image();
            Guide.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "guide.png") as ImageSource;

            ///  var Mou = new System.Windows.Controls.Image();

            //Top, Left, 프로퍼티로 애니메이션을 주기 위해 일부러 캔버스에 담았다.
            FruitScoreCanvas.Children.Add(FruitScore);
            GuideCanvas.Children.Add(Guide);
            //  MouCanvas.Children.Add(Mou);

            //원을 품고 있는 캔버스를 다시 자식으로 
            canvas1.Children.Add(FruitScoreCanvas);
            canvas1.Children.Add(GuideCanvas);
            //  canvas1.Children.Add(MouCanvas);

            DoubleAnimation_Ready();
        }

        //애니메이션을 제어하기 위한 변수 
        int counter = 0;

        public void DoubleAnimation_Ready()
        {
            FruitScoreCanvas.Visibility = Visibility.Hidden;
            bgm2.Play();
            DoubleAnimation ReadyDoubleAnimation = new DoubleAnimation();
            ReadyDoubleAnimation.From = 0;
            ReadyDoubleAnimation.To = 0;
            ReadyDoubleAnimation.AccelerationRatio = 0;
            ReadyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(5));
            ReadyDoubleAnimation.FillBehavior = FillBehavior.Stop;
            ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;

            GuideCanvas.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);

        }
          

       void ReadyDoubleAnimation_Completed(object sender, EventArgs e)
        {
            DoubleAnimation ReadyDoubleAnimation = new DoubleAnimation();
            DoubleAnimation MouAnimation = new DoubleAnimation();
            switch (counter)
            {
                case 0:
                    counter++;
                    GuideCanvas.Visibility = Visibility.Hidden;
                    FruitScoreCanvas.Visibility = Visibility.Visible;
                    ReadyDoubleAnimation.From =-900;
                    ReadyDoubleAnimation.To = 0;
                    ReadyDoubleAnimation.AccelerationRatio = 0;
                    ReadyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2)); //3초간 보여준다
                    ReadyDoubleAnimation.FillBehavior = FillBehavior.Stop;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed; //이벤트 핸들러
                    FruitScoreCanvas.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
                    break;
              
                //과일 점수판 3초간 잠시 멈춰 서 있기 
                case 1:
                    Ready.Visibility = Visibility.Hidden;
                    counter++;
                    ReadyDoubleAnimation.From = 0;
                    ReadyDoubleAnimation.To = 0;
                    ReadyDoubleAnimation.AccelerationRatio = 0;
                    ReadyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(3));
                    ReadyDoubleAnimation.FillBehavior = FillBehavior.Stop;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                    
                    FruitScoreCanvas.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
                    break;

            // 과일점수판 끝까지 떨어뜨림 
                case 2:
                    counter++;
                    Ready.Visibility = Visibility.Hidden;
                    ReadyDoubleAnimation.From = 0;
                    ReadyDoubleAnimation.To = 1200;
                    ReadyDoubleAnimation.AccelerationRatio = 1;
                    ReadyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));
                    ReadyDoubleAnimation.FillBehavior = FillBehavior.HoldEnd;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                  
                    FruitScoreCanvas.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
                    break;

           // Ready Go 애니메이션을 보여준다. 
                case 3:
                    counter++;
                    FruitScoreCanvas.Visibility = Visibility.Hidden;
                    var image = new BitmapImage();
                    Ready.Visibility = Visibility.Visible;
                    ReadyDoubleAnimation.From = 0.0;
                    ReadyDoubleAnimation.To = 0.0;
                    ReadyDoubleAnimation.AccelerationRatio=0.0;
                    ReadyDoubleAnimation.Duration= new Duration(TimeSpan.FromSeconds(2));
                    ReadyDoubleAnimation.FillBehavior=FillBehavior.HoldEnd;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                    
                    image.BeginInit();
                    image.UriSource = new Uri(baseDirectory + "ready.gif");
                    ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(3));
                    image.EndInit();
                    ImageBehavior.SetAnimatedSource(Ready, image);
                  
                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);    
                    break;

           
                // Ready Go 애니메이션을 숨기고 게임 시작 

                case 4:
                    counter++;
                    Ready.Visibility = Visibility.Hidden;
                    Go.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "go.png") as ImageSource;
                    Go.Visibility = Visibility.Visible;
                    ReadyDoubleAnimation.From = 0.0;
                    ReadyDoubleAnimation.To = 0.0;
                    ReadyDoubleAnimation.AccelerationRatio=0.0;
                    ReadyDoubleAnimation.Duration= new Duration(TimeSpan.FromSeconds(1));
                    ReadyDoubleAnimation.FillBehavior=FillBehavior.HoldEnd;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
                    break;

                case 5:
                    counter++;
                    Ready.Visibility = Visibility.Hidden;
                    Go.Visibility = Visibility.Hidden;
                    hand.Visibility = Visibility.Hidden;
                    SungHwa.Visibility = Visibility.Visible;
                    Mou.Visibility = Visibility.Visible;

                    ReadyDoubleAnimation.From = 0.0;
                    ReadyDoubleAnimation.To = 0.0;
                    ReadyDoubleAnimation.AccelerationRatio=0.0;
                    ReadyDoubleAnimation.Duration= new Duration(TimeSpan.FromSeconds(57)); //게임시간 약 30초 
                    ReadyDoubleAnimation.FillBehavior=FillBehavior.Stop;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                    gameState = playing;

                    //쥐 
                    MouAnimation.From = 0.0;
                    MouAnimation.To = 1050;
                    MouAnimation.AccelerationRatio = 1;
                    MouAnimation.Duration = new Duration(TimeSpan.FromSeconds(4));
                    MouAnimation.FillBehavior = FillBehavior.Stop;
                    MouAnimation.Completed += MouAnimation_Completed;
                    Mou.BeginAnimation(Canvas.LeftProperty, MouAnimation);
 
                    DropFruit();        //게임 시작! 이미지 떨어뜨린다! 
                    Score(MouScore, 1);
                    Score(SungScore, 2);

                   
                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);    
                    break;

                // 게임 끝난후 화면에 점수 표시, Home Replay 버튼을 보여준다. 
                case 6:
                    int winScore;
                    var LastSung = new BitmapImage();
                    var LastMou = new BitmapImage();
                    counter++;
                    bgm2.Stop();
                    Console.WriteLine("MouScore" + MouScore);
                    Console.WriteLine("SungScore" + SungScore);
                    
                    if (SungScore > MouScore)
                    {
                        winScore = SungScore;
                        
                        LastSung.BeginInit();
                        LastSung.UriSource = new Uri(baseDirectory + "lastsung.gif");
                        ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(7));
                        LastSung.EndInit();
                        ImageBehavior.SetAnimatedSource(SungGIF, LastSung);
                        SungGIF.Visibility = Visibility.Visible;

                        LastMou.BeginInit();
                        LastMou.UriSource = new Uri(baseDirectory + "losermou.gif");
                        ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(7));
                        LastMou.EndInit();
                        ImageBehavior.SetAnimatedSource(MouGIF, LastMou);
                        MouGIF.Visibility = Visibility.Visible;
                    }
                       
                    else
                    {
                        winScore = MouScore;

                        LastMou.BeginInit();
                        LastMou.UriSource = new Uri(baseDirectory + "lastmou.gif");
                        ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(7));
                        LastMou.EndInit();
                        ImageBehavior.SetAnimatedSource(MouGIF, LastMou);
                        MouGIF.Visibility = Visibility.Visible;

                        LastSung.BeginInit();
                        LastSung.UriSource = new Uri(baseDirectory + "losersung.gif");
                        ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(7));
                        LastSung.EndInit();
                        ImageBehavior.SetAnimatedSource(SungGIF, LastSung);
                        SungGIF.Visibility = Visibility.Visible;
                    }
                    
                
                    gameover.Play();
                    Thread.Sleep(1);
                    Home.Visibility = Visibility.Visible;
                    Replay.Visibility = Visibility.Visible;
                    hand.Visibility = Visibility.Visible;
                    SungHwa.Visibility = Visibility.Hidden;
                    Mou.Visibility = Visibility.Hidden;

                    s1.Visibility = Visibility.Hidden;
                    s10.Visibility = Visibility.Hidden;
                    s100.Visibility = Visibility.Hidden;
                    s1000.Visibility = Visibility.Hidden;
                    s10000.Visibility = Visibility.Hidden;

                    m1.Visibility = Visibility.Hidden;
                    m10.Visibility = Visibility.Hidden;
                    m100.Visibility = Visibility.Hidden;
                    m1000.Visibility = Visibility.Hidden;
                    m10000.Visibility = Visibility.Hidden;


                   
                    LastScore(winScore); //최종 점수 출력 

                    gameState = option; // Home replay, 선택할 수 있도록 상태 변경
               
                    break;

                default:
                    break;
            }
      
        }
            
        void MouAnimation_Completed(object sender, EventArgs e)
        {
            DoubleAnimation MouAnimation = new DoubleAnimation();
            Random r2 = new Random();
            switch(counter1)
            {
                case 0:
                    counter1++;
                    start=1050;
                    MouAnimation.From = start;     
                    end = r2.Next(10, 1050);
                    MouAnimation.To = end;
                    MouAnimation.AccelerationRatio = 1;
                    MouAnimation.Duration = new Duration(TimeSpan.FromSeconds(3));
                    MouAnimation.FillBehavior = FillBehavior.Stop;
                    MouAnimation.Completed += MouAnimation_Completed;
                    Mou.BeginAnimation(Canvas.LeftProperty, MouAnimation);
                    break;
                
                case 1:
                    start=end;
                    MouAnimation.From = start;

                    Console.WriteLine(start);
                    if (start+300>1050)
                        end = r2.Next(10, start - 300);
                    else
                        end = r2.Next(start + 300, 1050);
                    MouAnimation.To = end;
                    MouAnimation.AccelerationRatio = 1;
                    MouAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));
                    MouAnimation.FillBehavior = FillBehavior.Stop;
                    MouAnimation.Completed += MouAnimation_Completed;
                    Mou.BeginAnimation(Canvas.LeftProperty, MouAnimation);
                    break;
            }
      

                
        }
        //준비가 되었을 때, 이벤트 
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // 받아오는 정보를 colorFrame에 받아온다. 
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;

                //pixel의 크기 초기화 
                byte[] pixels = new byte[colorFrame.PixelDataLength];

                //pixel 의 정보 담아오기 
                colorFrame.CopyPixelDataTo(pixels);

                //stride = r,g,b, none 의 정보가 하나의 pixel에 있기에 *4를 한다.
                int stride = colorFrame.Width * 4;

                //screen image의 source를 결정해준다. 
                Screen.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32,
                                                    null, pixels, stride);
            }
            Skeleton me = null;
            GetSkelton(e, ref me);

            if (me == null) 
                return;
            StartGame(me, e); //게임을 시작.
 

        }

        //1이면 Mou 2이면 Sung 
        void Score(int Score, int check)
        {
            Image img1, img10, img100, img1000, img10000;



            int NScore = 0;
            if (Score < 0)
                NScore = Math.Abs(Score);
            else
                NScore = Score;

            //check가 1인 경우 쥐 
            if (check == 1)
            {
                img1 = m1;
                img10 = m10;
                img100 = m100;
                img1000 = m1000;
                img10000 = m10000;
            }
            else // check 2인 경우 숭화 
            {
                img1 = s1;
                img10 = s10;
                img100 = s100;
                img1000 = s1000;
                img10000 = s10000;
            }

            //  Console.WriteLine(SungScorre / 1000);
            if (Score < 0)
                img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n-.png") as ImageSource;
            else
            {
                if (NScore / 10000 == 0) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n0.png") as ImageSource;
                else if (NScore / 10000 == 1) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n1.png") as ImageSource;
                else if (NScore / 10000 == 2) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n2.png") as ImageSource;
                else if (NScore / 10000 == 3) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n3.png") as ImageSource;
                else if (NScore / 10000 == 4) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n4.png") as ImageSource;
                else if (NScore / 10000 == 5) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n5.png") as ImageSource;
                else if (NScore / 10000 == 6) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n6.png") as ImageSource;
                else if (NScore / 10000 == 7) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n7.png") as ImageSource;
                else if (NScore / 10000 == 8) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n8.png") as ImageSource;
                else if (NScore / 10000 == 9) img10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n9.png") as ImageSource;
                else return;
            }
            img10000.Visibility = Visibility.Visible;

            NScore = NScore % 10000;
            if (NScore / 1000 == 0) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n0.png") as ImageSource;
            else if (NScore / 1000 == 1) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n1.png") as ImageSource;
            else if (NScore / 1000 == 2) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n2.png") as ImageSource;
            else if (NScore / 1000 == 3) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n3.png") as ImageSource;
            else if (NScore / 1000 == 4) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n4.png") as ImageSource;
            else if (NScore / 1000 == 5) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n5.png") as ImageSource;
            else if (NScore / 1000 == 6) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n6.png") as ImageSource;
            else if (NScore / 1000 == 7) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n7.png") as ImageSource;
            else if (NScore / 1000 == 8) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n8.png") as ImageSource;
            else if (NScore / 1000 == 9) img1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n9.png") as ImageSource;
            else return;

            img1000.Visibility = Visibility.Visible;

            NScore = NScore % 1000;
            //  Console.WriteLine(NScore/100);
            if (NScore / 100 == 0) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n0.png") as ImageSource;
            else if (NScore / 100 == 1) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n1.png") as ImageSource;
            else if (NScore / 100 == 2) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n2.png") as ImageSource;
            else if (NScore / 100 == 3) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n3.png") as ImageSource;
            else if (NScore / 100 == 4) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n4.png") as ImageSource;
            else if (NScore / 100 == 5) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n5.png") as ImageSource;
            else if (NScore / 100 == 6) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n6.png") as ImageSource;
            else if (NScore / 100 == 7) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n7.png") as ImageSource;
            else if (NScore / 100 == 8) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n8.png") as ImageSource;
            else if (NScore / 100 == 9) img100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n9.png") as ImageSource;
            else return;
            img100.Visibility = Visibility.Visible;

            NScore = NScore % 100;
            //   Console.WriteLine(NScore/10);
            if (NScore / 10 == 0) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n0.png") as ImageSource;
            else if (NScore / 10 == 1) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n1.png") as ImageSource;
            else if (NScore / 10 == 2) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n2.png") as ImageSource;
            else if (NScore / 10 == 3) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n3.png") as ImageSource;
            else if (NScore / 10 == 4) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n4.png") as ImageSource;
            else if (NScore / 10 == 5) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n5.png") as ImageSource;
            else if (NScore / 10 == 6) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n6.png") as ImageSource;
            else if (NScore / 10 == 7) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n7.png") as ImageSource;
            else if (NScore / 10 == 8) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n8.png") as ImageSource;
            else if (NScore / 10 == 9) img10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n9.png") as ImageSource;
            else return;
            img10.Visibility = Visibility.Visible;

            //  Console.WriteLine(NScore%10);
            if (NScore % 10 == 0) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n0.png") as ImageSource;
            else if (NScore % 10 == 1) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n1.png") as ImageSource;
            else if (NScore % 10 == 2) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n2.png") as ImageSource;
            else if (NScore % 10 == 3) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n3.png") as ImageSource;
            else if (NScore % 10 == 4) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n4.png") as ImageSource;
            else if (NScore % 10 == 5) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n5.png") as ImageSource;
            else if (NScore % 10 == 6) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n6.png") as ImageSource;
            else if (NScore % 10 == 7) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n7.png") as ImageSource;
            else if (NScore % 10 == 8) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n8.png") as ImageSource;
            else if (NScore % 10 == 9) img1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "n9.png") as ImageSource;
            else return;
            img1.Visibility = Visibility.Visible;

        }

        void LastScore(int NScore)
        {
            if (NScore < 0)
                NScore = Math.Abs(NScore);

            //  Console.WriteLine(SungScore / 1000);
            if (NScore < 0)
                _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc-.png") as ImageSource;
            else
            {
                if (NScore / 10000 == 0) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc0.png") as ImageSource;
                else if (NScore / 10000 == 1) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc1.png") as ImageSource;
                else if (NScore / 10000 == 2) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc2.png") as ImageSource;
                else if (NScore / 10000 == 3) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc3.png") as ImageSource;
                else if (NScore / 10000 == 4) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc4.png") as ImageSource;
                else if (NScore / 10000 == 5) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc5.png") as ImageSource;
                else if (NScore / 10000 == 6) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc6.png") as ImageSource;
                else if (NScore / 10000 == 7) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc7.png") as ImageSource;
                else if (NScore / 10000 == 8) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc8.png") as ImageSource;
                else if (NScore / 10000 == 9) _10000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc9.png") as ImageSource;
                else return;
            }
            _10000.Visibility = Visibility.Visible;


            NScore = NScore % 10000;
            if (NScore / 1000 == 0) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc0.png") as ImageSource;
            else if (NScore / 1000 == 1) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc1.png") as ImageSource;
            else if (NScore / 1000 == 2) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc2.png") as ImageSource;
            else if (NScore / 1000 == 3) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc3.png") as ImageSource;
            else if (NScore / 1000 == 4) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc4.png") as ImageSource;
            else if (NScore / 1000 == 5) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc5.png") as ImageSource;
            else if (NScore / 1000 == 6) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc6.png") as ImageSource;
            else if (NScore / 1000 == 7) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc7.png") as ImageSource;
            else if (NScore / 1000 == 8) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc8.png") as ImageSource;
            else if (NScore / 1000 == 9) _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc9.png") as ImageSource;
            else return;

            _1000.Visibility = Visibility.Visible;
            NScore = NScore % 1000;
            //  Console.WriteLine(NScore/100);
            if (NScore / 100 == 0) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc0.png") as ImageSource;
            else if (NScore / 100 == 1) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc1.png") as ImageSource;
            else if (NScore / 100 == 2) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc2.png") as ImageSource;
            else if (NScore / 100 == 3) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc3.png") as ImageSource;
            else if (NScore / 100 == 4) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc4.png") as ImageSource;
            else if (NScore / 100 == 5) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc5.png") as ImageSource;
            else if (NScore / 100 == 6) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc6.png") as ImageSource;
            else if (NScore / 100 == 7) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc7.png") as ImageSource;
            else if (NScore / 100 == 8) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc8.png") as ImageSource;
            else if (NScore / 100 == 9) _100.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc9.png") as ImageSource;
            else return;
            _100.Visibility = Visibility.Visible;

            NScore = NScore % 100;
            //   Console.WriteLine(NScore/10);
            if (NScore / 10 == 0) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc0.png") as ImageSource;
            else if (NScore / 10 == 1) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc1.png") as ImageSource;
            else if (NScore / 10 == 2) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc2.png") as ImageSource;
            else if (NScore / 10 == 3) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc3.png") as ImageSource;
            else if (NScore / 10 == 4) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc4.png") as ImageSource;
            else if (NScore / 10 == 5) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc5.png") as ImageSource;
            else if (NScore / 10 == 6) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc6.png") as ImageSource;
            else if (NScore / 10 == 7) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc7.png") as ImageSource;
            else if (NScore / 10 == 8) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc8.png") as ImageSource;
            else if (NScore / 10 == 9) _10.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc9.png") as ImageSource;
            else return;
            _10.Visibility = Visibility.Visible;

            //  Console.WriteLine(NScore%10);
            if (NScore % 10 == 0) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc0.png") as ImageSource;
            else if (NScore % 10 == 1) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc1.png") as ImageSource;
            else if (NScore % 10 == 2) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc2.png") as ImageSource;
            else if (NScore % 10 == 3) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc3.png") as ImageSource;
            else if (NScore % 10 == 4) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc4.png") as ImageSource;
            else if (NScore % 10 == 5) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc5.png") as ImageSource;
            else if (NScore % 10 == 6) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc6.png") as ImageSource;
            else if (NScore % 10 == 7) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc7.png") as ImageSource;
            else if (NScore % 10 == 8) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc8.png") as ImageSource;
            else if (NScore % 10 == 9) _1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc9.png") as ImageSource;
            else return;
            _1.Visibility = Visibility.Visible;

        }

     //   int handdepth = 0;
        //게임을 startButton을 누르고 시작한다. 
        private void StartGame(Skeleton me, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                Point HomeTopLeft = new Point(Canvas.GetLeft(Home), Canvas.GetTop(Home));
                Point ReplayTopLeft = new Point(Canvas.GetLeft(Replay), Canvas.GetTop(Replay));
                if (depth == null || sensor == null)
                    return;

                // 게임 시작 
                if (gameState == playing)
                {
                    //rendering - Hipcenter  
                    CoordinateMapper coorMap = new CoordinateMapper(sensor);
                    DepthImagePoint kneeDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.KneeRight].Position, depth.Format);
                    ColorImagePoint kneeColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, kneeDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);
                    Canvas.SetLeft(SungHwa, kneeColorPoint.X - SungHwa.Width / 2);
                    // Canvas.SetTop(SungHwa, kneeColorPoint.Y - SungHwa.Height / 2);
                    Canvas.SetTop(SungHwa, 580);
                }

                if (gameState == option)
                {
                    //Right hand 좌표 잡기 
                    CoordinateMapper coorMap = new CoordinateMapper(sensor);
                    DepthImagePoint HandRightDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                    ColorImagePoint HandRightColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandRightDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);
                    DepthImagePoint HandLeftDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, depth.Format);
                    ColorImagePoint HandLeftColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, HandLeftDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);

                    if (HandRightDepthPoint.Depth <= HandLeftDepthPoint.Depth)
                    {
                        //Canvas.SetLeft(Hand, HandRightColorPoint.X - hand.Width / 2);
                        //Canvas.SetTop(Hand, HandRightColorPoint.Y - hand.Height / 2);
                        //handdepth = HandRightDepthPoint.Depth;    
                        UserHand = HandRightDepthPoint;
                    }
                    else
                    {
                        //Canvas.SetLeft(Hand, HandLeftColorPoint.X - hand.Width / 2);
                        //Canvas.SetTop(Hand, HandLeftColorPoint.Y - hand.Height / 2);
                        //handdepth = HandLeftDepthPoint.Depth;
                        UserHand = HandLeftDepthPoint;
                    }

                    Canvas.SetLeft(hand, UserHand.X * 2 - hand.Width / 2);
                    Canvas.SetTop(hand, UserHand.Y - hand.Width / 2);

                    //home button 클릭시 
                    if ((Canvas.GetLeft(hand) + hand.Width / 2) > Canvas.GetLeft(Home) &&
                          (Canvas.GetLeft(hand) + hand.Width / 2) < Canvas.GetLeft(Home) + Home.ActualWidth &&
                          (Canvas.GetTop(hand) + hand.Height / 2) > Canvas.GetTop(Home) &&
                          (Canvas.GetTop(hand) + hand.Height / 2) < Canvas.GetTop(Home) + Home.ActualHeight)
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit_on.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit.png") as ImageSource;

                        Press.detectPressure(UserHand.Depth, ref hand);
                        if (Press.isConfirmed() == true)
                        {
                            Press.reset(ref hand);
                            gameover.Stop();
                            MainWindow next = new MainWindow();
                            App.Current.MainWindow = next;
                            this.Close();
                            next.Show();
                            return;
                        }
                    }

                    //Replay Button 클릭시 
                    else if ((Canvas.GetLeft(hand) + hand.Width / 2) > Canvas.GetLeft(Replay) &&
                          (Canvas.GetLeft(hand) + hand.Width / 2) < Canvas.GetLeft(Replay) + Replay.ActualWidth &&
                          (Canvas.GetTop(hand) + hand.Height / 2) > Canvas.GetTop(Replay) &&
                          (Canvas.GetTop(hand) + hand.Height / 2) < Canvas.GetTop(Replay) + Replay.ActualHeight)
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit_on.png") as ImageSource;

                        Press.detectPressure(UserHand.Depth, ref hand);
                        if (Press.isConfirmed() == true)
                        {
                            Press.reset(ref hand);
                            gameover.Stop();
                            RestartGame();
                        }
                    }

                    //아무것도 안눌렀을 때, 
                    else
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit.png") as ImageSource;
                        Press.reset(ref hand);
                    }
                }

            }
        }

        void RestartGame()
        {
            Replay.Visibility = Visibility.Hidden;
            Home.Visibility = Visibility.Hidden;
            hand.Visibility = Visibility.Hidden;

            _1.Visibility = Visibility.Hidden;
            _10.Visibility = Visibility.Hidden;
            _100.Visibility = Visibility.Hidden;
            _1000.Visibility = Visibility.Hidden;
            _10000.Visibility = Visibility.Hidden;
            SungGIF.Visibility = Visibility.Hidden;
            MouGIF.Visibility = Visibility.Hidden;
            SungScore = 0;
            MouScore = 0;

            GuideCanvas.Visibility = Visibility.Visible;
            FruitScoreCanvas.Visibility = Visibility.Visible;
            gameState = begin;
            counter = 0;
            counter1 = 0;

            DoubleAnimation_Ready();
        }

        void DropFruit()
        {
            Random r1 = new Random();

            for (int i = 0; i < 80; i++)
            {
                if (i % 8 == 0) // 바나나
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "bnn_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }
                else if (i % 8 == 1) //수박 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "wm_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }
                else if (i % 8 == 2) // 복숭아
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "pe_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }

                else if (i % 8 == 3) //똥 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "dd_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }

                else if (i % 8 == 4) //메론
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "melon_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }

                else if (i % 8 == 5) // 썩은 바나나 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "rbnn_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }

                else if (i % 8 == 6) // 레인보우바나나 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "rb_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }

                else // 여친
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "wsung.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 50));
                }
            }
        }
        void InitShape(Canvas shape, String address, int XPos, int sec, int begin)
        {

            //Image 초기화 
            var Image = new System.Windows.Controls.Image();
            Image.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + address) as ImageSource;
            Image.Width = 70;
            Image.Height = 70;

            shape.Width = 70;
            shape.Height = 70;

            //Top, Left, 프로퍼티로 애니메이션을 주기 위해 일부러 캔버스에 담았다.
            shape.Children.Add(Image);

            //원을 품고 있는 캔버스를 다시 자식으로 
            canvas1.Children.Add(shape);

            DoubleAnimation_Drop(shape, address, XPos, 1, sec, begin);
        }

        public void DoubleAnimation_Drop(Canvas shape, string address, int Xpos, int Acceleration, int sec, int begin)
        {
            //더블 애니메이션 하나 설정
            DoubleAnimation MyDoubleAnimation = new DoubleAnimation();

            //if(SungHwaTopLeft.Y == shape.RenderTransform.Value.OffsetY )
            Random r1 = new Random();
            MyDoubleAnimation.From = 0; // 처음 시작 하는 값 from
            MyDoubleAnimation.To = 560; // 끝나는 값 to 

            Canvas.SetZIndex(Background1, 1);
            Canvas.SetZIndex(shape, 2);
            Canvas.SetZIndex(Background2, 3);
            Canvas.SetZIndex(SungHwa, 4);
            Canvas.SetZIndex(Mou, 4);
            Canvas.SetZIndex(SungGIF, 4);
            Canvas.SetZIndex(MouGIF, 4);
            Canvas.SetZIndex(Home, 4);
            Canvas.SetZIndex(Replay, 4);
            Canvas.SetZIndex(Ready, 4);
            Canvas.SetZIndex(Go, 4);
            Canvas.SetZIndex(FruitScoreCanvas, 6);
            Canvas.SetZIndex(GuideCanvas, 6);
            Canvas.SetZIndex(_1, 5);
            Canvas.SetZIndex(_10, 5);
            Canvas.SetZIndex(_100, 5);
            Canvas.SetZIndex(_1000, 5);
            Canvas.SetZIndex(_10000, 5);


            Canvas.SetZIndex(s1, 5);
            Canvas.SetZIndex(s10, 5);
            Canvas.SetZIndex(s100, 5);
            Canvas.SetZIndex(s1000, 5);
            Canvas.SetZIndex(s10000, 5);

            Canvas.SetZIndex(m1, 5);
            Canvas.SetZIndex(m10, 5);
            Canvas.SetZIndex(m100, 5);
            Canvas.SetZIndex(m1000, 5);
            Canvas.SetZIndex(m10000, 5);

            Canvas.SetZIndex(hand, 5);

            Canvas.SetLeft(shape, Xpos);  // 바나나 X좌표 위치 

            //가속도 값 설정하기  0.0 ~1.0까지, Deceleration 속성을 지정하면 점점 느려지게 움직인다. 
            MyDoubleAnimation.AccelerationRatio = Acceleration;                                //파라미터로 받아와서 랜덤 설정 

            // Duration 설정 
            // 2인 경우, 2초 동안 적절히 분배되어, 이루어진다.     
            MyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(sec));             //파라미터로 받아와서 랜덤 설정 

            //애니메이션 효과를 적용한후에는 속성 값 변경하기 
            //FillBehavior 애니메이션의 반복설정 stop : 한번하고 끝, forever 애니메이션 후 계속 반복
            MyDoubleAnimation.FillBehavior = FillBehavior.Stop;
            MyDoubleAnimation.BeginTime = new TimeSpan(0, 0, begin);

            //Complete 내부 구현 xPos값을 가져와서 쓰기위하여 불러온다
            MyDoubleAnimation.Completed += (object sender, EventArgs e) =>
            {
                DoubleAnimation MyAnimation = new DoubleAnimation();
                //Xpos와 숭화의 좌표가 어긋나면 그대로 떨어진다
                //** 여길 수정
                if ((Xpos < Canvas.GetLeft(SungHwa) - 15 || Xpos > (Canvas.GetLeft(SungHwa) + SungHwa.ActualWidth) + 15) &&
                    (Xpos < Canvas.GetLeft(Mou) - 15 || Xpos > (Canvas.GetLeft(Mou) + Mou.ActualWidth) + 15))
                {
                    //Console.WriteLine(Canvas.GetLeft(SungHwa) + " " + (Canvas.GetLeft(SungHwa) + 100) + " " + Xpos);
                    MyAnimation.From = 560; // 처음 시작 하는 값 from
                    MyAnimation.To = this.Height; // 끝나는 값 to 
                    Canvas.SetLeft(shape, Xpos);  // 바나나 X좌표 위치 

                    //가속도 값 설정하기  0.0 ~1.0까지, Deceleration 속성을 지정하면 점점 느려지게 움직인다. 
                    MyAnimation.AccelerationRatio = 0;                                //파라미터로 받아와서 랜덤 설정 

                    // Duration 설정 
                    // 2인 경우, 2초 동안 적절히 분배되어, 이루어진다.     
                    MyAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.3));             //파라미터로 받아와서 랜덤 설정 
                    //애니메이션 효과를 적용한후에는 속성 값 변경하기 
                    //FillBehavior 애니메이션의 반복설정 stop : 한번하고 끝, forever 애니메이션 후 계속 반복
                    MyAnimation.FillBehavior = FillBehavior.Stop;
                    shape.BeginAnimation(Canvas.TopProperty, MyAnimation);
                }

                //SungHwa와 Mou가 동시에같은 좌표에 있으면 Mou(computer)가 먹은걸로 처리한다. 
                if ((Xpos > Canvas.GetLeft(Mou) - 15 && Xpos < (Canvas.GetLeft(Mou) + Mou.ActualWidth + 15)) &&
                     (Xpos > Canvas.GetLeft(SungHwa) - 15 && Xpos < (Canvas.GetLeft(SungHwa) + SungHwa.ActualWidth + 15)))
                {

                    //똥 -100 
                    if (address == "dd_1.png")
                    {
                        MouScore -= 100;
                        Score(MouScore, 1);
                    }

                    //레인보우 바나나 +100
                    else if (address == "rb_1.png")
                    {
                        MouScore += 100;
                        Score(MouScore, 1);
                    }
                    //바나나 +50
                    else if (address == "bnn_1.png")
                    {
                        MouScore += 50;
                        Score(MouScore, 1);
                    }

                    //메론 +30
                    else if (address == "melon_1.png")
                    {
                        MouScore += 30;
                        Score(MouScore, 1);
                    }

                    //수박 +10
                    else if (address == "wm_1.png")
                    {
                        MouScore += 10;
                        Score(MouScore, 1);
                    }

                    //복숭아 + 5
                    else if (address == "pe_1.png")
                    {
                        MouScore += 5;
                        Score(MouScore, 1);
                    }

                    //썩은바나나 - 50
                    else if (address == "rbnn_1.png")
                    {
                        MouScore -= 50;
                        Score(MouScore, 1);
                    }

                    //귀요미 여친  
                    else if (address == "wsung.png")
                    {
                        MouScore += 200;
                        Score(MouScore, 1);
                    }
                    else
                        return;

                }

                //sung만 먹었을때, 
                else if (Xpos > Canvas.GetLeft(SungHwa) - 15 && Xpos < (Canvas.GetLeft(SungHwa) + SungHwa.ActualWidth + 15))
                {

                    //똥 -100 
                    if (address == "dd_1.png")
                    {
                        SungScore -= 100;
                        Score(SungScore, 2);
                    }

                    //레인보우 바나나 +100
                    else if (address == "rb_1.png")
                    {
                        SungScore += 100;
                        Score(SungScore, 2);
                    }
                    //바나나 +50
                    else if (address == "bnn_1.png")
                    {
                        SungScore += 50;
                        Score(SungScore, 2);
                    }

                    //메론 +30
                    else if (address == "melon_1.png")
                    {
                        SungScore += 30;
                        Score(SungScore, 2);
                    }

                    //수박 +10
                    else if (address == "wm_1.png")
                    {
                        SungScore += 10;
                        Score(SungScore, 2);
                    }

                    //복숭아 + 5
                    else if (address == "pe_1.png")
                    {
                        SungScore += 5;
                        Score(SungScore, 2);
                    }

                    //썩은바나나 - 50
                    else if (address == "rbnn_1.png")
                    {
                        SungScore -= 50;
                        Score(SungScore, 2);
                    }

                    //귀요미 여친  
                    else if (address == "wsung.png")
                    {
                        SungScore += 200;
                        Score(SungScore, 2);
                    }
                    else
                        return;

                }
                else if (Xpos > Canvas.GetLeft(Mou) - 15 && Xpos < (Canvas.GetLeft(Mou) + Mou.ActualWidth + 15))
                {

                    //똥 -100 
                    if (address == "dd_1.png")
                    {
                        MouScore -= 100;
                        Score(MouScore, 1);
                    }

                    //레인보우 바나나 +100
                    else if (address == "rb_1.png")
                    {
                        MouScore += 100;
                        Score(MouScore, 1);
                    }
                    //바나나 +50
                    else if (address == "bnn_1.png")
                    {
                        MouScore += 50;
                        Score(MouScore, 1);
                    }

                    //메론 +30
                    else if (address == "melon_1.png")
                    {
                        MouScore += 30;
                        Score(MouScore, 1);
                    }

                    //수박 +10
                    else if (address == "wm_1.png")
                    {
                        MouScore += 10;
                        Score(MouScore, 1);
                    }

                    //복숭아 + 5
                    else if (address == "pe_1.png")
                    {
                        MouScore += 5;
                        Score(MouScore, 1);
                    }

                    //썩은바나나 - 50
                    else if (address == "rbnn_1.png")
                    {
                        MouScore -= 50;
                        Score(MouScore, 1);
                    }

                    //귀요미 여친  
                    else if (address == "wsung.png")
                    {
                        MouScore += 200;
                        Score(MouScore, 1);
                    }
                    else
                        return;
                }
                else
                    return;

            };
            shape.BeginAnimation(Canvas.TopProperty, MyDoubleAnimation);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //키넥트가 연결되어 있는지 확인한다. 만일 연결되어 있으면 선언한 sensor와 연결된 kinect 정보를 준다. 
            if (KinectSensor.KinectSensors.Count > 0)
                sensor = KinectSensor.KinectSensors[0];

            //연결에 성공하면.. 
            if (sensor.Status == KinectStatus.Connected)
            {
                sensor.ColorStream.Enable(); //색깔정보    
                sensor.DepthStream.Enable();//깊이정보
                sensor.SkeletonStream.Enable();// 사람 인체 인식 정보 

                //if using window kinect only!
                //Detph stream이 가까우면 할 수 있다. ( 참고로 xbox kinect는 nearmode가 없다.) 
                /*
                sensor.DepthStream.Range = DepthRange.Near;
                sensor.SkeletonStream.EnableTrackingInNearRange = true;
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                */

                //kinect 가 준비하면 이벤트를 발생시키라는 명령문 
                //sensor.AllFramesReady += sensor_AllFramesReady;
                sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

                //sensor를 시작한다. thread와 같다고 보면 된다. 
                sensor.Start();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
