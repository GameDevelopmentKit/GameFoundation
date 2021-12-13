namespace DA_Assets.Exceptions
{
    class InvalidApiKeyException : CustomException
    {
        public InvalidApiKeyException() 
            : base(string.Format("Need new authentication."))
        {

        }
    }
}
