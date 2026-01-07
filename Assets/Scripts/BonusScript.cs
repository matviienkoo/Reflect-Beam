using UnityEngine;
using System;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class BonusScript : MonoBehaviour
{
    private const string LastBonusKey = "LastBonusTime";
    private const string AutoWinCountKey = "AutoWinCount";
    private static readonly TimeSpan BonusInterval = TimeSpan.FromHours(12);

    public bool bonusAvailable { get; private set; }
    
    [Header("Scripts")]
    public MeetSelectionScript MeetScript;

    private void Start()
    {
        // Check if the bonus is ready on startup
        CheckBonusAvailability();

        // If available, trigger auto-win UI or logic
        if (bonusAvailable)
        {
            MeetScript.OpenAutoWin();
        }
    }
    private void CheckBonusAvailability()
    {
        if (!MirraSDK.Data.HasKey(LastBonusKey))
        {
            // First launch: bonus is available
            bonusAvailable = true;
        }
        else
        {
            // Calculate elapsed time since last claim
            long storedTicks = long.Parse(MirraSDK.Data.GetString(LastBonusKey));
            DateTime lastClaimUtc = new DateTime(storedTicks, DateTimeKind.Utc);
            TimeSpan elapsed = DateTime.UtcNow - lastClaimUtc;

            bonusAvailable = elapsed >= BonusInterval;
        }
    }

    public void TakeBonus()
    {
        if (!bonusAvailable) return;

        // Increment stored AutoWin count
        int currentCount = MirraSDK.Data.GetInt(AutoWinCountKey, 0);
        if (currentCount < 1)
        {
            MirraSDK.Data.SetInt(AutoWinCountKey, currentCount + 1);
        }

        // Record the current UTC time for next 12-hour window
        MirraSDK.Data.SetString(LastBonusKey, DateTime.UtcNow.Ticks.ToString());
        MirraSDK.Data.Save();

        // Mark bonus as claimed
        bonusAvailable = false;

        // Meet Scene
        MeetScript.OpenMeet();

        // Additional logic: update UI, notify player, etc.
        Debug.Log("Bonus claimed! AutoWin count: " + (currentCount + 1));
    }
    public TimeSpan GetTimeUntilNextBonus()
    {
        if (!MirraSDK.Data.HasKey(LastBonusKey))
            return TimeSpan.Zero;

        long storedTicks = long.Parse(MirraSDK.Data.GetString(LastBonusKey));
        DateTime lastClaimUtc = new DateTime(storedTicks, DateTimeKind.Utc);
        TimeSpan elapsed = DateTime.UtcNow - lastClaimUtc;

        return elapsed >= BonusInterval ? TimeSpan.Zero : BonusInterval - elapsed;
    }
}
