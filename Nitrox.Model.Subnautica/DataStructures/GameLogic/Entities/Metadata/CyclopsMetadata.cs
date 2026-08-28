using System;
using System.Linq;
using System.Runtime.Serialization;
using BinaryPack.Attributes;

namespace Nitrox.Model.Subnautica.DataStructures.GameLogic.Entities.Metadata;

[Serializable]
[DataContract]
public class CyclopsMetadata : EntityMetadata
{
    [DataMember(Order = 1)]
    public bool SilentRunningOn { get; set; }

    [DataMember(Order = 2)]
    public bool ShieldOn { get; set; }

    [DataMember(Order = 3)]
    public bool SonarOn { get; set; }

    [DataMember(Order = 4)]
    public bool EngineOn { get; set; }

    [DataMember(Order = 5)]
    public int EngineMode { get; set; }

    [DataMember(Order = 6)]
    public float Health { get; set; }

    [DataMember(Order = 7)]
    public bool IsDestroyed { get; set; }

    [DataMember(Order = 8)]
    public float DamageManagerHealth { get; set; }

    [DataMember(Order = 9)]
    public float SubFireHealth { get; set; }
    
    [DataMember(Order = 10)]
    public int[] DamagePointIndexes { get; set; }

    [DataMember(Order = 11)]
    public CyclopsFireData[] RoomFires { get; set; }

    [IgnoreConstructor]
    protected CyclopsMetadata()
    {
        // Constructor for serialsation
    }

    public CyclopsMetadata(bool silentRunningOn, bool shieldOn, bool sonarOn, bool engineOn, int engineMode, float health, bool isDestroyed,
                            float damageManagerHealth, float subFireHealth, int[] damagePointIndexes, CyclopsFireData[] roomFires)
    {
        SilentRunningOn = silentRunningOn;
        ShieldOn = shieldOn;
        SonarOn = sonarOn;
        EngineOn = engineOn;
        EngineMode = engineMode;
        Health = health;
        IsDestroyed = isDestroyed;
        DamageManagerHealth = damageManagerHealth;
        SubFireHealth = subFireHealth;
        DamagePointIndexes = damagePointIndexes;
        RoomFires = roomFires;
    }

    public override string ToString()
    {
        string damagePointIndexes = DamagePointIndexes == null ? "" : string.Join(", ", DamagePointIndexes.Select(x => x.ToString()));
        string roomFires = RoomFires == null ? "" : string.Join(", ", RoomFires.Select(x => x.ToString()));
        return $"[CyclopsMetadata SilentRunningOn: {SilentRunningOn}, ShieldOn: {ShieldOn}, SonarOn: {SonarOn}, EngineOn: {EngineOn}, EngineMode: {EngineMode}, Health: {Health}, IsDestroyed: {IsDestroyed}, DamageManagerHealth: {DamageManagerHealth}, SubFireHealth: {SubFireHealth}, DamagePointIndexes: [{damagePointIndexes}], RoomFires: [{roomFires}]]";
    }
}
