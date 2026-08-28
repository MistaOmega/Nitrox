using System.Collections.Generic;
using Nitrox.Model.DataStructures;
using Nitrox.Model.Subnautica.DataStructures.GameLogic;
using Nitrox.Model.Subnautica.DataStructures.GameLogic.Entities.Metadata;
using NitroxClient.GameLogic.Spawning.Metadata.Extractor.Abstract;
using UnityEngine;
using static NitroxClient.GameLogic.Spawning.Metadata.Extractor.CyclopsMetadataExtractor;

namespace NitroxClient.GameLogic.Spawning.Metadata.Extractor;

public class CyclopsMetadataExtractor : EntityMetadataExtractor<CyclopsGameObject, CyclopsMetadata>
{
    public override CyclopsMetadata Extract(CyclopsGameObject cyclops)
    {
        GameObject gameObject = cyclops.GameObject;
        CyclopsSilentRunningAbilityButton silentRunning = gameObject.RequireComponentInChildren<CyclopsSilentRunningAbilityButton>(true);

        CyclopsEngineChangeState engineState = gameObject.RequireComponentInChildren<CyclopsEngineChangeState>(true);
        bool engineShuttingDown = (engineState.motorMode.engineOn && engineState.invalidButton);
        bool engineOn = (engineState.startEngine || engineState.motorMode.engineOn) && !engineShuttingDown;

        CyclopsShieldButton shield = gameObject.GetComponentInChildren<CyclopsShieldButton>(true);
        bool shieldOn = (shield) ? shield.active : false;

        CyclopsSonarButton sonarButton = gameObject.GetComponentInChildren<CyclopsSonarButton>(true);
        bool sonarOn = (sonarButton) ? sonarButton._sonarActive : false;

        CyclopsMotorMode.CyclopsMotorModes motorMode = engineState.motorMode.cyclopsMotorMode;

        LiveMixin liveMixin = gameObject.RequireComponentInChildren<LiveMixin>();
        float health = liveMixin.health;

        SubRoot subRoot = gameObject.RequireComponentInChildren<SubRoot>();
        bool isDestroyed = subRoot.subDestroyed || health <= 0f;

        CyclopsExternalDamageManager damageManager = gameObject.RequireComponentInChildren<CyclopsExternalDamageManager>();
        SubFire subFire = gameObject.RequireComponentInChildren<SubFire>();

        int[] damagePointIndexes = GetActiveDamagePointIndexes(damageManager);
        CyclopsFireData[] roomFires = GetActiveRoomFires(gameObject, subFire);

        return new(silentRunning.active, shieldOn, sonarOn, engineOn, (int)motorMode, health, isDestroyed,
                    damageManager.subLiveMixin.health, subFire.liveMixin.health, damagePointIndexes, roomFires);
    }

    private static int[] GetActiveDamagePointIndexes(CyclopsExternalDamageManager damageManager)
    {
        List<int> indexes = [];
        for (int i = 0; i < damageManager.damagePoints.Length; i++)
        {
            if (damageManager.damagePoints[i].gameObject.activeSelf)
            {
                indexes.Add(i);
            }
        }
        return indexes.ToArray();
    }
    
    private static CyclopsFireData[] GetActiveRoomFires(GameObject subRootObject, SubFire subFire)
    {
        if (!subRootObject.TryGetIdOrWarn(out NitroxId subRootId))
        {
            return [];
        }

        List<CyclopsFireData> roomFires = [];
        foreach (KeyValuePair<CyclopsRooms, SubFire.RoomFire> roomFire in subFire.roomFires)
        {
            for (int i = 0; i < roomFire.Value.spawnNodes.Length; i++)
            {
                if (roomFire.Value.spawnNodes[i].childCount > 0)
                {
                    if (!roomFire.Value.spawnNodes[i].GetComponentInChildren<Fire>().TryGetIdOrWarn(out NitroxId fireId))
                    {
                        continue;
                    }

                    roomFires.Add(new CyclopsFireData(fireId, subRootId, roomFire.Key, i));
                }
            }
        }
        return roomFires.ToArray();
    }

    public struct CyclopsGameObject
    {
        public GameObject GameObject { get; set; }
    }
}
