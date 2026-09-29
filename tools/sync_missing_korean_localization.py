"""Fill missing Korean localization keys with English fallbacks.

Existing Korean translations are never replaced. This keeps newly merged
content playable until a dedicated Korean translation is available.
"""

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1] / "NightMustStay" / "localization"
for english_path in sorted((ROOT / "eng").glob("*.json")):
    korean_path = ROOT / "kor" / english_path.name
    english = json.loads(english_path.read_text(encoding="utf-8-sig"))
    korean = json.loads(korean_path.read_text(encoding="utf-8-sig"))
    missing = {key: value for key, value in english.items() if key not in korean}
    if not missing:
        continue
    korean.update(missing)
    korean_path.write_text(
        json.dumps(korean, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"{english_path.name}: added {len(missing)} English fallbacks")
