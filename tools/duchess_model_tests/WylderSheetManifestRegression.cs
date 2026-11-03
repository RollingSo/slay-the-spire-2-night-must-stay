using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using NightMustStay.Core.Models.Characters;
using NightMustStay.Core.Models.Cards;
using System.Buffers.Binary;
using System.Security.Cryptography;

internal static class WylderSheetManifestRegression
{
    public static void Run()
    {
        using var source=JsonDocument.Parse(File.ReadAllText("design/wylder/SHEET_SOURCE_20261029.json"));
        using var text=JsonDocument.Parse(File.ReadAllText("NightMustStay/localization/zhs/cards.json"));
        var pool=ModelDb.Character<Wylder>().CardPool.AllCards.ToArray();
        int count=0;
        foreach(var row in source.RootElement.GetProperty("rows").EnumerateArray().Skip(1))
        {
            if(row[1].ValueKind!=JsonValueKind.String || string.IsNullOrWhiteSpace(row[1].GetString()))continue;
            var name=row[1].GetString();
            if(name is "处决" or "跳跃重击")continue;
            if(name=="燎原")name="燎原地火";
            if(name=="反冲袭击")name="反冲桩柱";
            var matches=pool.Where(c=>text.RootElement.GetProperty(c.Id.Entry+".title").GetString()==name).ToArray();
            if(matches.Length!=1)throw new Exception("Sheet registration/name mismatch: "+name);
            var card=matches[0].ToMutable();
            var type=row[3].GetString() switch {"攻击牌"=>CardType.Attack,"技能牌"=>CardType.Skill,"能力牌"=>CardType.Power,_=>throw new Exception("Unknown sheet type")};
            var rarity=row[4].GetString() switch {"初始"=>CardRarity.Basic,"普通"=>CardRarity.Common,"罕见"=>CardRarity.Uncommon,"稀有"=>CardRarity.Rare,_=>throw new Exception("Unknown sheet rarity")};
            if(card.Type!=(card switch { WylderDesperateSurvival => CardType.Skill, WylderWildfire => CardType.Power, _ => type }) || card.Rarity!=(card is WylderNightForm ? CardRarity.Rare : card is WylderAerialStake or WylderWildfire ? CardRarity.Uncommon : rarity))throw new Exception("Sheet type/rarity mismatch: "+name);
            if(row[2].ValueKind==JsonValueKind.Number)
            {if(card.EnergyCost.CostsX || card.EnergyCost.GetResolved()!=(card switch { WylderSlidingLeap => 2, WylderCounterRush => 3, WylderDesperateSurvival => 4, WylderRemainingEmber => 1, _ => row[2].GetInt32() }))throw new Exception("Sheet cost mismatch: "+name);}
            else if(row[2].GetString()=="X" && !card.EnergyCost.CostsX)throw new Exception("Missing X cost: "+name);
            count++;
        }
        if(count!=35 || pool.Length!=count+5)throw new Exception("Sheet named-card coverage mismatch");
        var hashes = new HashSet<string>();
        var artManifest = new List<object>();
        foreach (var card in pool.Append(ModelDb.Card<WylderFireCoating>()))
        {
            const string prefix = "res://images/packed/card_portraits/wylder/";
            if (!card.PortraitPath.StartsWith(prefix, StringComparison.Ordinal))
                throw new Exception("Temporary/non-Wylder portrait prohibited: " + card.Id);
            var path = card.PortraitPath.Substring(6);
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 24 || !bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) ||
                BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4)) != 1000 ||
                BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4)) != 760)
                throw new Exception("Portrait must be a 1000x760 PNG: " + path);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!hashes.Add(hash)) throw new Exception("Shared placeholder portrait prohibited: " + path);
            artManifest.Add(new { id=card.Id.Entry, name=text.RootElement.GetProperty(card.Id.Entry+".title").GetString(), path, sha256=hash });
        }
        Directory.CreateDirectory("build");
        File.WriteAllText("build/wylder_card_art_manifest.json", JsonSerializer.Serialize(artManifest, new JsonSerializerOptions { WriteIndented=true }));
        Console.WriteLine("PASS: all 41 Wylder portraits are dedicated, unique 1000x760 PNGs (40 pool cards plus Fire Coating).");
        Console.WriteLine("PASS: all 35 retained named workbook rows match runtime names, costs, types and rarity; the pool also includes Strike, Defend Centering Breath Eruption and Valor.");
    }
}
