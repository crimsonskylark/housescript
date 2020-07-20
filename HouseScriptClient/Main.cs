using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace HouseScript.Client
{

    public class InvalidLicenseException : Exception
    {
        public InvalidLicenseException() { }
        public InvalidLicenseException(string msg) : base(msg) { }
    }

    // <summary>
    // Main class of the HouseArch script.
    // </summary>
    // <param name="houseObjects">All of the props within a 250 unit radius of the player.</param>
    // <param name="currSelectedProp">Used to track the state of the currently selected prop.</param>
    public class HouseScriptClientMain : BaseScript
    {
        List<HouseObject> houseObjects = new List<HouseObject>();
        Prop currSelectedProp;
        bool houseSpawned = false;
        public HouseScriptClientMain()
        {
            Debug.WriteLine("Starting up HouseArchCore");
            Debug.WriteLine("Join us on Discord at: https://discord.gg/r8aPEm6 to browse our store.");
        }

        // <summary>
        // This function is called by the server once the user calls the correct command.
        // </summary>
        [EventHandler("HouseArchClient:OpenInterface")]
        public void Init()
        {

            World.GetAllProps()
                .Where(e => e.Position.DistanceToSquared(Game.PlayerPed.Position) < 250)
                .ToList()
                .ForEach(e => houseObjects.Add(new HouseObject(e.Model.Hash, e.Position, e.Rotation, 0, "license:", 350)));

            //if (houseObjects.Count < 1)
            //{
            //    Debug.WriteLine("No props were found in a 250 unit radius.");
            //    Debug.WriteLine("Assuming the house is empty.");
            //} else
            //{
            //    DumpObjects();
            //}
            OnEnterHouse(0);
        }

        /*
         * <summary>
         * This function dumps all the objects saved in <c>houseObjects</c>.
         * </summary>
         */
        private void DumpObjects()
        {
            //houseObjects.ForEach(obj => TriggerServerEvent("HouseArch:PlayerAcquireObject", JsonConvert.SerializeObject(obj)));
        }

        [EventHandler("HouseArchClient:OnEnterHouse")]
        private void OnEnterHouse(int houseId)
        {
            TriggerServerEvent("HouseArch:GetAllUserObjects", 1);
        }

        [EventHandler("HouseArchClient:OnReceiveHouseObjects")]
        private async void OnReceiveHouseObjects(string obj)
        {
            Debug.WriteLine("here");
            houseObjects = JsonConvert.DeserializeObject<List<HouseObject>>(obj);
            //houseObjects.ForEach(
            //    async e =>
            //    {
            //        Prop p = await World.CreateProp(new Model(e.propHash), new Vector3(Game.PlayerPed.Position.X + new Random().Next(-5, 5), Game.PlayerPed.Position.Y + new Random().Next(-5, 5), Game.PlayerPed.Position.Z), true, true);
            //        p.Rotation = e.propRotation;
            //    }
            //);
            while (!HasModelLoaded((uint)GetHashKey("dons_house_sm1_shell")))
            {
                RequestModel((uint)GetHashKey("dons_house_sm1_shell"));
                await Delay(100);
            }
            Prop houseObjHandle;
            Vector3 houseLocation = new Vector3(-895.58f, -49.86f, 50.04f);
            float gz = 0;
            GetGroundZFor_3dCoord(houseLocation.X, houseLocation.Y, houseLocation.Z, ref gz, false);
            if (!houseSpawned)
            {
                houseObjHandle = await World.CreateProp(GetHashKey("dons_house_sm1_shell"), houseLocation - new Vector3(0.0f, 0.0f, gz), false, false);
                FreezeEntityPosition(houseObjHandle.Handle, true);
                houseSpawned = true;
            }
            Debug.WriteLine(GetHashKey("v_16_dt").ToString());
            await World.CreateProp(-647884455, Game.PlayerPed.Position, false, false);
            await Delay(0);
        }
    }
}
