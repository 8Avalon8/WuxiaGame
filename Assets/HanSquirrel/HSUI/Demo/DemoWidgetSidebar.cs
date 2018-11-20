using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HSUI;
using System;
using UnityEngine.UI;

namespace HSUI.Demo
{

    public class DemoWidgetSidebar : MonoBehaviour, IHSUIComponent
    {
        public bool IsInited { get; set; }
        private bool _sidebarInited = false;

        public bool IsMonopolized
        {
            get
            {
                return false;
            }
        }

        const int MAX_ITEMS = 6;

        private ArrayList widgetItems = new ArrayList();

        private List<SideBarWidgetSelectItem> _selections = new List<SideBarWidgetSelectItem>();
        private WidgetType _itemType;

        public class SideBarWidgetSelectItem
        {
            public SideBarWidgetSelectItem(string name, Action callback)
            {
                DisplayName = name;
                OnSelectCallback = callback;
            }
            public string DisplayName;
            public Action OnSelectCallback;
        }

        public enum WidgetType
        {
            Button,
            Toggle,
        }

        public void Init(List<SideBarWidgetSelectItem> selections = null, WidgetType itemType = WidgetType.Toggle)
        {
            if (_sidebarInited == true)
                return;
            _sidebarInited = true;
            //设置显示模式
            gameObject.AddComponent<ToggleGroup>();
            gameObject.AddComponent<GridLayoutGroup>();
            var layout = GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(65, 75);
            layout.spacing = new Vector2(0, 15);


            _selections = selections;
            _itemType = itemType;

            if (selections == null)
                return;
            if (selections.Count > MAX_ITEMS)
            {
                throw new Exception("sidebar数量超过了组件预设的最大数量：" + MAX_ITEMS);
            }
            foreach (var item in selections)
            {
                AddItem(item);
            }
        }

        public void Init(List<string> textList, WidgetType itemType = WidgetType.Toggle)
        {
            if (_sidebarInited == true)
                return;
            _sidebarInited = true;
            //设置显示模式
            gameObject.AddComponent<ToggleGroup>();
            gameObject.AddComponent<GridLayoutGroup>();
            var layout = GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(65, 75);
            layout.spacing = new Vector2(0, 15);
            foreach (var text in textList)
            {
                SideBarWidgetSelectItem selectItem = new SideBarWidgetSelectItem(text, null);
                AddItem(selectItem);
            }
        }

        public void Init(WidgetType itemType = WidgetType.Toggle)
        {
            if (_sidebarInited == true)
                return;
            _sidebarInited = true;
            gameObject.AddComponent<ToggleGroup>();
            gameObject.AddComponent<GridLayoutGroup>();
            var layout = GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(65, 75);
            layout.spacing = new Vector2(0, 15);
        }

        public int GetItemCount()
        {
            return widgetItems.Count;
        }
        public void AddItem(SideBarWidgetSelectItem item)
        {
            if (_itemType == WidgetType.Button)
            {
                AddButton(item);
            }
            else if (_itemType == WidgetType.Toggle)
            {
                AddToggle(item);
            }
        }

        //暂时注释
        public void Bind(List<SideBarWidgetSelectItem> selections)
        {
            //if(selections.Count > MAX_ITEMS)
            //{
            //    throw new Exception("sidebar数量超过了组件预设的最大数量：" + MAX_ITEMS);
            //}

            //for(int i = 0; i < MAX_ITEMS; ++i)
            //{
            //    var toggle = toggles[i];
            //    var selection = selections[i];
            //    if (i < selections.Count)
            //    {

            //    }
            //    else
            //    {
            //        toggle.gameObject.SetActive(false);
            //    }
            //}

            ////挪到最后
            //transform.SetAsLastSibling();

            ////移动动画
            //MoveIn(this.transform, -80, 0, 0.05f, 0.2f);
        }



        /// <summary>
        /// 增加多个WidgetItem（Toggle or Button）
        /// </summary>
        public void AddSelections(List<SideBarWidgetSelectItem> selections)
        {
            foreach (var item in selections)
            {
                AddItem(item);
            }
        }


        public void SetItemText(int index, string text)
        {
            if (_itemType == WidgetType.Button)
            {
                Button button = (Button)widgetItems[index];
                var textChild = button.GetComponentInChildren<Text>();
                if (textChild == null) return;
                textChild.text = text;
            }
            else if (_itemType == WidgetType.Toggle)
            {
                Toggle toggle = (Toggle)widgetItems[index];
                var textChild = toggle.GetComponentInChildren<Text>();
                if (textChild == null) return;
                textChild.text = text;
            }
        }

        public void SetItemAction(int index, Action clickaction)
        {
            if (_itemType == WidgetType.Button)
            {
                Button button = (Button)widgetItems[index];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => { clickaction(); });
            }
            else if (_itemType == WidgetType.Toggle)
            {
                Toggle toggle = (Toggle)widgetItems[index];
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn && clickaction != null)
                    {
                        clickaction();
                    }
                });
            }
        }

        public void SetItemActive(int index, bool active = true)
        {
            if (index > widgetItems.Count - 1 || index < 0)
                return;
            if (_itemType == WidgetType.Button)
            {
                Button button = (Button)widgetItems[index];
                button.gameObject.SetActive(active);
            }
            else if (_itemType == WidgetType.Toggle)
            {
                Toggle toggle = (Toggle)widgetItems[index];
                toggle.gameObject.SetActive(active);
            }

        }

        public void SetItemDot(int index, bool showDot)
        {
            if (index > widgetItems.Count - 1 || index < 0)
                return;

            try
            {
                Button btn = (Button)widgetItems[index];
                btn.transform.Find("Dot").gameObject.SetActive(showDot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("此Widget为Toggle!");
            }

        }

        public void SetToggleNewSign(int index, int count)
        {
            if (index > widgetItems.Count - 1 || index < 0)
                return;
            try
            {
                Toggle toggle = (Toggle)widgetItems[index];
                if (count == 0)
                    toggle.transform.Find("NewSign").gameObject.SetActive(false);
                else
                {
                    toggle.transform.Find("NewSign").gameObject.SetActive(true);
                    toggle.transform.Find("NewSign/Text").GetComponent<Text>().text = count.ToString();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("此Widget为Button!");
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public void Refresh(int index)
        {
            if (_itemType == WidgetType.Button)
                return;

        }

        #region Private Mathod
        void BindToggle(Toggle toggle, SideBarWidgetSelectItem data)
        {
            toggle.gameObject.SetActive(true);
            //SetText(toggle, data.DisplayName);
            var textChild = toggle.GetComponentInChildren<Text>();
            textChild.text = data.DisplayName;
            var callback = data.OnSelectCallback;

            //BindListener(toggle, callback);
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn && callback != null)
                {
                    callback();
                }
            });
        }

        void BindButton(Button button, SideBarWidgetSelectItem data)
        {
            button.gameObject.SetActive(true);
            var textChild = button.GetComponentInChildren<Text>();
            textChild.text = data.DisplayName;
            //SetText(button, data.DisplayName);
            var callback = data.OnSelectCallback;
            //BindListener(button, callback);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { callback(); });
        }
        void AddToggle(SideBarWidgetSelectItem item)
        {
            Toggle toggle = CreateToggle();
            toggle.group = GetComponent<ToggleGroup>();
            if (toggle.transform.Find("Active") != null)
            {
                toggle.graphic = toggle.transform.Find("Active").GetComponent<Image>();
            }
            toggle.isOn = false;
            toggle.transform.SetParent(transform);
            toggle.transform.localScale = Vector3.one;
            toggle.transform.localPosition = Vector3.zero;
            var textChild = toggle.GetComponentInChildren<Text>();
            textChild.text = item.DisplayName;
            //SetText(toggle, item.DisplayName);
            BindToggle(toggle, item);
            widgetItems.Add(toggle);
        }

        void AddButton(SideBarWidgetSelectItem item)
        {
            Button button = CreateButton();
            button.transform.SetParent(transform);
            button.transform.localScale = Vector3.one;
            button.transform.localPosition = Vector3.zero;
            var textChild = button.GetComponentInChildren<Text>();
            textChild.text = item.DisplayName;
            //SetText(button, item.DisplayName);
            BindButton(button, item);
            widgetItems.Add(button);
        }

        Toggle CreateToggle()
        {
            GameObject obj = null;
#if UNITY_EDITOR
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BuildSource/Prefabs/Base/BaseSideBarToggle.prefab");
#endif
            return GameObject.Instantiate(obj).GetComponent<Toggle>();
        }

        Button CreateButton()
        {
            GameObject obj = null;
#if UNITY_EDITOR
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BuildSource/Prefabs/Base/BaseSideBarButton.prefab");
#endif
            return GameObject.Instantiate(obj).GetComponent<Button>();
        }
        #endregion

        #region ISHUIComponent

        public void OnCreate()
        {

        }

        public void OnDespawn()
        {
            Debug.Log("DemoWidgetSidebar OnDespawn called.");
        }

        public void OnSpawn(params object[] paras)
        {

        }
        #endregion
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}