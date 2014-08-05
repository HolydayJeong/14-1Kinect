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
        PressButton Press = new PressButton(); // Button 클릭 

        //kinect sensor를 선언함 
        KinectSensor sensor;
        const int SKELETON_COUNT = 6;
        Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];
        
        //과일을 담는 캔버스 
        Canvas[] canvasPool1 = new Canvas[40];
        Canvas FruitScoreCanvas = new Canvas();

        int score = 0;
        
        //게임상태 설정 
        const int begin = 0 ; 
        const int playing = 1;
        const int option = 2;
        int gameState = begin;
        
        //Debug 폴더의 위치 
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory+"/과일/";

        //음악 
        System.Media.SoundPlayer bgm1 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/과일/" + "fruitbgm-wav.wav");
   //     System.Media.SoundPlayer bgm2 = new System.Media.SoundPlayer(AppDomain.CurrentDomain.BaseDirectory + "/과일/" + "Pixel Peeker Polka - faster-wav.wav");

        
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

            Replay.Visibility = Visibility.Hidden;
            Home.Visibility = Visibility.Hidden;

            Background1.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "bg.png") as ImageSource;
            Background2.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "bg_cloud.png") as ImageSource;
            SungHwa.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "sung_basket.png") as ImageSource;
            Hand.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "Hand.png") as ImageSource;
            Hand.Visibility = System.Windows.Visibility.Hidden;

        }

        void ReadyGame()
        {
            var FruitScore = new System.Windows.Controls.Image();
             FruitScore.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "score.png") as ImageSource;

            //Top, Left, 프로퍼티로 애니메이션을 주기 위해 일부러 캔버스에 담았다.
            FruitScoreCanvas.Children.Add(FruitScore);

            //원을 품고 있는 캔버스를 다시 자식으로 
            canvas1.Children.Add(FruitScoreCanvas);

            DoubleAnimation_Ready();
        }

        //애니메이션을 제어하기 위한 변수 
        int counter = 0;
       
        public void DoubleAnimation_Ready()
        {
            DoubleAnimation ReadyDoubleAnimation = new DoubleAnimation();

            ReadyDoubleAnimation.From =-900;
            ReadyDoubleAnimation.To = 0;
            ReadyDoubleAnimation.AccelerationRatio = 0;
            ReadyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(3)); //3초간 보여준다
            ReadyDoubleAnimation.FillBehavior = FillBehavior.Stop;
            ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed; //이벤트 핸들러
            
            FruitScoreCanvas.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
        }
          
        void ReadyDoubleAnimation_Completed(object sender, EventArgs e)
        {
            DoubleAnimation ReadyDoubleAnimation = new DoubleAnimation();
            switch (counter)
            {
                //과일 점수판 3초간 잠시 멈춰 서 있기 
                case 0:
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
                case 1:
                    counter++;
                    bgm1.Play();
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
                case 2:
                    counter++;
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
                    ImageBehavior.SetRepeatBehavior(Ready, new RepeatBehavior(1));
                    image.EndInit();
                    ImageBehavior.SetAnimatedSource(Ready, image);
                  
                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);    
                    break;

           
                // Ready Go 애니메이션을 숨기고 게임 시작 

                case 3:
                    counter++;
                    Ready.Visibility = Visibility.Hidden;
                    Go.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "go.png") as ImageSource;
                    Go.Visibility = Visibility.Visible;
                    ReadyDoubleAnimation.From = 0.0;
                    ReadyDoubleAnimation.To = 0.0;
                    ReadyDoubleAnimation.AccelerationRatio=0.0;
                    ReadyDoubleAnimation.Duration= new Duration(TimeSpan.FromSeconds(2));
                    ReadyDoubleAnimation.FillBehavior=FillBehavior.HoldEnd;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;

                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);
                    break;

                case 4:
                    counter++;
                    Ready.Visibility = Visibility.Hidden;
                    Go.Visibility = Visibility.Hidden;
                    Hand.Visibility = Visibility.Hidden;

                    ReadyDoubleAnimation.From = 0.0;
                    ReadyDoubleAnimation.To = 0.0;
                    ReadyDoubleAnimation.AccelerationRatio=0.0;
                    ReadyDoubleAnimation.Duration= new Duration(TimeSpan.FromSeconds(30)); //게임시간 약 30초 
                    ReadyDoubleAnimation.FillBehavior=FillBehavior.Stop;
                    ReadyDoubleAnimation.Completed += ReadyDoubleAnimation_Completed;
                    gameState = playing;
                    
                    DropFruit();        //게임 시작! 이미지 떨어뜨린다! 
                    canvas1.BeginAnimation(Canvas.TopProperty, ReadyDoubleAnimation);    
                    break;

                // 게임 끝난후 화면에 점수 표시, Home Replay 버튼을 보여준다. 
                case 5:
                    counter++;
                    bgm1.Stop();
                    Home.Visibility = Visibility.Visible;
                    Replay.Visibility = Visibility.Visible;
                    Hand.Visibility = Visibility.Visible;
                    LastScore(); //최종 점수 출력 

                   
                  
                    gameState = option; // Home replay, 선택할 수 있도록 상태 변경 
                    break;

                default:
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
      
        void LastScore()
        {
            int NScore=0;
            if (score < 0)
                NScore = Math.Abs(score);
            else
                NScore = score;

          //  Console.WriteLine(score / 1000);
            if(score<0)
                _1000.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "nc-.png") as ImageSource;
            else
            {
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
            }

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
        //게임을 startButton을 누르고 시작한다. 
        private void StartGame(Skeleton me, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                    return;
               
                // 게임 시작 
                if(gameState==playing)
                {
                    //rendering - Hipcenter  
                    CoordinateMapper coorMap = new CoordinateMapper(sensor);
                    DepthImagePoint kneeDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.KneeRight].Position, depth.Format);
                    ColorImagePoint kneeColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, kneeDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);
                    Canvas.SetLeft(SungHwa, kneeColorPoint.X - SungHwa.Width / 2);
                    // Canvas.SetTop(SungHwa, kneeColorPoint.Y - SungHwa.Height / 2);
                    Canvas.SetTop(SungHwa, 650);
                }

                if (gameState == option)
                {
                    //Right hand 좌표 잡기 
                    CoordinateMapper coorMap = new CoordinateMapper(sensor);
                    DepthImagePoint handDepthPoint = coorMap.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, depth.Format);
                    ColorImagePoint handColorPoint = coorMap.MapDepthPointToColorPoint(depth.Format, handDepthPoint, ColorImageFormat.RgbResolution1280x960Fps12);
                    Canvas.SetLeft(Hand, handColorPoint.X - Hand.Width / 2);
                    Canvas.SetTop(Hand, handColorPoint.Y - Hand.Height / 2);

                    //home button 클릭시 
                    if ((Canvas.GetLeft(Hand) + Hand.Width / 2) > Canvas.GetLeft(Home) &&
                          (Canvas.GetLeft(Hand) + Hand.Width / 2) < Canvas.GetLeft(Home) + Home.ActualWidth &&
                          (Canvas.GetTop(Hand) + Hand.Height / 2) > Canvas.GetTop(Home) &&
                          (Canvas.GetTop(Hand) + Hand.Height / 2) < Canvas.GetTop(Home) + Home.ActualHeight)
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit_on.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit.png") as ImageSource;
                        Press.detectPressure(handDepthPoint.Depth);
                        if (Press.isPressed() == true)
                        {
                            MainWindow main = new MainWindow();
                            App.Current.MainWindow = main;
                            this.Close();
                            main.Show();
                            return;
                        }
                    }

                    //Replay Button 클릭시 
                    else if ((Canvas.GetLeft(Hand) + Hand.Width / 2) > Canvas.GetLeft(Replay) &&
                          (Canvas.GetLeft(Hand) + Hand.Width / 2) < Canvas.GetLeft(Replay) + Replay.ActualWidth &&
                          (Canvas.GetTop(Hand) + Hand.Height / 2) > Canvas.GetTop(Replay) &&
                          (Canvas.GetTop(Hand) + Hand.Height / 2) < Canvas.GetTop(Replay) + Replay.ActualHeight)
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit_on.png") as ImageSource;

                        Press.detectPressure(handDepthPoint.Depth);
                        if (Press.isPressed() == true)
                        {
                            Press.reset();
                            RestartGame();
                        }
                    }

                    //아무것도 안눌렀을 때, 
                    else
                    {
                        Home.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "home_fruit.png") as ImageSource;
                        Replay.Source = new ImageSourceConverter().ConvertFromString(baseDirectory + "re_fruit.png") as ImageSource;
                        Press.reset();
                    }
                }
              
            }
        }

        void RestartGame()
        {
            Replay.Visibility = Visibility.Hidden;
            Home.Visibility = Visibility.Hidden;
            Hand.Visibility = Visibility.Hidden;
            
            _1.Visibility = Visibility.Hidden;
            _10.Visibility = Visibility.Hidden;
            _100.Visibility = Visibility.Hidden;
            _1000.Visibility = Visibility.Hidden;
            
            score = 0;
            //Ready.Visibility = Visibility.Visible;
            //Go.Visibility = Visibility.Visible;
            FruitScoreCanvas.Visibility = Visibility.Visible;
            gameState = begin;
            counter = 0;
            Score.Text = score.ToString();
            DoubleAnimation_Ready();
       }
      
        void DropFruit()
        {
            Random r1 = new Random();

            for (int i = 0; i < 40; i++)
            {
                if (i % 8 == 0) // 바나나
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "bnn_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }
                else if (i % 8 == 1) //수박 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "wm_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }
                else if (i % 8 == 2) // 복숭아
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "pe_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }

                else if (i % 8 == 3) //똥 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "dd_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }

                else if(i % 8 == 4) //메론
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "melon_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }
                
                else if (i % 8 == 5) // 썩은 바나나 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "rbnn_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }

                else if (i % 8 == 6) // 레인보우바나나 
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "rb_1.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                }

                else // 여친
                {
                    canvasPool1[i] = new Canvas();
                    InitShape(canvasPool1[i], "wsung.png", r1.Next(60, 1180), r1.Next(4, 9), r1.Next(1, 20));
                } 
            }
        }
        void InitShape (Canvas shape, String address, int XPos,int sec,int begin)
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
    
        public void DoubleAnimation_Drop(Canvas shape, string address, int Xpos, int Acceleration, int sec,int begin)
        {
           //더블 애니메이션 하나 설정
            DoubleAnimation MyDoubleAnimation = new DoubleAnimation();

            //if(SungHwaTopLeft.Y == shape.RenderTransform.Value.OffsetY )
            Random r1 = new Random();
            MyDoubleAnimation.From = 0; // 처음 시작 하는 값 from
            MyDoubleAnimation.To = 560; // 끝나는 값 to 

            Canvas.SetZIndex(Background1, 1);
            Canvas.SetZIndex(shape, 2);
            Canvas.SetZIndex(Background2,3);
            Canvas.SetZIndex(SungHwa, 4);
            Canvas.SetZIndex(Score, 4);
            Canvas.SetZIndex(Home, 4);
            Canvas.SetZIndex(Replay, 4);
            Canvas.SetZIndex(Ready, 4);
            Canvas.SetZIndex(Go, 4);
            Canvas.SetZIndex(FruitScoreCanvas, 6);
            Canvas.SetZIndex(_1, 5);
            Canvas.SetZIndex(_10, 5);
            Canvas.SetZIndex(_100, 5);
            Canvas.SetZIndex(_1000, 5);
            Canvas.SetZIndex(Hand, 5);

            Canvas.SetLeft(shape,Xpos);  // 바나나 X좌표 위치 
         
            //가속도 값 설정하기  0.0 ~1.0까지, Deceleration 속성을 지정하면 점점 느려지게 움직인다. 
            MyDoubleAnimation.AccelerationRatio = Acceleration;                                //파라미터로 받아와서 랜덤 설정 

            // Duration 설정 
            // 2인 경우, 2초 동안 적절히 분배되어, 이루어진다.     
            MyDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(sec));             //파라미터로 받아와서 랜덤 설정 

            //애니메이션 효과를 적용한후에는 속성 값 변경하기 
            //FillBehavior 애니메이션의 반복설정 stop : 한번하고 끝, forever 애니메이션 후 계속 반복
            MyDoubleAnimation.FillBehavior = FillBehavior.Stop;
            MyDoubleAnimation.BeginTime = new TimeSpan(0,0,begin);

            //Complete 내부 구현 xPos값을 가져와서 쓰기위하여 불러온다
            MyDoubleAnimation.Completed += (object sender, EventArgs e) =>
            {
                DoubleAnimation MyAnimation = new DoubleAnimation();
                //Xpos와 숭화의 좌표가 어긋나면 그대로 떨어진다
                //** 여길 수정
                if (Xpos < Canvas.GetLeft(SungHwa) || Xpos > (Canvas.GetLeft(SungHwa) + SungHwa.ActualWidth))
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
                
                //GetScore 
                if (Xpos > Canvas.GetLeft(SungHwa) && Xpos < (Canvas.GetLeft(SungHwa) + SungHwa.ActualWidth))
                {
                    //똥 -100 
                    if (address == "dd_1.png")
                    {
                        score -= 100;
                        Score.Text = score.ToString();
                    }
                        
                    //레인보우 바나나 +100
                    else if (address == "rb_1.png")
                    {
                        score += 100;
                        Score.Text = score.ToString();
                    }
                    //바나나 +50
                    else if (address == "bnn_1.png")
                    {
                        score += 50;
                        Score.Text = score.ToString();
                    }

                    //메론 +30
                    else if (address == "melon_1.png")
                    {
                        score += 30;
                        Score.Text = score.ToString();
                    }

                    //수박 +10
                    else if (address == "wn_1.png")
                    {
                        score += 10;
                        Score.Text = score.ToString();
                    }

                    //복숭아 + 5
                    else if (address == "pe_1.png")
                    {
                        score += 5 ;
                        Score.Text = score.ToString();
                    }

                    //썩은바나나 - 50
                    else if (address == "rbnn_1.png")
                    {
                        score -= 50;
                        Score.Text = score.ToString();
                    }
                   
                    //귀요미 여친  
                    else
                    {
                        score += 200;
                        Score.Text = score.ToString();
                    }
                    
                }
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
