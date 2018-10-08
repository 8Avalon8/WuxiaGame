using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HSUI;
using System;

namespace HSUI.Demo
{
    public class HSUIDemo : MonoBehaviour
    {
        IUIManager manager;
        IHSUIComponent panel;
        IHSUIComponent panel2;
        IHSUIComponent panel3;
        IHSUIComponent panel4;
        //IHSUIComponent widget;

        // Use this for initialization
        void Start()
        {

            manager = UIManagerFactory.CreateDefaultUIManager();
            manager.Init(this.transform);

            panel = manager.CreatePanel<DemoPanel1>("panel1", "PANEL 1");
            panel2 = manager.CreatePanel<DemoPanel1>("panel1", "PANEL 2");
            panel3 = manager.CreatePanel<DemoPanel1>("panel1", "PANEL 3");
            panel4 = manager.CreatePanel<DemoPanel1>("panel1", "PANEL 4");

            DemoWidgetSidebar widget = manager.CreateWidget<DemoWidgetSidebar>("sidebar", panel2, panel2.transform);
            widget.Init(DemoWidgetSidebar.WidgetType.Toggle);
            widget.AddItem(new DemoWidgetSidebar.SideBarWidgetSelectItem("签到", ()=> { Debug.Log("sdf"); }));
            widget.AddItem(new DemoWidgetSidebar.SideBarWidgetSelectItem("抽签", () => { Debug.Log("sdfsdf"); }));
            widget.AddItem(new DemoWidgetSidebar.SideBarWidgetSelectItem("开始", () => { Debug.Log("sdfsdfsdf"); }));
            //manager.CreateWidget<DemoWidgetSidebar>("sidebar", panel3, panel3.transform);
            manager.DestroyPanel(panel4);

            StartCoroutine(Test());
        }

        IEnumerator Test()
        {
            yield return new WaitForSeconds(3f);
            manager.DestroyPanel(panel3);
        }


        // Update is called once per frame
        void Update()
        {
        }
    }
}