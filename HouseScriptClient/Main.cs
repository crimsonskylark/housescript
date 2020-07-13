using CarScriptClient;
using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace HouseScriptClient
{
    // <summary>
    // Main class of the HouseArch script.
    // </summary>
    // <param name="houseObjects">All of the props within a 250 unit radius of the player.</param>
    // <param name="curr_prop">Used to track the state of the currently selected prop.</param>
    public class HouseScriptClientMain : BaseScript
    {
        List<HouseObject> houseObjects = new List<HouseObject>();
        Prop curr_prop;

        public HouseScriptClientMain()
        {
            Debug.WriteLine("Starting up HouseArchCore");
            Debug.WriteLine("Join us on Discord at: https://discord.gg/r8aPEm6 to browse our store.");
        }

        // <summary>
        // This function will be called by the runtime once the resource has been started.
        // </summary>
        // <param name="resname">The name of the resource. This parameter will be passed by the runtime.</param>
        [EventHandler("onClientResourceStart")]
        public void Init(string resname)
        {
            if (GetCurrentResourceName() != resname) { return; }

            World.GetAllProps()
                .Where(e => e.Position.DistanceToSquared(Game.PlayerPed.Position) < 250)
                .ToList()
                .ForEach(e => houseObjects.Add(new HouseObject(e.Model.Hash, e.Position, e.Rotation, 0, "license:", 350)));

            if (houseObjects.Count < 1)
            {
                Debug.WriteLine("No props were found in a 250 unit radius.");
                Debug.WriteLine("Assuming the house is empty.");
            } else
            {
                DumpObjects();
            }
        }
        /*
         * <summary>
         * This function dumps all the objects saved in <c>houseObjects</c>.
         * </summary>
         */
        private void DumpObjects()
        {
            houseObjects.ForEach(obj => Debug.WriteLine(obj.ToString()));
        }
    }
}
