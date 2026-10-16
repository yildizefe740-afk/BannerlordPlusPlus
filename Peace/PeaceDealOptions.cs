using System;

namespace BannerlordPlusPlus
{
    [Flags]
    public enum PeaceDealOptions
    {
        None = 0,
        WarReparation = 1,      // Krallık günlük gelirinin %10'u tazminat kesintisi
        TerritoryExchange = 2,  // Sınır/bölge takası mantığı
        PlunderTreasury = 4,    // Hedef krallık hazinesindeki TÜM parayı çekip alma
        Subjectize = 8,         // Krallığı tabi/vassal kılma
        ClaimThrone = 16,       // Taht üzerinde hak iddia etme
        ExecuteRuler = 32,      // Krallık liderini infaz etme
        CededSettlement = 64,   // Yerleşim yeri devri
        GainIndependence = 128, // Bağımsızlık kazanma
        Truce = 256,            // 100 Günlük Ateşkes kilidi (Seçilmezse standart 30 gün)
    }
}