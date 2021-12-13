namespace DA_Assets.Exceptions
{
    class InvalidAuthException : CustomException
    {
        public InvalidAuthException() 
            : base(string.Format("Authentication aborted or failed."))
        {

        }
    }
}