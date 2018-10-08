/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/26 10:46:52 
** desc： UI管理类接口 

*********************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace HSUI
{
    /// <summary>
    /// UI管理器接口
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        /// <param name="rootGameObject">UI节点的根对象</param>
        /// <param name="provider">UI资源提供器，默认为从Resource载入prefab，使用LeanPool进行对象池管理</param>
        void Init(Transform rootGameObject, IUIProvider provider = null);

        /// <summary>
        /// 创建一个面板
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paras"></param>
        /// <returns></returns>
        T CreatePanel<T>(string path, params object[] paras) where T : MonoBehaviour, IHSUIComponent;

        /// <summary>
        /// 获取窗体栈中所有的面板
        /// </summary>
        /// <returns></returns>
        IEnumerable<IHSUIComponent> GetAllPanels();

        /// <summary>
        /// 销毁窗体栈中的面板
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        bool DestroyPanel(IHSUIComponent panel);

        /// <summary>
        /// 清空所有的panel
        /// </summary>
        void Clear();

        /// <summary>
        /// 创建一个控件
        /// 
        /// 这个控件将和父亲Panel具有共同的生命周期（Panel回收时，控件也将一并被回收）
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="path">载入路径</param>
        /// <param name="parentPanel">所属面板</param>
        /// <param name="parentNode">父节点</param>
        /// <param name="paras">传入参数</param>
        /// <returns>控件对象</returns>
        T CreateWidget<T>(string path, IHSUIComponent parentPanel, Transform parentNode, params object[] paras) where T : MonoBehaviour, IHSUIComponent;
    }
}
