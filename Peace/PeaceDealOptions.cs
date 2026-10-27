using System;

namespace BannerlordPlusPlus
{
    [Flags]
    public enum PeaceDealOptions
    {
        None = 0,
        WarReparation = 1,      // Deduct 10% of the kingdom's daily income as reparations.
        TerritoryExchange = 2,  // Exchange border regions or territory.
        PlunderTreasury = 4,    // Take all money from the target kingdom's treasury.
        Subjectize = 8,         // Make the kingdom a subject or vassal.
        ClaimThrone = 16,       // Claim the throne.
        ExecuteRuler = 32,      // Execute the kingdom's ruler.
        CededSettlement = 64,   // Transfer a settlement.
        GainIndependence = 128, // Gain independence.
        Truce = 256,            // 100-day truce lock (30 days by default if not selected).
    }
}