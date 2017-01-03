using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;
using data;
using structure;


namespace denNorske_gta5.gamemode
{
    public class Freeroam : Script
    {

        #region vars
        public static List<CarInfo> Cars = new List<CarInfo>(); //to save car objects
        public static List<Player> Players = new List<Player>(); // holds all the player objects
        Database db = new Database();
        userdatamanaging userdb = new userdatamanaging();
        #endregion

        #region Commands
        [Command("v", "~r~Usage: ~w~/v [car name] - Spawn a car. Use exact car name.",GreedyArg = true)]
        public void v(Client player, string carname)
        {
            VehicleHash vHash = API.vehicleNameToModel(carname);
            if (vHash == 0)
            {
                API.sendChatMessageToPlayer(player, "~r~Error; ~w~This car model doesn't exist.");
                return;
            }
            var pos = API.getEntityPosition(player);
            // var spawnPos = pos; // Car Spawnpoint
            var heading = API.getEntityRotation(player);

            var car = API.createVehicle(vHash, pos, new Vector3(0, 0, heading.Z), 0, 0);
            API.setEntityData(car, "RESPAWNABLE", false);
            API.setEntityData(car, "SPAWN_POS", pos);
            API.setEntityData(car, "SPAWN_ROT", heading);
            API.sendChatMessageToPlayer(player, "Server", "~w~A ~r~" + carname + "~w~ has been spawned for you. Enjoy!");
            API.setPlayerIntoVehicle(player, car, 0);
            CarInfo car2 = new CarInfo(car, player); //make a new object out of it
            Cars.Add(car2); //add to object list 
            foreach(Player pl in Players)
            {
                if(pl.player == player)
                {
                    pl.carHandle = car;
                }
            }




        }

        [Command("register", "~r~Usage: ~w~/register [password > 5 chars] - Saves your stats", GreedyArg = true, SensitiveInfo = true )]
        public void register(Client player, string password)
        {
            if (password.Length <= 5)
            {
                API.sendChatMessageToPlayer(player, "~r~Error: ~w~Password is not long enough, needs to be ~r~5~w~ or more characters");
            }
            else //password is long enough
            {
                //hash it
                string katt = BCrypt.Net.BCrypt.HashPassword(password);
                //save it

                foreach(Player pl in Players)
                {
                    if(pl.playerHandle == player.handle)
                    {
                        if (!userdb.userNameExist(player.name))
                        {
                            int uID = userdb.CreateAccount(player.name, katt);
                            if (uID != 0)
                            {
                                pl.userid = uID; //save the userid to the object
                                API.sendChatMessageToPlayer(player, "~g~Success! ~w~Your account has been saved. You have been logged in, remember the password for the next time!");
                                pl.logged_in = true;
                                break;
                            }
                            else //userid was equal to 0, so something went wrong
                            {
                                API.sendChatMessageToPlayer(player, "~r~Error: ~w~Something when wrong when creating your account. Please try again or contact an owner..");
                                break;
                            }
                        }
                        else //the user already exists
                        {
                            API.sendChatMessageToPlayer(player, "~r~Error:~w~ This name seems to be registered already. Please ~b~/login [pass]");
                        } 
                    }
                }//end loop  
            }
        }

        [Command("login", "~r~Usage: ~w~/login [password] - Logs you in to your account.", GreedyArg = true, SensitiveInfo = true)]
        public void login(Client player, string password)
        {
            if(userdb.userNameExist(player.name))
            {
                if (userdb.GetUserPass(player.name) == BCrypt.Net.BCrypt.HashPassword(password))
                {
                    foreach(Player pl in Players)
                    {
                        if(pl.playerHandle == player.handle)
                        {
                            pl.logged_in = true;
                            userdb.LoadUserStats(pl);
                        }
                    }
                }
                else
                {
                   
                    foreach (Player pl in Players)
                    {
                        if (pl.playerHandle == player.handle)
                        {

                            pl.wrongpass ++;
                            API.sendChatMessageToPlayer(player, "You entered the wrong password " + pl.wrongpass +" time(s)");
                            if(pl.wrongpass == 3)
                            {
                                API.sendChatMessageToPlayer(player, "You have been kicked. If you forgot your password, please contact an admin");
                                API.kickPlayer(player, "3 invalid password attempts");
                            }
                        }
                    }
                }
            }
            else
            {
                API.sendChatMessageToPlayer(player, "~r~Error: ~w~This name ("+ player.name+") is not registered before. Use ~r~/register");
            }
        }

        [Command("setlevel", GreedyArg = true)]
        public void setlevel(Client player, Client target, int aLevel)
        {
            foreach(Player pl in Players)
            {
                if(player.handle == pl.playerHandle)
                {
                    if(pl.level != 10)
                    {
                        API.sendChatMessageToPlayer(player, "~r~You are not permitted to perform this action!");
                        break;
                    }
                    else
                    {
                        foreach(Player cl in Players)
                        {
                            if(cl.player == target)
                            {
                                if(cl.level == aLevel)
                                {
                                    API.sendChatMessageToPlayer(player, "~r~Error: ~w~This player already has this admin level");
                                    break;
                                }
                                if (cl.level > aLevel) //demoted
                                    API.sendChatMessageToPlayer(target, "~r~Demoted! ~w~You were demoted to level " + aLevel + " by " + player.name);
                                cl.level = aLevel;
                            }
                        }
                    }
                }
            }
        }


        [Command("godmode")]
        public void godmode(Client player)
        {
            foreach(Player pl in Players)
            {
                if(pl.carHandle == player.handle)
                {
                    if (pl.logged_in == false)
                    {
                        API.sendChatMessageToPlayer(player, "~r~This command is only available for registered users. /register or /login please.");
                        return;
                    }
                    if (player.invincible)
                    {
                        player.invincible = false;
                        API.sendChatMessageToPlayer(player, "GodMode ~r~Disabled!");
                    }
                    else
                    {
                        player.invincible = true;
                        API.sendChatMessageToPlayer(player, "GodMode ~g~Enabled!");
                    }
                }
            }
        }

        #endregion

        #region other publics

        public Freeroam()
        {
            API.onResourceStart += resourcesStart;
            API.onPlayerConnected += onPlayerConnected;
            API.onVehicleDeath += onVehicleDeath;
            //API.onVehicleExplode += onVehicleExplode;

        }

        public void onPlayerConnected(Client sender)
        {
            API.sendChatMessageToPlayer(sender, "~o~Welcome! ");
            Players.Add(new Player(sender, sender.handle));
            API.setPlayerSkin(sender, API.pedNameToModel("Indian01AMY"));
            API.sendChatMessageToAll(sender.name + " has connected to the server!");
            API.sendChatMessageToPlayer(sender, "~g~Log in or register with ~w~/login~g~ or ~w~/register");

        }
        private void resourcesStart()
        {
            API.consoleOutput("Resorce has been loaded, and script from denNorske.gta5.freeroam has been loaded.");
        }


        public void onVehicleDeath(NetHandle handle)
        {
            if (API.getEntityData(handle, "RESPAWNABLE") == true)
            {
                API.delay(7000, true, () =>
                {
                    var color1 = API.getVehiclePrimaryColor(handle);
                    var color2 = API.getVehicleSecondaryColor(handle);
                    var model = API.getEntityModel(handle);

                    var spawnPos = API.getEntityData(handle, "SPAWN_POS");
                    var spawnH = API.getEntityData(handle, "SPAWN_ROT");

                    API.deleteEntity(handle);

                    API.createVehicle((VehicleHash)model, spawnPos, new Vector3(0, 0, spawnH), color1, color2);

                    API.setEntityData(handle, "SPAWN_POS", spawnPos);
                    API.setEntityData(handle, "SPAWN_ROT", spawnH);
                    API.setEntityData(handle, "RESPAWNABLE", true);
                });
            }
            else
            {

                foreach (CarInfo car in Cars)
                {
                    if (car.handle == handle)
                    {
                        Cars.Remove(car);
                        API.deleteEntity(car.handle);//delete the chassis
                    }
                }
            }   
                


                // ...
            

        }
      /*  public void onVehicleExplode(NetHandle vehicle)
        {
            string name;
            if (Player.VehDic.TryGetValue(vehicle, out name) == true)
            { 
                foreach(CarInfo i in Cars)
                {
                    if (i.handle == vehicle)
                    {
                        API.sendChatMessageToPlayer(i.player, "Mechanic", "~r~Your " + API.getEntityModel(i.handle) + " just exploded.");
                        Player.VehDic.Remove(i.handle); //remove the vehicle entry from the list
                        //Cars.Remove(i); //already called when dead above
                    }
                }
                
            }

        }
        
        {

        }*/
        #endregion
    }

  
}