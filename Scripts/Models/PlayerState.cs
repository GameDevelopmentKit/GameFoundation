namespace Mech.Models
{
    using System.Collections.Generic;
    using MechSharingCode.WebService.Inventory;
    using MechSharingCode.WebService.Leaderboards;

    public class PlayerState
    {
        public PlayerData       PlayerData { get; set; } = new PlayerData();
    }
}