namespace Mech.GameManager
{
    using Mech.Scenes.LoadingScene.LoginScreen;

    /// <summary>
    /// Manage all the local data in Game
    /// </summary>
    public partial class GameFoundationLocalData
    {
        // user data
        public UserDataLogin    UserDataLogin  { get; set; } = new UserDataLogin();
        public ServerToken      ServerToken    { get; set; } = new ServerToken();
        public BlueprintModel   BlueprintModel { get; set; } = new BlueprintModel();
    }

    public class UserDataLogin
    {
        public TypeLogIn  LastLogin     { get; set; }
        public LoginModel FacebookLogin { get; set; } = new LoginModel();
        public LoginModel GoogleLogin   { get; set; } = new LoginModel();
    }

    public class LoginModel
    {
        public string UserName    { get; set; }
        public string URLImage    { get; set; }
        public string AccessToken { get; set; }
    }

    public class ServerToken
    {
        public string RefreshToken   { get; set; }
        public long   ExpirationTime { get; set; }
        public string JwtToken       { get; set; }
    }

    public class BlueprintModel
    {
        public string BlueprintDownloadUrl { get; set; }
    }
}