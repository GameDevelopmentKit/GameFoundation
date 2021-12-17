namespace GameFoundation.Scripts.Models
{
    using UnityEngine;

    public class PlayerData
    {
        public string    Email         { get; set; } = "";
        public string    Name          { get; set; } = "";
        public string    Avatar        { get; set; } = "";
        public Texture2D AvatarTexture { get; set; } = null;
    }
}