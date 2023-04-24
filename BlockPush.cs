using Kitchen;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenSmartGrabbersWereBugged
{
    [UpdateAfter(typeof(ApplyItemProcesses))]
    public class BlockPush : GameSystemBase, IModSystem
    {
        struct CBlockPush : IModComponent { }

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
            using NativeArray<Entity> conveyors = eq.ToEntityArray(Allocator.TempJob);

            foreach (Entity conveyor in conveyors)
            {
                if (Require(conveyor, out CConveyPushItems push))
                {
                    if (EntityManager.RemoveComponent(conveyor, typeof(CBlockPush)))
                    {
                        push.Push = true;
                        EntityManager.SetComponentData(conveyor, push);
                    }
                    if (!push.Push || push.State == CConveyPushItems.ConveyState.Grab)
                    {
                        continue;
                    }
                }

                if ((Require(conveyor, out CConveyCooldown cooldown) && cooldown.Remaining > 0f) || (Require(conveyor, out CItemHolder held) && !GetComponentDataFromEntity<CItem>().HasComponent(held.HeldItem)))
                {
                    continue;
                }

                Orientation o = Orientation.Up;
                if (Require(conveyor, out CConveyPushRotatable comp) && comp.Target != 0)
                {
                    o = comp.Target;
                }

                CPosition pos = GetComponent<CPosition>(conveyor);
                Vector3 vector = pos.Rotation.RotateOrientation(o).ToOffset() * ((!push.Reversed) ? 1 : (-1));
                Entity occupant = GetOccupant(vector + pos);

                if (CanReach(pos, vector + pos) && !GetComponentDataFromEntity<CPreventItemTransfer>().HasComponent(occupant))
                {
                    if (Require(occupant, out CConveyPushItems targetPush) && targetPush.GrabSpecificType)
                    {
                        CItem cItem = GetComponentDataFromEntity<CItem>(isReadOnly: true)[held.HeldItem];

                        if (targetPush.SpecificType != cItem.ID)
                        {
                            push.Push = false;
                            push.Progress = 0f;
                            push.State = CConveyPushItems.ConveyState.None;

                            EntityManager.SetComponentData(conveyor, push);
                            EntityManager.AddComponent(conveyor, typeof(CBlockPush));
                        }
                    }
                }
            }
        }
    }
}
