using UnityEngine;

public enum AOGTeam
{
    Neutral,
    Blue,
    Red
}

public enum AOGUnitType
{
    Minion,
    Tower,
    Nexus,
    Boss
}

public class AOGCombatUnit : MonoBehaviour
{
    public AOGTeam team = AOGTeam.Neutral;
    public AOGUnitType unitType = AOGUnitType.Minion;
}