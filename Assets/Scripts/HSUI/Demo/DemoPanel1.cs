using System;
using System.Collections;
using System.Collections.Generic;
using HSUI;
using UnityEngine;
using UnityEngine.UI;

namespace HSUI.Demo
{
    public class DemoPanel1 : MonoBehaviour, IHSUIComponent
    {
        public bool IsInited { get; set; }
        public Text panelText;

        public bool IsMonopolized
        {
            get
            {
                return true;
            }
        }

        public void Hide()
        {
            this.gameObject.SetActive(false);
        }

        public void OnSpawn(params object[] paras)
        {
            this.gameObject.SetActive(true);
            if (paras.Length > 0)
            {
                string text = paras[0] as string;
                panelText.text = text;
            }
            else
            {
                panelText.text = "";
            }
        }

        public void OnCreate()
        {
            
        }

        public void OnDespawn()
        {
            Debug.Log("DemoPanel1 OnDespawn called.");
        }

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