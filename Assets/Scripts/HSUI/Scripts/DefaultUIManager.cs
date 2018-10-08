/********************************************************************************

** Copyright(c) 2018 汉家松鼠工作室 All Rights Reserved. 

** auth： cg
** date： 2018/7/26 10:39:00 
** desc： HSUI管理类

*********************************************************************************/

using System.Collections.Generic;

using UnityEngine;

namespace HSUI
{
    /// <summary>
    /// UI总管理类
    /// </summary>
    public class DefaultUIManager : IUIManager
    {
        //节点链表
        class PanelLink
        {
            public PanelLink prev;
            public PanelLink next;
            public IHSUIComponent node;
        }

        class PanelWidgetContainer
        {
            public PanelWidgetContainer(IHSUIComponent parent, IHSUIComponent widget)
            {
                this.parent = parent;
                this.widget = widget;
            }
            public IHSUIComponent parent;
            public IHSUIComponent widget;
        }

        IUIProvider _provider;
        Transform _uiRoot;

        //当前是否独占页面
        bool _isCurrentMonolized = false;

        //保存layer的名字
        Dictionary<IHSUIComponent, CanvasGroup> _panelMap = new Dictionary<IHSUIComponent, CanvasGroup>();

        //widget对应关系
        List<PanelWidgetContainer> _panelWidgets = new List<PanelWidgetContainer>();
        
        //当前显示在最上层的panel
        PanelLink _current = null;

        public void Init(Transform rootGameObject, IUIProvider provider = null)
        {
            if(provider == null)
            {
                _provider = new ResourcePrefabProvider();
            }
            else
            {
                _provider = provider;
            }
            
            _uiRoot = rootGameObject;
        }

        public T CreatePanel<T>(string path, params object[] paras) where T : MonoBehaviour, IHSUIComponent
        {
            T panel = _provider.Spawn<T>(path);
            panel.transform.SetParent(_uiRoot, false);
            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            if (!panel.IsInited)
            {
                panel.OnCreate();
                panel.IsInited = true;
            }
            panel.OnSpawn(paras);

            AddPanel(panel);
            return panel;
        }

        public bool DestroyPanel(IHSUIComponent panel)
        {
            if (!_panelMap.ContainsKey(panel))
            {
                return false;
            }

            _panelMap.Remove(panel);

            DespawnPanel(panel);
            
            //如果销毁的是最上层的panel，则重新设置隐藏属性
            if(panel == _current.node)
            {
                while(true)
                {
                    var link = _current.prev;
                    if(link == null)
                    {
                        _current = null;
                        break;
                    }
                    else if (_panelMap.ContainsKey(link.node)) //找到了上一个存在的panel
                    {
                        _current = link;
                        RefreshMonopolizedInfo(link.node);
                        break;
                    }else if(link.node == panel)
                    {
                        LogError("没有按照堆栈顺序关闭面板，引发了未知错误，请程序员查代码！！Name=" + panel.gameObject.name);
                        break;
                    }else if(link.node == null || link.node.ToString() == "null")
                    {
                        LogError("关闭面板内部错误，HSUI内部逻辑错误，请通知HSUI维护者！！Name=" + panel.gameObject.name);
                        break;
                    }
                }
            }
            else //否则连接其两端（将它前后的panel连接起来）
            {
                var p = _current;
                while (p.node != panel && p != null)
                {
                    p = p.prev;
                }

                if(p == null)
                {
                    LogError("关闭面板内部错误2，HSUI内部逻辑错误，请通知HSUI维护者！！Name=" + panel.gameObject.name);
                }
                else //将这个面板的两端相连
                {
                    var prevLink = p.prev;
                    var nextLink = p.next;
                    if (nextLink != null) nextLink.prev = prevLink;
                    if (prevLink != null) prevLink.next = nextLink;
                }
            }

            return true;
        }

        public IEnumerable<IHSUIComponent> GetAllPanels()
        {
            foreach(var kv in _panelMap)
            {
                yield return kv.Key;
            }
        }

        public void Clear()
        {
            while(_current != null)
            {
                DestroyPanel(_current.node);
            }
            _panelWidgets.Clear();
            _panelMap.Clear();
        }

        public T CreateWidget<T>(string path, IHSUIComponent parentPanel, Transform parentNode, params object[] paras) where T : MonoBehaviour, IHSUIComponent
        {
            T widget = _provider.Spawn<T>(path);
            widget.transform.SetParent(parentNode, false);
            if (!widget.gameObject.activeSelf)
            {
                widget.gameObject.SetActive(true);
            }

            if (!widget.IsInited)
            {
                widget.OnCreate();
                widget.IsInited = true;
            }
            widget.OnSpawn(paras);
            
            //只有父节点是用本管理器创建的，在建立映射关系
            if (_panelMap.ContainsKey(parentPanel))
            {
                AddWidget(widget, parentPanel);
            }
            return widget;
        }

        #region private

        //隐藏（但不销毁）窗体
        private void HidePanel(IHSUIComponent p)
        {
            if (!_panelMap.ContainsKey(p)) return;
            var canvasGroup = _panelMap[p];
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0;
        }

        //显示窗体
        private void ShowPanel(IHSUIComponent p)
        {
            if (!_panelMap.ContainsKey(p)) return;

            var canvasGroup = _panelMap[p];
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1;
        }

        //添加窗体
        private void AddPanel(IHSUIComponent panel)
        {
            var canvasGroup = panel.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }
            _panelMap.Add(panel, canvasGroup);

            var link = new PanelLink();
            link.prev = _current;//双向link
            if(_current != null)
            {
                _current.next = link;//双向link
            }
            
            link.node = panel;
            _current = link;
            RefreshMonopolizedInfo(panel);
        }

        //添加组件
        private void AddWidget(IHSUIComponent widget,IHSUIComponent parent)
        {
            _panelWidgets.Add(new PanelWidgetContainer(parent, widget));
        }

        //刷新窗体独占状态
        private void RefreshMonopolizedInfo(IHSUIComponent p)
        {
            //如果是独占的，隐藏其他panel
            if (p.IsMonopolized)
            {
                Log("RefreshMonopolizedInfo 1");
                _isCurrentMonolized = true;
                foreach (var panel in GetAllPanels())
                {
                    if (panel != p)
                    {
                        HidePanel(panel);
                    }
                    else
                    {
                        ShowPanel(panel);
                    }
                }
            }
            else if(_isCurrentMonolized) //如果当前是独占的，则需要把所有的都显示出来
            {
                Log("RefreshMonopolizedInfo 2");
                _isCurrentMonolized = false;
                foreach (var panel in GetAllPanels())
                {
                    if (panel != p)
                    {
                        ShowPanel(panel);
                    }
                }
            }
        }

        private void DespawnPanel(IHSUIComponent p)
        {
            //先回收panel所有包含的widget
            for(int i=_panelWidgets.Count - 1; i >= 0; i--)
            {
                var c = _panelWidgets[i];
                if(c.parent == p)
                {
                    c.widget.OnDespawn();
                    _provider.Despawn(c.widget);
                    _panelWidgets.RemoveAt(i);
                }
            }

            //再回收panel
            p.OnDespawn();
            _provider.Despawn(p);
        }

        private void Log(string msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#endif
        }

        private void LogError(string msg)
        {
#if UNITY_EDITOR
            Debug.LogError(msg);
#endif
        }
        #endregion


    }
}
