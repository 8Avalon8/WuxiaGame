using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using HSUI;
using HSUI.Demo;
using System;
using System.Collections.Generic;

namespace HSUI.Test
{
    public class UnitTestHSUI
    {
        IUIManager CreateUIManagerForTest()
        {
            GameObject obj = new GameObject();

            Assert.IsNotNull(obj);

            var manager = UIManagerFactory.CreateDefaultUIManager();
            manager.Init(obj.transform);
            Assert.IsNotNull(manager);
            return manager;
        }

        [UnityTest]
        public IEnumerator CreatePanelTest()
        {
            var manager = CreateUIManagerForTest();
            yield return 0;
            var panel = manager.CreatePanel<DemoPanel1>("panel1", "PANEL 1");
            yield return 0;
            Assert.IsNotNull(panel);
        }

        [UnityTest]
        public IEnumerator CreateWidgetTest()
        {
            var manager = CreateUIManagerForTest();
            yield return 0;
            var panel = manager.CreatePanel<DemoPanel1>("panel1","");
            var widget = manager.CreateWidget<DemoWidgetSidebar>("sidebar", panel, panel.transform);
            Assert.IsNotNull(widget);
            manager.DestroyPanel(panel);
        }

        [UnityTest]
        public IEnumerator MonolizedPanelTest()
        {
            var manager = CreateUIManagerForTest();
            yield return 0;
            var panel0 = manager.CreatePanel<DemoPanel1>("panel1", "");
            var panel1 = manager.CreatePanel<DemoPanel1>("panel1", "");
            yield return 0;
            Assert.AreEqual(panel1.IsMonopolized, true);
            var panel2 = manager.CreatePanel<DemoPanel1>("panel1", "");
            yield return 0;

            var canvasGroup = panel1.GetComponent<CanvasGroup>();
            Assert.IsNotNull(canvasGroup);
            Assert.AreEqual(canvasGroup.alpha, 0f);
            Assert.AreEqual(canvasGroup.blocksRaycasts, false);

            var panel3 = manager.CreatePanel<DemoWidgetSidebar>("sidebar");
            yield return 0;
            Assert.AreEqual(canvasGroup.alpha, 1f);
            Assert.AreEqual(canvasGroup.blocksRaycasts, true);

            manager.DestroyPanel(panel3);
            yield return 0;
            Assert.AreEqual(canvasGroup.alpha, 0f);
            Assert.AreEqual(canvasGroup.blocksRaycasts, false);

            manager.DestroyPanel(panel0);
            yield return 0;
            Assert.AreEqual(canvasGroup.alpha, 0f);
            Assert.AreEqual(canvasGroup.blocksRaycasts, false);
        }

        [UnityTest]
        public IEnumerator RemoveAllTest()
        {
            var manager = CreateUIManagerForTest();
            yield return 0;
            for (int i = 0; i < 10; ++i)
            {
                manager.CreatePanel<DemoPanel1>("panel1","");
            }

            manager.Clear();

            int count = 0;
            foreach (var panel in manager.GetAllPanels())
            {
                count++;
            }
            Assert.AreEqual(count, 0);
        }
        
        [UnityTest]
        public IEnumerator CreateSideBarTest()
        {
            var manager = CreateUIManagerForTest();
            yield return 0;
            var panel = manager.CreatePanel<DemoPanel1>("panel1", "");
            var widget = manager.CreateWidget<DemoWidgetSidebar>("sidebar", panel, panel.transform);
            yield return 0;
            var textList = new List<String> { "1", "2", "3" };
            widget.Init(textList);
            Assert.AreEqual(3, widget.GetItemCount());
            yield return 0;
            widget.Init(textList);
            Assert.AreEqual(3, widget.GetItemCount());
            manager.DestroyPanel(widget);
            //Assert.IsNull(widget);
            yield return 0;
            widget = manager.CreateWidget<DemoWidgetSidebar>("sidebar", panel, panel.transform);
            widget.Init(DemoWidgetSidebar.WidgetType.Button);
            Assert.AreEqual(0, widget.GetItemCount());
            List<DemoWidgetSidebar.SideBarWidgetSelectItem> itemList = new List<DemoWidgetSidebar.SideBarWidgetSelectItem> {
            new DemoWidgetSidebar.SideBarWidgetSelectItem("1", ()=>{ Debug.Log(1); }),
            new DemoWidgetSidebar.SideBarWidgetSelectItem("2", ()=>{ Debug.Log(2); }),
            new DemoWidgetSidebar.SideBarWidgetSelectItem("3", ()=>{ Debug.Log(3); }),
            new DemoWidgetSidebar.SideBarWidgetSelectItem("4", ()=>{ Debug.Log(4); }),
            new DemoWidgetSidebar.SideBarWidgetSelectItem("5", ()=>{ Debug.Log(5); }),
            new DemoWidgetSidebar.SideBarWidgetSelectItem("6", ()=>{ Debug.Log(6); }),};
            widget.AddSelections(itemList);
            Assert.AreEqual(6, widget.GetItemCount());
        }
    }
}
