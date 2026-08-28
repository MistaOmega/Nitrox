using System.Collections.Generic;
using System.Linq;
using NitroxClient.Communication;
using NitroxClient.Communication.Abstract;
using NitroxClient.GameLogic.Spawning.Metadata.Processor.Abstract;
using Nitrox.Model.DataStructures;
using Nitrox.Model.Subnautica.DataStructures.GameLogic;
using Nitrox.Model.Subnautica.DataStructures.GameLogic.Entities.Metadata;
using Nitrox.Model.Subnautica.Packets;
using UnityEngine;

namespace NitroxClient.GameLogic.Spawning.Metadata.Processor;

public class CyclopsMetadataProcessor : EntityMetadataProcessor<CyclopsMetadata>
{
    private readonly IPacketSender packetSender;
    private readonly LiveMixinManager liveMixinManager;
    private readonly Fires fires;

    public CyclopsMetadataProcessor(IPacketSender packetSender, LiveMixinManager liveMixinManager, Fires fires)
    {
        this.packetSender = packetSender;
        this.liveMixinManager = liveMixinManager;
        this.fires = fires;
    }

    public override void ProcessMetadata(GameObject cyclops, CyclopsMetadata metadata)
    {
        using (PacketSuppressor<EntityMetadataUpdate>.Suppress())
        {
            SetEngineMode(cyclops, (CyclopsMotorMode.CyclopsMotorModes)metadata.EngineMode);
            ChangeSilentRunning(cyclops, metadata.SilentRunningOn);
            ChangeShieldMode(cyclops, metadata.ShieldOn);
            ChangeSonarMode(cyclops, metadata.SonarOn);
            SetEngineState(cyclops, metadata.EngineOn);
            SetHealth(cyclops, metadata.Health);
            SetDestroyed(cyclops, metadata.IsDestroyed);
            
            // This will grab all of these when some state changes, so should remain compatible with old save
            if (metadata.DamagePointIndexes != null)
            {
                SetActiveDamagePoints(cyclops, metadata.DamagePointIndexes);
                SetActiveRoomFires(cyclops, metadata.RoomFires);
                SetDamageManagerHealth(cyclops, metadata.DamageManagerHealth);
                SetSubFireHealth(cyclops, metadata.SubFireHealth);
            }
        }
    }

    private void SetEngineState(GameObject cyclops, bool isOn)
    {
        CyclopsEngineChangeState engineState = cyclops.RequireComponentInChildren<CyclopsEngineChangeState>(true);

        if (isOn == engineState.motorMode.engineOn)
        {
            // engine state is the same - nothing to do.
            return;
        }

        // During initial sync or when the cyclops HUD isn't shown (from outside of the cyclops)
        if (Player.main.currentSub != engineState.subRoot)
        {
            engineState.startEngine = isOn;
            engineState.subRoot.BroadcastMessage(nameof(CyclopsMotorMode.InvokeChangeEngineState), isOn, SendMessageOptions.RequireReceiver);
            engineState.invalidButton = true;
            engineState.Invoke(nameof(CyclopsEngineChangeState.ResetInvalidButton), 2.5f);
        }
        // When inside of the cyclops, we play the cinematics
        else
        {
            // To invoke the whole OnClick method we need to set the right parameters first
            engineState.invalidButton = false;
            using (PacketSuppressor<EntityMetadataUpdate>.Suppress())
            {
                engineState.OnClick();
            }
        }
    }

    private void SetEngineMode(GameObject cyclops, CyclopsMotorMode.CyclopsMotorModes mode)
    {
        CyclopsMotorMode.CyclopsMotorModes oldMode = cyclops.GetComponent<SubControl>().cyclopsMotorMode.cyclopsMotorMode;
        if (oldMode == mode)
        {
            return;
        }

        foreach (CyclopsMotorModeButton button in cyclops.GetComponentsInChildren<CyclopsMotorModeButton>(true))
        {
            // At initial sync, this kind of processor is executed before the Start()
            if (!button.subRoot)
            {
                button.Start();
            }

            button.SetCyclopsMotorMode(mode);
        }
    }

    private void ChangeSilentRunning(GameObject cyclops, bool isOn)
    {
        CyclopsSilentRunningAbilityButton ability = cyclops.RequireComponentInChildren<CyclopsSilentRunningAbilityButton>(true);

        if (isOn == ability.active)
        {
            return;
        }

        Log.Debug($"Set silent running to {isOn} for cyclops");
        ability.active = isOn;
        if (isOn)
        {
            ability.image.sprite = ability.activeSprite;
            ability.subRoot.BroadcastMessage("RigForSilentRunning");
            ability.InvokeRepeating(nameof(CyclopsSilentRunningAbilityButton.SilentRunningIteration), 0f, ability.silentRunningIteration);
        }
        else
        {
            ability.image.sprite = ability.inactiveSprite;
            ability.subRoot.BroadcastMessage("SecureFromSilentRunning");
            ability.CancelInvoke(nameof(CyclopsSilentRunningAbilityButton.SilentRunningIteration));
        }
    }

    private void ChangeShieldMode(GameObject cyclops, bool isOn)
    {
        CyclopsShieldButton shield = cyclops.GetComponentInChildren<CyclopsShieldButton>(true);

        if (!shield)
        {
            // may not have a shield installed.
            return;
        }

        bool isShieldOn = shield.activeSprite == shield.image.sprite;

        if (isShieldOn == isOn)
        {
            return;
        }

        if (isOn)
        {
            shield.StartShield();
        }
        else
        {
            shield.StopShield();
        }
    }

    private void ChangeSonarMode(GameObject cyclops, bool isOn)
    {
        CyclopsSonarButton sonarButton = cyclops.GetComponentInChildren<CyclopsSonarButton>(true);
        if (sonarButton && sonarButton.sonarActive != isOn)
        {
            if (isOn)
            {
                sonarButton.TurnOnSonar();
            }
            else
            {
                sonarButton.TurnOffSonar();
            }
        }
    }

    private void SetHealth(GameObject gameObject, float health)
    {
        LiveMixin liveMixin = gameObject.RequireComponentInChildren<LiveMixin>(true);
        liveMixinManager.SyncRemoteHealth(liveMixin, health);
    }

    private void SetDestroyed(GameObject gameObject, bool isDestroyed)
    {
        CyclopsDestructionEvent destructionEvent = gameObject.RequireComponentInChildren<CyclopsDestructionEvent>(true);

        // Don't play VFX and SFX if the Cyclops is already destroyed or was spawned in as destroyed
        if (destructionEvent.subRoot.subDestroyed == isDestroyed) return;

        if (isDestroyed)
        {
            // Use packet suppressor as sentinel so the patch callback knows not to spawn loot
            using (PacketSuppressor<EntitySpawnedByClient>.Suppress())
            {
                destructionEvent.DestroyCyclops();
            }
        }
        else
        {
            destructionEvent.RestoreCyclops();
        }
    }
    
    private void SetDamageManagerHealth(GameObject cyclops, float health)
    {
        cyclops.RequireComponentInChildren<CyclopsExternalDamageManager>().subLiveMixin.health = health;
    }
    
    private void SetSubFireHealth(GameObject cyclops, float health)
    {
        cyclops.RequireComponentInChildren<SubFire>().liveMixin.health = health;
    }

    private void SetActiveDamagePoints(GameObject cyclops, int[] damagePointIndexes)
    {
        CyclopsExternalDamageManager damageManager = cyclops.RequireComponentInChildren<CyclopsExternalDamageManager>();
        List<CyclopsDamagePoint> unusedDamagePoints = damageManager.unusedDamagePoints;
        
        if (damagePointIndexes != null && damagePointIndexes.Length > 0)
        {
            int packetDamagePointsIndex = 0;

            for (int damagePointsIndex = 0; damagePointsIndex < damageManager.damagePoints.Length; damagePointsIndex++)
            {
                if (packetDamagePointsIndex < damagePointIndexes.Length
                    && damagePointIndexes[packetDamagePointsIndex] == damagePointsIndex)
                {
                    if (!damageManager.damagePoints[damagePointsIndex].gameObject.activeSelf)
                    {
                        // Copied from CyclopsExternalDamageManager.CreatePoint(), except without the random index pick.
                        damageManager.damagePoints[damagePointsIndex].gameObject.SetActive(true);
                        damageManager.damagePoints[damagePointsIndex].RestoreHealth();
                        GameObject prefabGo = damageManager.fxPrefabs[Random.Range(0, damageManager.fxPrefabs.Length - 1)];
                        damageManager.damagePoints[damagePointsIndex].SpawnFx(prefabGo);
                        unusedDamagePoints.Remove(damageManager.damagePoints[damagePointsIndex]);
                    }

                    packetDamagePointsIndex++;
                }
                else
                {
                    // If it's active, but not in the list, it must have been repaired.
                    if (damageManager.damagePoints[damagePointsIndex].gameObject.activeSelf)
                    {
                        RepairDamagePoint(damageManager, damagePointsIndex, 999);
                    }
                }
            }

            // Looks like the list came in unordered. I've uttered "That shouldn't happen" enough to do sanity checks for what should be impossible.
            if (packetDamagePointsIndex < damagePointIndexes.Length)
            {
                Log.Error($"[CyclopsMetadataProcessor DamagePointIndexes did not fully iterate! Id: {damagePointIndexes[packetDamagePointsIndex]} had no matching Id in damageManager.damagePoints, or the order is incorrect!]");
            }
        }
        else
        {
            // None should be active.
            for (int i = 0; i < damageManager.damagePoints.Length; i++)
            {
                if (damageManager.damagePoints[i].gameObject.activeSelf)
                {
                    RepairDamagePoint(damageManager, i, 999);
                }
            }
        }

        // unusedDamagePoints is checked against damagePoints to determine if there's enough damage points.
        // Failing to set the new list of unusedDamagePoints will cause random DamagePoints to appear.
        damageManager.unusedDamagePoints = unusedDamagePoints;
        damageManager.ToggleLeakPointsBasedOnDamage();
    }

    /// <summary>
    /// Add/remove fires until it matches the <paramref name="roomFires"/> array.
    /// </summary>
    private void SetActiveRoomFires(GameObject cyclops, CyclopsFireData[] roomFires)
    {
        SubFire subFire = cyclops.RequireComponentInChildren<SubFire>();
        Dictionary<CyclopsRooms, SubFire.RoomFire> roomFiresDict = subFire.roomFires;

        if (!subFire.subRoot.TryGetIdOrWarn(out NitroxId subRootId))
        {
            return;
        }

        using (PacketSuppressor<FireDoused>.Suppress())
        {
            if (roomFires != null && roomFires.Length > 0)
            {
                foreach (KeyValuePair<CyclopsRooms, SubFire.RoomFire> keyValuePair in roomFiresDict)
                {
                    for (int nodeIndex = 0; nodeIndex < keyValuePair.Value.spawnNodes.Length; nodeIndex++)
                    {
                        CyclopsFireData fireNode = roomFires.SingleOrDefault(x => x.Room == keyValuePair.Key && x.NodeIndex == nodeIndex);

                        if (fireNode == null)
                        {
                            if (keyValuePair.Value.spawnNodes[nodeIndex].childCount > 0)
                            {
                                keyValuePair.Value.spawnNodes[nodeIndex].GetComponentInChildren<Fire>().Douse(10000);
                            }
                        }
                        else
                        {
                            if (keyValuePair.Value.spawnNodes[nodeIndex].childCount < 1)
                            {
                                fires.Create(new CyclopsFireData(fireNode.FireId, subRootId, fireNode.Room, fireNode.NodeIndex));
                            }
                        }
                    }
                }
            }
            // Clear out the fires, there should be none active
            else
            {
                foreach (KeyValuePair<CyclopsRooms, SubFire.RoomFire> keyValuePair in roomFiresDict)
                {
                    foreach (Transform spawnNode in keyValuePair.Value.spawnNodes)
                    {
                        if (spawnNode.childCount > 0)
                        {
                            spawnNode.GetComponentInChildren<Fire>().Douse(10000);
                        }
                    }
                }
            }
        }
    }
    
    private void RepairDamagePoint(CyclopsExternalDamageManager damageManager, int damagePointIndex, float repairAmount)
    {
        damageManager.damagePoints[damagePointIndex].liveMixin.AddHealth(repairAmount);
    }
}
