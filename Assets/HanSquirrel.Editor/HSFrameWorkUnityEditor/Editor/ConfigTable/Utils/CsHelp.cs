using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HSFrameWork.ConfigTable.Editor.Impl
{
    public class CsHelp : EditorWindow
    {
        [MenuItem("Tools/CSHelp", false, 1)]
        static void AddWindow()
        {
            Rect wr = new Rect(0, 0, 500, 500);
            CsHelp window = EditorWindow.GetWindow<CsHelp>();
        }

        void Awake()
        {
            this.titleContent = new GUIContent("CsHelp");
            Show();
            UnityEngine.Object.DontDestroyOnLoad(this);
        }

        private string m_NamespaceText;
        public string m_FileText;
        private string m_CheckText;
        private static string pattern = @"\b";
        private void OnGUI()
        {
            EditorGUILayout.LabelField("命名空间名字");
            m_NamespaceText = EditorGUILayout.TextArea(m_NamespaceText, GUILayout.Width(200));
            EditorGUILayout.LabelField("文件夹位置");
            m_FileText = EditorGUILayout.TextArea(m_FileText, GUILayout.Width(400));
            EditorGUILayout.LabelField("待检测的单词");
            m_CheckText = EditorGUILayout.TextArea(m_CheckText, GUILayout.Width(200));

            if (GUILayout.Button("添加", GUILayout.Width(100)))
            {
                AddNamespace(m_NamespaceText, m_FileText, m_CheckText);
            }
        }


        static public void AddNamespace(string spacename, string m_FileText,string m_CheckText)
        {
            var files = GetFile(m_FileText, new List<string>()).ToArray();

            int startIndex = 0;
            int succendIndex = 0;

            EditorApplication.update = delegate ()
            {
                var item = files[startIndex];
                bool isCancel = EditorUtility.DisplayCancelableProgressBar("添加命名空间中", item, (float)startIndex / (float)files.Length);

                if (item.Contains(".cs") && !item.Contains(".meta"))
                {
                    if (!ContainNameSpace(item, spacename,m_CheckText))
                    {
                        if (AddNameSpace(item, spacename, m_CheckText))
                            succendIndex++;
                    }
                }
                startIndex++;
                if (isCancel || startIndex >= files.Length)
                {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update = null;
                    Debug.Log("给"+ succendIndex + "个文件添加了命名空间");
                }

            };
        }

        public static List<string> GetFile(string path, List<string> FileList)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] fil = dir.GetFiles();
            DirectoryInfo[] dii = dir.GetDirectories();
            foreach (FileInfo f in fil)
            {
                FileList.Add(f.FullName);//添加文件路径到列表中
            }
            //获取子文件夹内的文件列表，递归遍历
            foreach (DirectoryInfo d in dii)
            {
                GetFile(d.FullName, FileList);
            }
            return FileList;
        }


        static public bool ContainNameSpace(string path,string spacename,string m_CheckText)
        {
            StreamReader sr_check = new StreamReader(path);
            string all = sr_check.ReadToEnd();

            sr_check.Close();
            return all.Contains(spacename);
        }

        static public bool AddNameSpace(string path, string spacename,string CheckText)
        {
            StreamReader sr = new StreamReader(path);
            StringBuilder sb = new StringBuilder();

            bool IsAdd=false;
            bool IsCheck = false;
            int lineIndex = 0;
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("using")&&!IsAdd)
                {
                    sb.Append("using " + spacename + ";"+ "\n");
                    IsAdd = true;
                }
                if (Regex.IsMatch(line, pattern + CheckText+pattern))
                {
                    IsCheck = true;
                }
                //if (line.Contains(CheckText))
                //{
                //    IsCheck = true;
                //}
                if (lineIndex == 0)
                    sb.Append(line);
                else
                    sb.Append("\n" + line);
                lineIndex++;
            }

            sr.Close();
            if (IsCheck)
            {
                StreamWriter sw = new StreamWriter(path);
                sw.WriteLine(sb.ToString());

                sw.Close();

                return true;
            }
            return false;
        }
    }
}
