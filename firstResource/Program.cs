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
    public void spawnVehicle(Client player, string carname)
    {
        VehicleHash hash = API.vehicleNameToModel(carname);
        var pos = API.getEntityPosition(player);
        // var spawnPos = pos; // Car Spawnpoint
        var heading = API.getEntityRotation(player);

        var car = API.createVehicle(hash, pos, new Vector3(0,0, heading.Z), 0, 0);
        API.setEntityData(car, "RESPAWNABLE", true);
        API.setEntityData(car, "SPAWN_POS", pos);
        API.setEntityData(car, "SPAWN_ROT", heading);

    }

    #endregion
}


