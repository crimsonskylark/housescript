using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MySqlConnector;
using System;
using System.Linq;

namespace HouseScript.Server
{
    public class HouseScriptServer : BaseScript
    {

        MySqlConnection dbInterface = new MySqlConnection(GetConvar("mysql_connection_string", ""));


        public HouseScriptServer()
        {}

        [EventHandler("playerJoining")]
        private void GetPlayerObjects([FromSource] Player player)
        {
            var playerRockstarIdentifier = player.Identifiers
                                            .ToList()
                                            .Where(e => e.StartsWith("license:"))
                                            .First()
                                            .Substring(8);
            Debug.WriteLine($"Player name: {player.Name}\nIdentifier: {playerRockstarIdentifier}");
        }

        [EventHandler("onServerResourceStart")]
        private void InitDb(string resName)
        {
            if (GetCurrentResourceName() != resName) { return; }
            dbInterface.Open();

            using (var command = dbInterface.CreateCommand())
            {
                command.CommandText = "SELECT * FROM items";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Debug.WriteLine(reader.GetString(0));
                    }
                }
            }
        }

        [EventHandler("onServerResourceStart")]
        private void LicenseCheck(string resName)
        {
            if (GetCurrentResourceName() != resName) { return; }
            string licenseKey = GetConvar("vfms_LicenseKey", "");
            if (licenseKey.Length < 1)
            {
                Debug.WriteLine("\n^1[!!]^9 ^0Your VFMS license key is^9 ^1invalid^9\n");
            } else
            {
                Debug.WriteLine("\n^1[!!]^9 ^0Your VFMS license key is has been ^2validated^9\n");
            }
        }
    }
}
