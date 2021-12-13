#if UNITY_EDITOR
namespace DA_Assets.Exceptions
{
    public class MissingAssetException : CustomException
    {
        public MissingAssetException(string assetName)
            : base($"Asset '{assetName.TextBold()}' is missing. You can download it from Package Manager, or any other safe source. After you import the asset into the project, include the corresponding Define Symbol.")
        {

        }
    }
}
#endif