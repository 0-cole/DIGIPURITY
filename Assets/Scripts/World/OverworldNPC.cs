using UnityEngine;

namespace DigiPurity.World
{
    /// <summary>
    /// Overworld NPC Creature component.
    /// Acts as a solid physical obstacle preventing Owen from phasing through characters,
    /// with automatic footprint collision sizing at the feet and dynamic depth sorting.
    /// </summary>
    [AddComponentMenu("DIGIPURITY/Overworld NPC (Solid Creature)")]
    public class OverworldNPC : OverworldCreature
    {
    }
}
