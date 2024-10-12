//-----------------------------------------------------------------------
// <copyright file="VectorIntPropertyResolvers.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER

namespace Sirenix.OdinInspector.Editor.Drawers
{
    using UnityEngine;

    public sealed class Vector2IntResolver : BaseMemberPropertyResolver<Vector2Int>
    {
        protected override InspectorPropertyInfo[] GetPropertyInfos()
        {
            return new InspectorPropertyInfo[]
            {
                InspectorPropertyInfo.CreateValue("x",
                    0,
                    this.Property.ValueEntry.SerializationBackend,
                    new GetterSetter<Vector2Int, int>(
                        (ref Vector2Int vec) => vec.x,
                        (ref Vector2Int vec, int value) => vec.x = value)),
                InspectorPropertyInfo.CreateValue("y",
                    0,
                    this.Property.ValueEntry.SerializationBackend,
                    new GetterSetter<Vector2Int, int>(
                        (ref Vector2Int vec) => vec.y,
                        (ref Vector2Int vec, int value) => vec.y = value)),
            };
        }
    }

    public sealed class Vector3IntResolver : BaseMemberPropertyResolver<Vector3Int>
    {
        protected override InspectorPropertyInfo[] GetPropertyInfos()
        {
            return new InspectorPropertyInfo[]
            {
                InspectorPropertyInfo.CreateValue("x",
                    0,
                    this.Property.ValueEntry.SerializationBackend,
                    new GetterSetter<Vector3Int, int>(
                        (ref Vector3Int vec) => vec.x,
                        (ref Vector3Int vec, int value) => vec.x = value)),
                InspectorPropertyInfo.CreateValue("y",
                    0,
                    this.Property.ValueEntry.SerializationBackend,
                    new GetterSetter<Vector3Int, int>(
                        (ref Vector3Int vec) => vec.y,
                        (ref Vector3Int vec, int value) => vec.y = value)),
                InspectorPropertyInfo.CreateValue("z",
                    0,
                    this.Property.ValueEntry.SerializationBackend,
                    new GetterSetter<Vector3Int, int>(
                        (ref Vector3Int vec) => vec.z,
                        (ref Vector3Int vec, int value) => vec.z = value)),
            };
        }
    }
}

#endif // UNITY_EDITOR && UNITY_2017_2_OR_NEWER