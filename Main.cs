using Kitchen;
using KitchenLib;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using KitchenMods;

// Namespace should have "Kitchen" in the beginning
namespace KitchenSmartGrabbersWereBugged
{
    public class Main : BaseMod
    {
        // guid must be unique and is recommended to be in reverse domain name notation
        // mod name that is displayed to the player and listed in the mods menu
        // mod version must follow semver e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.SmartGrabbersWereBugged";
        public const string MOD_NAME = "Smart Grabbers Were Bugged";
        public const string MOD_VERSION = "0.1.2";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.1";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.1" current and all future
        // e.g. ">=1.1.1 <=1.2.3" for all from/until

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void Initialise()
        {
            base.Initialise();
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        protected override void OnUpdate()
        {
            
        }

        #region Logging
        // You can remove this, I just prefer a more standardized logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
    
    [UpdateBefore(typeof(PushItems))]
    [UpdateAfter(typeof(ApplyItemProcesses))]
    public class BlockPush : GameSystemBase
    {
        struct CBlockPush : IModComponent { }

        bool DEBUG_LOGGING = false;

        EntityQuery eq;

        protected override void Initialise()
        {
            base.Initialise();
            eq = GetEntityQuery(new QueryHelper()
                                .All(typeof(CConveyCooldown),
                                     typeof(CConveyPushItems),
                                     typeof(CItemHolder),
                                     typeof(CPosition))
                                .None(typeof(CDisableAutomation)));
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> conveyors = eq.ToEntityArray(Allocator.TempJob);

            foreach(Entity conveyor in conveyors)
            {
                if (DEBUG_LOGGING)
                    Main.LogInfo($"Checking Conveyor {conveyor.Index}");

                if (Require(conveyor,out CConveyPushItems push))
                {
                    if (DEBUG_LOGGING)
                    {
                        Main.LogInfo($"push.Progress = {push.Progress}");
                        Main.LogInfo($"push.State = {push.State}");
                        Main.LogInfo($"push.Push = {push.Push}");
                    }

                    if (EntityManager.RemoveComponent(conveyor, typeof(CBlockPush)))
                    {
                        push.Push = true;
                        EntityManager.SetComponentData(conveyor, push);
                        if (DEBUG_LOGGING)
                            Main.LogInfo("Removed CBlockPush and reset push.Push");
                    }
                    if (!push.Push || push.State == CConveyPushItems.ConveyState.Grab)
                    {
                        if (DEBUG_LOGGING)
                            Main.LogInfo("Not push.Push or is in ConveyState.Grab. Skipping");
                        continue;
                    }
                }
                
                if ((Require(conveyor, out CConveyCooldown cooldown) && cooldown.Remaining > 0f) || (Require(conveyor, out CItemHolder held) && !GetComponentDataFromEntity<CItem>().HasComponent(held.HeldItem)))
                {
                    if (DEBUG_LOGGING)
                        Main.LogInfo("cooldown.Remaining > 0 or conveyor has no held item");
                    continue;
                }
                
                Orientation o = Orientation.Up;
                if(Require(conveyor, out CConveyPushRotatable comp) && comp.Target != 0)
                {
                    o = comp.Target;
                    if (DEBUG_LOGGING)
                        Main.LogInfo($"Is rotating grabber. Set orientation to {o}");
                }

                CPosition pos = GetComponent<CPosition>(conveyor);
                if (DEBUG_LOGGING)
                    Main.LogInfo($"pos = {pos}");
                Vector3 vector = pos.Rotation.RotateOrientation(o).ToOffset() * ((!push.Reversed) ? 1 : (-1));
                Entity occupant = GetOccupant(vector + pos);
                if (DEBUG_LOGGING)
                    Main.LogInfo($"Occupant = {occupant.Index}");


                if (CanReach(pos, vector + pos) && !GetComponentDataFromEntity<CPreventItemTransfer>().HasComponent(occupant))
                {
                    if (DEBUG_LOGGING)
                        Main.LogInfo($"Occupant allows ItemTransfer");
                    if (Require(occupant, out CConveyPushItems targetPush) && targetPush.GrabSpecificType)
                    {
                        if (DEBUG_LOGGING)
                            Main.LogInfo($"Occupant is smart grabber.");


                        CItem cItem = GetComponentDataFromEntity<CItem>(isReadOnly: true)[held.HeldItem];

                        if (DEBUG_LOGGING)
                        {
                            Main.LogInfo($"targetPush.SpecificType = {targetPush.SpecificType}");
                            Main.LogInfo($"cItem.ID = {cItem.ID}");

                        }
                        if (targetPush.SpecificType != cItem.ID)
                        {
                            if (DEBUG_LOGGING)
                                Main.LogInfo($"Item does not match smart grabber filter.");
                            push.Push = false;
                            push.Progress = 0f;
                            push.State = CConveyPushItems.ConveyState.None;

                            EntityManager.SetComponentData(conveyor, push);
                            EntityManager.AddComponent(conveyor, typeof(CBlockPush));

                            if (DEBUG_LOGGING)
                                Main.LogInfo($"Set conveyor push.Push = false to inhibit PushItems");

                            if (DEBUG_LOGGING && Require(conveyor, out CConveyPushItems push2))
                            {
                                Main.LogInfo($"Fetching conveyor push again.");
                                Main.LogInfo($"Updated push.Progress = {push2.Progress}");
                                Main.LogInfo($"Updated push.State = {push2.State}");
                            }
                        }
                        else
                        {
                            Main.LogInfo($"Item matches smart grabber filter.");
                        }
                    }
                    else
                    {
                        if (DEBUG_LOGGING)
                            Main.LogInfo($"Occupant cannot be reached or has CPreventItemTransfer");
                    }
                }
            }

            conveyors.Dispose();
        }
    }
    
}
