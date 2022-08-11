namespace GameFoundation.Scripts.Network.WebService.Models.UserData
{
    using System;

    public class UserInfo
    {
        public string    Id            { get; set; }
        public string    Name          { get; set; }
        public string    Avatar        { get; set; }
        public DateTime? Birthday      { get; set; }
        public string    WalletAddress { get; set; }
    }
}