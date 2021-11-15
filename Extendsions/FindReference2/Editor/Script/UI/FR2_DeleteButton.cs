using System;
using UnityEditor;
using UnityEngine;
using vietlabs.fr2;

public class FR2_DeleteButton
{
    public string     warningMessage;
    public string     confirmMessage;
    public GUIContent deleteLabel;
    public bool       hasConfirm;

    public bool Draw(Action onConfirmDelete)
    {
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.HelpBox(this.warningMessage, MessageType.Warning);
            GUILayout.BeginVertical();
            {
                GUILayout.Space(2f);
                this.hasConfirm = GUILayout.Toggle(this.hasConfirm, this.confirmMessage);
                EditorGUI.BeginDisabledGroup(!this.hasConfirm);
                {
                    GUI2.BackgroundColor(() =>
                    {
                        if (GUILayout.Button(this.deleteLabel, EditorStyles.miniButton))
                        {
                            this.hasConfirm = false;
                            onConfirmDelete();
                            GUIUtility.ExitGUI();
                        }
                    }, GUI2.darkRed, 0.8f);
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        return false;
    }
}