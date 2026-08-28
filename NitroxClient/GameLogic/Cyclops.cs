using System.Collections;
using NitroxClient.Communication;
using NitroxClient.Communication.Abstract;
using NitroxClient.MonoBehaviours;
using NitroxClient.Unity.Helper;
using Nitrox.Model.DataStructures;
using Nitrox.Model.Subnautica.Packets;
using UnityEngine;
using static NitroxClient.GameLogic.Spawning.Metadata.Extractor.CyclopsMetadataExtractor;

namespace NitroxClient.GameLogic
{
    public class Cyclops
    {
        private readonly IPacketSender packetSender;
        private readonly Vehicles vehicles;
        private readonly Entities entities;

        public Cyclops(IPacketSender packetSender, Vehicles vehicles, Entities entities)
        {
            this.packetSender = packetSender;
            this.vehicles = vehicles;
            this.entities = entities;
        }

        public void BroadcastMetadataChange(NitroxId id)
        {
            GameObject gameObject = NitroxEntity.RequireObjectFrom(id);
            CyclopsGameObject cyclops = new CyclopsGameObject() { GameObject = gameObject };
            entities.EntityMetadataChanged(cyclops, id);
        }

        public void BroadcastLaunchDecoy(NitroxId id)
        {
            CyclopsDecoyLaunch packet = new CyclopsDecoyLaunch(id);
            packetSender.Send(packet);
        }

        public void BroadcastActivateFireSuppression(NitroxId id)
        {
            CyclopsFireSuppression packet = new CyclopsFireSuppression(id);
            packetSender.Send(packet);
        }

        public void LaunchDecoy(NitroxId id)
        {
            GameObject cyclops = NitroxEntity.RequireObjectFrom(id);
            CyclopsDecoyManager decoyManager = cyclops.RequireComponent<CyclopsDecoyManager>();
            using (PacketSuppressor<EntityMetadataUpdate>.Suppress())
            {
                decoyManager.Invoke(nameof(CyclopsDecoyManager.LaunchWithDelay), 3f);
                decoyManager.decoyLaunchButton.UpdateText();
                decoyManager.subRoot.voiceNotificationManager.PlayVoiceNotification(decoyManager.subRoot.decoyNotification, false, true);
                decoyManager.subRoot.BroadcastMessage("UpdateTotalDecoys", decoyManager.decoyCount, SendMessageOptions.DontRequireReceiver);
                CyclopsDecoyLaunchButton decoyLaunchButton = cyclops.RequireComponentInChildren<CyclopsDecoyLaunchButton>();
                decoyLaunchButton.StartCooldown();
            }
        }

        public void StartFireSuppression(NitroxId id)
        {
            GameObject cyclops = NitroxEntity.RequireObjectFrom(id);
            CyclopsFireSuppressionSystemButton fireSuppButton = cyclops.RequireComponentInChildren<CyclopsFireSuppressionSystemButton>();
            using (PacketSuppressor<CyclopsFireSuppression>.Suppress())
            {
                // Infos from SubFire.StartSystem
                fireSuppButton.subFire.StartCoroutine(StartFireSuppressionSystem(fireSuppButton.subFire));
                fireSuppButton.StartCooldown();
            }
        }

        // Remake of the StartSystem Coroutine from original player. Some Methods are not used from the original coroutine
        // For example no temporaryClose as this will be initiated anyway from the originating Player
        // Also the fire extiguishing will not start cause the initial player is already extiguishing the fires. Else this could double/triple/... the extinguishing
        private IEnumerator StartFireSuppressionSystem(SubFire fire)
        {
            fire.subRoot.voiceNotificationManager.PlayVoiceNotification(fire.subRoot.fireSupressionNotification, false, true);
            yield return Yielders.WaitFor3Seconds;
            fire.fireSuppressionActive = true;
            fire.subRoot.fireSuppressionState = true;
            fire.subRoot.BroadcastMessage("NewAlarmState", null, SendMessageOptions.DontRequireReceiver);
            fire.Invoke(nameof(SubFire.CancelFireSuppression), fire.fireSuppressionSystemDuration);
            float doorCloseDuration = 30f;
            fire.gameObject.BroadcastMessage("TemporaryLock", doorCloseDuration, SendMessageOptions.DontRequireReceiver);
        }
    }
}
