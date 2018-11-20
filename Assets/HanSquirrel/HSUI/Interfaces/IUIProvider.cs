/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/26 11:02:46 
** desc： UI组建提供器 

*********************************************************************************/

using UnityEngine;

namespace HSUI
{
    /// <summary>
    /// UI组建提供器
    /// </summary>
    public interface IUIProvider
    {
        /// <summary>
        /// 生成一个窗体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Spawn<T>(string path = null) where T : MonoBehaviour, IHSUIComponent;

        /// <summary>
        /// 回收/销毁一个窗体
        /// </summary>
        /// <param name="panel"></param>
        void Despawn(IHSUIComponent panel);
    }
}
