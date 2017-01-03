using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTANetworkServer;
using GTANetworkShared;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using System.IO;
using System.Diagnostics;
using denNorske_gta5;

namespace structure
{
    public class Player
    {
        public NetHandle playerHandle;
        public Client player;
        public int userid;
        public int kills;
        public int deaths;
        public int level;
        public int score;
        public int cookies;
        public int hashouse;
        public int skin;
        public bool logged_in = false;
        public int wrongpass = 0;
        public NetHandle carHandle;
        public string playerName;
        public static Dictionary<NetHandle, string> VehDic = new Dictionary<NetHandle, string>();

        public Player(Client player, NetHandle playerHandle)
        {
            this.player = player;
            this.playerHandle = playerHandle;
            this.playerName = API.shared.getPlayerName(player);
        }
    }
}
