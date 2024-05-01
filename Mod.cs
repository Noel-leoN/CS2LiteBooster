using System;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
//using HarmonyLib;
using Unity.Entities;
using UnityEngine;


namespace GameLiteBooster
{
    public sealed class Mod : IMod
    {
        public const string ModName = "GameLiteBooster";
        public const string ModNameCN = "游戏精简加速";

        public static ILog log = LogManager.GetLogger($"{nameof(GameLiteBooster)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            // harmony patches.
            //var harmony = new Harmony(harmonyID);
            //harmony.PatchAll(typeof(Mod).Assembly);
            //var patchedMethods = harmony.GetPatchedMethods().ToArray();
            //log.Info($"Plugin {harmonyID} made patches! Patched methods: " + patchedMethods.Length);
            //foreach (var patchedMethod in patchedMethods)
            //{
            //    log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            //}

            // UI settings; 
            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            // UI locale;
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            GameManager.instance.localizationManager.AddSource("zh-HANS", new LocaleCN(m_Setting));

            // Load UI saved setting;
            AssetDatabase.global.LoadSettings(nameof(GameLiteBooster), m_Setting, new Setting(this));

            //Disable vanilla systmes | enable custom systems；
                // Do you know why there are so many animal-related systems? :-)
                //Pet Systems;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Citizens.HouseholdPetInitializeSystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Citizens.HouseholdPetRemoveSystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.PetAISystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.HouseholdPetBehaviorSystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.HouseholdPetSpawnSystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Serialization.PetSystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Serialization.HouseholdAnimalSystem>().Enabled = !m_Setting.DisablePetSystem;
                //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.AnimalNavigationSystem>().Enabled = !Mod.Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.AnimalMoveSystem>().Enabled = !m_Setting.DisablePetSystem;
                //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.CreatureSpawnerSystem>().Enabled = !Mod.Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.DomesticatedAISystem>().Enabled = !m_Setting.DisablePetSystem;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.WildlifeAISystem>().Enabled = !m_Setting.DisablePetSystem;
                       
          
                //Traffic Systems;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TrafficSpawnerAISystem>().Enabled = !m_Setting.DisableRamdonTraffic;
                //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TripNeededSystem>().Enabled = !Mod.Setting.DisableRamdonTraffic;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.RandomTrafficDispatchSystem>().Enabled = !m_Setting.DisableRamdonTraffic;

                //Taxi Systems;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.TaxiDispatchSystem>().Enabled = !m_Setting.DisableTaxiDispatch;
            


            //if(m_Setting.      == true)
            //{
            //    World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.      >().Enabled = false;
               
            //}


            
        }

       
        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            // un-Harmony;
            //var harmony = new Harmony(harmonyID);
            //harmony.UnpatchAll(harmonyID);

            // un-Setting;
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
