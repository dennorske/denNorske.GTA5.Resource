using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTANetwork;
using GTANetworkShared;
using GTANetworkServer;



public class Freeroam : Script
{
    public Freeroam()
    {
        API.onResourceStart += API_onResourceStart;
    }

    private void API_onResourceStart()
    {
        API.consoleOutput("Starting it up");
    }

    #region Commands
    [Command("v", GreedyArg = true)]
    public void spawnVehicle(Client player)
    {
        var pos = API.getEntityPosition(player);

        // var spawnPos = pos; // Car Spawnpoint
        var heading = API.getEntityRotation(player);

        var car = API.createVehicle(hash, pos, heading, 0, 0);
        API.setLocalEntityData(car, "RESPAWNABLE", true);
        API.setLocalEntityData(car, "SPAWN_POS", spawnPos);
        API.setLocalEntityData(car, "SPAWN_ROT", heading);
    }

    #endregion
}


