// This allows faster debugging when need to simualte other platforms by commenting the custom define directive

#if UNITY_EDITOR_WIN
#define OSA_UNITY_EDITOR_WIN
#endif

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using frame8.Logic.Misc.Other;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomParams;
using Com.ForbiddenByte.OSA.CustomAdapters.GridView;
using Com.ForbiddenByte.OSA.Editor.OSAWizard.CustomAdapterConfigurators;
using System.Reflection;

namespace Com.ForbiddenByte.OSA.Editor.OSAWizard
{
    public class InitOSAWindow : BaseOSAWindow<InitOSAWindow.Parameters>
    {
        [SerializeField] private State _State = State.NONE;

        protected override string CompilingScriptsText =>
            base.CompilingScriptsText + (this._WindowParams != null && this._WindowParams.indexOfExistingImplementationToUse == 0 ? "\n(Unity could briefly switch to the code editor and back. This is normal)" : "");

        private Dictionary<Type, Action<BaseParams, RectTransform>> _MapParamBaseTypeToPrefabSetter;
        //bool _VSSolutionReloaded;

        #if OSA_PLAYMAKER
		Dictionary<string, string[]> _Playmaker_MapControllerToSupportedItemPrefabs = new Dictionary<string, string[]>
			{
				{
					"PMPlainArrayController",
					new string[]
					{
						"PMGridPlainArrayItem",
						"PMListPlainArrayItem",
					}
				},

				{
					"PMLazyDataHelperController",
					new string[]
					{
						"PMGridLazyDataHelperItem",
						"PMListLazyDataHelperItem",
						"PMListLazyDataHelperItem_ContentSizeFitter"
					}
				},

				{
					"PMLazyDataHelperXMLController",
					new string[]
					{
						"PMGridLazyDataHelperXMLItem",
						"PMListLazyDataHelperXMLItem"
					}
				}
			};
        #endif

        #region Visual studio solution reload code for windows

        // This prevents another visual studio instance from being opened when the solution was externally modified
        // by automatically presing the 'Reload' button.
        // Some changes were made
        // Original source https://gamedev.stackexchange.com/questions/124320/force-reload-vs-soution-explorer-when-adding-new-c-script-via-unity3d
        #if OSA_UNITY_EDITOR_WIN

        private class NativeMethods
        {
            internal enum ShowWindowEnum
            {
                Hide                 = 0,
                ShowNormal           = 1,
                ShowMinimized        = 2,
                ShowMaximized        = 3,
                Maximize             = 3,
                ShowNormalNoActivate = 4,
                Show                 = 5,
                Minimize             = 6,
                ShowMinNoActivate    = 7,
                ShowNoActivate       = 8,
                Restore              = 9,
                ShowDefault          = 10,
                ForceMinimized       = 11,
            };

            // = Is minimized
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern bool IsIconic(IntPtr handle);

            //[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
            //private static extern IntPtr GetForegroundWindow();

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "FindWindow", SetLastError = true)]
            internal static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern IntPtr FindWindow(string ClassName, string WindowName);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            internal static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern int SetForegroundWindow(IntPtr hwnd);

            // TFG: method added
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
            internal static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, IntPtr wParam, IntPtr lParam);

            // Delegate to filter which windows to include 
            internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

            /// <summary> Find all windows that match the given filter </summary>
            /// <param name="filter"> A delegate that returns true for windows
            ///    that should be returned and false for windows that should
            ///    not be returned </param>
            internal static List<IntPtr> FindWindows(EnumWindowsProc filter)
            {
                var windows = new List<IntPtr>();

                EnumWindows(delegate(IntPtr wnd, IntPtr param)
                    {
                        if (filter(wnd, param))
                            // only add the windows that pass the filter
                            windows.Add(wnd);

                        // but return true here so that we iterate all windows
                        return true;
                    },
                    IntPtr.Zero);

                return windows;
            }

            internal static IntPtr FindWindow(EnumWindowsProc filter)
            {
                var list = FindWindows(filter);
                return list.Count > 0 ? list[0] : IntPtr.Zero;
            }
        }

        private const string WINDOW_CAPTION = "File Modification Detected";

        private static string GetWindowText(IntPtr hwnd)
        {
            var charCount = 512;
            var sb        = new System.Text.StringBuilder(charCount);
            NativeMethods.GetWindowText(hwnd, sb, charCount);
            return sb.ToString();
        }

        private static string GetProjectName()
        {
            var s = Application.dataPath.Split('/');
            return s[s.Length - 2];
        }

        private static string[] GetTargetVSWindowNames(string projectName)
        {
            return new string[]
                {
                    "UnityVS." + projectName + "-csharp - Microsoft Visual Studio",
                    "UnityVS." + projectName + " - Microsoft Visual Studio",
                    projectName + " - Microsoft Visual Studio",
                    projectName + "-csharp - Microsoft Visual Studio",
                }
                ;
        }

        private static bool ContainsTargeVSWindowName(string title)
        {
            var projectName = GetProjectName();
            return Array.Exists(GetTargetVSWindowNames(projectName), pName => title.Contains(pName));
        }

        private static IntPtr GetVisualStudioHWNDIfOpenedWithCurrentProject()
        {
            return NativeMethods.FindWindow((hwnd, __) => ContainsTargeVSWindowName(GetWindowText(hwnd)));
        }

        private static bool IsVisualStudioOpenedWithCurrentProjectAndBusy()
        {
            if (GetVisualStudioHWNDIfOpenedWithCurrentProject() == IntPtr.Zero) return false;

            var vsProcesses = System.Diagnostics.Process.GetProcessesByName("devenv");

            // Exactly one visual studio instance is needed. Otherwise, we can't tell
            if (vsProcesses.Length != 1) return false;

            var process = vsProcesses[0];

            //return process.MainWindowHandle == IntPtr.Zero;
            return !process.Responding;
        }

        private static bool ReloadVisualStudioSolutionIfOpened(out bool canOpenScript)
        {
            canOpenScript = false;

            var projectName             = GetProjectName();
            var projectVisualStudioHWND = GetVisualStudioHWNDIfOpenedWithCurrentProject();
            if (projectVisualStudioHWND == IntPtr.Zero)
            {
                canOpenScript = true;
                return false;
            }

            if (NativeMethods.IsIconic(projectVisualStudioHWND))
            {
                var succ = NativeMethods.ShowWindow(projectVisualStudioHWND, NativeMethods.ShowWindowEnum.Restore);
                if (!succ) Debug.Log("ShowWindow(projectVisualStudioHWND) failed");
            }
            NativeMethods.SetForegroundWindow(projectVisualStudioHWND);

            var maxAttempts                  = 400;
            var ms                           = 5;
            var i                            = 0;
            var fileModificationDetectedHWND = IntPtr.Zero;
            do
            {
                fileModificationDetectedHWND = NativeMethods.FindWindowByCaption(IntPtr.Zero, WINDOW_CAPTION);
                System.Threading.Thread.Sleep(ms);
            } while (fileModificationDetectedHWND == IntPtr.Zero && ++i < maxAttempts);

            if (fileModificationDetectedHWND == IntPtr.Zero) // found no window modification => stay here to edit (since this is the final goal)
            {
                canOpenScript = true;
                return false;
            }

            NativeMethods.SetForegroundWindow(fileModificationDetectedHWND);

            var    buttonPtr = IntPtr.Zero;
            var    ii        = 0;
            string label     = null;
            var    found     = false;
            do
            {
                buttonPtr = NativeMethods.FindWindowEx(fileModificationDetectedHWND, buttonPtr, "Button", null);
                label     = GetWindowText(buttonPtr);
                found     = label == "&Reload" || label.ToLower().Contains("reload");
            } while (!found && ++ii < 5 /*avoid potential infinite loop*/ && buttonPtr != IntPtr.Zero);

            if (found)
                NativeMethods.SendMessage(buttonPtr, 0x00F5 /*BM_CLICK*/, IntPtr.Zero, IntPtr.Zero);
            else
            {
                // shouldn't happen...
            }

            System.Threading.Thread.Sleep(100);

            string winText;
            var    unityHWND = NativeMethods.FindWindow((win, _) => (winText = GetWindowText(win)).Contains("Unity ") && (winText.Contains(".unity - ") || winText.Contains("- Untitled -") /*the current scene is new & not saved*/) && winText.Contains("- " + projectName + " -"));
            if (unityHWND == IntPtr.Zero)
            {
                // TODO
            }
            else
            {
                if (NativeMethods.IsIconic(unityHWND)) NativeMethods.ShowWindow(unityHWND, NativeMethods.ShowWindowEnum.Restore);
                NativeMethods.SetForegroundWindow(unityHWND);
            }

            System.Threading.Thread.Sleep(100);

            //// Send 'Enter'
            //keybd_event(0x0D, 0, 0, 0);
            canOpenScript = true;
            return true;

            //var vsProcesses = System.Diagnostics.Process.GetProcessesByName("devenv");

            //// Exactly one visual studio instance is needed
            //if (vsProcesses.Length != 1)
            //{
            //	Debug.Log("Len=" + vsProcesses.Length);
            //	canOpenScript = true;
            //	return false;
            //}

            //var visualStudioProcess = vsProcesses[0];
            //visualStudioProcess.Refresh();

            //if (visualStudioProcess.MainWindowHandle == IntPtr.Zero)
            //	return false;

            //visualStudioProcess.Refresh();

            //int i = 0;
            ////for (; i < 20 && visualStudioProcess.MainWindowHandle == IntPtr.Zero; ++i)
            ////{
            ////	System.Threading.Thread.Sleep(100);
            ////	visualStudioProcess.Refresh();
            ////}
            //if (visualStudioProcess.MainWindowHandle != IntPtr.Zero)
            //	Debug.Log("i="+ i + ", " + visualStudioProcess.MainWindowHandle + ", " + visualStudioProcess.Handle);

            //if (visualStudioProcess.MainWindowHandle == IntPtr.Zero)
            //	return false;

            //bool windowShown = false;
            //if (IsIconic(visualStudioProcess.MainWindowHandle))
            //{
            //	// The window is minimized. try to restore it before setting focus
            //	ShowWindow(visualStudioProcess.MainWindowHandle, ShowWindowEnum.Restore);

            //	windowShown = true;
            //}

            //var unityProcess = System.Diagnostics.Process.GetCurrentProcess();

            //var sb = GetWindowText(visualStudioProcess.MainWindowHandle);
            //if (sb.Length <= 0)
            //{
            //	if (windowShown && (int)unityProcess.MainWindowHandle != 0)
            //		SetForegroundWindow(unityProcess.MainWindowHandle);

            //	canOpenScript = true;
            //	return false;
            //}
            //Debug.Log("LLL: " + sb + ", " + visualStudioProcess.MainWindowTitle);

            //Debug.Log(sb + "\n" + projectName);
            //if (!sb.Contains(projectName)) // this visual studio doesn't point to our solution => go back
            //{
            //	canOpenScript = true;
            //	if (windowShown)
            //	{
            //		if (unityProcess.MainWindowHandle == IntPtr.Zero) // hidden => show it
            //		{
            //			if (unityProcess.Handle == IntPtr.Zero)
            //				return false;

            //			ShowWindow(unityProcess.Handle, ShowWindowEnum.Restore);
            //		}

            //		if (unityProcess.MainWindowHandle != IntPtr.Zero)
            //			SetForegroundWindow(unityProcess.MainWindowHandle);
            //	}

            //	return false;
            //}

            //SetForegroundWindow(visualStudioProcess.MainWindowHandle);

            //var fileModificationDetectedHWND = FindWindowByCaption(IntPtr.Zero, WINDOW_CAPTION);
            //Debug.Log("fileModificationDetectedHWND="+fileModificationDetectedHWND);
            //if (fileModificationDetectedHWND == IntPtr.Zero) // found no window modification => stay here to edit (since this is the final goal)
            //{
            //	canOpenScript = true;
            //	// Switch back to unity
            //	//var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            //	//if ((int)currentProcess.MainWindowHandle != 0)
            //	//{
            //	//	SetForegroundWindow(currentProcess.MainWindowHandle);
            //	//}
            //	return false;
            //}

            //SetForegroundWindow(fileModificationDetectedHWND);

            //// Send 'Enter'
            //keybd_event(0x0D, 0, 0, 0);
            //canOpenScript = true;
            //return true;
        }
        #endif

        private static bool IfPossible_ReloadVisualStudioSolutionIfOpened(out bool canOpenScript)
        {
            canOpenScript = true;
            #if OSA_UNITY_EDITOR_WIN
            try
            {
                return ReloadVisualStudioSolutionIfOpened(out canOpenScript);
            }
            catch
            {
            }
            #endif
            return false;
        }

        private static bool CheckIfPossible_IsVisualStudioOpenedWithCurrentProjectAndBusy()
        {
            #if OSA_UNITY_EDITOR_WIN
            try
            {
                return IsVisualStudioOpenedWithCurrentProjectAndBusy();
            }
            catch
            {
            }
            #endif
            return false;
        }

        #endregion

        public static bool IsOpen()
        {
            return Resources.FindObjectsOfTypeAll(typeof(InitOSAWindow)).Length > 0;
        }

        public static void Open(Parameters windowParams)
        {
            var windowInstance = GetWindow<InitOSAWindow>();
            windowInstance.InitWithNewParams(windowParams);
        }

        public static ValidationResult Validate(bool checkForWindows, ScrollRect scrollRect, bool allowMultipleScrollbars, Parameters parametersIfAlreadyCreated = null)
        {
            var result = new ValidationResult();
            result.scrollRect = scrollRect;

            if (!BaseValidate(out result.reasonIfNotValid)) return result;

            if (checkForWindows)
            {
                if (CreateOSAWindow.IsOpen())
                {
                    result.reasonIfNotValid = "Creation window already opened";
                    return result;
                }
                if (IsOpen())
                {
                    result.reasonIfNotValid = "Initialization window already opened";
                    return result;
                }
            }

            if (!scrollRect)
            {
                result.reasonIfNotValid = "The provided scrollrect is now null. Maybe it was destroyed meanwhile?";
                return result;
            }

            if (scrollRect.horizontal == scrollRect.vertical)
            {
                result.reasonIfNotValid = "Both 'horizontal' and 'vertical' properties are set to " + scrollRect.horizontal + ". Exactly one needs to be true.";
                return result;
            }

            var existingOSAComponents = scrollRect.GetComponents(typeof(IOSA));
            if (existingOSAComponents.Length > 0)
            {
                var s  = DotNETCoreCompat.ConvertAllToArray(existingOSAComponents, c => " '" + c.GetType().Name + "' ");
                var sc = string.Concat(s);
                result.reasonIfNotValid = "ScrollRect contains " + existingOSAComponents.Length + " existing component(s) extending OSA (" + sc + "). Please remove any existing OSA component before proceeding";
                return result;
            }

            var requiresADirectViewportChild = "The ScrollRect requires a direct, active child named 'Viewport', which will contain the Content";

            var activeChildrenNamedViewport = new List<Transform>();
            foreach (Transform child in scrollRect.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.name == "Viewport") activeChildrenNamedViewport.Add(child);
            }

            if (activeChildrenNamedViewport.Count == 0)
            {
                result.reasonIfNotValid = requiresADirectViewportChild;
                return result;
            }

            if (activeChildrenNamedViewport.Count > 1)
            {
                result.reasonIfNotValid = "The ScrollRect has more than one direct, active child named 'Viewport'";
                return result;
            }
            result.viewportRT = activeChildrenNamedViewport[0] as RectTransform;
            if (!result.viewportRT)
            {
                result.reasonIfNotValid = "The ScrollRect's child 'Viewport' does not have a RectTransform component";
                return result;
            }

            if (!scrollRect.content)
            {
                result.reasonIfNotValid = "The 'content' property is not set";
                return result;
            }

            if (scrollRect.content.parent != result.viewportRT)
            {
                result.reasonIfNotValid = "The 'content' property points to " + scrollRect.content + ", which is not a direct child of the ScrollRect";
                return result;
            }

            if (!scrollRect.content.gameObject.activeSelf)
            {
                result.reasonIfNotValid = "The 'content' property points to a game object that's not active";
                return result;
            }

            if (scrollRect.content.childCount > 0)
            {
                result.reasonIfNotValid = "The 'content' property points to a game object that has some children. The content should have none";
                return result;
            }

            var activeChildrenScrollbars = new List<Scrollbar>();
            foreach (Transform child in scrollRect.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                var sb = child.GetComponent<Scrollbar>();
                if (sb) activeChildrenScrollbars.Add(sb);
            }

            if (activeChildrenScrollbars.Count > 0)
            {
                if (!allowMultipleScrollbars)
                    if (activeChildrenScrollbars.Count > 1)
                    {
                        result.reasonIfNotValid = "Found more than 1 Scrollbar among the ScrollRect's direct, active children";
                        return result;
                    }

                result.scrollbar = activeChildrenScrollbars[0];
                var sbIsHorizontal = result.scrollbar.direction == Scrollbar.Direction.LeftToRight || result.scrollbar.direction == Scrollbar.Direction.RightToLeft;
                if (sbIsHorizontal != scrollRect.horizontal)
                    // Only showing a warning, because the user may intentionally set it this way
                    result.warning = "Init OSA: The scrollbar's direction is " + (sbIsHorizontal ? "horizontal" : "vertical") + ", while the ScrollRect is not. If this was intended, ignore this warning";
            }

            if (parametersIfAlreadyCreated != null)
            {
                #if OSA_PLAYMAKER
				if (parametersIfAlreadyCreated.playmakerSetupStarted)
				{
					if (!parametersIfAlreadyCreated.playmakerController)
					{
						result.reasonIfNotValid = "Controller not selected";
						return result;
					}

					if (!parametersIfAlreadyCreated.itemPrefab)
					{
						result.reasonIfNotValid = "itemPrefab not selected";
						return result;
					}

					if (!parametersIfAlreadyCreated.itemPrefab.GetComponent(typeof(PlayMakerFSM)))
					{
						result.reasonIfNotValid = "PlaymakerFSM not found on item prefab";
						return result;
					}
				}
                #endif
            }

            result.isValid = true;
            return result;
        }

        protected override void InitWithNewParams(Parameters windowParams)
        {
            base.InitWithNewParams(windowParams);

            // Commented: alraedy done in the constructor with paramater
            //_WindowParams.ResetValues();
            this.InitializeAfterParamsSet();
            this._WindowParams.UpdateAvailableOSAImplementations(true);
        }

        protected override void InitWithExistingParams()
        {
            //Debug.Log("InitWithExistingParams: _WindowParams.scrollRect=" + _WindowParams.scrollRect + "_WindowParams.Scrollbar=" + _WindowParams.Scrollbar);
            if (this.ScheduleCloseIfUndefinedState()) return;

            base.InitWithExistingParams();
            this._WindowParams.InitNonSerialized();
            this.InitializeAfterParamsSet();

            var    scriptName = this._WindowParams.generatedScriptNameToUse;
            string fullName;

            if (this._State == State.ATTACH_EXISTING_OSA_PENDING)
            {
                // TODO if have time: create property only for keeping track of the selected template
            }
            else if (this._State == State.RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING) // this represents the previous state, so now PRE changes to POST
            {
                var implementationsString                                                            = this._WindowParams.availableImplementations.Count + ": ";
                foreach (var t in this._WindowParams.availableImplementations) implementationsString += t.Name + ", ";
                implementationsString = implementationsString.Substring(0, implementationsString.Length - 2);

                if (string.IsNullOrEmpty(scriptName))
                    throw new OSAException("Internal error: _WindowParams.generatedScriptNameToUse is null after recompilation; " + "availableImplementations=" + implementationsString);
                else if ((fullName = this.GetFullNameIfScriptExists(scriptName)) == null)
                    throw new OSAException("Internal error: Couldn't find the type's fullName for script '" + scriptName + "'. Did you delete the newly created script?\n " + "availableImplementations=" + implementationsString);
                else
                {
                    // Commented this is done in initInFirstOnGUI
                    //_WindowParams.UpdateAvailableOSAImplementations();
                    var index = this._WindowParams.availableImplementations.FindIndex(t => t.FullName == fullName);
                    if (index == -1) throw new OSAException("Internal error: Couldn't find index of new implementation of '" + scriptName + "': " + "availableImplementations=" + this._WindowParams.availableImplementations.Count + ", " + "given fullName=" + fullName);

                    this._WindowParams.indexOfExistingImplementationToUse = index + 1; // skip the <generate> option
                }

                //_State = State.POST_RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING;
                //_State = State.POST_RECOMPILATION_RELOAD_SOLUTION_PENDING;
                this._State = State.NONE;

                // Switch to visual studio to fake-press Reload due to solution being changed
                //if (_WindowParams.openForEdit)// && _WindowParams.indexOfExistingImplementationToUse > 0)
                //ReloadVisualStudioSolutionIfOpenedAndIfPossible();
                //bool b;
                //ReloadVisualStudioSolutionIfOpenedAndIfPossible(out b);
            }
        }

        protected override void GetErrorAndWarning(out string error, out string warning)
        {
            var vr = Validate(
                false,
                this._WindowParams == null ? null : this._WindowParams.scrollRect,
                this._WindowParams == null ? false : this._WindowParams.allowMultipleScrollbars,
                this._WindowParams
            );
            error   = vr.reasonIfNotValid;
            warning = vr.warning;
            // TODO check if prefab is allowed and if the prefab is NOT the viewport, scrollrect content, scrollbar
        }

        protected override void UpdateImpl()
        {
            if (this._State != State.CLOSE_PENDING) this.ScheduleCloseIfUndefinedState(); // do not return in case of true, since the close code is below

            switch (this._State)
            {
                case State.CLOSE_PENDING:
                    this._State = State.NONE;
                    this.Close();
                    break;

                case State.RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING:
                    // TODO think about if need to wait or something. maybe import/refresh assets
                    break;

                //case State.POST_RECOMPILATION_RELOAD_SOLUTION_PENDING:
                //	//bool canOpenScript;
                //	//if (ReloadVisualStudioSolutionIfOpenedAndIfPossible(out canOpenScript) || canOpenScript)
                //	if (!CheckIfPossible_IsVisualStudioOpenedWithCurrentProjectAndBusy())
                //		_State = State.NONE;
                //	break;

                case State.ATTACH_EXISTING_OSA_PENDING:
                    //case State.POST_RECOMPILATION_ATTACH_GENERATED_OSA_PENDING:
                    if (!this._WindowParams.ImplementationsInitialized) throw new OSAException("wat. it shold've been initialized in initwithexistingparams");

                    // Don't disable the existing scrollbar, if it's to be reused
                    if (this._WindowParams.useScrollbar && !this._WindowParams.GenerateDefaultScrollbar && this._WindowParams.MiscScrollbarWasAlreadyPresent && this._WindowParams.ScrollbarRT)
                        this.ConfigureScrollView(this._WindowParams.scrollRect, this._WindowParams.viewportRT, this._WindowParams.itemPrefab, this._WindowParams.ScrollbarRT);
                    else
                        this.ConfigureScrollView(this._WindowParams.scrollRect, this._WindowParams.viewportRT, this._WindowParams.itemPrefab);
                    Canvas.ForceUpdateCanvases();
                    if (this._WindowParams.useScrollbar && this._WindowParams.GenerateDefaultScrollbar) this._WindowParams.scrollbar = this.InstantiateDefaultOSAScrollbar();
                    var t                                                                                                            = this._WindowParams.ExistingImplementationToUse;
                    //Debug.Log(t);
                    this._WindowParams.ScrollRectRT.gameObject.AddComponent(t);
                    #if OSA_PLAYMAKER
					if (_WindowParams.playmakerSetupStarted)
						(_WindowParams.ScrollRectRT.GetComponent(typeof(IOSA)) as MonoBehaviour).enabled = false; // need to start as disabled for playmaker
                    #endif
                    // Selecting the game object is important. Unity starts the initial serialization of a script (and thus, setting a valid value to the OSA's _Params field)
                    // only if its inspector is shown
                    Selection.activeGameObject = this._WindowParams.ScrollRectRT.gameObject;

                    this._State = State.POST_ATTACH_CONFIGURE_OSA_PENDING;
                    break;

                case State.POST_ATTACH_CONFIGURE_OSA_PENDING:
                    if (this._WindowParams == null || !this._WindowParams.ScrollRectRT)
                    {
                        this._State = State.CLOSE_PENDING;
                        break;
                    }

                    var iAdapter = this._WindowParams.ScrollRectRT.GetComponent(typeof(IOSA)) as IOSA;
                    if (iAdapter == null)
                    {
                        this._State = State.CLOSE_PENDING;
                        break;
                    }

                    if (iAdapter.BaseParameters == null) break;

                    var requiredInterfaceType = this._WindowParams.GetOnlyAllowImplementationsHavingInterface();
                    var byConfigurator        = false;
                    if (requiredInterfaceType != null)
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.IsAbstract) continue;

                            if (!OSAUtil.DotNETCoreCompat_IsAssignableFrom(typeof(ICustomAdapterConfigurator), type)) continue;

                            var attr = Attribute.GetCustomAttribute(type, typeof(CustomAdapterConfiguratorAttribute)) as CustomAdapterConfiguratorAttribute;
                            if (attr == null) continue;

                            if (attr.ConfiguredType != requiredInterfaceType) continue;

                            var conf = Activator.CreateInstance(type) as ICustomAdapterConfigurator;
                            conf.ConfigureNewAdapter(iAdapter);
                            byConfigurator = true;
                            break;
                        }

                    if (!byConfigurator)
                    {
                        if (this._WindowParams.scrollbar) this.ConfigureScrollbar(iAdapter);

                        #if OSA_PLAYMAKER
						if (_WindowParams.playmakerSetupStarted)
							PostAttachConfigurePlaymakerSetup();

                        #endif

                        this.OnOSAParamsInitialized(iAdapter);
                    }

                    if (this._WindowParams.openForEdit)
                    {
                        var monoScript = MonoScript.FromMonoBehaviour(iAdapter.AsMonoBehaviour);
                        var success    = AssetDatabase.OpenAsset(monoScript);
                        if (success)
                        {
                            //	ReloadVisualStudioSolutionIfOpenedAndIfPossible();
                        }
                        else
                            Debug.Log("OSA: Could not open '" + iAdapter.GetType().Name + "' in external code editor");
                    }

                    this._State = State.PING_SCROLL_RECT_PENDING;
                    break;

                //case State.POST_ATTACH_AND_POST_PING_CONFIGURE_OSA_PARAMS_PENDING:
                //	if (ConfigureOSAParamsPostAttachAndPostPing())
                //		_State = State.CLOSE_PENDING;
                //	break;
            }
        }

        protected override void OnGUIImpl()
        {
            if (this._State != State.CLOSE_PENDING && this.ScheduleCloseIfUndefinedState()) return;

            switch (this._State)
            {
                case State.PING_SCROLL_RECT_PENDING: // can only be done in OnGUI because EditorStyles are used by EditorGUIUtility.PingObject
                    if (this._WindowParams.scrollRect)
                    {
                        this.PingAndSelect(this._WindowParams.ScrollRectRT);
                        //ShowNotification(new GUIContent("OSA: Initialized"));

                        var msg               = "OSA: Initialized";
                        var shownNotification = false;
                        try
                        {
                            var inspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
                            var allInspectors       = Resources.FindObjectsOfTypeAll(inspectorWindowType);
                            if (allInspectors != null && allInspectors.Length == 1)
                            {
                                (allInspectors[0] as EditorWindow).ShowNotification(new(msg));
                                shownNotification = true;
                            }
                        }
                        catch
                        {
                        }

                        if (!shownNotification) Debug.Log(msg);

                        if (this._WindowParams.destroyScrollRectAfter) DestroyImmediate(this._WindowParams.scrollRect);
                    }
                    else
                        Debug.Log("OSA: Unexpected state: the scrollrect was destroyed meanwhile. Did you delete it from the scene?");

                    this._State = State.CLOSE_PENDING;

                    break;

                case State.RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING:
                    EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.BeginHorizontal(this._BoxGUIStyle);
                    {
                        var scriptName                                                                                                   = "(???)";
                        if (this._WindowParams != null && !string.IsNullOrEmpty(this._WindowParams.generatedScriptNameToUse)) scriptName = this._WindowParams.generatedScriptNameToUse;
                        scriptName = "'" + scriptName + "'";

                        var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                        EditorGUILayout.LabelField("Waiting for script " + scriptName + " to be generated & attached...", style);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                    EditorGUI.EndDisabledGroup();

                    break;

                case State.POST_ATTACH_CONFIGURE_OSA_PENDING:
                    EditorGUI.BeginDisabledGroup(true);
                {
                    var s = this._WindowParams != null && this._WindowParams.scrollRect != null ? "(named '" + this._WindowParams.scrollRect.name + "')" : "";
                    EditorGUILayout.LabelField(
                        "If this window stays open for too long, please select the newly initialized ScrollView in hierarchy " + s + "\n" + "This is done automatically, but it fails if you have locked the inspector window.\n" + "This also happens if your code editor is open with some pending changes and a 'Solution changed externally' " + "dialog box is shown - in this case, switch to it and select 'Reload solution'",
                        GUILayout.Height(100f)
                    );
                }
                    EditorGUI.EndDisabledGroup();

                    break;

                case State.PING_PREFAB_PENDING:
                    if (this._WindowParams != null && this._WindowParams.itemPrefab) this.PingAndSelect(this._WindowParams.itemPrefab);

                    this._State = State.NONE;
                    goto case State.NONE;

                case State.NONE:
                    if (this._WindowParams.ImplementationsInitialized) // wait for params intialization
                        this.DrawDefaultGUI();
                    break; // continue drawing normally

                default: break;
            }
        }

        protected override void ConfigureScrollView(ScrollRect scrollRect, RectTransform viewport, params Transform[] objectsToSkipDisabling)
        {
            base.ConfigureScrollView(scrollRect, viewport, objectsToSkipDisabling);

            scrollRect.enabled = false;
            if (!this._WindowParams.destroyScrollRectAfter) Debug.Log("OSA: Starting with v4.0, the ScrollRect component is not needed anymore. It was disabled and you should remove it to not interfere with OSA");
        }

        protected override void OnSubmitClicked()
        {
            // Commented: this is already checked and if there's and error, the submit button is disabled
            //// Validate again, to make sure the hierarchy wasn't modified
            //var validationRes = Validate(_WindowParams.scrollRect);
            //if (!validationRes.isValid)
            //{
            //	DemosUtil.ShowCouldNotExecuteCommandNotification(this);
            //	Debug.Log("OSA: Could not initialize (the hierarchy was probably modified): " + validationRes.reasonIfNotValid);
            //	return;
            //}

            var generateNew = this._WindowParams.ExistingImplementationToUse == null;
            if (generateNew)
            {
                if (string.IsNullOrEmpty(this._WindowParams.generatedScriptNameToUse))
                {
                    CWiz.ShowNotification("Invalid script name", true, this);
                    return;
                }

                var alreadyExistingTypeFullName = this.GetFullNameIfScriptExists(this._WindowParams.generatedScriptNameToUse);
                if (alreadyExistingTypeFullName != null)
                {
                    CWiz.ShowNotification("Invalid script name. A script already exists as '" + alreadyExistingTypeFullName + "'", true, this);
                    return;
                }

                var genScriptDirectoryPath = Application.dataPath + "/Scripts";
                var genScriptPath          = genScriptDirectoryPath + "/" + this._WindowParams.generatedScriptNameToUse + ".cs";

                if (File.Exists(genScriptPath))
                {
                    CWiz.ShowNotification("A script named '" + this._WindowParams.generatedScriptNameToUse + "' already exists", true, this);
                    return;
                }

                if (!Directory.Exists(genScriptDirectoryPath))
                    try
                    {
                        Directory.CreateDirectory(genScriptDirectoryPath);
                    }
                    catch
                    {
                        Debug.LogError("OSA: Could not create directory: " + genScriptDirectoryPath);
                        return;
                    }

                var templateText = this._WindowParams.TemplateToUseForNewScript;

                // Replace the class name with the chosen one
                templateText = templateText.Replace(
                    CWiz.TEMPLATE_TEXT_CLASSNAME_PREFIX + this._WindowParams.availableTemplatesNames[this._WindowParams.IndexOfTemplateToUseForNewScript],
                    CWiz.TEMPLATE_TEXT_CLASSNAME_PREFIX + this._WindowParams.generatedScriptNameToUse
                );

                // Add header
                templateText = this._WindowParams.TemplateHeader + templateText;

                // Create unique namespace. Even if we're checking for any existing monobehaviour with the same name before creating a new one, 
                // the params, views holder and the model classes still have the same name
                CWiz.ReplaceTemplateDefaultNamespaceWithUnique(ref templateText);

                // Create, import and wait for recompilation
                try
                {
                    File.WriteAllText(genScriptPath, templateText);
                }
                catch
                {
                    CWiz.ShowCouldNotExecuteCommandNotification(this);
                    Debug.LogError("OSA: Could not create file: " + genScriptPath);
                    return;
                }
                // ImportAssetOptions
                //var v = AssetImporter.GetAtPath(FileUtil.GetProjectRelativePath(genScriptPath));
                //Debug.Log("v.GetInstanceID()" + v.GetInstanceID());
                //Debug.Log(FileUtil.GetProjectRelativePath(genScriptPath)+", " + genScriptPath);
                //AssetDatabase.ImportAsset(FileUtil.GetProjectRelativePath(genScriptPath));
                //AssetDatabase.ImportAsset(FileUtil.GetProjectRelativePath(genScriptPath), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(FileUtil.GetProjectRelativePath(genScriptPath));
                //AssetDatabase.Refresh();
                // Will be executed in Update, but after re-compilation
                // TODO check
                this._State = State.RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING;
            }
            else
                // Will be executed in the next Update
                this._State = State.ATTACH_EXISTING_OSA_PENDING;
        }

        private bool ScheduleCloseIfUndefinedState()
        {
            if (!this._WindowParams.scrollRect)
            {
                if (this._State != State.CLOSE_PENDING)
                {
                    this._State = State.CLOSE_PENDING;
                    Debug.Log("OSA wizard closed because the ScrollRect was destroyed or the scene changed");
                }
                //DemosUtil.ShowNotification("OSA wizard closed because the ScrollRect was destroyed", false, false);
                //DestroyImmediate(this);
                return true;
            }

            return false;
        }

        private void InitializeAfterParamsSet()
        {
            this._MapParamBaseTypeToPrefabSetter                               = new();
            this._MapParamBaseTypeToPrefabSetter[typeof(GridParams)]           = (parms, pref) => (parms as GridParams).Grid.CellPrefab      = pref;
            this._MapParamBaseTypeToPrefabSetter[typeof(BaseParamsWithPrefab)] = (parms, pref) => (parms as BaseParamsWithPrefab).ItemPrefab = pref;
        }

        #if OSA_PLAYMAKER
		bool IsPlaymakerImplementation(out GameObject[] controllerPrefabs, out bool isGrid)
		{
			controllerPrefabs = null;
			isGrid = false;

			if (_WindowParams.ExistingImplementationToUse == null)
				return false;

			var nam = _WindowParams.ExistingImplementationToUse.Name;
			//if (nam == typeof(Playmaker.Adapters.PlaymakerGridOSA).Name)
			if (nam == "PlaymakerGridOSA") // using string directly, as we can't reference the playmaker supporting code
			{
				isGrid = true;
			}
			//else if (nam == typeof(Playmaker.Adapters.PlaymakerListOSA).Name)
			else if (nam == "PlaymakerListOSA") // using string directly, as we can't reference the playmaker supporting code
			{
			}
			else
				return false;

			string playmakerControllerPrefabsFolder = CWiz.TEMPLATES_PLAYMAKER_CONTROLLER_PREFABS_RESPATH;
			controllerPrefabs = Resources.LoadAll<GameObject>(playmakerControllerPrefabsFolder);
			
			return true;
		}
        #endif

        private RectTransform BasicTemplate_GetItemPrefabResourceForParamsBaseType(Type type)
        {
            string nameToUse;
            if (type == typeof(GridParams))
                nameToUse = Parameters.GRID_TEMPLATE_NAME;
            else if (type == typeof(BaseParamsWithPrefab))
                nameToUse = Parameters.LIST_TEMPLATE_NAME;
            //else if (type == typeof(TableParams))
            //	nameToUse = Parameters.TABLE_TEMPLATE_NAME;
            else
                return null;

            //string prefabNameWithoutAdapter = nameToUse.Replace("Adapter", "");

            var go = Resources.Load<GameObject>(CWiz.GetExampleItemPrefabResPath(nameToUse));
            if (!go) return null;

            return go.transform as RectTransform;
        }

        private void DrawDefaultGUI()
        {
            this.DrawSectionTitle("Implement OSA");

            // Game Object to initialize
            this.DrawObjectWithPath(this._BoxGUIStyle, "ScrollRect to initialize", this._WindowParams.scrollRect == null ? null : this._WindowParams.scrollRect.gameObject);

            // Scrollbar
            EditorGUI.BeginDisabledGroup(!this._WindowParams.canChangeScrollbars);
            EditorGUILayout.BeginVertical(this._BoxGUIStyle);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Scrollbar", EditorStyles.boldLabel, CWiz.LABEL_WIDTH);
                    this._WindowParams.useScrollbar = EditorGUILayout.Toggle(this._WindowParams.useScrollbar, CWiz.VALUE_WIDTH);
                }
                EditorGUILayout.EndHorizontal();

                if (this._WindowParams.useScrollbar)
                {
                    EditorGUILayout.Space();

                    if (this._WindowParams.MiscScrollbarWasAlreadyPresent)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Generate scrollbar", CWiz.LABEL_WIDTH);
                            this._WindowParams.overrideMiscScrollbar = EditorGUILayout.Toggle("", this._WindowParams.overrideMiscScrollbar, CWiz.VALUE_WIDTH);
                        }
                        EditorGUILayout.EndHorizontal();
                        this._WindowParams.scrollbar.gameObject.SetActive(!this._WindowParams.overrideMiscScrollbar);
                    }

                    if (!this._WindowParams.MiscScrollbarWasAlreadyPresent || this._WindowParams.overrideMiscScrollbar)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Scrollbar position", CWiz.LABEL_WIDTH);
                            this._WindowParams.isScrollbarPosAtStart =
                                GUILayout.SelectionGrid(this._WindowParams.isScrollbarPosAtStart ? 0 : 1,
                                    this._WindowParams.isHorizontal ? new string[] { "Top", "Bottom" } : new string[] { "Left", "Right" },
                                    2,
                                    CWiz.VALUE_WIDTH
                                )
                                == 0
                                    ? true
                                    : false;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (this._WindowParams.canChangeScrollbars && this._WindowParams.MiscScrollbarWasAlreadyPresent)
                        EditorGUILayout.HelpBox(this._WindowParams.overrideMiscScrollbar
                                ? "'" + this._WindowParams.scrollbar.name + "' was disabled. The default scrollbar will be generated"
                                : "An existing scrollbar was found ('" + this._WindowParams.scrollbar.name + "') and it'll be automatically linked to OSA. " + "If you want to disable it & generate the default one instead, tick 'Generate scrollbar'",
                            MessageType.Info
                        );
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // OSA implementation
            EditorGUILayout.BeginVertical(this._BoxGUIStyle);
            {
                EditorGUILayout.LabelField("Script to use", EditorStyles.boldLabel, CWiz.LABEL_WIDTH);

                EditorGUILayout.Space();

                var indexOfExistingImplBefore = this._WindowParams.indexOfExistingImplementationToUse;

                // Implementation to use
                #if OSA_PLAYMAKER
				EditorGUI.BeginDisabledGroup(_WindowParams.playmakerSetupStarted);
                #else
                EditorGUI.BeginDisabledGroup(false);
                #endif
                {
                    // Exclude examples/demos toggle
                    EditorGUI.BeginDisabledGroup(!this._WindowParams.allowChoosingExampleImplementations);
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Exclude examples/demos", CWiz.LABEL_WIDTH);
                        var before = this._WindowParams.excludeExampleImplementations;
                        this._WindowParams.excludeExampleImplementations = EditorGUILayout.Toggle(this._WindowParams.excludeExampleImplementations, CWiz.VALUE_WIDTH);
                        if (this._WindowParams.excludeExampleImplementations != before) this._WindowParams.UpdateAvailableOSAImplementations(true);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                    if (!this._WindowParams.excludeExampleImplementations) EditorGUILayout.HelpBox("Using the provided example/demo scripts have no use in production. Intead, use them as a guide for implementing your own", MessageType.Warning);

                    this._WindowParams.indexOfExistingImplementationToUse =
                        EditorGUILayout.Popup(this._WindowParams.indexOfExistingImplementationToUse, this._WindowParams.availableImplementationsStringsOptions, GUILayout.Width(CWiz.VALUE_WIDTH2_FLOAT));
                }
                EditorGUI.EndDisabledGroup();

                // When the user manually switches from generate to existing, don't keep the value of "openForEdit"
                if (indexOfExistingImplBefore != this._WindowParams.indexOfExistingImplementationToUse && this._WindowParams.indexOfExistingImplementationToUse > 0) this._WindowParams.openForEdit = false;

                // OSA template to use if need to generate new implementation. 0 = <Create new>
                if (this._WindowParams.indexOfExistingImplementationToUse == 0)
                {
                    if (this._WindowParams.availableTemplates.Length == 0)
                        EditorGUILayout.HelpBox("There are no templates in */Resources/" + CWiz.TEMPLATE_SCRIPTS_RESPATH + ". Did you manually delete them? If not, this is a Unity bug and you can solve it by re-opening Unity", MessageType.Error);
                    else
                    {
                        this._WindowParams.IndexOfTemplateToUseForNewScript =
                            GUILayout.SelectionGrid(this._WindowParams.IndexOfTemplateToUseForNewScript, this._WindowParams.availableTemplatesNames, 3, GUILayout.MinWidth(CWiz.VALUE_WIDTH_FLOAT));

                        // Script name
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Generated script name", CWiz.LABEL_WIDTH);
                            this._WindowParams.generatedScriptNameToUse = EditorGUILayout.TextField(this._WindowParams.generatedScriptNameToUse, CWiz.VALUE_WIDTH);

                            // Name validation
                            var filteredChars = new List<char>(this._WindowParams.generatedScriptNameToUse.ToCharArray());
                            filteredChars.RemoveAll(c => !char.IsLetterOrDigit(c));
                            while (filteredChars.Count > 0 && char.IsDigit(filteredChars[0])) filteredChars.RemoveAt(0);
                            this._WindowParams.generatedScriptNameToUse = new(filteredChars.ToArray());
                        }
                        EditorGUILayout.EndHorizontal();

                        // Open for edit toggle
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Open for edit", CWiz.LABEL_WIDTH);
                            this._WindowParams.openForEdit = EditorGUILayout.Toggle(this._WindowParams.openForEdit, CWiz.VALUE_WIDTH);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    if (this._WindowParams.availableImplementations == null)
                    {
                        // TODO: shouldn't happen though
                    }
                    else
                    {
                        // Prefab, if applicable
                        var implToUse  = this._WindowParams.ExistingImplementationToUse;
                        var paramsType = this.GetBaseTypeOfPrefabContainingParams(implToUse);
                        if (paramsType == null)
                        {
                            // Having an interface enforced means there's a specific configurator that'll take care of prefab assigning and such
                            if (this._WindowParams.GetOnlyAllowImplementationsHavingInterface() == null)
                                EditorGUILayout.HelpBox(
                                    "Couldn't detect the params of '" + implToUse.Name + "' to set the prefab. Make sure to manually set it after, in inspector or (advanced) in code",
                                    MessageType.Warning
                                );
                        }
                        else
                        {
                            var hidePrefabNotice = this._WindowParams.itemPrefab != null;
                            #if OSA_PLAYMAKER
							GameObject[] playmakerControllerPrefabs;
							bool isGridPlaymaker;
							bool isPlaymakerImpl = IsPlaymakerImplementation(out playmakerControllerPrefabs, out isGridPlaymaker);
							hidePrefabNotice = hidePrefabNotice || isPlaymakerImpl;
                            #endif

                            EditorGUILayout.HelpBox(
                                "Params are of type '" + paramsType.Name + "', which contain a prefab property" + (hidePrefabNotice ? ":" : ". If you don't set it here, make sure to do it after, through inspector or (advanced) in code"),
                                MessageType.Info
                            );

                            EditorGUILayout.BeginHorizontal();
                            {
                                #if OSA_PLAYMAKER
								if (isPlaymakerImpl)
								{
									if (_WindowParams.playmakerSetupStarted)
									{
										if (!_WindowParams.playmakerController)
										{
											CWiz.ShowCouldNotExecuteCommandNotification(this);
											Debug.LogError("OSA: playmakerController externally deleted. Closing... ");
											Close();
											return;
										}

										EditorGUILayout.HelpBox(
											"Using Playmaker example controller '" + _WindowParams.playmakerController.name + "'", MessageType.Info);

										if (!_WindowParams.itemPrefab)
											DrawPlaymakerItemPrefabsForCurrentController(isGridPlaymaker);
									}
									else
									{
										DrawPlaymakerControllers(playmakerControllerPrefabs, isGridPlaymaker);
									}
								}
								else
                                #endif
                                {
                                    EditorGUILayout.LabelField("Item prefab", CWiz.LABEL_WIDTH);
                                    this._WindowParams.itemPrefab = EditorGUILayout.ObjectField(this._WindowParams.itemPrefab, typeof(RectTransform), true, CWiz.VALUE_WIDTH) as RectTransform;

                                    if (!this._WindowParams.itemPrefab)
                                    {
                                        var itemPreabRes = this.BasicTemplate_GetItemPrefabResourceForParamsBaseType(paramsType);
                                        if (itemPreabRes) this.DrawItemPrefabs("Generate example for ", new GameObject[] { itemPreabRes.gameObject });
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Create button
            this.DrawSubmitButon(this._WindowParams.ExistingImplementationToUse == null ? "Step 1/2: Generate script" : "Step 2/2: Initialize");
        }

        #if OSA_PLAYMAKER
		void DrawPlaymakerControllers(GameObject[] controllerPrefabs, bool isGrid)
		{
			if (controllerPrefabs == null || controllerPrefabs.Length == 0)
			{
				EditorGUILayout.HelpBox("No controllers found for this Implementation. Choose another one.", MessageType.Warning);
				return;
			}

			EditorGUILayout.BeginVertical();
			int drawn = 0;
			for (int i = 0; i < controllerPrefabs.Length; i++)
			{
				var p = controllerPrefabs[i];

				var itemPrefabsAvailable = GetItemPrefabsAvailableForPlaymakerController(p.gameObject, isGrid);
				if (itemPrefabsAvailable.Count == 0)
					continue;

				string t = "Generate " + p.name.Replace("(Clone)", "");
				float w = GUI.skin.button.CalcSize(new GUIContent(t)).x + 10f;
				//float w = Mathf.Min(200f, Mathf.Max(350f, t.Length * 2))
				var buttonRect = EditorGUILayout.GetControlRect(GUILayout.Width(w), GUILayout.Height(20f));
				if (GUI.Button(buttonRect, t))
				{
					var instanceRT = (Instantiate(p) as GameObject).GetComponent<RectTransform>();
					instanceRT.name = instanceRT.name.Replace("(Clone)", "");
					instanceRT.SetParent(_WindowParams.ScrollRectRT.parent, false);
					instanceRT.SetAsLastSibling();
					instanceRT.SetSiblingIndex(_WindowParams.ScrollRectRT.GetSiblingIndex() + 1);
					_WindowParams.playmakerSetupStarted = true;
					_WindowParams.playmakerController = instanceRT;
				}
				++drawn;
			}
			EditorGUILayout.EndVertical();
			if (drawn == 0)
			{
				EditorGUILayout.HelpBox("Found controllers for this Implementation, but none of them has compatible item prefabs for it. Choose another one implementation.", MessageType.Warning);
			}
		}

		void DrawPlaymakerItemPrefabsForCurrentController(bool isGrid)
		{
			var filtered = GetItemPrefabsAvailableForPlaymakerController(_WindowParams.playmakerController.gameObject, isGrid);

			DrawItemPrefabs("Generate example prefab: ", filtered);
		}

		List<GameObject> GetItemPrefabsAvailableForPlaymakerController(GameObject controllerPrefab, bool isGrid)
		{
			var filtered = new List<GameObject>();

			string[] itemPrefabsForThisController;
			if (!_Playmaker_MapControllerToSupportedItemPrefabs.TryGetValue(controllerPrefab.name, out itemPrefabsForThisController))
				return filtered;

			string toRemoveThoseNotContainingThis;
			string loadPath;

			if (isGrid)
			{
				toRemoveThoseNotContainingThis = "PMGrid";
				loadPath = CWiz.TEMPLATES_PLAYMAKER_GRID_ITEM_PREFABS_RESPATH;
			}
			else
			{
				loadPath = CWiz.TEMPLATES_PLAYMAKER_LIST_ITEM_PREFABS_RESPATH;
				toRemoveThoseNotContainingThis = "PMList";
			}
			filtered.AddRange(Resources.LoadAll<GameObject>(loadPath));
			filtered.RemoveAll(itemPref => Array.IndexOf(itemPrefabsForThisController, itemPref.name) == -1 || !itemPref.name.Contains(toRemoveThoseNotContainingThis));

			return filtered;
		}
        #endif

        private void DrawItemPrefabs(string headline, IList<GameObject> itemPrefabs)
        {
            EditorGUILayout.BeginVertical();
            for (var i = 0; i < itemPrefabs.Count; i++)
            {
                var itemPrefab = itemPrefabs[i];

                var t = headline + itemPrefab.name.Replace("(Clone)", "");
                var w = GUI.skin.button.CalcSize(new(t)).x + 10f;
                //float w = Mathf.Min(200f, Mathf.Max(350f, t.Length * 2))
                var buttonRect = EditorGUILayout.GetControlRect(GUILayout.Width(w), GUILayout.Height(20f));
                if (GUI.Button(buttonRect, t))
                {
                    var instanceRT = (Instantiate(itemPrefab) as GameObject).GetComponent<RectTransform>();
                    instanceRT.name = instanceRT.name.Replace("(Clone)", "");
                    instanceRT.SetParent(this._WindowParams.ScrollRectRT, false);
                    instanceRT.SetAsLastSibling();
                    this._WindowParams.itemPrefab = instanceRT;
                    this._State                   = State.PING_PREFAB_PENDING;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private string GetFullNameIfScriptExists(string scriptName, bool fullnameProvided = false)
        {
            //var scriptNameOrig = scriptName;
            scriptName = scriptName.ToLower();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract) continue;
                if (!type.IsClass) continue;
                if (type.IsGenericType) continue;
                if (type.IsNotPublic) continue;
                if (!OSAUtil.DotNETCoreCompat_IsAssignableFrom(typeof(MonoBehaviour), type)) continue;

                if (fullnameProvided)
                {
                    if (type.FullName.ToLower() == scriptName) return type.FullName;
                }
                else
                {
                    if (type.Name.ToLower() == scriptName) return type.FullName;
                }
            }

            return null;
        }

        private Type GetBaseTypeOfPrefabContainingParams(Type derivedType)
        {
            var curDerivedType              = derivedType;
            var prefabContainingParamsTypes = new List<Type>(this._MapParamBaseTypeToPrefabSetter.Keys);
            while (curDerivedType != null && curDerivedType != typeof(object))
            {
                var genericArguments = curDerivedType.GetGenericArguments();
                var tParams          = new List<Type>(genericArguments).Find(t => OSAUtil.DotNETCoreCompat_IsAssignableFrom(typeof(BaseParams), t));

                if (tParams != null)
                {
                    var type = prefabContainingParamsTypes.Find(t => CWiz.IsSubclassOfRawGeneric(t, tParams));
                    if (type != null) return type;
                }

                curDerivedType = curDerivedType.BaseType;
            }

            return null;
        }

        private bool SetPrefab(IOSA iAdapter, RectTransform prefab)
        {
            var type = this.GetBaseTypeOfPrefabContainingParams(iAdapter.GetType());
            if (type == null) return false;

            this._MapParamBaseTypeToPrefabSetter[type](iAdapter.BaseParameters, prefab);

            return true;
        }

        private Scrollbar InstantiateDefaultOSAScrollbar()
        {
            var respath      = this._WindowParams.isHorizontal ? CWiz.HOR_SCROLLBAR_RESPATH : CWiz.VERT_SCROLLBAR_RESPATH;
            var sbPrefab     = Resources.Load<GameObject>(respath);
            var sbInstanceRT = (Instantiate(sbPrefab) as GameObject).transform as RectTransform;
            sbInstanceRT.name = sbInstanceRT.name.Replace("(Clone)", "");
            sbInstanceRT.SetParent(this._WindowParams.ScrollRectRT, false);

            return sbInstanceRT.GetComponent<Scrollbar>();
        }

        private void ConfigureScrollbar(IOSA iAdapter)
        {
            OSAUtil.ConfigureDinamicallyCreatedScrollbar(this._WindowParams.scrollbar, iAdapter, this._WindowParams.viewportRT);

            if (this._WindowParams.checkForMiscComponents) this.DisableOrNotifyAboutMiscComponents(this._WindowParams.scrollbar.gameObject, "scrollbar", typeof(ScrollbarFixer8), typeof(Scrollbar));

            //if (_WindowParams.ScrollbarIsFromOSAPrefab)
            if (this._WindowParams.GenerateDefaultScrollbar)
                // The scrollbar is initially placed at end if it's from the default scrollbar prefab 
                if (this._WindowParams.isScrollbarPosAtStart)
                {
                    var sbInstanceRT = this._WindowParams.ScrollbarRT;
                    var newAnchPos   = sbInstanceRT.anchoredPosition;
                    var i            = 1 - this._WindowParams.Hor0_Vert1;
                    var v            = sbInstanceRT.anchorMin;
                    v[i]                          = 1f - v[i];
                    sbInstanceRT.anchorMin        = v;
                    v                             = sbInstanceRT.anchorMax;
                    v[i]                          = 1f - v[i];
                    sbInstanceRT.anchorMax        = v;
                    v                             = sbInstanceRT.pivot;
                    v[i]                          = 1f - v[i];
                    sbInstanceRT.pivot            = v;
                    newAnchPos[i]                 = -newAnchPos[i];
                    sbInstanceRT.anchoredPosition = newAnchPos;
                }
        }

        private void PingAndSelect(Component c)
        {
            Selection.activeGameObject = c.gameObject;
            EditorGUIUtility.PingObject(c);
        }

        private void OnOSAParamsInitialized(IOSA iAdapter)
        {
            //if (_WindowParams == null || _WindowParams.scrollRect == null)
            //	return true;

            //var iAdapter = _WindowParams.scrollRect.GetComponent(typeof(IOSA)) as IOSA;
            //if (iAdapter == null)
            //	return true; // shouldn't happen

            var baseParams = iAdapter.BaseParameters;
            //if (baseParams == null)
            //	return false; // wait until params initialized

            baseParams.ContentSpacing = 10f;
            baseParams.ContentPadding = new(10, 10, 10, 10);

            baseParams.Content     = this._WindowParams.scrollRect.content;
            baseParams.Orientation = this._WindowParams.isHorizontal ? BaseParams.OrientationEnum.HORIZONTAL : BaseParams.OrientationEnum.VERTICAL;
            baseParams.Scrollbar   = this._WindowParams.scrollbar;

            var gridParams = baseParams as GridParams;
            if (gridParams != null)
            {
                gridParams.Grid.GroupPadding            = baseParams.IsHorizontal ? new(0, 0, 10, 10) : new RectOffset(10, 10, 0, 0);
                gridParams.Grid.AlignmentOfCellsInGroup = TextAnchor.MiddleCenter;
            }

            baseParams.Viewport = this._WindowParams.viewportRT;
            if (this._WindowParams.itemPrefab)
            {
                var success = this.SetPrefab(iAdapter, this._WindowParams.itemPrefab);
                if (!success) Debug.Log("OSA: Could not set the item prefab for '" + iAdapter.GetType().Name + "'. Make sure to manually set it through inspector or (advanced) in code");
            }
        }

        #if OSA_PLAYMAKER
		// Updated to use reflection, as we can't directly reference code that's outside the Plugins folder
		void PostAttachConfigurePlaymakerSetup()
		{
			string playmakerOSAProxyTypeFullName = "Com.ForbiddenByte.OSA.Playmaker.PlaymakerOSAProxy";
			var type = CWiz.GetTypeFromAllAssemblies(playmakerOSAProxyTypeFullName);
			if (type == null)
				throw new OSAException(playmakerOSAProxyTypeFullName + " not found");

			//var osaProxy = _WindowParams.ScrollRectRT.gameObject.AddComponent<Com.ForbiddenByte.OSA.Playmaker.PlaymakerOSAProxy>();
			var osaProxy = _WindowParams.ScrollRectRT.gameObject.AddComponent(type);
			var controllerFSM = _WindowParams.playmakerController.GetComponent<PlayMakerFSM>();
			var itemPrefabFSM = _WindowParams.itemPrefab.GetComponent<PlayMakerFSM>();

			var itemPrefabFSMVar__config_osa_controller = itemPrefabFSM.FsmVariables.FindFsmGameObject("config_osa_controller");
			if (itemPrefabFSMVar__config_osa_controller != null)
				itemPrefabFSMVar__config_osa_controller.Value = controllerFSM.gameObject;

			var controllerFSMVar__config_osa = controllerFSM.FsmVariables.FindFsmObject("config_osa");
			if (controllerFSMVar__config_osa != null)
				controllerFSMVar__config_osa.Value = osaProxy;

			string playmakerOSALazyDataHelperProxyTypeFullName = "Com.ForbiddenByte.OSA.Playmaker.PlaymakerOSALazyDataHelperProxy";
			type = CWiz.GetTypeFromAllAssemblies(playmakerOSALazyDataHelperProxyTypeFullName);
			if (type == null)
				throw new OSAException(playmakerOSALazyDataHelperProxyTypeFullName + " not found");

			//var lazyDataHelper = controllerFSM.GetComponent<Com.ForbiddenByte.OSA.Playmaker.PlaymakerOSALazyDataHelperProxy>();
			var lazyDataHelper = controllerFSM.GetComponent(type);
			if (lazyDataHelper)
			{
				//lazyDataHelper.InitWithNewOSAProxy(osaProxy);
				var mi = type.GetMethod("InitWithNewOSAProxy", BindingFlags.Instance | BindingFlags.Public);
				if (mi == null)
					throw new OSAException("Method InitWithNewOSAProxy not found on type " + type);
				mi.Invoke(lazyDataHelper, new object[] { osaProxy });
			}

			// In case of CSF, override the default sample titles to have larger strings
			if (_WindowParams.itemPrefab.name.Contains("ContentSizeFitter"))
			{
				var controllerFSMVar__sample_titles = controllerFSM.FsmVariables.FindFsmArray("sample_titles");
				if (controllerFSMVar__sample_titles != null)
				{
					var len = Demos.Common.DemosUtil.LOREM_IPSUM.Length;
					for (int i = 0; i < controllerFSMVar__sample_titles.Length; i++)
						controllerFSMVar__sample_titles.Set(i, Demos.Common.DemosUtil.GetRandomTextBody(len / 10 + 1, len));
					controllerFSMVar__sample_titles.SaveChanges();
				}
			}
		}
        #endif

        [Serializable]
        public class Parameters : BaseWindowParams, ISerializationCallbackReceiver
        {
            #region Serialization

            public ScrollRect    scrollRect;
            public RectTransform viewportRT;
            public Scrollbar     scrollbar;

            // View state
            public bool          useScrollbar, isScrollbarPosAtStart, overrideMiscScrollbar, allowMultipleScrollbars, canChangeScrollbars;
            public bool          excludeExampleImplementations;
            public bool          allowChoosingExampleImplementations;
            public string        onlyAllowSpecificTemplate;
            public string        onlyAllowImplementationsHavingInterface; // full name of the interface or null
            public int           indexOfExistingImplementationToUse;
            public string        generatedScriptNameToUse;
            public RectTransform itemPrefab;
            public bool          destroyScrollRectAfter;
            #if OSA_PLAYMAKER
			public RectTransform playmakerController;
			public bool playmakerSetupStarted;
            #endif
            public bool openForEdit;

            [SerializeField] private int  _IndexOfTemplateToUseForNewScript = 0;
            [SerializeField] private bool _MiscScrollbarWasAlreadyPresent   = false;

            #endregion

            [NonSerialized] public string[] availableTemplates;
            [NonSerialized] public string[] availableTemplatesNames;

            public string TemplateHeader
            {
                get
                {
                    if (this._TemplateHeader == null)
                    {
                        var headerComment = Resources.Load<TextAsset>(CWiz.TEMPLATE_SCRIPTS_HEADERCOMMENT_RESPATH);
                        this._TemplateHeader = headerComment.text;
                    }

                    return this._TemplateHeader;
                }
            }

            [NonSerialized] public List<Type> availableImplementations;
            [NonSerialized] public string[]   availableImplementationsStringsOptions;

            public override Vector2 MinSize                        => new(700f, 500f);
            public          bool    ImplementationsInitialized     => this.availableImplementations != null;
            public          bool    MiscScrollbarWasAlreadyPresent => this._MiscScrollbarWasAlreadyPresent;

            public int IndexOfTemplateToUseForNewScript
            {
                get => this._IndexOfTemplateToUseForNewScript;
                set
                {
                    if (this._IndexOfTemplateToUseForNewScript != value)
                    {
                        this._IndexOfTemplateToUseForNewScript = value;
                        this.generatedScriptNameToUse          = null;
                    }

                    if (this.generatedScriptNameToUse == null && value >= 0) this.generatedScriptNameToUse = this.availableTemplatesNames[value];
                }
            }

            public string TemplateToUseForNewScript => this.IndexOfTemplateToUseForNewScript < 0 ? null : this.availableTemplates[this.IndexOfTemplateToUseForNewScript];

            //public string TemplateNameToUse { get { return indexOfTemplateToUse < 1 ? null : availableTemplatesNames[indexOfTemplateToUse - 1]; } }
            public Type          ExistingImplementationToUse => this.indexOfExistingImplementationToUse < 1 ? null : this.availableImplementations[this.indexOfExistingImplementationToUse - 1];
            public bool          GenerateDefaultScrollbar    => !this.MiscScrollbarWasAlreadyPresent || this.overrideMiscScrollbar;
            public RectTransform ScrollRectRT                => this.scrollRect.transform as RectTransform;
            public RectTransform ScrollbarRT                 => this.scrollbar.transform as RectTransform;

            public const string LIST_TEMPLATE_NAME  = "BasicListAdapter";
            public const string GRID_TEMPLATE_NAME  = "BasicGridAdapter";
            public const string TABLE_TEMPLATE_NAME = "BasicTableAdapter";

            private const string DEFAULT_TEMPLATE_TO_USE_FOR_NEW_SCRIPT_IF_EXISTS = LIST_TEMPLATE_NAME;

            private string _TemplateHeader;

            public Parameters()
            {
            } // For unity serialization

            public static Parameters Create(
                ValidationResult validationResult,
                bool             allowMultipleScrollbars,
                bool             canChangeScrollbars,
                bool             allowChoosingExampleImplementations,
                bool             destroyScrollRectAfter                  = false,
                bool             checkForMiscComponents                  = true,
                string           onlyAllowSpecificTemplate               = null,
                Type             onlyAllowImplementationsHavingInterface = null
            )
            {
                var p = new Parameters();
                p.scrollRect                      = validationResult.scrollRect;
                p.viewportRT                      = validationResult.viewportRT;
                p.scrollbar                       = validationResult.scrollbar;
                p._MiscScrollbarWasAlreadyPresent = p.scrollbar != null;

                p.ResetValues();

                p.destroyScrollRectAfter                  = destroyScrollRectAfter;
                p.checkForMiscComponents                  = checkForMiscComponents;
                p.allowMultipleScrollbars                 = allowMultipleScrollbars;
                p.canChangeScrollbars                     = canChangeScrollbars;
                p.onlyAllowSpecificTemplate               = onlyAllowSpecificTemplate;
                p.onlyAllowImplementationsHavingInterface = onlyAllowImplementationsHavingInterface == null ? null : onlyAllowImplementationsHavingInterface.FullName;
                p.allowChoosingExampleImplementations     = allowChoosingExampleImplementations;
                if (!p.allowChoosingExampleImplementations) p.excludeExampleImplementations = true;

                p.InitNonSerialized();

                return p;
            }

            #region ISerializationCallbackReceiver implementation

            public void OnBeforeSerialize()
            {
            }

            //public void OnAfterDeserialize() { InitNonSerialized(); }
            // Commented: "Load is not allowed to be called durng serialization"
            public void OnAfterDeserialize()
            {
            }

            #endregion

            public void InitNonSerialized()
            {
                var allTemplatesTextAssets = Resources.LoadAll<TextAsset>(CWiz.TEMPLATE_SCRIPTS_RESPATH);
                this.availableTemplatesNames = new string[allTemplatesTextAssets.Length];
                this.availableTemplates      = new string[allTemplatesTextAssets.Length];
                for (var i = 0; i < allTemplatesTextAssets.Length; i++)
                {
                    var ta = allTemplatesTextAssets[i];
                    this.availableTemplatesNames[i] = ta.name;
                    this.availableTemplates[i]      = ta.text;
                }
                if (!string.IsNullOrEmpty(this.onlyAllowSpecificTemplate))
                {
                    var index = Array.IndexOf(this.availableTemplatesNames, this.onlyAllowSpecificTemplate);
                    if (index == -1)
                    {
                        this.availableTemplatesNames = new string[0];
                        this.availableTemplates      = new string[0];
                    }
                    else
                    {
                        this.availableTemplatesNames = new string[] { this.availableTemplatesNames[index] };
                        this.availableTemplates      = new string[] { this.availableTemplates[index] };
                    }
                }
                else
                {
                    var list    = new List<string>(this.availableTemplatesNames);
                    var tbTempl = TABLE_TEMPLATE_NAME;
                    var idx     = list.IndexOf(tbTempl);
                    if (idx != -1)
                    {
                        // Table adapter can only be created by specifying it when initializing window
                        list.RemoveAt(idx);
                        this.availableTemplatesNames = list.ToArray();
                        list                         = new(this.availableTemplates);
                        list.RemoveAt(idx);
                        this.availableTemplates = list.ToArray();
                    }
                }

                if (this._IndexOfTemplateToUseForNewScript >= this.availableTemplatesNames.Length) this._IndexOfTemplateToUseForNewScript = this.availableTemplatesNames.Length - 1;

                this.UpdateAvailableOSAImplementations(false);
                if (this.indexOfExistingImplementationToUse >= this.availableImplementationsStringsOptions.Length) this.indexOfExistingImplementationToUse = this.availableImplementationsStringsOptions.Length - 1;
            }

            public Type GetOnlyAllowImplementationsHavingInterface()
            {
                if (string.IsNullOrEmpty(this.onlyAllowImplementationsHavingInterface)) return null;

                var requiredInterfaceType = CWiz.GetTypeFromAllAssemblies(this.onlyAllowImplementationsHavingInterface);
                if (requiredInterfaceType != null) return requiredInterfaceType;

                return null;
            }

            public override void ResetValues()
            {
                base.ResetValues();

                this.isHorizontal          = this.scrollRect.horizontal;
                this.useScrollbar          = this.MiscScrollbarWasAlreadyPresent;
                this.overrideMiscScrollbar = false;
                this.isScrollbarPosAtStart = false;

                // OSA implementation
                this.excludeExampleImplementations       = true;
                this.allowChoosingExampleImplementations = true;
                this.indexOfExistingImplementationToUse  = 0; // create new
                //ResetIndexOfTemplateToUse();
                this._IndexOfTemplateToUseForNewScript = -1;
                //itemPrefab = null;
                this.openForEdit                             = true;
                this.allowMultipleScrollbars                 = false;
                this.canChangeScrollbars                     = true;
                this.onlyAllowSpecificTemplate               = null;
                this.onlyAllowImplementationsHavingInterface = null;
                this.destroyScrollRectAfter                  = false;
            }

            public void UpdateAvailableOSAImplementations(bool resetSelectedTemplateAndImplementation)
            {
                if (this.availableImplementations == null)
                    this.availableImplementations = new();
                else
                    this.availableImplementations.Clear();

                var  requiredInterfaceType   = this.GetOnlyAllowImplementationsHavingInterface();
                Type requiredNoInterfaceType = null;
                if (requiredInterfaceType == null)
                    // Table adapter can only be created by specifying it when initializing window
                    //requiredNoInterfaceType = typeof(ITableAdapter);
                    // Using direct string, as TableView package may not be imported
                    requiredNoInterfaceType = CWiz.GetTypeFromAllAssemblies(CWiz.TV.TABLE_ADAPTER_INTERFACE_FULL_NAME);
                CWiz.GetAvailableOSAImplementations(this.availableImplementations, this.excludeExampleImplementations, requiredInterfaceType, requiredNoInterfaceType);

                this.availableImplementationsStringsOptions    = new string[this.availableImplementations.Count + 1];
                this.availableImplementationsStringsOptions[0] = "<Generate new from template>";
                for (var i = 0; i < this.availableImplementations.Count; i++) this.availableImplementationsStringsOptions[i + 1] = this.availableImplementations[i].Name;

                if (resetSelectedTemplateAndImplementation)
                {
                    this.indexOfExistingImplementationToUse = 0; // default to create new
                    this.ResetIndexOfTemplateToUse();
                }
            }

            private void ResetIndexOfTemplateToUse()
            {
                var index                                  = 0;
                if (this.availableTemplates != null) index = Array.IndexOf(this.availableTemplates, DEFAULT_TEMPLATE_TO_USE_FOR_NEW_SCRIPT_IF_EXISTS); // -1 if not exists
                if (index == -1 && this.availableTemplates.Length > 0)                                                                                 // ..but 0 if there are others
                    index = 0;
                this.IndexOfTemplateToUseForNewScript = index;
            }
        }

        public class ValidationResult
        {
            public bool          isValid;
            public string        reasonIfNotValid;
            public string        warning;
            public RectTransform viewportRT;
            public Scrollbar     scrollbar;
            public ScrollRect    scrollRect;

            public override string ToString()
            {
                return "isValid = " + this.isValid + "\n" + "viewportRT = " + (this.viewportRT == null ? "(null)" : this.viewportRT.name) + "\n" + "scrollbar = " + (this.scrollbar == null ? "(null)" : this.scrollbar.name) + "\n" + "scrollRect = " + (this.scrollRect == null ? "(null)" : this.scrollRect.name) + "\n";
            }
        }

        private enum State
        {
            NONE,

            //POST_RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING,
            RECOMPILATION_SELECT_GENERATED_IMPLEMENTATION_PENDING,

            //POST_RECOMPILATION_RELOAD_SOLUTION_PENDING,
            ATTACH_EXISTING_OSA_PENDING,
            POST_ATTACH_CONFIGURE_OSA_PENDING,
            PING_SCROLL_RECT_PENDING,

            //POST_ATTACH_AND_POST_PING_CONFIGURE_OSA_PARAMS_PENDING,
            PING_PREFAB_PENDING,

            //PING_PREFAB_PENDING_STEP_2,
            CLOSE_PENDING,
        }
    }
}