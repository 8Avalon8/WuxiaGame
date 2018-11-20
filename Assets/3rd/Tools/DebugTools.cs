using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using JianghuX;
using System.IO;
using UnityEngine.UI;
using System.Reflection;
using HSFrameWork.Common;
using HSUI;

public class DebugTools : EditorWindow {
	[MenuItem("Tools/[OPEN DEBUG PANEL]", false, 1)]
	static void AddWindow()
	{
		Rect wr = new Rect(0, 0, 500, 500);
		DebugTools window = EditorWindow.GetWindow<DebugTools>();
	}

	public void Init(){
		this.titleContent = new GUIContent("Debug Console");
		StreamReader sr = new StreamReader("DebugCommands.txt");
		string content = sr.ReadToEnd();
		sr.Close ();
		cmds.Clear ();
		foreach(var line in content.Split('\n'))
		{
			if(!line.Contains(":")) continue;
			cmds.Add (line.Split (':')[0].Replace ("\n","").Replace ("\r", ""), line.Split (':') [1].Replace ("\n","").Replace ("\r", ""));
		}
	}
	
	static private Dictionary<string,string> cmds = new Dictionary<string, string>();

	private string text;

	void Awake()
	{
		Init();
        Show();
		UnityEngine.Object.DontDestroyOnLoad(this);
	}

	void OnGUI () {
		EditorGUILayout.LabelField("指令");
		text = EditorGUILayout.TextArea(text, GUILayout.Width(200));
		if(GUILayout.Button("执行",GUILayout.Width(100)))
		{
			ExecuteCommand(text);
		}

		EditorGUILayout.Space();

		if(GUILayout.Button("刷新指令集",GUILayout.Width(100))){
			this.Init ();
			return;
		}

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		int count = 0;
		foreach(var cmd in cmds){
			string key = cmd.Key;
			string value = cmd.Value;
			if(count % 4 == 0){
				GUILayout.BeginHorizontal();
			}
			if(GUILayout.Button(key,GUILayout.Width(60), GUILayout.Height(30))){
				ExecuteCommand(value);
			}

			count ++;
			if(count % 4 == 0)
			{
				GUILayout.EndHorizontal();
			}
		}

	}

	void OnInspectorUpdate()
	{
		this.Repaint();
	}

	void ExecuteCommand(string cmd){
		string msg = cmd;
		string[] tmp = msg.Split(' ');
		string type = tmp[0];
		string value = "";
		if(tmp.Length>1)
			value = tmp[1];

        Container.Resolve<IStoryManager>().RunCommand(type, value);
	}
}
