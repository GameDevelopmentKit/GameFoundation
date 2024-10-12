namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimelineEvents
{
    using System;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    public class EventInvocationInfo
    {
        public        Behaviour  TargetBehaviour;
        public        MethodInfo MethodInfo;
        public static Type[]     SupportedTypes = { typeof(string), typeof(float), typeof(int), typeof(bool) };

        //used for tracking
        public string Key;

        public EventInvocationInfo(string key, Behaviour targetBehaviour, MethodInfo methodInfo)
        {
            this.Key             = key;
            this.MethodInfo      = methodInfo;
            this.TargetBehaviour = targetBehaviour;
        }

        public void Invoke(object value)
        {
            if (this.MethodInfo != null) this.MethodInfo.Invoke(this.TargetBehaviour, new[] { value });
        }

        public void InvokEnum(int value)
        {
            var type      = this.MethodInfo.GetParameters()[0].ParameterType;
            var enumValue = Enum.ToObject(type, value);
            if (this.MethodInfo != null) this.MethodInfo.Invoke(this.TargetBehaviour, new[] { enumValue });
        }

        public void InvokeNoArgs()
        {
            if (this.MethodInfo != null) this.MethodInfo.Invoke(this.TargetBehaviour, null);
        }

        public void Invoke(bool isSingleArg, string value)
        {
            try
            {
                if (isSingleArg)
                {
                    var paramType = this.MethodInfo.GetParameters()[0].ParameterType;
                    if (paramType.IsEnum)
                    {
                        this.Invoke(ConvertToType<int>(value));
                    }
                    else if (SupportedTypes.Contains(paramType))
                    {
                        if (paramType == typeof(string))
                            this.Invoke(value);
                        else if (paramType == typeof(int))
                            this.Invoke(ConvertToType<int>(value));
                        else if (paramType == typeof(float))
                            this.Invoke(ConvertToType<float>(value));
                        else if (paramType == typeof(bool))
                            this.Invoke(ConvertToType<bool>(value));
                        else
                            Debug.Log(
                                "Could not parse argument value " + value + " for method " + this.MethodInfo.Name + ". Ignoring");
                    }
                    else
                    {
                        Debug.Log("Could not parse argument value " + value + " for method " + this.MethodInfo.Name + ". Ignoring");
                    }
                }
                else
                {
                    this.InvokeNoArgs();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while executing timeline event " + this.MethodInfo.Name + " " + e);
            }
        }

        public static T ConvertToType<T>(string input)
        {
            var isConverted = false;
            var type        = typeof(T);
            if (type == typeof(string))
            {
                return (T)(object)input;
            }
            else if (type == typeof(float))
            {
                isConverted = float.TryParse(input, out var f);
                if (isConverted) return (T)(object)f;
            }
            else if (type == typeof(int))
            {
                isConverted = int.TryParse(input, out var i);
                if (isConverted) return (T)(object)i;
            }
            else if (type == typeof(bool))
            {
                isConverted = bool.TryParse(input, out var b);
                if (isConverted) return (T)(object)b;
            }

            return default;
        }

        public static bool IsValidAsType(string input, Type type)
        {
            var isConverted = false;
            if (type == typeof(string))
                isConverted = true;
            else if (type == typeof(float))
                isConverted = float.TryParse(input, out _);
            else if (type == typeof(int))
                isConverted                            = int.TryParse(input, out _);
            else if (type == typeof(bool)) isConverted = bool.TryParse(input, out _);

            return isConverted;
        }
    }
}