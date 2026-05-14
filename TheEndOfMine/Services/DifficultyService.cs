using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class DecayRates
{
    public float HungerDecay { get; init; }      // ต่อนาทีในเกม
    public float ThirstDecay { get; init; }
    public float FatigueDecay { get; init; }
    public float StarveHpDecay { get; init; }    // HP ที่ลดต่อนาทีเมื่อหิวจัด
    public float DehydrateHpDecay { get; init; } // HP ที่ลดต่อนาทีเมื่อกระหายจัด
    public float DamageMultiplier { get; init; } // ตัวคูณผลเสียจาก event
}

public class DifficultyService
{
    public DecayRates GetDecayRates(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => new DecayRates
        {
            HungerDecay       = 0.012f,
            ThirstDecay       = 0.016f,
            FatigueDecay      = 0.010f,
            StarveHpDecay     = 0.04f,
            DehydrateHpDecay  = 0.07f,
            DamageMultiplier  = 0.55f
        },
        Difficulty.Normal => new DecayRates
        {
            HungerDecay       = 0.020f,
            ThirstDecay       = 0.028f,
            FatigueDecay      = 0.018f,
            StarveHpDecay     = 0.08f,
            DehydrateHpDecay  = 0.12f,
            DamageMultiplier  = 0.85f
        },
        Difficulty.Hard => new DecayRates
        {
            HungerDecay       = 0.032f,
            ThirstDecay       = 0.044f,
            FatigueDecay      = 0.028f,
            StarveHpDecay     = 0.16f,
            DehydrateHpDecay  = 0.22f,
            DamageMultiplier  = 1.25f
        },
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}
