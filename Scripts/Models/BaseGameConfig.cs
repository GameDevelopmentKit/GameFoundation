namespace Models
{
    using UnityEngine;

    public interface IGameConfig
    {
    }

    public abstract class BaseGameConfigSO : ScriptableObject,IGameConfig
    {
    }
}