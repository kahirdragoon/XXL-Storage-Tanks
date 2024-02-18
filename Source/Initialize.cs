using PipeSystem;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace XXLStorageTanks
{
    [StaticConstructorOnStartup]
    public class Initialize
    {
        private const string modName = "XXL Storage Tanks";

        private static readonly IList<(ThingDef, CompProperties_ResourceStorage, float)> storageData = new List<(ThingDef, CompProperties_ResourceStorage, float)>();

        static Initialize()
        {
            // Looking for StorageComps and saving them and the original StackLimit of their things
            foreach(var thingDef in DefDatabase<ThingDef>.AllDefs.Where(t => t.comps != null))
            {
                var storageComps = thingDef.comps
                    .Where(comp => comp.GetType() == typeof(CompProperties_ResourceStorage))
                    .Select(comp => (CompProperties_ResourceStorage)comp)
                    .ToList();

                foreach(var comp in storageComps)
                {
                    string ressourceName;

                    // Workaround for things where the ressource name is different from the item thing name
                    switch (comp.pipeNet.resource.name)
                    {
                        case "Nutrient paste":
                            ressourceName = "MealNutrientPaste";
                            break;
                        case "Hemogen":
                            ressourceName = "HemogenPack";
                            break;
                        case "nutrient solution":
                            ressourceName = "HydroponicNutrientSolution";
                            break;
                        case "Helixien gas":
                            ressourceName = "VHGE_Helixien";
                            break;
                        default:
                            ressourceName = comp.pipeNet.resource.name;
                            break;
                    }

                    var item = DefDatabase<ThingDef>.GetNamedSilentFail(ressourceName);

                    if (item == null)
                    {
                        if (comp.extractOptions != null)
                            item = comp.extractOptions.thing;
                    }

                    if (item is null)
                    {
                        Log.Warning($"[{modName}] item for {comp} of {thingDef.defName} is null. Skipping");
                        continue;
                    }

                    storageData.Add((item, comp, item.stackLimit));
                }

            }
                
            // Taken from OgreStackXXL (https://github.com/cmdprompt/OgreStack/blob/33950195fa65f5cbaf6aa713fc7bb6399791252a/1.4/OgreStackMod.cs#L58C4-L65C41)

            // this appears to be a technique to attempt
            // processing after other mods have loaded
            // its not a guarantee though
            Verse.LongEventHandler.QueueLongEvent(() => {
                Verse.LongEventHandler.QueueLongEvent(() => {
                    ModifyTankCapacities();
                }, "XXLTank_Init_Execute", true, null);
            }, "XXLTank_Init_Reg", true, null);
        }

        private static void ModifyTankCapacities()
        {
            foreach(var (itemDef, storageComp, originalStackLimit) in storageData)
            {
                //Log.Message($"Modifiyng tank storage capacity for {itemDef.defName}");
                
                float ratio = storageComp.storageCapacity / originalStackLimit;
                float newStorageCapacity = itemDef.stackLimit * ratio;
                // Rounding up to to the next 100 to prevent wierd numbers
                int storageCapacityRoundedUp = (int)Math.Round(newStorageCapacity / 100, 0, MidpointRounding.AwayFromZero);
                storageComp.storageCapacity = storageCapacityRoundedUp * 100;
                
                //Log.Message($"Finished {itemDef.defName} with ({storageComp.storageCapacity}) = {itemDef.stackLimit} * {ratio}");
            }

            Log.Message($"[{modName}] Finished modifying storage tank capacities");
        }
    }
}