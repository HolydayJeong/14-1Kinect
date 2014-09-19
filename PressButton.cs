﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
//2014/07/25 Press Button class 버튼 누르는것을 detect 한다
namespace SungJik_SungHwa
{
    class PressButton
    {
        private string hand = null;
        private string pressHand = null;
        private const int PRESS_CONFIRM = 10;
        private const int PULL_CONFIRM = 10;
        private int Press = 0;
        private int Pull = 0;
        private int originalDepth = 0;
        private Boolean nearest = false;

        public PressButton()
        {

        }

        public PressButton(string hand, string handP)
        {
            this.hand = hand;
            pressHand = handP;
        }

        /// <summary>
        /// 버튼의 누름 경도를 알려준다
        /// </summary>
        /// <param name="currentDepth"> 현재의 손등의 깊이 </param>
        public void detectPressure(int currentDepth)
        {
            if (currentDepth < originalDepth)
            {
                Press++;
            }
            else Press = 0;
            originalDepth = currentDepth;

        }

        /// <summary>
        /// 버튼의 누름 경도를 알려준다
        /// </summary>
        /// <param name="currentDepth"> 현재의 손등의 깊이 </param>
        public void detectPressure(int currentDepth, ref Image hand)
        {
            if (nearest == false && currentDepth < originalDepth)
            {
                Press++;

                if (Press == PRESS_CONFIRM)
                {
                    hand.Source = new ImageSourceConverter().ConvertFromString(pressHand) as ImageSource;

                    nearest = true;
                }
            }
            else if (nearest == true && currentDepth > originalDepth)
            {
                Pull++;
            }
                /*
            else
            {
                reset(ref hand);
                Console.WriteLine(Press + " " + Pull);
            }
                 * */
            originalDepth = currentDepth;
        }

        /// <summary>
        /// 초기화 함수
        /// </summary>
        public void reset()
        {
            Press = 0;
            Pull = 0;
            nearest = false;
            originalDepth = 0;
        }

        public void reset(ref Image hand)
        {
            Press = 0;
            Pull = 0;
            nearest = false;
            originalDepth = 0;
            hand.Source = new ImageSourceConverter().ConvertFromString(this.hand) as ImageSource;
        }

        /// <summary>
        /// 현재의 버튼의 누른상태를 알려준다
        /// </summary>
        /// <returns>눌럿으면 true를 return 아니면 false</returns>
        public Boolean isPressed()
        {
            if (Press == PRESS_CONFIRM)
            {
                return true;
            }
            else return false;
        }
        public Boolean isConfirmed()
        {
            if (Pull == PULL_CONFIRM)
            {
                return true;
            }
            else return false;
        }
        public int wideRange()
        {
            if (originalDepth == 0)
                return 0;
            else return 15;
        }

    }
}
