using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;


[InitializeOnLoad]
public class ToolBarEditor
{
    static class ToolbarStyles
    {
        public static readonly GUIStyle CommandButtonStyle;
        public static readonly GUIStyle CommandButtonStyle2;

        static ToolbarStyles()
        {
            CommandButtonStyle = new GUIStyle("Command")
            {
                font = null,
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                contentOffset = default,
                fixedWidth = 40,
                fixedHeight = 25,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                richText = true,
            };
            CommandButtonStyle2 = new GUIStyle("Command")
            {
                font = null,
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                contentOffset = default,
                fixedWidth = 70,
                fixedHeight = 20,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                richText = true,
            };
        }
    }

    static ToolBarEditor()
    {
        //git工具
        if (EditorPrefs.GetBool("Git_Conflict", false))
        {
            ToolbarExtender.LeftToolbarGUI.Add(ClearConflictButton);
        }
        else
        {
            ToolbarExtender.LeftToolbarGUI.Add(GitToolToggle);
            ToolbarExtender.LeftToolbarGUI.Add(GitPull);
            ToolbarExtender.LeftToolbarGUI.Add(GitCommitAndPush);
            ToolbarExtender.LeftToolbarGUI.Add(DropDown);
            ToolbarExtender.LeftToolbarGUI.Add(RefreshBranchInfo);
        }

        //场景切换
        ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);

        //RefreshBranchInfo();
    }

    private static void ClearConflictButton()
    {
        GUIContent buttonContent = EditorGUIUtility.IconContent("CollabConflict");
        buttonContent.text = "git冲突中 别点我";
        if (GUILayout.Button(buttonContent))
        {
            EditorPrefs.SetBool("Git_Conflict", false);
            EditorUtility.RequestScriptReload();
        }
    }

    private static void GitCommitAndPush()
    {
        if (!EditorPrefs.GetBool("GitTool")) return;
        GUIContent buttonContent = EditorGUIUtility.IconContent("Update-Available");
        buttonContent.text = "git上传";
        if (GUILayout.Button(buttonContent))
        {
            T().Forget();
        }


        async UniTask T()
        {
            (_displayedOptions, _currentBranchName) = await GitHelper.GetBranchInfo();
            _selectedIndex = _displayedOptions.ToList().IndexOf(_currentBranchName);
            var (files, message) = await GitHelper.OpenCommitWindow(); //玩家选择的文件 和提交log
            if (files == null || files.Count == 0 || message == "")
            {
                EditorUtility.DisplayDialog("未选择文件", "未选择文件", "ok");
                return;
            }

            if (EditorUtility.DisplayDialog($"推送确认", $"是否要提交到{_displayedOptions[_selectedIndex]}分支？\n log信息：{message}",
                    "确认", "取消"))
            {
                GitBlockWindow.OpenWindow();
                var success = await GitHelper.CommitAndPush(files, message);
                if (success)
                {
                    EditorUtility.DisplayDialog("推送成功", "推送成功", "ok");
                }
                else
                {
                    Debug.LogError("更新失败");
                }

                GitBlockWindow.CloseWindow();
            }
        }
    }

    private static void GitToolToggle()
    {
        if (GUILayout.Toggle(EditorPrefs.GetBool("GitTool", true), "git工具"))
        {
            EditorPrefs.SetBool("GitTool", true);
        }
        else
        {
            EditorPrefs.SetBool("GitTool", false);
        }
    }

    private static void RefreshBranchInfo()
    {
        if (!EditorPrefs.GetBool("GitTool")) return;
        if (GUILayout.Button("获取分支列表", ToolbarStyles.CommandButtonStyle2))
        {
            Func().Forget();
        }

        async UniTask Func()
        {
            GitBlockWindow.OpenWindow();
            (_displayedOptions, _currentBranchName) = await GitHelper.GetBranchInfo();
            _selectedIndex = _displayedOptions.ToList().IndexOf(_currentBranchName);
            DropDown();
            GitBlockWindow.CloseWindow();
        }
    }


    private static int _selectedIndex = 0;
    private static string[] _displayedOptions = new[] {"请先获取分支列表"};
    private static string _currentBranchName;

    private static void DropDown()
    {
        if (!EditorPrefs.GetBool("GitTool")) return;

        T().Forget();

        async UniTask T()
        {
            if (_displayedOptions == null)
            {
                // (_displayedOptions, _currentBranchName) = await GitHelper.GetBranchInfo();
                // _selectedIndex = _displayedOptions.ToList().IndexOf(_currentBranchName);
                return;
            }

            // 创建一个下拉框
            var oldIndex = _selectedIndex;
            var width = GUILayout.Width(_displayedOptions[_selectedIndex].Length * 5 + 70);
            var height = GUILayout.Height(33);
            var tryToCheckOutIndex = -1;
            try
            {
                GUILayout.Label("切分支");
                tryToCheckOutIndex = EditorGUILayout.Popup(_selectedIndex, _displayedOptions, width, height);
                return;
            }
            catch
            {
                // ignored
            }

            if (tryToCheckOutIndex == oldIndex || tryToCheckOutIndex == -1) return;
            //询问是否切换
            string message =
                $"是否要从 {_displayedOptions[oldIndex]}\n  切换到    {_displayedOptions[tryToCheckOutIndex]} 分支？\n\n本地未提交的修改会被清空\n本地未提交的修改会被清空\n本地未提交的修改会被清空";
            if (EditorUtility.DisplayDialog("切分支", message, "确认", "取消"))
            {
                // 在编辑器中显示所选值
                bool commitEmpty = await GitHelper.CheckCommit(_displayedOptions[oldIndex]);
                if (!commitEmpty)
                {
                    EditorUtility.DisplayDialog("检测到未提交的commit", "本地有未提交的commit 无法切分支 请先提交",
                        "ok");
                    return;
                }

                GitBlockWindow.OpenWindow();
                await GitHelper.CheckOut(_displayedOptions[tryToCheckOutIndex]);
                _selectedIndex = tryToCheckOutIndex;
                EditorUtility.RequestScriptReload();
                GitBlockWindow.CloseWindow();
            }
            else
            {
                _selectedIndex = oldIndex;
            }
        }
    }

    private static void GitPull()
    {
        if (!EditorPrefs.GetBool("GitTool")) return;

        GUIContent buttonContent = EditorGUIUtility.IconContent("Download-Available");
        buttonContent.text = "git更新";
        if (GUILayout.Button(buttonContent))
        {
            string message =
                $"是否要更新{_displayedOptions[_selectedIndex]} 分支？\n\n      本地的修改会被清空";
            if (EditorUtility.DisplayDialog("更新工程", message, "确认", "取消"))
            {
                T().Forget();
            }

            async UniTask T()
            {
                GitBlockWindow.OpenWindow();
                (_displayedOptions, _currentBranchName) = await GitHelper.GetBranchInfo();
                _selectedIndex = _displayedOptions.ToList().IndexOf(_currentBranchName);
                await GitHelper.GitPull();
                EditorUtility.RequestScriptReload();
                GitBlockWindow.CloseWindow();
            }
        }
    }

    static void OnRightToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Title", "Start Title Scene "), ToolbarStyles.CommandButtonStyle))
        {
            SceneHelper.StartScene("Title");
        }
    }
}


#region git

public class GitBlockWindow : EditorWindow
{
    // git运行的时候的提示窗口 //todo 显示进度 目前只是告诉你正在运行

    private static GitBlockWindow window;

    public static void OpenWindow()
    {
        if (window != null) return;
        window = GetWindow<GitBlockWindow>();
        window.minSize = new Vector2(1200, 400);
        window.titleContent = new GUIContent("git运行中 请勿操作");
    }

    public static void CloseWindow()
    {
        if (window == null) return;
        window.Close();
        window = null;
    }

    // 在这里绘制窗口内容
    private void OnGUI()
    {
        GUIStyle coloredLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal =
            {
                textColor = Color.red
            },
            fontSize = 30
        };
        GUILayout.Label("git运行中 请勿操作 如果这个界面长时间未关闭 请截图发群里", coloredLabelStyle);
        GUILayout.Label("git运行中 请勿操作 如果这个界面长时间未关闭 请截图发群里", coloredLabelStyle);
        GUILayout.Label("git运行中 请勿操作 如果这个界面长时间未关闭 请截图发群里", coloredLabelStyle);
    }
}


public class GitCommitWindow : EditorWindow
{
    private GitCommitWindow window;
    public string State = "Idle";
    public string CommitMessage = "log记得填";

    private List<string> availableFiles = new();
    public List<string> selectedFiles = new();

    public GitCommitWindow OpenWindow(List<string> changedFiles)
    {
        State = "Idle";
        selectedFiles = new List<string>();
        if (window != null)
        {
            return window;
        }

        availableFiles = changedFiles;
        selectedFiles.Clear();
        window = GetWindow<GitCommitWindow>();
        window.maximized = true;
        window.minSize = new Vector2(800, 400);
        window.titleContent = new GUIContent("git上传工具");
        return window;
    }

    private void OnDisable()
    {
        if (State == "Idle")
        {
            State = "Cancel";
        }
    }

    private float splitPercentage = 0.5f;
    private Rect leftRect, rightRect;


    // 在这里绘制窗口内容
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();

        // 左侧：可用文件列表
        GUILayout.BeginVertical();
        GUILayout.Label("本地有变更的文件", EditorStyles.boldLabel);
        GUILayout.Label("如果发现下面出现了xxx.meta文件 但没有出现xxx文件 请提醒相关人员上传.meta文件", EditorStyles.boldLabel);
        scrollPositionAvailable = GUILayout.BeginScrollView(scrollPositionAvailable);
        var leftAlignedButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft
        };
        foreach (string file in availableFiles)
        {
            if (GUILayout.Button(file, leftAlignedButtonStyle))
            {
                selectedFiles.Add(file);
                availableFiles.Remove(file);
                break; // Important to break to avoid modifying the collection during iteration.
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // 右侧：已选择文件列表
        GUILayout.BeginVertical();
        GUILayout.Label("会被上传的文件", EditorStyles.boldLabel);
        scrollPositionSelected = GUILayout.BeginScrollView(scrollPositionSelected);

        for (int i = selectedFiles.Count - 1; i >= 0; i--)
        {
            string selectedFile = selectedFiles[i];
            GUILayout.BeginHorizontal();
            GUILayout.Label(selectedFile);
            if (GUILayout.Button("取消选择", GUILayout.Width(100)))
            {
                availableFiles.Add(selectedFile);
                selectedFiles.RemoveAt(i);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        CommitMessage = GUILayout.TextField(CommitMessage, GUILayout.Width(position.width - 200));
        if (GUILayout.Button("提交并推送", GUILayout.Width(200)))
        {
            State = "Confirm";
            window.Close();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private Vector2 scrollPositionAvailable;

    private Vector2 scrollPositionSelected;
}

public static class GitHelper
{
    /// <summary>
    /// 检查是否有未推送的提交
    /// </summary>
    /// <returns></returns>
    public static async UniTask<bool> CheckCommit(string currentBranch)
    {
        var (output, error) = await RunGitCommand($"log origin/{currentBranch[2..]}..HEAD");
        // 如果输出为空，表示没有未推送的提交
        return string.IsNullOrEmpty(output);
    }

    /// <summary>
    /// 打开commit窗口 返回玩家选择的文件
    /// </summary>
    /// <returns></returns>
    public static async UniTask<(List<string>, string)> OpenCommitWindow()
    {
        var modifiedFiles = await GitHelper.GetModifiedFiles();
        var window = ScriptableObject.CreateInstance<GitCommitWindow>().OpenWindow(modifiedFiles);
        await UniTask.WaitWhile(() => window.State == "Idle");
        Debug.Log(window.State);
        switch (window.State)
        {
            case "Cancel":
                return (null, "");
                break;
            case "Confirm":
                return (window.selectedFiles, window.CommitMessage);
                break;
        }

        return (null, "");
    }

    public static async UniTask<bool> CommitAndPush(List<string> files, string message)
    {
        if (files == null || files.Count == 0)
        {
            EditorUtility.DisplayDialog("未选择文件", "未选择文件", "ok");
            return false;
        }

        StringBuilder addCommand = new StringBuilder("add");
        foreach (var file in files)
        {
            addCommand.Append($" {file}");
        }

        await RunGitCommand(addCommand.ToString());
        await RunGitCommand($"commit -m {message}");
        var (output, error) = await RunGitCommand($"pull");
        if (output.Contains("CONFLICT"))
        {
            EditorUtility.DisplayDialog("提交的文件与远端冲突 请截图发群里", "提交的文件与远端冲突 请截图发群里 保留现场", "ok");
            EditorPrefs.SetBool("Git_Conflict", true);
            return false;
        }

        await RunGitCommand($"push");
        return true;
    }


    /// <summary>
    /// 切分支
    /// </summary>
    /// <param name="targetBranch"></param>
    public static async UniTask CheckOut(string targetBranch)
    {
        await RunGitCommand("reset --hard");
        await RunGitCommand("clean -df");
        await RunGitCommand("fetch");
        await RunGitCommand($"checkout {targetBranch}");

        EditorUtility.RequestScriptReload();
    }

    /// <summary>
    /// 获取git变更的文件列表
    /// </summary>
    /// <param name="gitRepoPath"></param>
    /// <returns></returns>
    public static async UniTask<List<string>> GetModifiedFiles()
    {
        var modifiedFiles = new List<string>();
        var (outPut, error) = await RunGitCommand("status --porcelain");
        if (string.IsNullOrEmpty(outPut)) return null;
        StringBuilder addCommand = null;
        var lines = outPut.Split('\n');
        foreach (var line in lines)
        {
            if (line.Length <= 3) continue;
            var status = line.Trim()[0];
            var filePath = line.Substring(3).Trim();
            switch (status)
            {
                case 'M' or 'A' or '?' or 'D':
                    if (filePath.Contains("Assets/ResLocalize/Scenario")) continue; //禁止客户端提交Secnario
                    if (filePath.Contains("Assets/ResLocalize/") && filePath.Contains("Message"))
                        continue; //禁止客户端提交本地化Asset
                    modifiedFiles.Add(filePath);
                    break;
            }
        }

        return modifiedFiles.OrderBy(str => str).ToList();
    }


    /// <summary>
    /// git更新 
    /// </summary>
    public static async UniTask GitPull()
    {
        string error;
        await RunGitCommand("reset --hard");
        await RunGitCommand("clean -df");
        (_, error) = await RunGitCommand("pull");
        if (!string.IsNullOrEmpty(error) && !error.Contains("SECURITY WARNING"))
        {
            EditorUtility.DisplayDialog("更新", "更新失败 请截图发群里 不要清log", "ok");
        }
        else
        {
            EditorUtility.DisplayDialog("更新成功1", "更新成功", "ok");

            EditorUtility.RequestScriptReload();
        }
    }

    /// <summary>
    /// 获取分支信息  分支名，当前分支的index
    /// </summary>
    /// <returns></returns>
    public static async UniTask<(string[] branches, string currentBranchName)> GetBranchInfo()
    {
        // 获取所有分支名
        var (output, error) = await RunGitCommand("branch -a");
        var branches = new List<string>();
        string currentBranch = "";
        // 分割输出并提取分支名称
        string[] branchLines = output.Replace("remotes/origin/", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < branchLines.Length; i++)
        {
            var item = branchLines[i];
            if (item.Length > 1 && item[0].Equals('*')) //匹配当前分支
            {
                branchLines[i] = item.Replace('*', ' ');
                currentBranch = item.Trim();
            }

            branches.Add(item.Trim());
        }

        Debug.Log(string.Join("\n", branchLines));
        Debug.Log("currentBranch: " + currentBranch);

        return (branches.ToArray(), currentBranch);
    }

    static async UniTask<(string output, string error)> RunGitCommand(string command)
    {
        Debug.Log("git " + command);
        using (Process process = new Process())
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = processStartInfo;

            // 开始进程
            if (!process.Start())
            {
                throw new InvalidOperationException("Could not start git process.");
            }

            // 异步地读取StandardOutput和StandardError
            var readOutputTask = process.StandardOutput.ReadToEndAsync().AsUniTask();
            var readErrorTask = process.StandardError.ReadToEndAsync().AsUniTask();

            var (output, error) = await UniTask.WhenAll(readOutputTask, readErrorTask);


            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
            {
                Debug.Log(output);
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log(error);
            }

            return (output, error);
        }
    }
}

#endregion

#region 场景切换

static class SceneHelper
{
    static string _sceneToOpen;
    private static bool _isRun;

    public static void ChangeScene(string sceneName)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        _sceneToOpen = sceneName;
        _isRun = false;
        EditorApplication.update += OnUpdate;
    }

    public static void StartScene(string sceneName)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        _sceneToOpen = sceneName;
        _isRun = true;
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (_sceneToOpen == null ||
            EditorApplication.isPlaying || EditorApplication.isPaused ||
            EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.update -= OnUpdate;

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // need to get scene via search because the path to the scene
            // file contains the package version so it'll change over time
            string[] guids = AssetDatabase.FindAssets("t:scene " + _sceneToOpen, null);
            if (guids.Length == 0)
            {
                Debug.LogWarning("Couldn't find scene file");
            }
            else
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorSceneManager.OpenScene(scenePath);
                EditorApplication.isPlaying = _isRun;
            }
        }

        _sceneToOpen = null;
    }
}

#endregion

#region ToolbarExtender

[InitializeOnLoad]
public static class ToolbarExtender
{
    //static int m_toolCount;
    //static GUIStyle m_commandStyle = null;

    public static readonly List<Action> LeftToolbarGUI = new List<Action>();
    public static readonly List<Action> RightToolbarGUI = new List<Action>();

    static ToolbarExtender()
    {
        ToolbarCallback.OnToolbarGUILeft = GUILeft;
        ToolbarCallback.OnToolbarGUIRight = GUIRight;
    }

#if UNITY_2019_1_OR_NEWER
    public const float playPauseStopWidth = 140;
#else
    public const float playPauseStopWidth = 100;
#endif

    private static void GUILeft()
    {
        var center = EditorGUIUtility.currentViewWidth / 2;
        GUILayout.BeginHorizontal();
        GUILayout.Space(center - 130 * LeftToolbarGUI.Count - playPauseStopWidth);
        for (int i = 0; i < LeftToolbarGUI.Count; i++)
        {
            LeftToolbarGUI[i].Invoke();
        }

        GUILayout.EndHorizontal();
    }

    private static void GUIRight()
    {
        GUILayout.BeginHorizontal();
        foreach (var handler in RightToolbarGUI)
        {
            handler();
        }

        GUILayout.EndHorizontal();
    }
}

public static class ToolbarCallback
{
    static Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
    static Type m_guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");

#if UNITY_2020_1_OR_NEWER
    static Type m_iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");

    static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
		static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif

    static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    static ScriptableObject m_currentToolbar;

    /// <summary>
    /// Callback for toolbar OnGUI method.
    /// </summary>
    public static Action OnToolbarGUI;

    public static Action OnToolbarGUILeft;
    public static Action OnToolbarGUIRight;

    static ToolbarCallback()
    {
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        // Relying on the fact that the toolbar is a ScriptableObject and gets deleted when layout changes
        if (m_currentToolbar == null)
        {
            // Find the toolbar
            var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
            m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            if (m_currentToolbar != null)
            {
#if UNITY_2021_1_OR_NEWER
                var root = m_currentToolbar.GetType()
                    .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                var rawRoot = root.GetValue(m_currentToolbar);
                var mRoot = rawRoot as VisualElement;
                RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
                RegisterCallback("ToolbarZoneRightAlign", OnToolbarGUIRight);

                void RegisterCallback(string root, Action cb)
                {
                    var toolbarZone = mRoot.Q(root);

                    var parent = new VisualElement()
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexDirection = FlexDirection.Row,
                        }
                    };
                    var container = new IMGUIContainer();
                    container.onGUIHandler += () => { cb?.Invoke(); };
                    parent.Add(container);
                    toolbarZone.Add(parent);
                }
#else
#if UNITY_2020_1_OR_NEWER
					var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

					// Get its visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(windowBackend, null);
#else
					// Get its visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif

					// Get the first child which 'happens' to be the toolbar IMGUIContainer
					var container = (IMGUIContainer) visualTree[0];

					// (Re)attach handler
					var handler = (Action) m_imguiContainerOnGui.GetValue(container);
					handler -= OnGUI;
					handler += OnGUI;
					m_imguiContainerOnGui.SetValue(container, handler);

#endif
            }
        }
    }

    static void OnGUI()
    {
        var handler = OnToolbarGUI;
        if (handler != null)
            handler();
    }
}

#endregion