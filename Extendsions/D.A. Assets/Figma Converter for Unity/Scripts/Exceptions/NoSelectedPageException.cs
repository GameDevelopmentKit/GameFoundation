#if UNITY_EDITOR
namespace DA_Assets.Exceptions
{
    class NoSelectedPageException : CustomException
    {
        public NoSelectedPageException() 
            : base(string.Format("The page for exporting frames is not selected."))
        {

        }
    }
}
#endif