/*! \cond PRIVATE */
using System;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [InitializeOnLoad]
    // ReSharper disable once CheckNamespace
    public class AudioScriptOrderManager
    {
        static AudioScriptOrderManager()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == null)
                {
                    continue;
                }

                foreach (var a in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(AudioScriptOrder)))
                {
                    var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                    var newOrder = ((AudioScriptOrder) a).Order;
                    if (currentOrder != newOrder)
                    {
                        MonoImporter.SetExecutionOrder(monoScript, newOrder);
                    }
                }
            }
        }
    }
}
/*! \endcond */