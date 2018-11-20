/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/26 10:57:43 
** desc： UI面板接口类 

*********************************************************************************/

using UnityEngine;

namespace HSUI
{
    /// <summary>
    /// UI面板接口类
    /// </summary>
    public interface IHSUIComponent
    {
        Transform transform { get; }
        GameObject gameObject { get; }
        bool IsInited { get; set; }

        /// <summary>
        /// 显示，只在创建时被调用一次，从对象池里拿出来不会再次被调用
        /// </summary>
        void OnCreate();

        /// <summary>
        /// 是否是独占的
        /// 如果为true，则本界面显示时，其他界面均隐藏（为了提高drawcall）
        /// 只有作为Panel时才有效，作为widget时本值默认设置为false即可
        /// </summary>
        bool IsMonopolized { get; }

        /// <summary>
        /// 被创建时，每次创建会被调用
        /// </summary>
        void OnSpawn(params object[] paras);

        /// <summary>
        /// 被销毁时，每次会被调用，如果是返回对象池的话，这个一般用于还原对象数据
        /// </summary>
        void OnDespawn();
    }
}
