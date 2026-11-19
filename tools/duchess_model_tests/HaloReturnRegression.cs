using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using NightMustStay.Core.Models.Power;

internal static class HaloReturnRegression
{
    public static void Run()
    {
        MethodInfo predicate = typeof(HaloReturnPower).GetMethod(
            "IsRecoverablePile",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new Exception("Halo return pile predicate is missing.");

        foreach (PileType pileType in Enum.GetValues<PileType>())
        {
            bool actual = (bool)(predicate.Invoke(null, new object[] { pileType })
                ?? throw new Exception($"No result for {pileType}."));
            bool expected = pileType == PileType.Discard;
            if (actual != expected)
                throw new Exception($"Halo return eligibility mismatch for {pileType}: {actual}.");
        }

        Console.WriteLine("PASS: delayed Halo recovery accepts only the discard pile (not hand, draw, play, or exhaust).");
    }
}
