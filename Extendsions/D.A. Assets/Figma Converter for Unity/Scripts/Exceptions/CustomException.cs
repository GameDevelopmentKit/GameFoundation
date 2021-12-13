using System;

namespace DA_Assets.Exceptions
{
    public class CustomException : Exception
    {
        public CustomException(string message) : base(message)
        {

        }

        public CustomException(Exception ex) : base(ex.Message)
        {

        }
    }
}