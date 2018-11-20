using System;
using UnityEditor;
using System.IO;
using UnityEngine;
using GLib;
using HSFrameWork.Common;
using System.Collections.Generic;
using HSFrameWork.ConfigTable.Editor.Impl;

namespace HSFrameWork.ConfigTable.Editor
{
    /// <summary>
    /// 两个版本的Values对比工具
    /// </summary>
    public class ValueCompareWindow : EditorWindow
    {
        string baseValueFile, currentValueFile, toolsExeable;
        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;
        GUIStyle _ErrorStyle;
        GUIStyle _NormalStyle;

        private void InitStyle()
        {
            if (_ErrorStyle != null)
                return;
            _ErrorStyle = new GUIStyle(GUI.skin.textArea);
            _ErrorStyle.normal.textColor = Color.red;
            _ErrorStyle.focused.textColor = Color.red;
            _NormalStyle = new GUIStyle(GUI.skin.textArea);
        }

        void OnEnable()
        {
            baseValueFile = PlayerPrefs.GetString("HSFrameWork.ConfigTable.ValueCompareWindow.baseValueFile");
            currentValueFile = PlayerPrefs.GetString("HSFrameWork.ConfigTable.ValueCompareWindow.currentValueFile");
            if (!currentValueFile.Visible())
            {
                if (HSCTC.CEValues.ExistsAsFile())
                    currentValueFile = HSCTC.CEValues;
            }
            toolsExeable = PlayerPrefs.GetString("HSFrameWork.ConfigTable.ValueCompareWindow.toolsExeable");
        }

        void OnDestroy()
        {
            PlayerPrefs.SetString("HSFrameWork.ConfigTable.ValueCompareWindow.baseValueFile", baseValueFile);
            PlayerPrefs.SetString("HSFrameWork.ConfigTable.ValueCompareWindow.currentValueFile", currentValueFile);
            PlayerPrefs.SetString("HSFrameWork.ConfigTable.ValueCompareWindow.toolsExeable", toolsExeable);
        }

        private void ShowFileComplex(string label, ref string fileName, string displayName, string saveName, string ext)
        {
            GUILayout.Label(label, EditorStyles.boldLabel);
            fileName = EditorGUILayout.TextField("", fileName, fileName.ExistsAsFile() ? _NormalStyle : _ErrorStyle);
            if (GUILayout.Button("↑修改↑"))
            {
                GUI.FocusControl(null); //如果不这么做，则如果当前激活正在要修改的TextField的时候，下面的修改只有当焦点移动到其他的文本框后才会显示出来。
                string path = EditorUtility.OpenFilePanel("请选择 " + displayName, fileName, ext);
                if (path.Visible())
                {
                    fileName = path;
                    PlayerPrefs.SetString("HSFrameWork.ConfigTable.ValueCompareWindow." + saveName, fileName);
                }
            }
        }

        void OnGUI()
        {
            InitStyle();
            ShowFileComplex("原始Values", ref baseValueFile, "原始Values文件", "baseValueFile", "");
            ShowFileComplex("当前Values", ref currentValueFile, "当前Values文件", "currentValueFile", "");
            ShowFileComplex("比较工具", ref toolsExeable, "对比工具EXE", "toolsExeable", "exe");
            if (GUILayout.Button("开始比较"))
            {
                Compare();
            }
            this.Repaint();
        }

        private void DecryptConvert(string cevalues, string name, string displayName)
        {
            string valueFile1 = HSCTC.DebugPath.StandardSub(name + "_values.dat");
            File.WriteAllBytes(valueFile1, HSPackToolEx.AutoDeFile(File.ReadAllBytes(cevalues)));
            using (ProgressBarAutoHide.Get(500))
            using (HSUtils.ExeTimer("ConvertBytes2Txt: " + displayName))
            {
                try
                {
                    BeanDictEditor.LoadAndConvertVerbose("转换" + displayName + "为TXT", valueFile1, name + "_converted");
                }
                catch (Exception e)
                {
                    HSUtils.LogException(e);
                    HSUtils.LogError("加载 [{0}] 失败。文件中的C#类和当前的类不同。", cevalues);
                }
            }
        }

        private bool CheckFile(string fileName)
        {
            if (!fileName.ExistsAsFile())
            {
                EditorUtility.DisplayDialog("错误", "文件不存在：\r\n[{0}]".f(fileName), "关闭窗口");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Compare()
        {
            if (!CheckFile(baseValueFile) || !CheckFile(currentValueFile) || !CheckFile(toolsExeable))
                return;

            if (!toolsExeable.EndsWith("TortoiseMerge.exe") && !toolsExeable.EndsWith("BCompare.exe"))
            {
                EditorUtility.DisplayDialog("错误", "当前仅仅支持 TortoiseMerge.exe 和 BCompare.exe", "关闭窗口");
                return;
            }

            Directory.CreateDirectory(HSCTC.DebugPath);
            DecryptConvert(baseValueFile, "C0", "原始的Values");
            DecryptConvert(currentValueFile, "C1", "最新的Values");

            string baseXml = BeanDictEditor.GetXMLPath("C0_converted");
            string currentXml = BeanDictEditor.GetXMLPath("C1_converted");

            List<string> arguments = new List<string>();
            arguments.Add("\"{0}\"".f(baseXml));
            arguments.Add("\"{0}\"".f(currentXml));

            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = toolsExeable,
                Arguments = string.Join(" ", arguments.ToArray())
            };
            p.Start();
        }

        /// <summary>
        /// 菜单 Tools♥/比较两个版本的Values
        /// </summary>
        [MenuItem("Tools♥/比较两个版本的Values", false)]
        public static void CompareValuesFile()
        {
            ValueCompareWindow window = (ValueCompareWindow)EditorWindow.GetWindow(typeof(ValueCompareWindow), true, "比较两个版本的Values文件差异");
            window.Show();
        }
    }
}
