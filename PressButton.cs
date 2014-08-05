using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//2014/07/25 Press Button class 버튼 누르는것을 detect 한다
namespace SungJik_SungHwa
{
    class PressButton
    {
        private const int PRESS_CONFIRM = 12;
        private int Press = 0;
        private int originalDepth = 0;

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
        /// 초기화 함수
        /// </summary>
        public void reset()
        {
            Press = 0;
            originalDepth = 0;
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
    }
}
