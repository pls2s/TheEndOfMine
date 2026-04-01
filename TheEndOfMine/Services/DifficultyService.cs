using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class DecayRates
{
    public float HungerDecay { get; init; }      // ต่อนาทีในเกม
    public float ThirstDecay { get; init; }
    public float FatigueDecay { get; init; }
    public float StarveHpDecay { get; init; }    // HP ที่ลดต่อนาทีเมื่อหิวจัด
    public float DehydrateHpDecay { get; init; } // HP ที่ลดต่อนาทีเมื่อกระหายจัด
    public float DamageMultiplier { get; init; } // ตัวคูณ damage จาก event
}

public class DifficultyService
{
    public DecayRates GetDecayRates(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => new DecayRates
        {
            HungerDecay       = 0.03f,
            ThirstDecay       = 0.05f,
            FatigueDecay      = 0.02f,
            StarveHpDecay     = 0.1f,
            DehydrateHpDecay  = 0.15f,
            DamageMultiplier  = 0.5f
        },
        Difficulty.Normal => new DecayRates
        {
            HungerDecay       = 0.06f,
            ThirstDecay       = 0.10f,
            FatigueDecay      = 0.04f,
            StarveHpDecay     = 0.2f,
            DehydrateHpDecay  = 0.3f,
            DamageMultiplier  = 1.0f
        },
        Difficulty.Hard => new DecayRates
        {
            HungerDecay       = 0.12f,
            ThirstDecay       = 0.20f,
            FatigueDecay      = 0.08f,
            StarveHpDecay     = 0.5f,
            DehydrateHpDecay  = 0.7f,
            DamageMultiplier  = 2.0f
        },
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}
