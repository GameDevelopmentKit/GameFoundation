namespace Localization.Interfaces
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Localization.Blueprint;

    public interface ILocalizationElement
    {
        UniTask<string> ManualInitLocalization(List<LocalizationElementData> localizationElementDatas);
    }
}
