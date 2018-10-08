/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/26 10:49:42 
** desc： UI管理器工厂类 

*********************************************************************************/


namespace HSUI
{
    public class UIManagerFactory
    {
        static public IUIManager CreateDefaultUIManager()
        {
            return new DefaultUIManager();
        }
    }
}
