using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;
using System.IO;
using System.Diagnostics;

namespace denNorske_gta5.gamemode.structure
{
    public class CarInfo
    {
        public NetHandle handle;
        public Client player;

        public CarInfo(NetHandle handle, Client player)
        {
            this.player = player;
            this.handle = handle;

        }
    }
}
