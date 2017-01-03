using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using GTANetworkShared;
using GTANetworkServer;
using System.IO;
using denNorske_gta5;
using System.Diagnostics;


namespace denNorske_gta5.gamemode.data
{
    public class Database
    {
        public MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        //Constructor
        public Database()
        {
            connection = new MySqlConnection(GetMysqlConnectionString());
        }

        public string GetMysqlConnectionString()
        {
            string conStr = "";
            try
            {
                using (StreamReader rdr = new StreamReader("connectionstring.txt"))
                {
                    conStr = rdr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                API.shared.consoleOutput("Failed to load MySql connectionstring file: " + ex.Message);
            }
            //API.shared.consoleOutput(conStr);
            return conStr;
        }

        //open connection to database
        public bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        API.shared.consoleOutput("Cannot connect to server.  Contact administrator" + ex.Message + " (ex.Number" + ex.Number + ")");
                        break;

                    case 1045:
                        API.shared.consoleOutput("Invalid username/password, please try again");
                        break;
                    default:
                        API.shared.consoleOutput("Other error: " + ex.Number);
                        break;
                }
                return false;
            }
        }

        //Close connection
        public bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                API.shared.consoleOutput(ex.Message);
                return false;
            }
        }

        //Backup
        public void Backup()
        {
            try
            {
                DateTime Time = DateTime.Now;
                int year = Time.Year;
                int month = Time.Month;
                int day = Time.Day;
                int hour = Time.Hour;
                int minute = Time.Minute;
                int second = Time.Second;

                //Save file to C:\ with the current date as a filename
                string path;
                path = "C:\\MySqlBackup" + year + "-" + month + "-" + day +
            "-" + hour + "-" + minute + "-" + second + "-" + ".sql";
                StreamWriter file = new StreamWriter(path);


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "mysqldump";
                psi.RedirectStandardInput = false;
                psi.RedirectStandardOutput = true;
                psi.Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}",
                    uid, password, server, database);
                psi.UseShellExecute = false;

                Process process = Process.Start(psi);

                string output;
                output = process.StandardOutput.ReadToEnd();
                file.WriteLine(output);
                process.WaitForExit();
                file.Close();
                process.Close();
            }
            catch (IOException ex)
            {
                API.shared.consoleOutput("Error , unable to backup!: " + ex);
            }
        }
    }
}
