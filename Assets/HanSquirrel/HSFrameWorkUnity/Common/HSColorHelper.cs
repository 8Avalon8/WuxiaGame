/// <summary>
/// 颜色辅助工具
/// 
/// author:CG
/// 汉家松鼠工作室
/// </summary>
using System;
using UnityEngine;

namespace HSFrameWork.Common
{
    public class HSColorHelper
    {
        public static Color fromString(string str){

            str = str.ToLower();
            if(str == "red") return Color.red;
            if(str == "blue") return Color.blue;
            if(str == "green") return Color.green;
            if(str == "yellow") return Color.yellow;
            if(str == "black") return Color.black;
            if(str == "white") return Color.white;
            if(str == "magenta") return Color.magenta;
            if(str == "grey") return Color.grey;
            if(str == "gray") return Color.gray;
            if(str == "cyan") return Color.cyan;
			if (str == "orange")
				return fromString("FFA400FF");

            try{
                int r = Convert.ToInt32(str[0].ToString() + str[1].ToString(), 16);
                int g = Convert.ToInt32(str[2].ToString() + str[3].ToString(), 16);
                int b = Convert.ToInt32(str[4].ToString() + str[5].ToString(), 16);
                int a = Convert.ToInt32(str[6].ToString() + str[7].ToString(), 16);
                return fromRGBA(r,g,b,a);    
            }catch{
                Debug.LogError("error color string:" + str);
                return Color.white;
            }
        }

        /// <summary>
        /// RGB (0-255)
        /// </summary>
        /// <returns>The RG.</returns>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <param name="a">The alpha component.</param>
        public static Color fromRGBA(float r, float g,float b,float a=255){
            return new Color(r/255f, g/255f,b/255f,a/255f);
        }


		// http://stackoverflow.com/questions/2288498/how-do-i-get-a-rainbow-color-gradient-in-c
		public static Color Rainbow(float progress)
		{
			progress = Mathf.Clamp01(progress);
			float r = 0.0f;
			float g = 0.0f;
			float b = 0.0f;
			int i = (int)(progress * 6);
			float f = progress * 6.0f - i;
			float q = 1 - f;

			switch (i % 6)
			{
			case 0:
				r = 1;
				g = f;
				b = 0;
				break;
			case 1:
				r = q;
				g = 1;
				b = 0;
				break;
			case 2:
				r = 0;
				g = 1;
				b = f;
				break;
			case 3:
				r = 0;
				g = q;
				b = 1;
				break;
			case 4:
				r = f;
				g = 0;
				b = 1;
				break;
			case 5:
				r = 1;
				g = 0;
				b = q;
				break;
			}
			return new Color(r, g, b);
		}
    }
}

