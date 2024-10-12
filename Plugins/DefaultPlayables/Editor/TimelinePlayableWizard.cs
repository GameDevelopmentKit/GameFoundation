#if UNITY_EDITOR
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

public class TimelinePlayableWizard : EditorWindow
{
    public enum CreationError
    {
        NoError,
        PlayableAssetAlreadyExists,
        PlayableBehaviourAlreadyExists,
        PlayableBehaviourMixerAlreadyExists,
        TrackAssetAlreadyExists,
        PlayableDrawerAlreadyExists,
    }

    /*readonly GUIContent m_SetClipDefaultsContent = new GUIContent("Set Clip Defaults", "Do you want to set the default timings and other settings for clips when they are first created?");
    readonly GUIContent m_ClipDefaultsContent = new GUIContent("Clip Defaults");
    readonly GUIContent m_CDClipTimingContent = new GUIContent("Clip Timing", "Various settings that affect the durations over which the playable will be active.");
    readonly GUIContent m_CDDurationContent = new GUIContent("Duration", "The default length of the clip in seconds.");
    readonly GUIContent m_CDEaseInContent = new GUIContent("Ease In Duration", "The default duration over which the clip's weight increases to one.  When clips are overlapped, this is controlled by their overlap.  A clip requires the Blending ClipCap to support this.");
    readonly GUIContent m_CDEaseOutContent = new GUIContent("Ease Out Duration", "The default duration over which the clip's weight decreases to zero.  When clips are overlapped, this is controlled by their overlap.  A clip requires the Blending ClipCap to support this.");
    readonly GUIContent m_CDClipInContent = new GUIContent("Clip In", "The length of time after the start that the clip should start.  A clip requires the ClipIn ClipCap to support this.");
    readonly GUIContent m_CDSpeedMultiplierContent = new GUIContent("Speed Multiplier", "The amount a clip's time dependent aspects will speed up or slow down by.  A clip requires the SpeedMultiplier ClipCap to support this.");
    */
    private const string k_Tab                          = "    ";
    private const string k_ShowHelpBoxesKey             = "TimelinePlayableWizard_ShowHelpBoxes";
    private const string k_TimelineClipAssetSuffix      = "Clip";
    private const string k_TimelineClipBehaviourSuffix  = "Behaviour";
    private const string k_PlayableBehaviourMixerSuffix = "MixerBehaviour";
    private const string k_TrackAssetSuffix             = "Track";
    private const string k_PropertyDrawerSuffix         = "Drawer";
    private const int    k_PlayableNameCharLimit        = 64;
    private const float  k_WindowWidth                  = 500f;
    private const float  k_MaxWindowHeight              = 800f;
    private const float  k_ScreenSizeWindowBuffer       = 50f;

    private static UsableType[] s_ComponentTypes;
    private static UsableType[] s_TrackBindingTypes;
    private static UsableType[] s_ExposedReferenceTypes;
    private static UsableType[] s_BehaviourVariableTypes;

    private static Type[] s_BlendableTypes =
    {
        typeof(float), typeof(int), typeof(double), typeof(Vector2), typeof(Vector3), typeof(Color),
    };

    private static Type[] s_AssignableTypes =
    {
        typeof(string), typeof(bool),
    };

    private static string[] s_DisallowedPropertyNames =
    {
        "name",
    };

    public bool                 showHelpBoxes = true;
    public string               playableName  = "";
    public bool                 isStandardBlendPlayable;
    public Component            defaultValuesComponent;
    public List<Variable>       exposedReferences               = new();
    public List<Variable>       playableBehaviourVariables      = new();
    public List<UsableProperty> standardBlendPlayableProperties = new();

    public ClipCaps clipCaps;

    /*public bool setClipDefaults;
    public float clipDefaultDurationSeconds = 5f;
    public float clipDefaultEaseInSeconds;
    public float clipDefaultEaseOutSeconds;
    public float clipDefaultClipInSeconds;
    public float clipDefaultSpeedMultiplier = 1f;*/
    public Color trackColor = new(0.855f, 0.8623f, 0.870f);

    private readonly GUIContent m_BehaviourVariablesContent = new("Behaviour Variables",
        "Behaviour Variables are all the variables you wish to use in your playable that do NOT need a reference to something in a scene.  For example a float for speed.");

    private readonly GUIContent m_CCAllContent = new("All", "Your playable supports all of the above features.");

    private readonly GUIContent m_CCBlendingContent = new("Blending", "Your playable supports overlapping of clips to blend between them.");

    private readonly GUIContent m_CCClipInContent = new("Clip In", "Your playable need not be at the start of the Timeline.");

    private readonly GUIContent m_CCExtrapolationContent = new("Extrapolation",
        "Your playable will persist beyond its end time and its results will continue until the next clip is encountered.");

    private readonly GUIContent m_CCLoopingContent = new("Looping",
        "Your playable has a specified time that it takes and will start again after it finishes until the clip's duration has played.");

    private readonly GUIContent m_CCNoneContent = new("None", "Your playable supports none of the features below.");

    private readonly GUIContent m_CCSpeedMultiplierContent = new("Speed Multiplier", "Your playable supports changes to the time scale.");

    private readonly GUIContent m_ClipCapsContent = new("Clip Caps",
        "Clip Caps are used to change the way Timelines work with your playables.  For example, enabling Blending will mean that your playables can blend when they overlap and have ease in and out durations.  To find out a little about each hover the cursor over the options.  For details, please see the documentation.");

    private readonly GUIContent m_CreateDrawerContent = new("Create Drawer?",
        "Checking this box will enable the creation of a PropertyDrawer for your playable.  Having this script will make it easier to customise how your playable appears in the inspector.");

    private readonly GUIContent m_DefaultValuesComponentContent = new("Default Values",
        "When the scripts are created, each of the selected properties are assigned a default from the selected Component.  If this is left blank no defaults will be used.");

    private readonly GUIContent m_ExposedReferencesContent = new("Exposed References",
        "Exposed References are references to objects in a scene that your Playable needs. For example, if you want to tween between two Transforms, they will need to be Exposed References.");

    private readonly GUIContent m_PlayableNameContent = new("Playable Name",
        "This is the name that will represent the playable.  E.G. TransformTween.  It will be the basis for the class names so it is best not to use the postfixes: 'Clip', 'Behaviour', 'MixerBehaviour' or 'Drawer'.");

    private readonly GUIContent m_ShowHelpBoxesContent = new("Show Help", "Do you want to see the help boxes as part of this wizard?");

    private readonly GUIContent m_StandardBlendPlayableContent = new("Standard Blend Playable",
        "Often when creating a playable it's intended purpose is just to briefly override the properties of a component for the playable's duration and then blend back to the defaults.  For example a playable that changes the color of a Light but changes it back.  To make a playable with this functionality, check this box.");

    private readonly GUIContent m_StandardBlendPlayablePropertiesContent = new("Standard Blend Playable Properties",
        "Having already selected a Track Binding type, you can select the properties of the bound component you want the playable to affect.  For example, if your playable is bound to a Transform, you can affect the position property.  Note that changing the component binding will clear the list of properties.");

    private readonly GUIContent m_TrackBindingTypeContent = new("Track Binding Type",
        "This is the type of object the Playable will affect.  E.G. To affect the position choose Transform.");

    private readonly GUIContent m_TrackColorContent = new("Track Color",
        "Timeline tracks have a colored outline, use this to select that color for your track.");

    private int            m_ComponentBindingTypeIndex;
    private bool           m_CreateButtonPressed;
    private bool           m_CreateDrawer;
    private CreationError  m_CreationError;
    private Vector2        m_ScrollViewPos;
    private FieldInfo[]    m_TrackBindingFields;
    private PropertyInfo[] m_TrackBindingProperties;

    private int                  m_TrackBindingTypeIndex;
    private List<UsableProperty> m_TrackBindingUsableProperties = new();
    public  UsableType           trackBinding;

    private void OnGUI()
    {
        if (s_ComponentTypes == null || s_TrackBindingTypes == null || s_ExposedReferenceTypes == null || s_BehaviourVariableTypes == null) Init();

        if (s_ComponentTypes == null || s_TrackBindingTypes == null || s_ExposedReferenceTypes == null || s_BehaviourVariableTypes == null)
        {
            EditorGUILayout.HelpBox("Failed to initialise.", MessageType.Error);
            return;
        }

        this.m_ScrollViewPos = EditorGUILayout.BeginScrollView(this.m_ScrollViewPos);

        var oldShowHelpBoxes = this.showHelpBoxes;
        this.showHelpBoxes = EditorGUILayout.Toggle(this.m_ShowHelpBoxesContent, this.showHelpBoxes);
        if (oldShowHelpBoxes != this.showHelpBoxes)
        {
            EditorPrefs.SetBool(k_ShowHelpBoxesKey, this.showHelpBoxes);
            EditorGUILayout.Space();
        }

        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox("This wizard is used to create the basics of a custom playable for the Timeline. "
                + "It will create 4 scripts that you can then edit to complete their functionality. "
                + "The purpose is to setup the boilerplate code for you.  If you are already familiar "
                + "with playables and the Timeline, you may wish to create your own scripts instead.",
                MessageType.None);
            EditorGUILayout.Space();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_PlayableNameContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        this.playableName = EditorGUILayout.TextField(this.m_PlayableNameContent, this.playableName);

        var playableNameNotEmpty = !string.IsNullOrEmpty(this.playableName);
        // bool playableNameFormatted = CodeGenerator.IsValidLanguageIndependentIdentifier(playableName);
        var playableNameFormatted = true;
        if (!playableNameNotEmpty || !playableNameFormatted)
            EditorGUILayout.HelpBox(
                "The Playable needs a name which starts with a capital letter and contains no spaces or special characters.",
                MessageType.Error);

        var playableNameTooLong = this.playableName.Length > k_PlayableNameCharLimit;
        if (playableNameTooLong)
            EditorGUILayout.HelpBox(
                "The Playable needs a name which is fewer than " + k_PlayableNameCharLimit + " characters long.",
                MessageType.Error);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_StandardBlendPlayableContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        var oldStandardBlendPlayable = this.isStandardBlendPlayable;
        this.isStandardBlendPlayable = EditorGUILayout.Toggle(this.m_StandardBlendPlayableContent, this.isStandardBlendPlayable);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_TrackBindingTypeContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        var oldIndex = -1;
        if (this.isStandardBlendPlayable)
        {
            oldIndex = this.m_ComponentBindingTypeIndex;

            this.m_ComponentBindingTypeIndex = EditorGUILayout.Popup(this.m_TrackBindingTypeContent,
                this.m_ComponentBindingTypeIndex,
                UsableType.GetGUIContentWithSortingArray(s_ComponentTypes));
            this.trackBinding = s_ComponentTypes[this.m_ComponentBindingTypeIndex];

            EditorGUILayout.Space();

            this.defaultValuesComponent = EditorGUILayout.ObjectField(this.m_DefaultValuesComponentContent,
                this.defaultValuesComponent,
                this.trackBinding.type,
                true) as Component;
        }
        else
        {
            this.m_TrackBindingTypeIndex = EditorGUILayout.Popup(this.m_TrackBindingTypeContent,
                this.m_TrackBindingTypeIndex,
                UsableType.GetGUIContentWithSortingArray(s_TrackBindingTypes));
            this.trackBinding = s_TrackBindingTypes[this.m_TrackBindingTypeIndex];
        }

        EditorGUILayout.EndVertical();

        var exposedVariablesNamesValid = true;
        var scriptVariablesNamesValid  = true;
        var allUniqueVariableNames     = true;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (this.isStandardBlendPlayable)
        {
            this.StandardBlendPlayablePropertyGUI(oldIndex != this.m_ComponentBindingTypeIndex || oldStandardBlendPlayable != this.isStandardBlendPlayable);
        }
        else
        {
            exposedVariablesNamesValid = this.VariableListGUI(this.exposedReferences,
                s_ExposedReferenceTypes,
                this.m_ExposedReferencesContent,
                "newExposedReference");

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            scriptVariablesNamesValid = this.VariableListGUI(this.playableBehaviourVariables,
                s_BehaviourVariableTypes,
                this.m_BehaviourVariablesContent,
                "newBehaviourVariable");

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            allUniqueVariableNames = this.AllVariablesUniquelyNamed();
            if (!allUniqueVariableNames)
                EditorGUILayout.HelpBox(
                    "Your variables to not have unique names.  Make sure all of your Exposed References and Behaviour Variables have unique names.",
                    MessageType.Error);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            this.ClipCapsGUI();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        /*ClipDefaultsGUI ();

        EditorGUILayout.Space ();
        EditorGUILayout.Space ();*/

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_TrackColorContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        this.trackColor = EditorGUILayout.ColorField(this.m_TrackColorContent, this.trackColor);
        EditorGUILayout.EndVertical();

        if (!this.isStandardBlendPlayable)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (this.showHelpBoxes)
            {
                EditorGUILayout.HelpBox(this.m_CreateDrawerContent.tooltip, MessageType.Info);
                EditorGUILayout.Space();
            }

            this.m_CreateDrawer = EditorGUILayout.Toggle(this.m_CreateDrawerContent, this.m_CreateDrawer);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (playableNameNotEmpty && playableNameFormatted && allUniqueVariableNames && exposedVariablesNamesValid && scriptVariablesNamesValid && !playableNameTooLong)
            if (GUILayout.Button("Create", GUILayout.Width(60f)))
            {
                this.m_CreateButtonPressed = true;

                for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++) this.standardBlendPlayableProperties[i].CreateSettingDefaultValueString(this.defaultValuesComponent);

                this.m_CreationError = this.CreateScripts();

                if (this.m_CreationError == CreationError.NoError) this.Close();
            }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (this.m_CreateButtonPressed)
            switch (this.m_CreationError)
            {
                case CreationError.NoError:
                    EditorGUILayout.HelpBox("Playable was successfully created.", MessageType.Info);
                    break;
                case CreationError.PlayableAssetAlreadyExists:
                    EditorGUILayout.HelpBox(
                        "The type " + this.playableName + k_TimelineClipAssetSuffix + " already exists, no files were created.",
                        MessageType.Error);
                    break;
                case CreationError.PlayableBehaviourAlreadyExists:
                    EditorGUILayout.HelpBox(
                        "The type " + this.playableName + k_TimelineClipBehaviourSuffix + " already exists, no files were created.",
                        MessageType.Error);
                    break;
                case CreationError.PlayableBehaviourMixerAlreadyExists:
                    EditorGUILayout.HelpBox(
                        "The type " + this.playableName + k_PlayableBehaviourMixerSuffix + " already exists, no files were created.",
                        MessageType.Error);
                    break;
                case CreationError.TrackAssetAlreadyExists:
                    EditorGUILayout.HelpBox(
                        "The type " + this.playableName + k_TrackAssetSuffix + " already exists, no files were created.",
                        MessageType.Error);
                    break;
                case CreationError.PlayableDrawerAlreadyExists:
                    EditorGUILayout.HelpBox(
                        "The type " + this.playableName + k_PropertyDrawerSuffix + " already exists, no files were created.",
                        MessageType.Error);
                    break;
            }

        if (GUILayout.Button("Reset", GUILayout.Width(60f))) this.ResetWindow();

        EditorGUILayout.EndScrollView();
    }

    [MenuItem("Window/Timeline Playable Wizard...")]
    private static void CreateWindow()
    {
        var wizard = GetWindow<TimelinePlayableWizard>(true, "Timeline Playable Wizard", true);

        var position                    = Vector2.zero;
        var sceneView                   = SceneView.lastActiveSceneView;
        if (sceneView != null) position = new(sceneView.position.x, sceneView.position.y);
        wizard.position = new(position.x + k_ScreenSizeWindowBuffer,
            position.y + k_ScreenSizeWindowBuffer,
            k_WindowWidth,
            Mathf.Min(Screen.currentResolution.height - k_ScreenSizeWindowBuffer, k_MaxWindowHeight));

        wizard.showHelpBoxes = EditorPrefs.GetBool(k_ShowHelpBoxesKey);
        wizard.Show();

        Init();
    }

    private static void Init()
    {
        var componentTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
            .Where(t => typeof(Component).IsAssignableFrom(t)).Where(t => t.IsPublic).ToArray();

        var componentUsableTypesList = UsableType.GetUsableTypeArray(componentTypes).ToList();
        componentUsableTypesList.Sort();
        s_ComponentTypes = componentUsableTypesList.ToArray();

        var gameObjectUsableType = new UsableType(typeof(GameObject));
        var defaultUsableTypes   = UsableType.GetUsableTypeArray(componentTypes, gameObjectUsableType);

        var exposedRefTypeList = defaultUsableTypes.ToList();
        exposedRefTypeList.Sort();
        s_ExposedReferenceTypes = exposedRefTypeList.ToArray();

        var noneType = new UsableType((Type)null);
        s_TrackBindingTypes = UsableType.AmalgamateUsableTypes(s_ExposedReferenceTypes, noneType);

        s_BehaviourVariableTypes = UsableType.AmalgamateUsableTypes(
            s_ExposedReferenceTypes,
            new UsableType("int"),
            new UsableType("bool"),
            new UsableType("float"),
            new UsableType("Color"),
            new UsableType("double"),
            new UsableType("string"),
            new UsableType("Vector2"),
            new UsableType("Vector3"),
            new UsableType("AudioClip"),
            new UsableType("AnimationCurve")
        );
        var scriptVariableTypeList = s_BehaviourVariableTypes.ToList();
        scriptVariableTypeList.Sort();
        s_BehaviourVariableTypes = scriptVariableTypeList.ToArray();
    }

    private void StandardBlendPlayablePropertyGUI(bool findNewProperties)
    {
        if (findNewProperties || (this.m_TrackBindingProperties == null && this.m_TrackBindingFields == null))
        {
            this.m_TrackBindingUsableProperties.Clear();

            IEnumerable<PropertyInfo> propertyInfos = this.trackBinding.type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty);
            propertyInfos =
                propertyInfos.Where(x => IsTypeBlendable(x.PropertyType) || IsTypeAssignable(x.PropertyType));
            propertyInfos = propertyInfos.Where(x => x.CanWrite && x.CanRead);
            propertyInfos = propertyInfos.Where(x => HasAllowedName(x));
            // Uncomment the below to stop Obsolete properties being selectable.
            //propertyInfos = propertyInfos.Where (x => !Attribute.IsDefined (x, typeof(ObsoleteAttribute)));
            this.m_TrackBindingProperties = propertyInfos.ToArray();
            foreach (var trackBindingProperty in this.m_TrackBindingProperties) this.m_TrackBindingUsableProperties.Add(new(trackBindingProperty));

            IEnumerable<FieldInfo> fieldInfos = this.trackBinding.type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            fieldInfos                = fieldInfos.Where(x => IsTypeBlendable(x.FieldType) || IsTypeAssignable(x.FieldType));
            this.m_TrackBindingFields = fieldInfos.ToArray();
            foreach (var trackBindingField in this.m_TrackBindingFields) this.m_TrackBindingUsableProperties.Add(new(trackBindingField));

            this.m_TrackBindingUsableProperties = this.m_TrackBindingUsableProperties.OrderBy(x => x.name).ToList();
            this.standardBlendPlayableProperties.Clear();
        }

        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_StandardBlendPlayablePropertiesContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField(this.m_StandardBlendPlayablePropertiesContent);

        var indexToRemove = -1;
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
            if (this.standardBlendPlayableProperties[i].GUI(this.m_TrackBindingUsableProperties))
                indexToRemove = i;

        if (indexToRemove != -1) this.standardBlendPlayableProperties.RemoveAt(indexToRemove);

        if (GUILayout.Button("Add", GUILayout.Width(40f))) this.standardBlendPlayableProperties.Add(this.m_TrackBindingUsableProperties[0].GetDuplicate());

        if (this.standardBlendPlayableProperties.Any(IsObsolete))
            EditorGUILayout.HelpBox(
                "One or more of your chosen properties are marked 'Obsolete'.  Consider changing them to avoid deprecation with future versions of Unity.",
                MessageType.Warning);

        EditorGUILayout.EndVertical();
    }

    private static bool IsTypeBlendable(Type type)
    {
        for (var i = 0; i < s_BlendableTypes.Length; i++)
            if (type == s_BlendableTypes[i])
                return true;

        return false;
    }

    private static bool IsTypeAssignable(Type type)
    {
        for (var i = 0; i < s_AssignableTypes.Length; i++)
            if (type == s_AssignableTypes[i] || type.IsEnum)
                return true;

        return false;
    }

    private static bool HasAllowedName(PropertyInfo propertyInfo)
    {
        for (var i = 0; i < s_DisallowedPropertyNames.Length; i++)
            if (propertyInfo.Name == s_DisallowedPropertyNames[i])
                return false;

        return true;
    }

    private static bool IsObsolete(UsableProperty usableProperty)
    {
        if (usableProperty.usablePropertyType == UsableProperty.UsablePropertyType.Field) return Attribute.IsDefined(usableProperty.fieldInfo, typeof(ObsoleteAttribute));
        return Attribute.IsDefined(usableProperty.propertyInfo, typeof(ObsoleteAttribute));
    }

    private bool VariableListGUI(List<Variable> variables, UsableType[] usableTypes, GUIContent guiContent, string newName)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(guiContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField(guiContent);

        var indexToRemove = -1;
        var allNamesValid = true;
        for (var i = 0; i < variables.Count; i++)
            if (variables[i].GUI(usableTypes))
                indexToRemove = i;
        // if (!CodeGenerator.IsValidLanguageIndependentIdentifier(variables[i].name)) allNamesValid = false;
        if (indexToRemove != -1) variables.RemoveAt(indexToRemove);

        if (GUILayout.Button("Add", GUILayout.Width(40f))) variables.Add(new(newName, usableTypes[0]));

        if (!allNamesValid)
            EditorGUILayout.HelpBox(
                "One of the variables has an invalid character, make sure they don't contain any spaces or special characters.",
                MessageType.Error);

        EditorGUILayout.EndVertical();

        return allNamesValid;
    }

    private bool AllVariablesUniquelyNamed()
    {
        for (var i = 0; i < this.exposedReferences.Count; i++)
        {
            var exposedRefName = this.exposedReferences[i].name;

            for (var j = 0; j < this.exposedReferences.Count; j++)
                if (i != j && exposedRefName == this.exposedReferences[j].name)
                    return false;

            for (var j = 0; j < this.playableBehaviourVariables.Count; j++)
                if (exposedRefName == this.playableBehaviourVariables[j].name)
                    return false;
        }

        for (var i = 0; i < this.playableBehaviourVariables.Count; i++)
        {
            var scriptPlayableVariableName = this.playableBehaviourVariables[i].name;

            for (var j = 0; j < this.exposedReferences.Count; j++)
                if (scriptPlayableVariableName == this.exposedReferences[j].name)
                    return false;

            for (var j = 0; j < this.playableBehaviourVariables.Count; j++)
                if (i != j && scriptPlayableVariableName == this.playableBehaviourVariables[j].name)
                    return false;
        }

        return true;
    }

    private void ClipCapsGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (this.showHelpBoxes)
        {
            EditorGUILayout.HelpBox(this.m_ClipCapsContent.tooltip, MessageType.Info);
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField(this.m_ClipCapsContent);

        var isLooping         = (this.clipCaps & ClipCaps.Looping) == ClipCaps.Looping;
        var isExtrapolation   = (this.clipCaps & ClipCaps.Extrapolation) == ClipCaps.Extrapolation;
        var isClipIn          = (this.clipCaps & ClipCaps.ClipIn) == ClipCaps.ClipIn;
        var isSpeedMultiplier = (this.clipCaps & ClipCaps.SpeedMultiplier) == ClipCaps.SpeedMultiplier;
        var isBlending        = (this.clipCaps & ClipCaps.Blending) == ClipCaps.Blending;

        var isNone = !isLooping && !isExtrapolation && !isClipIn && !isSpeedMultiplier && !isBlending;
        var isAll  = isLooping && isExtrapolation && isClipIn && isSpeedMultiplier && isBlending;

        EditorGUI.BeginChangeCheck();
        isNone = EditorGUILayout.ToggleLeft(this.m_CCNoneContent, isNone);
        if (EditorGUI.EndChangeCheck())
            if (isNone)
            {
                isLooping         = false;
                isExtrapolation   = false;
                isClipIn          = false;
                isSpeedMultiplier = false;
                isBlending        = false;
                isAll             = false;
            }

        EditorGUI.BeginChangeCheck();
        isLooping         = EditorGUILayout.ToggleLeft(this.m_CCLoopingContent, isLooping);
        isExtrapolation   = EditorGUILayout.ToggleLeft(this.m_CCExtrapolationContent, isExtrapolation);
        isClipIn          = EditorGUILayout.ToggleLeft(this.m_CCClipInContent, isClipIn);
        isSpeedMultiplier = EditorGUILayout.ToggleLeft(this.m_CCSpeedMultiplierContent, isSpeedMultiplier);
        isBlending        = EditorGUILayout.ToggleLeft(this.m_CCBlendingContent, isBlending);
        if (EditorGUI.EndChangeCheck())
        {
            isNone = !isLooping && !isExtrapolation && !isClipIn && !isSpeedMultiplier && !isBlending;
            isAll  = isLooping && isExtrapolation && isClipIn && isSpeedMultiplier && isBlending;
        }

        EditorGUI.BeginChangeCheck();
        isAll = EditorGUILayout.ToggleLeft(this.m_CCAllContent, isAll);
        if (EditorGUI.EndChangeCheck())
            if (isAll)
            {
                isNone            = false;
                isLooping         = true;
                isExtrapolation   = true;
                isClipIn          = true;
                isSpeedMultiplier = true;
                isBlending        = true;
            }

        EditorGUILayout.EndVertical();

        this.clipCaps = ClipCaps.None;

        if (isNone) return;

        if (isAll)
        {
            this.clipCaps = ClipCaps.All;
            return;
        }

        if (isLooping) this.clipCaps |= ClipCaps.Looping;

        if (isExtrapolation) this.clipCaps |= ClipCaps.Extrapolation;

        if (isClipIn) this.clipCaps |= ClipCaps.ClipIn;

        if (isSpeedMultiplier) this.clipCaps |= ClipCaps.SpeedMultiplier;

        if (isBlending) this.clipCaps |= ClipCaps.Blending;
    }

    /*void ClipDefaultsGUI ()
    {
        EditorGUILayout.BeginVertical (GUI.skin.box);

        setClipDefaults = EditorGUILayout.Toggle (m_SetClipDefaultsContent, setClipDefaults);

        if (!setClipDefaults)
        {
            EditorGUILayout.EndVertical ();
            return;
        }

        if (showHelpBoxes)
        {
            EditorGUILayout.HelpBox (m_ClipDefaultsContent.tooltip, MessageType.Info);
        }

        EditorGUILayout.LabelField (m_ClipDefaultsContent);

        EditorGUILayout.Space ();

        EditorGUILayout.LabelField (m_CDClipTimingContent);
        EditorGUI.indentLevel++;
        clipDefaultDurationSeconds = EditorGUILayout.FloatField(m_CDDurationContent, clipDefaultDurationSeconds);

        EditorGUILayout.Space ();

        clipDefaultEaseInSeconds = EditorGUILayout.FloatField(m_CDEaseInContent, clipDefaultEaseInSeconds);
        clipDefaultEaseOutSeconds = EditorGUILayout.FloatField (m_CDEaseOutContent, clipDefaultEaseOutSeconds);

        if (isStandardBlendPlayable)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space();

        clipDefaultClipInSeconds = EditorGUILayout.FloatField(m_CDClipInContent, clipDefaultClipInSeconds);

        EditorGUILayout.Space();

        clipDefaultSpeedMultiplier = EditorGUILayout.FloatField(m_CDSpeedMultiplierContent, clipDefaultSpeedMultiplier);
        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();
    }*/

    private CreationError CreateScripts()
    {
        if (ScriptAlreadyExists(this.playableName + k_TimelineClipAssetSuffix)) return CreationError.PlayableAssetAlreadyExists;

        if (ScriptAlreadyExists(this.playableName + k_TimelineClipBehaviourSuffix)) return CreationError.PlayableBehaviourAlreadyExists;

        if (ScriptAlreadyExists(this.playableName + k_PlayableBehaviourMixerSuffix)) return CreationError.PlayableBehaviourMixerAlreadyExists;

        if (ScriptAlreadyExists(this.playableName + k_TrackAssetSuffix)) return CreationError.TrackAssetAlreadyExists;

        if (this.m_CreateDrawer && ScriptAlreadyExists(this.playableName + k_PropertyDrawerSuffix)) return CreationError.PlayableDrawerAlreadyExists;

        AssetDatabase.CreateFolder("Assets", this.playableName);

        if (this.isStandardBlendPlayable)
        {
            this.CreateScript(this.playableName + k_TimelineClipAssetSuffix, this.StandardBlendPlayableAsset());
            this.CreateScript(this.playableName + k_TimelineClipBehaviourSuffix, this.StandardBlendPlayableBehaviour());
            this.CreateScript(this.playableName + k_PlayableBehaviourMixerSuffix, this.StandardBlendPlayableBehaviourMixer());
            this.CreateScript(this.playableName + k_TrackAssetSuffix, this.StandardBlendTrackAssetScript());

            AssetDatabase.CreateFolder("Assets/" + this.playableName, "Editor");

            var path = Application.dataPath + "/" + this.playableName + "/Editor/" + this.playableName + k_PropertyDrawerSuffix + ".cs";
            using (var writer = File.CreateText(path)) writer.Write(this.StandardBlendPlayableDrawer());
        }
        else
        {
            this.CreateScript(this.playableName + k_TimelineClipAssetSuffix, this.PlayableAsset());
            this.CreateScript(this.playableName + k_TimelineClipBehaviourSuffix, this.PlayableBehaviour());
            this.CreateScript(this.playableName + k_PlayableBehaviourMixerSuffix, this.PlayableBehaviourMixer());
            this.CreateScript(this.playableName + k_TrackAssetSuffix, this.TrackAssetScript());

            if (this.m_CreateDrawer)
            {
                AssetDatabase.CreateFolder("Assets/" + this.playableName, "Editor");

                var path = Application.dataPath + "/" + this.playableName + "/Editor/" + this.playableName + k_PropertyDrawerSuffix + ".cs";
                using (var writer = File.CreateText(path)) writer.Write(this.PlayableDrawer());
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return CreationError.NoError;
    }

    private static bool ScriptAlreadyExists(string scriptName)
    {
        var guids = AssetDatabase.FindAssets(scriptName);

        if (guids.Length == 0) return false;

        for (var i = 0; i < guids.Length; i++)
        {
            var path      = AssetDatabase.GUIDToAssetPath(guids[i]);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType == typeof(MonoScript)) return true;
        }

        return false;
    }

    private void CreateScript(string fileName, string content)
    {
        var path = Application.dataPath + "/" + this.playableName + "/" + fileName + ".cs";
        using (var writer = File.CreateText(path)) writer.Write(content);
    }

    private void ResetWindow()
    {
        this.playableName                    = "";
        this.isStandardBlendPlayable         = false;
        this.trackBinding                    = s_TrackBindingTypes[0];
        this.defaultValuesComponent          = null;
        this.exposedReferences               = new();
        this.playableBehaviourVariables      = new();
        this.standardBlendPlayableProperties = new();
        this.clipCaps                        = ClipCaps.None;
        /*setClipDefaults = false;
        clipDefaultDurationSeconds = 5f;
        clipDefaultEaseInSeconds = 0f;
        clipDefaultEaseOutSeconds = 0f;
        clipDefaultClipInSeconds = 0f;
        clipDefaultSpeedMultiplier = 1f;*/
        this.trackColor = new(0.855f, 0.8623f, 0.870f);

        this.m_TrackBindingTypeIndex        = 0;
        this.m_ComponentBindingTypeIndex    = 0;
        this.m_TrackBindingProperties       = null;
        this.m_TrackBindingFields           = null;
        this.m_TrackBindingUsableProperties = null;
        this.m_CreateDrawer                 = false;
    }

    private string TrackAssetScript()
    {
        return
            "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "[TrackColor(" + this.trackColor.r + "f, " + this.trackColor.g + "f, " + this.trackColor.b + "f)]\n" + "[TrackClipType(typeof(" + this.playableName + k_TimelineClipAssetSuffix + "))]\n" + this.TrackBindingToString() + "public class " + this.playableName + k_TrackAssetSuffix + " : TrackAsset\n" + "{\n" + k_Tab + "public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "return ScriptPlayable<" + this.playableName + k_PlayableBehaviourMixerSuffix + ">.Create (graph, inputCount);\n" + k_Tab + "}\n" + "}\n";
    }

    private string PlayableAsset()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "[Serializable]\n" + "public class " + this.playableName + k_TimelineClipAssetSuffix + " : PlayableAsset, ITimelineClipAsset\n" + "{\n" + k_Tab + "public " + this.playableName + k_TimelineClipBehaviourSuffix + " template = new " + this.playableName + k_TimelineClipBehaviourSuffix + " ();\n" + this.ExposedReferencesToString() + "\n" + k_Tab + "public ClipCaps clipCaps\n" + k_Tab + "{\n" + k_Tab + k_Tab + "get { return " + this.ClipCapsToString() + "; }\n" + k_Tab + "}\n" + "\n" + k_Tab + "public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "var playable = ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + ">.Create (graph, template);\n" + this.ExposedReferencesResolvingToString() + k_Tab + k_Tab + "return playable;\n" + k_Tab + "}\n" + "}\n";
    }

    private string PlayableBehaviour()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "[Serializable]\n" + "public class " + this.playableName + k_TimelineClipBehaviourSuffix + " : PlayableBehaviour\n" + "{\n" + this.ExposedReferencesAsScriptVariablesToString() + this.PlayableBehaviourVariablesToString() + "\n" + k_Tab + "public override void OnPlayableCreate (Playable playable)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "\n" + k_Tab + "}\n" + "}\n";
    }

    private string PlayableBehaviourMixer()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "public class " + this.playableName + k_PlayableBehaviourMixerSuffix + " : PlayableBehaviour\n" + "{\n" + k_Tab + "// NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.\n" + k_Tab + "public override void ProcessFrame(Playable playable, FrameData info, object playerData)\n" + k_Tab + "{\n" + this.MixerTrackBindingLocalVariableToString() + k_Tab + k_Tab + "int inputCount = playable.GetInputCount ();\n" + "\n" + k_Tab + k_Tab + "for (int i = 0; i < inputCount; i++)\n" + k_Tab + k_Tab + "{\n" + k_Tab + k_Tab + k_Tab + "float inputWeight = playable.GetInputWeight(i);\n" + k_Tab + k_Tab + k_Tab + "ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + "> inputPlayable = (ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + ">)playable.GetInput(i);\n" + k_Tab + k_Tab + k_Tab + this.playableName + k_TimelineClipBehaviourSuffix + " input = inputPlayable.GetBehaviour ();\n" + k_Tab + k_Tab + k_Tab + "\n" + k_Tab + k_Tab + k_Tab + "// Use the above variables to process each frame of this playable.\n" + k_Tab + k_Tab + k_Tab + "\n" + k_Tab + k_Tab + "}\n" + k_Tab + "}\n" + "}\n";
    }

    private string PlayableDrawer()
    {
        return
            "using UnityEditor;\n" + "using UnityEngine;\n" + "\n" + "[CustomPropertyDrawer(typeof(" + this.playableName + k_TimelineClipBehaviourSuffix + "))]\n" + "public class " + this.playableName + k_PropertyDrawerSuffix + " : PropertyDrawer\n" + "{\n" + k_Tab + "public override float GetPropertyHeight (SerializedProperty property, GUIContent label)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "int fieldCount = " + this.playableBehaviourVariables.Count + ";\n" + k_Tab + k_Tab + "return fieldCount * EditorGUIUtility.singleLineHeight;\n" + k_Tab + "}\n" + "\n" + k_Tab + "public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)\n" + k_Tab + "{\n" + this.ScriptVariablesAsSerializedPropAssignmentToString() + "\n" + k_Tab + k_Tab + "Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);\n" + this.ScriptVariablesAsSerializedPropGUIToString() + k_Tab + "}\n" + "}\n";
    }

    private string TrackBindingToString()
    {
        if (this.m_TrackBindingTypeIndex != 0) return "[TrackBindingType(typeof(" + this.trackBinding.name + "))]\n";
        return "";
    }

    private string AdditionalNamespacesToString()
    {
        var exposedReferenceTypes = Variable.GetUsableTypesFromVariableArray(this.exposedReferences.ToArray());
        var behaviourVariableTypes =
            Variable.GetUsableTypesFromVariableArray(this.playableBehaviourVariables.ToArray());
        var allUsedTypes                                                       = new UsableType[exposedReferenceTypes.Length + behaviourVariableTypes.Length + 1];
        for (var i = 0; i < exposedReferenceTypes.Length; i++) allUsedTypes[i] = exposedReferenceTypes[i];

        for (var i = 0; i < behaviourVariableTypes.Length; i++) allUsedTypes[i + exposedReferenceTypes.Length] = behaviourVariableTypes[i];

        allUsedTypes[allUsedTypes.Length - 1] = this.trackBinding;

        var distinctNamespaces = UsableType.GetDistinctAdditionalNamespaces(allUsedTypes)
            .Where(x => !string.IsNullOrEmpty(x)).ToArray();
        var returnVal                                                 = "";
        for (var i = 0; i < distinctNamespaces.Length; i++) returnVal += "using " + distinctNamespaces[i] + ";\n";

        return returnVal;
    }

    private string ExposedReferencesToString()
    {
        var expRefText                                            = "";
        foreach (var expRef in this.exposedReferences) expRefText += k_Tab + "public ExposedReference<" + expRef.usableType.name + "> " + expRef.name + ";\n";
        return expRefText;
    }

    private string ExposedReferencesResolvingToString()
    {
        var returnVal = "";
        returnVal += k_Tab + k_Tab + this.playableName + k_TimelineClipBehaviourSuffix + " clone = playable.GetBehaviour ();\n";
        for (var i = 0; i < this.exposedReferences.Count; i++) returnVal += k_Tab + k_Tab + "clone." + this.exposedReferences[i].name + " = " + this.exposedReferences[i].name + ".Resolve (graph.GetResolver ());\n";

        return returnVal;
    }

    /*string OnCreateFunctionToString ()
    {
        if (!setClipDefaults)
            return "";

        string returnVal = "\n";
            returnVal += k_Tab + "public override void OnCreate ()\n";
            returnVal += k_Tab + "{\n";
            returnVal += k_Tab + k_Tab + "owner.duration = " + clipDefaultDurationSeconds + ";\n";
            returnVal += k_Tab + k_Tab + "owner.easeInDuration = " + clipDefaultEaseInSeconds + ";\n";
            returnVal += k_Tab + k_Tab + "owner.easeOutDuration = " + clipDefaultEaseOutSeconds + ";\n";
            returnVal += k_Tab + k_Tab + "owner.clipIn = " + clipDefaultClipInSeconds + ";\n";
            returnVal += k_Tab + k_Tab + "owner.timeScale = " + clipDefaultSpeedMultiplier + ";\n";
            returnVal += k_Tab + "}\n";
        return returnVal;
    }*/

    private string ClipCapsToString()
    {
        var message = this.clipCaps.ToString();
        var splits  = message.Split(' ');

        for (var i = 0; i < splits.Length; i++)
            if (splits[i][splits[i].Length - 1] == ',')
                splits[i] = splits[i].Substring(0, splits[i].Length - 1);

        var returnVal = "";

        for (var i = 0; i < splits.Length; i++)
        {
            returnVal += "ClipCaps." + splits[i];

            if (i < splits.Length - 1) returnVal += " | ";
        }

        return returnVal;
    }

    private string ExposedReferencesAsScriptVariablesToString()
    {
        var returnVal                                                    = "";
        for (var i = 0; i < this.exposedReferences.Count; i++) returnVal += k_Tab + "public " + this.exposedReferences[i].usableType.name + " " + this.exposedReferences[i].name + ";\n";

        return returnVal;
    }

    private string PlayableBehaviourVariablesToString()
    {
        var returnVal                                                             = "";
        for (var i = 0; i < this.playableBehaviourVariables.Count; i++) returnVal += k_Tab + "public " + this.playableBehaviourVariables[i].usableType.name + " " + this.playableBehaviourVariables[i].name + ";\n";

        return returnVal;
    }

    private string MixerTrackBindingLocalVariableToString()
    {
        if (this.m_TrackBindingTypeIndex != 0)
            return
                k_Tab + k_Tab + this.trackBinding.name + " trackBinding = playerData as " + this.trackBinding.name + ";\n\n" + k_Tab + k_Tab + "if (!trackBinding)\n" + k_Tab + k_Tab + k_Tab + "return;\n" + "\n";
        return "";
    }

    private string ScriptVariablesAsSerializedPropAssignmentToString()
    {
        var returnVal                                                             = "";
        for (var i = 0; i < this.playableBehaviourVariables.Count; i++) returnVal += k_Tab + k_Tab + "SerializedProperty " + this.playableBehaviourVariables[i].name + "Prop = property.FindPropertyRelative(\"" + this.playableBehaviourVariables[i].name + "\");\n";

        return returnVal;
    }

    private string ScriptVariablesAsSerializedPropGUIToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.playableBehaviourVariables.Count; i++)
        {
            returnVal += k_Tab + k_Tab + "EditorGUI.PropertyField(singleFieldRect, " + this.playableBehaviourVariables[i].name + "Prop);\n";

            if (i < this.playableBehaviourVariables.Count - 1)
            {
                returnVal += "\n";
                returnVal += k_Tab + k_Tab + "singleFieldRect.y += EditorGUIUtility.singleLineHeight;\n";
            }
        }

        return returnVal;
    }

    private string StandardBlendPlayableAsset()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + "\n" + "[Serializable]\n" + "public class " + this.playableName + k_TimelineClipAssetSuffix + " : PlayableAsset, ITimelineClipAsset\n" + "{\n" + k_Tab + "public " + this.playableName + k_TimelineClipBehaviourSuffix + " template = new " + this.playableName + k_TimelineClipBehaviourSuffix + " ();\n" + "\n" + k_Tab + "public ClipCaps clipCaps\n" + k_Tab + "{\n" + k_Tab + k_Tab + "get { return ClipCaps.Blending; }\n" + k_Tab + "}\n" + "\n" + k_Tab + "public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "var playable = ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + ">.Create (graph, template);\n" + k_Tab + k_Tab + "return playable;\n" + k_Tab + "}\n" + "}\n";
    }

    private string StandardBlendPlayableBehaviour()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "[Serializable]\n" + "public class " + this.playableName + k_TimelineClipBehaviourSuffix + " : PlayableBehaviour\n" + "{\n" + this.StandardBlendScriptPlayablePropertiesToString() + "}\n";
    }

    private string StandardBlendPlayableBehaviourMixer()
    {
        return
            "using System;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + "using UnityEngine.Timeline;\n" + this.AdditionalNamespacesToString() + "\n" + "public class " + this.playableName + k_PlayableBehaviourMixerSuffix + " : PlayableBehaviour\n" + "{\n" + this.StandardBlendTrackBindingPropertiesDefaultsDeclarationToString() + "\n" + this.StandardBlendTrackBindingPropertiesBlendedDeclarationToString() + "\n" + k_Tab + this.trackBinding.name + " m_TrackBinding;\n" + "\n" + k_Tab + "public override void ProcessFrame(Playable playable, FrameData info, object playerData)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "m_TrackBinding = playerData as " + this.trackBinding.name + ";\n" + "\n" + k_Tab + k_Tab + "if (m_TrackBinding == null)\n" + k_Tab + k_Tab + k_Tab + "return;\n" + "\n" + this.StandardBlendTrackBindingPropertiesDefaultsAssignmentToString() + "\n" + k_Tab + k_Tab + "int inputCount = playable.GetInputCount ();\n" + "\n" + this.StandardBlendBlendedVariablesCreationToString() + k_Tab + k_Tab + "float totalWeight = 0f;\n" + k_Tab + k_Tab + "float greatestWeight = 0f;\n" + this.StandardBlendPlayableCurrentInputsDeclarationToString() + "\n" + k_Tab + k_Tab + "for (int i = 0; i < inputCount; i++)\n" + k_Tab + k_Tab + "{\n" + k_Tab + k_Tab + k_Tab + "float inputWeight = playable.GetInputWeight(i);\n" + k_Tab + k_Tab + k_Tab + "ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + "> inputPlayable = (ScriptPlayable<" + this.playableName + k_TimelineClipBehaviourSuffix + ">)playable.GetInput(i);\n" + k_Tab + k_Tab + k_Tab + this.playableName + k_TimelineClipBehaviourSuffix + " input = inputPlayable.GetBehaviour ();\n" + k_Tab + k_Tab + k_Tab + "\n" + this.StandardBlendBlendedVariablesWeightedIncrementationToString() + k_Tab + k_Tab + k_Tab + "totalWeight += inputWeight;\n" + "\n" + this.StandardBlendAssignableVariablesAssignedBasedOnGreatestWeightToString() + this.StandardBlendPlayableCurrentInputIterationToString() + k_Tab + k_Tab + "}\n" + this.StandardBlendTrackBindingPropertiesBlendedAssignmentToString() + this.StandardBlendTrackBindingPropertiesAssignableAssignmentToString() + k_Tab + "}\n" + "}\n";
    }

    private string StandardBlendTrackAssetScript()
    {
        return
            "using UnityEngine;\n"
            + "using UnityEngine.Playables;\n"
            + "using UnityEngine.Timeline;\n"
            + "using System.Collections.Generic;\n"
            + this.AdditionalNamespacesToString()
            + "\n"
            + "[TrackColor("
            + this.trackColor.r
            + "f, "
            + this.trackColor.g
            + "f, "
            + this.trackColor.b
            + "f)]\n"
            + "[TrackClipType(typeof("
            + this.playableName
            + k_TimelineClipAssetSuffix
            + "))]\n"
            + this.StandardBlendComponentBindingToString()
            + "public class "
            + this.playableName
            + k_TrackAssetSuffix
            + " : TrackAsset\n"
            + "{\n"
            + k_Tab
            + "public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)\n"
            + k_Tab
            + "{\n"
            + k_Tab
            + k_Tab
            + "return ScriptPlayable<"
            + this.playableName
            + k_PlayableBehaviourMixerSuffix
            + ">.Create (graph, inputCount);\n"
            + k_Tab
            + "}\n"
            + "\n"
            + k_Tab
            + "// Please note this assumes only one component of type "
            + this.trackBinding.name
            + " on the same gameobject.\n"
            + k_Tab
            + "public override void GatherProperties (PlayableDirector director, IPropertyCollector driver)\n"
            + k_Tab
            + "{\n"
            + "#if UNITY_EDITOR\n"
            + k_Tab
            + k_Tab
            + this.trackBinding.name
            + " trackBinding = director.GetGenericBinding(this) as "
            + this.trackBinding.name
            + ";\n"
            + k_Tab
            + k_Tab
            + "if (trackBinding == null)\n"
            + k_Tab
            + k_Tab
            + k_Tab
            + "return;\n"
            + "\n"
            +
            //k_Tab + k_Tab + "var serializedObject = new UnityEditor.SerializedObject (trackBinding);\n" +
            //k_Tab + k_Tab + "var iterator = serializedObject.GetIterator();\n" +
            //k_Tab + k_Tab + "while (iterator.NextVisible(true))\n" +
            //k_Tab + k_Tab + "{\n" +
            //k_Tab + k_Tab + k_Tab + "if (iterator.hasVisibleChildren)\n" +
            //k_Tab + k_Tab + k_Tab + k_Tab + "continue;\n" +
            //"\n" +
            //k_Tab + k_Tab + k_Tab + "driver.AddFromName<" + trackBinding.name + ">(trackBinding.gameObject, iterator.propertyPath);\n" +
            //k_Tab + k_Tab + "}\n" +
            this.StandardBlendPropertiesAssignedToPropertyDriverToString()
            + "#endif\n"
            + k_Tab
            + k_Tab
            + "base.GatherProperties (director, driver);\n"
            + k_Tab
            + "}\n"
            + "}\n";
    }

    private string StandardBlendPlayableDrawer()
    {
        return
            "using UnityEditor;\n" + "using UnityEngine;\n" + "using UnityEngine.Playables;\n" + this.AdditionalNamespacesToString() + "\n" + "[CustomPropertyDrawer(typeof(" + this.playableName + k_TimelineClipBehaviourSuffix + "))]\n" + "public class " + this.playableName + k_PropertyDrawerSuffix + " : PropertyDrawer\n" + "{\n" + k_Tab + "public override float GetPropertyHeight (SerializedProperty property, GUIContent label)\n" + k_Tab + "{\n" + k_Tab + k_Tab + "int fieldCount = " + this.standardBlendPlayableProperties.Count + ";\n" + k_Tab + k_Tab + "return fieldCount * EditorGUIUtility.singleLineHeight;\n" + k_Tab + "}\n" + "\n" + k_Tab + "public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)\n" + k_Tab + "{\n" + this.StandardBlendTrackBindingPropertiesAsSerializedPropsDeclarationToString() + "\n" + k_Tab + k_Tab + "Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);\n" + this.StandardBlendSerializedPropertyGUIToString() + k_Tab + "}\n" + "}\n";
    }

    private string StandardBlendScriptPlayablePropertiesToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];
            if (prop.defaultValue == "")
                returnVal += k_Tab + "public " + prop.type + " " + prop.name + ";\n";
            else
                returnVal += k_Tab + "public " + prop.type + " " + prop.name + " = " + prop.defaultValue + ";\n";
        }

        return returnVal;
    }

    private string StandardBlendTrackBindingPropertiesDefaultsDeclarationToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];
            returnVal += k_Tab + prop.type + " " + prop.NameAsPrivateDefault + ";\n";
        }

        return returnVal;
    }

    private string StandardBlendTrackBindingPropertiesBlendedDeclarationToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];

            returnVal += k_Tab + prop.type + " " + prop.NameAsPrivateAssigned + ";\n";
        }

        return returnVal;
    }

    private string StandardBlendTrackBindingPropertiesDefaultsAssignmentToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];

            switch (prop.type)
            {
                case "float":
                    returnVal += k_Tab + k_Tab + "if (!Mathf.Approximately(m_TrackBinding." + prop.name + ", " + prop.NameAsPrivateAssigned + "))\n";
                    returnVal += k_Tab + k_Tab + k_Tab + prop.NameAsPrivateDefault + " = m_TrackBinding." + prop.name + ";\n";
                    break;
                case "double":
                    returnVal += k_Tab + k_Tab + "if (!Mathf.Approximately((float)m_TrackBinding." + prop.name + ", (float)" + prop.NameAsPrivateAssigned + "))\n";
                    returnVal += k_Tab + k_Tab + k_Tab + prop.NameAsPrivateDefault + " = m_TrackBinding." + prop.name + ";\n";
                    break;
                default:
                    returnVal += k_Tab + k_Tab + "if (m_TrackBinding." + prop.name + " != " + prop.NameAsPrivateAssigned + ")\n";
                    returnVal += k_Tab + k_Tab + k_Tab + prop.NameAsPrivateDefault + " = m_TrackBinding." + prop.name + ";\n";
                    break;
            }
        }

        return returnVal;
    }

    private string StandardBlendBlendedVariablesCreationToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];

            if (prop.usability != UsableProperty.Usability.Blendable) continue;

            var type    = prop.type == "int" ? "float" : prop.type;
            var zeroVal = prop.type == "int" ? "0f" : prop.ZeroValueAsString();
            returnVal += k_Tab + k_Tab + type + " " + prop.NameAsLocalBlended + " = " + zeroVal + ";\n";
        }

        return returnVal;
    }

    private string StandardBlendPlayableCurrentInputsDeclarationToString()
    {
        if (this.standardBlendPlayableProperties.Any(x => x.usability == UsableProperty.Usability.Assignable)) return k_Tab + k_Tab + "int currentInputs = 0;\n";

        return "";
    }

    private string StandardBlendBlendedVariablesWeightedIncrementationToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];

            if (prop.usability == UsableProperty.Usability.Blendable) returnVal += k_Tab + k_Tab + k_Tab + prop.NameAsLocalBlended + " += input." + prop.name + " * inputWeight;\n";
        }

        return returnVal;
    }

    private string StandardBlendAssignableVariablesAssignedBasedOnGreatestWeightToString()
    {
        if (this.standardBlendPlayableProperties.Count == 0) return "";

        var returnVal = k_Tab + k_Tab + k_Tab + "if (inputWeight > greatestWeight)\n";
        returnVal += k_Tab + k_Tab + k_Tab + "{\n";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];
            if (prop.usability == UsableProperty.Usability.Assignable)
            {
                returnVal += k_Tab + k_Tab + k_Tab + k_Tab + prop.NameAsPrivateAssigned + " = input." + prop.name + ";\n";
                returnVal += k_Tab + k_Tab + k_Tab + k_Tab + "m_TrackBinding." + prop.name + " = " + prop.NameAsPrivateAssigned + ";\n";
            }
        }

        returnVal += k_Tab + k_Tab + k_Tab + k_Tab + "greatestWeight = inputWeight;\n";
        returnVal += k_Tab + k_Tab + k_Tab + "}\n";
        return returnVal;
    }

    private string StandardBlendPlayableCurrentInputIterationToString()
    {
        if (this.standardBlendPlayableProperties.Any(x => x.usability == UsableProperty.Usability.Assignable))
        {
            var returnVal = "\n";
            returnVal += k_Tab + k_Tab + k_Tab + "if (!Mathf.Approximately (inputWeight, 0f))\n";
            returnVal += k_Tab + k_Tab + k_Tab + k_Tab + "currentInputs++;\n";
            return returnVal;
        }

        return "";
    }

    private string StandardBlendTrackBindingPropertiesBlendedAssignmentToString()
    {
        var returnVal    = "";
        var firstNewLine = false;
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];
            if (prop.usability != UsableProperty.Usability.Blendable) continue;

            if (!firstNewLine)
            {
                firstNewLine =  true;
                returnVal    += "\n";
            }

            if (prop.type == "int")
                returnVal += k_Tab + k_Tab + prop.NameAsPrivateAssigned + " = Mathf.RoundToInt (" + prop.NameAsLocalBlended + " + " + prop.NameAsPrivateDefault + " * (1f - totalWeight));\n";
            else
                returnVal += k_Tab + k_Tab + prop.NameAsPrivateAssigned + " = " + prop.NameAsLocalBlended + " + " + prop.NameAsPrivateDefault + " * (1f - totalWeight);\n";

            returnVal += k_Tab + k_Tab + "m_TrackBinding." + prop.name + " = " + prop.NameAsPrivateAssigned + ";\n";
        }

        return returnVal;
    }

    private string StandardBlendTrackBindingPropertiesAssignableAssignmentToString()
    {
        if (this.standardBlendPlayableProperties.Count == 0) return "";

        if (this.standardBlendPlayableProperties.Any(x => x.usability == UsableProperty.Usability.Assignable))
        {
            var returnVal = "\n" + k_Tab + k_Tab + "if (currentInputs != 1 && 1f - totalWeight > greatestWeight)\n";
            returnVal += k_Tab + k_Tab + "{\n";
            for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
            {
                var prop = this.standardBlendPlayableProperties[i];
                if (prop.usability != UsableProperty.Usability.Assignable) continue;

                returnVal += k_Tab + k_Tab + k_Tab + "m_TrackBinding." + prop.name + " = " + prop.NameAsPrivateDefault + ";\n";
            }

            returnVal += k_Tab + k_Tab + "}\n";
            return returnVal;
        }

        return "";
    }

    private string StandardBlendComponentBindingToString()
    {
        return "[TrackBindingType(typeof(" + this.trackBinding.name + "))]\n";
    }

    private string StandardBlendPropertiesAssignedToPropertyDriverToString()
    {
        if (this.standardBlendPlayableProperties.Count == 0) return "";

        var returnVal = k_Tab + k_Tab + "// These field names are procedurally generated estimations based on the associated property names.\n";
        returnVal += k_Tab + k_Tab + "// If any of the names are incorrect you will get a DrivenPropertyManager error saying it has failed to register the name.\n";
        returnVal += k_Tab + k_Tab + "// In this case you will need to find the correct backing field name.\n";
        returnVal += k_Tab + k_Tab + "// The suggested way of finding the field name is to:\n";
        returnVal += k_Tab + k_Tab + "// 1. Make sure your scene is serialized to text.\n";
        returnVal += k_Tab + k_Tab + "// 2. Search the text for the track binding component type.\n";
        returnVal += k_Tab + k_Tab + "// 3. Look through the field names until you see one that looks correct.\n";

        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];

            if (prop.usablePropertyType == UsableProperty.UsablePropertyType.Field)
                returnVal += k_Tab + k_Tab + "driver.AddFromName<" + this.trackBinding.name + ">(trackBinding.gameObject, \"" + prop.name + "\");\n";
            else
                returnVal += k_Tab + k_Tab + "driver.AddFromName<" + this.trackBinding.name + ">(trackBinding.gameObject, \"" + prop.NameAsPrivate + "\");\n";
        }

        return returnVal;
    }

    private string StandardBlendTrackBindingPropertiesAsSerializedPropsDeclarationToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            var prop = this.standardBlendPlayableProperties[i];
            returnVal += k_Tab + k_Tab + "SerializedProperty " + prop.NameAsLocalSerializedProperty + " = property.FindPropertyRelative(\"" + prop.name + "\");\n";
        }

        return returnVal;
    }

    private string StandardBlendSerializedPropertyGUIToString()
    {
        var returnVal = "";
        for (var i = 0; i < this.standardBlendPlayableProperties.Count; i++)
        {
            if (i != 0)
            {
                returnVal += "\n";
                returnVal += k_Tab + k_Tab + "singleFieldRect.y += EditorGUIUtility.singleLineHeight;\n";
            }

            returnVal += k_Tab + k_Tab + "EditorGUI.PropertyField(singleFieldRect, " + this.standardBlendPlayableProperties[i].NameAsLocalSerializedProperty + ");\n";
        }

        return returnVal;
    }

    public class Variable : IComparable
    {
        private int        m_TypeIndex;
        public  string     name;
        public  UsableType usableType;

        public Variable(string name, UsableType usableType)
        {
            this.name       = name;
            this.usableType = usableType;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var other = (UsableType)obj;

            if (other == null) throw new ArgumentException("This object is not a Variable.");

            return this.name.ToLower().CompareTo(other.name.ToLower());
        }

        public bool GUI(UsableType[] usableTypes)
        {
            var removeThis = false;
            EditorGUILayout.BeginHorizontal();
            this.name        = EditorGUILayout.TextField(this.name);
            this.m_TypeIndex = EditorGUILayout.Popup(this.m_TypeIndex, UsableType.GetNamewithSortingArray(usableTypes));
            this.usableType  = usableTypes[this.m_TypeIndex];
            if (GUILayout.Button("Remove", GUILayout.Width(60f))) removeThis = true;

            EditorGUILayout.EndHorizontal();

            return removeThis;
        }

        public static UsableType[] GetUsableTypesFromVariableArray(Variable[] variables)
        {
            var usableTypes                                             = new UsableType[variables.Length];
            for (var i = 0; i < usableTypes.Length; i++) usableTypes[i] = variables[i].usableType;

            return usableTypes;
        }
    }

    public class UsableType : IComparable
    {
        public const string blankAdditionalNamespace = "";

        private const   string     k_NameForNullType = "None";
        public readonly string     additionalNamespace;
        public readonly GUIContent guiContentWithSorting;
        public readonly string     name;
        public readonly string     nameWithSorting;
        public readonly Type       type;

        public readonly string[] unrequiredNamespaces =
        {
            "UnityEngine",
            "UnityEngine.Timeline",
            "UnityEngine.Playables",
        };

        public UsableType(Type usableType)
        {
            this.type = usableType;

            if (this.type != null)
            {
                this.name            = usableType.Name;
                this.nameWithSorting = this.name.ToUpper()[0] + "/" + this.name;
                this.additionalNamespace = this.unrequiredNamespaces.All(t => usableType.Namespace != t)
                    ? usableType.Namespace
                    : blankAdditionalNamespace;
            }
            else
            {
                this.name                = k_NameForNullType;
                this.nameWithSorting     = k_NameForNullType;
                this.additionalNamespace = blankAdditionalNamespace;
            }

            this.guiContentWithSorting = new(this.nameWithSorting);
        }

        public UsableType(string name)
        {
            this.name                  = name;
            this.nameWithSorting       = name.ToUpper()[0] + "/" + name;
            this.additionalNamespace   = blankAdditionalNamespace;
            this.guiContentWithSorting = new(this.nameWithSorting);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var other = (UsableType)obj;

            if (other == null) throw new ArgumentException("This object is not a UsableType.");

            return this.name.ToLower().CompareTo(other.name.ToLower());
        }

        public static UsableType[] GetUsableTypeArray(Type[] types, params UsableType[] additionalUsableTypes)
        {
            var usableTypeList = new List<UsableType>();
            for (var i = 0; i < types.Length; i++) usableTypeList.Add(new(types[i]));

            usableTypeList.AddRange(additionalUsableTypes);
            return usableTypeList.ToArray();
        }

        public static UsableType[] AmalgamateUsableTypes(UsableType[] usableTypeArray, params UsableType[] usableTypes)
        {
            var usableTypeList = new List<UsableType>();
            for (var i = 0; i < usableTypes.Length; i++) usableTypeList.Add(usableTypes[i]);

            usableTypeList.AddRange(usableTypeArray);
            return usableTypeList.ToArray();
        }

        public static string[] GetNamewithSortingArray(UsableType[] usableTypes)
        {
            if (usableTypes == null || usableTypes.Length == 0) return new string[0];

            var displayNames                                              = new string[usableTypes.Length];
            for (var i = 0; i < displayNames.Length; i++) displayNames[i] = usableTypes[i].nameWithSorting;

            return displayNames;
        }

        public static GUIContent[] GetGUIContentWithSortingArray(UsableType[] usableTypes)
        {
            if (usableTypes == null || usableTypes.Length == 0) return new GUIContent[0];

            var guiContents                                             = new GUIContent[usableTypes.Length];
            for (var i = 0; i < guiContents.Length; i++) guiContents[i] = usableTypes[i].guiContentWithSorting;

            return guiContents;
        }

        public static string[] GetDistinctAdditionalNamespaces(UsableType[] usableTypes)
        {
            if (usableTypes == null || usableTypes.Length == 0) return new string[0];

            var namespaceArray                                                = new string[usableTypes.Length];
            for (var i = 0; i < namespaceArray.Length; i++) namespaceArray[i] = usableTypes[i].additionalNamespace;

            return namespaceArray.Distinct().ToArray();
        }
    }

    public class UsableProperty : IComparable
    {
        public enum Usability
        {
            Blendable,
            Assignable,
            Not,
        }

        public enum UsablePropertyType
        {
            Property,
            Field,
        }

        public string    defaultValue;
        public FieldInfo fieldInfo;

        private int          m_TypeIndex;
        public  string       name;
        public  PropertyInfo propertyInfo;

        public string             type;
        public Usability          usability;
        public UsablePropertyType usablePropertyType;

        public UsableProperty(PropertyInfo propertyInfo)
        {
            this.usablePropertyType = UsablePropertyType.Property;
            this.propertyInfo       = propertyInfo;

            if (propertyInfo.PropertyType.Name == "Single")
                this.type = "float";
            else if (propertyInfo.PropertyType.Name == "Int32")
                this.type = "int";
            else if (propertyInfo.PropertyType.Name == "Double")
                this.type = "double";
            else if (propertyInfo.PropertyType.Name == "Boolean")
                this.type = "bool";
            else if (propertyInfo.PropertyType.Name == "String")
                this.type = "string";
            else
                this.type = propertyInfo.PropertyType.Name;

            this.name = propertyInfo.Name;

            if (IsTypeBlendable(propertyInfo.PropertyType))
                this.usability = Usability.Blendable;
            else if (IsTypeAssignable(propertyInfo.PropertyType))
                this.usability = Usability.Assignable;
            else
                this.usability = Usability.Not;
        }

        public UsableProperty(FieldInfo fieldInfo)
        {
            this.usablePropertyType = UsablePropertyType.Field;
            this.fieldInfo          = fieldInfo;

            if (fieldInfo.FieldType.Name == "Single")
                this.type = "float";
            else if (fieldInfo.FieldType.Name == "Int32")
                this.type = "int";
            else if (fieldInfo.FieldType.Name == "Double")
                this.type = "double";
            else if (fieldInfo.FieldType.Name == "Boolean")
                this.type = "bool";
            else if (fieldInfo.FieldType.Name == "String")
                this.type = "string";
            else
                this.type = fieldInfo.FieldType.Name;

            this.name = fieldInfo.Name;

            if (IsTypeBlendable(fieldInfo.FieldType))
                this.usability = Usability.Blendable;
            else if (IsTypeAssignable(fieldInfo.FieldType))
                this.usability = Usability.Assignable;
            else
                this.usability = Usability.Not;
        }

        public string NameWithCaptial => this.name.First().ToString().ToUpper() + this.name.Substring(1);

        public string NameAsPrivate => "m_" + this.NameWithCaptial;

        public string NameAsPrivateDefault => "m_Default" + this.NameWithCaptial;

        public string NameAsPrivateAssigned => "m_Assigned" + this.NameWithCaptial;

        public string NameAsLocalBlended => "blended" + this.NameWithCaptial;

        public string NameAsLocalSerializedProperty => this.name + "Prop";

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var other = (UsableType)obj;

            if (other == null) throw new ArgumentException("This object is not a UsableProperty.");

            return this.name.ToLower().CompareTo(other.name.ToLower());
        }

        public string ZeroValueAsString()
        {
            if (this.usability != Usability.Blendable) throw new UnityException("UsableType is not blendable, shouldn't be looking for zero value as string.");

            switch (this.type)
            {
                case "float":   return "0f";
                case "int":     return "0";
                case "double":  return "0.0";
                case "Vector2": return "Vector2.zero";
                case "Vector3": return "Vector3.zero";
                case "Color":   return "Color.clear";
            }

            return "";
        }

        public void CreateSettingDefaultValueString(Component defaultValuesComponent)
        {
            if (defaultValuesComponent == null)
            {
                this.defaultValue = "";
                return;
            }

            var defaultValueObj = this.usablePropertyType == UsablePropertyType.Property
                ? this.propertyInfo.GetValue(defaultValuesComponent, null)
                : this.fieldInfo.GetValue(defaultValuesComponent);

            switch (this.type)
            {
                case "float":
                    var defaultFloatValue = (float)defaultValueObj;
                    this.defaultValue = defaultFloatValue + "f";
                    break;
                case "int":
                    var defaultIntValue = (int)defaultValueObj;
                    this.defaultValue = defaultIntValue.ToString();
                    break;
                case "double":
                    var defaultDoubleValue = (double)defaultValueObj;
                    this.defaultValue = defaultDoubleValue.ToString();
                    break;
                case "Vector2":
                    var defaultVector2Value = (Vector2)defaultValueObj;
                    this.defaultValue = "new Vector2(" + defaultVector2Value.x + "f, " + defaultVector2Value.y + "f)";
                    break;
                case "Vector3":
                    var defaultVector3Value = (Vector3)defaultValueObj;
                    this.defaultValue = "new Vector3(" + defaultVector3Value.x + "f, " + defaultVector3Value.y + "f, " + defaultVector3Value.z + "f)";
                    break;
                case "Color":
                    var defaultColorValue = (Color)defaultValueObj;
                    this.defaultValue = "new Color(" + defaultColorValue.r + "f, " + defaultColorValue.g + "f, " + defaultColorValue.b + "f, " + defaultColorValue.a + "f)";
                    break;
                case "string":
                    this.defaultValue = "\"" + defaultValueObj + "\"";
                    break;
                case "bool":
                    var defaultBoolValue = (bool)defaultValueObj;
                    this.defaultValue = defaultBoolValue.ToString().ToLower();
                    break;
                default:
                    var defaultEnumValue = (Enum)defaultValueObj;
                    var enumSystemType   = defaultEnumValue.GetType();
                    var splits           = enumSystemType.ToString().Split('+');
                    var enumType         = splits[splits.Length - 1];
                    var enumConstantName = Enum.GetName(enumSystemType, defaultEnumValue);
                    this.defaultValue = enumType + "." + enumConstantName;
                    break;
            }
        }

        public bool GUI(List<UsableProperty> allUsableProperties)
        {
            var removeThis = false;
            EditorGUILayout.BeginHorizontal();
            this.m_TypeIndex        = EditorGUILayout.Popup(this.m_TypeIndex, GetNameWithSortingArray(allUsableProperties));
            this.type               = allUsableProperties[this.m_TypeIndex].type;
            this.name               = allUsableProperties[this.m_TypeIndex].name;
            this.usablePropertyType = allUsableProperties[this.m_TypeIndex].usablePropertyType;
            this.propertyInfo       = allUsableProperties[this.m_TypeIndex].propertyInfo;
            this.fieldInfo          = allUsableProperties[this.m_TypeIndex].fieldInfo;
            this.usability          = allUsableProperties[this.m_TypeIndex].usability;
            if (GUILayout.Button("Remove", GUILayout.Width(60f))) removeThis = true;

            EditorGUILayout.EndHorizontal();
            return removeThis;
        }

        public static string[] GetNameWithSortingArray(List<UsableProperty> usableProperties)
        {
            var returnVal                                           = new string[usableProperties.Count];
            for (var i = 0; i < returnVal.Length; i++) returnVal[i] = usableProperties[i].name;

            return returnVal;
        }

        public UsableProperty GetDuplicate()
        {
            var duplicate = this.usablePropertyType == UsablePropertyType.Property
                ? new(this.propertyInfo)
                : new UsableProperty(this.fieldInfo);
            duplicate.defaultValue = this.defaultValue;
            return duplicate;
        }
    }
}
#endif