"""Refresh generated asset inventory after the approved Duchess item replacement."""
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
old_relics = {"mended_pocketwatch", "lace_cuff", "silver_thimble", "dance_shoes", "unsent_letter", "blue_ribbon"}
old_potions = {"silver_perfume", "veil_vial", "memory_draught"}
new_relics = ["reverse_pocketwatch", "crown_badge", "golden_dewdrop", "primal_glintstone_blade",
              "blessed_iron_coin", "night_of_wisdom", "blue_stained_blade", "carian_badge"]
new_potions = ["smoke_bottle", "radiant_blade_crystal", "regret_potion"]

report_path = ROOT / "design/duchess/asset_report.json"
report = json.loads(report_path.read_text(encoding="utf-8"))
for kind, names in (("relics", old_relics), ("potions", old_potions)):
    for name in names:
        report.pop(f"duchess_assets/{kind}/duchess_{name}.png", None)
for kind, names in (("relics", new_relics), ("potions", new_potions)):
    for name in names:
        relative = f"duchess_assets/{kind}/duchess_{name}.png"
        with Image.open(ROOT / relative) as image:
            report[relative] = {"size": list(image.size), "mode": image.mode,
                                "bounds": list(image.getbbox()) if image.getbbox() else None}
for name in ("zero_cost_attack_power", "graceful_sword_dance_power", "phantom_killer_power"):
    for folder in ("images/powers", "powers"):
        relative = f"{folder}/duchess_{name}.png"
        with Image.open(ROOT / relative) as image:
            report[relative] = {"size": list(image.size), "mode": image.mode,
                                "bounds": list(image.getbbox()) if image.getbbox() else None}
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

manifest_path = ROOT / "design/duchess/art_manifest.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
manifest["icons"] = [entry for entry in manifest["icons"]
                     if entry["key"] not in {f"duchess_{name}" for name in old_relics | old_potions}]
existing = {entry["key"] for entry in manifest["icons"]}
for kind, names in (("relics", new_relics), ("potions", new_potions)):
    for name in names:
        key = f"duchess_{name}"
        if key not in existing:
            manifest["icons"].append({"key": key, "hint": f"Approved 2026-09-30: duchess_assets/{kind}/{key}.png"})
for name in ("zero_cost_attack_power", "graceful_sword_dance_power", "phantom_killer_power"):
    key = f"duchess_{name}"
    if key not in existing:
        manifest["icons"].append({"key": key, "hint": f"2026-09-30: images/powers/{key}.png"})
known_cards = {entry["key"] for entry in manifest["cards"]}
for name in ("Memory", "InchVictory", "LorettaSlash", "SacredHalo", "GracefulSwordDance",
             "MemoryFragment", "PhantomKiller", "GreatCaria", "Fate"):
    if name not in known_cards:
        manifest["cards"].append({"key": name,
                                  "hint": "Preview awaiting approval: design/card art preview/Duchess_2026-09-30_NewCards/processed"})
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
