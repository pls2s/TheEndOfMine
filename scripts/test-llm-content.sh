#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${ENV_FILE:-TheEndOfMine/Resources/Raw/llm.env}"
if [[ -f "$ENV_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

PROVIDER="${LLM_PROVIDER:-typhoon}"
if [[ "$PROVIDER" == "openai" ]]; then
  MODEL="${OPENAI_MODEL:-${LLM_MODEL:-gpt-5.4-mini}}"
  ENDPOINT="${OPENAI_ENDPOINT:-${LLM_ENDPOINT:-https://api.openai.com/v1/responses}}"
  API_KEY="${OPENAI_API_KEY:-${LLM_API_KEY:-}}"
  PLACEHOLDER_KEY="sk-your_api_key_here"
else
  PROVIDER="typhoon"
  MODEL="${TYPHOON_MODEL:-${LLM_MODEL:-typhoon-v2.5-30b-a3b-instruct}}"
  ENDPOINT="${TYPHOON_ENDPOINT:-${LLM_ENDPOINT:-https://api.opentyphoon.ai/v1/chat/completions}}"
  API_KEY="${TYPHOON_API_KEY:-${LLM_API_KEY:-}}"
  PLACEHOLDER_KEY="tp-your_api_key_here"
fi

SURVIVOR_NAME="${1:-ผู้รอดชีวิตทดสอบ}"
SURVIVOR_GENDER="${2:-Female}"
OUT_DIR="${OUT_DIR:-tmp}"
OUT_FILE="${OUT_FILE:-$OUT_DIR/generated-story.json}"

if [[ -z "$API_KEY" || "$API_KEY" == "$PLACEHOLDER_KEY" ]]; then
  echo "missing API key for provider: $PROVIDER"
  echo "edit $ENV_FILE first, then run: $0 [survivor_name] [Male|Female]"
  exit 1
fi

mkdir -p "$OUT_DIR"

REQUEST_FILE="$(mktemp)"
RESPONSE_FILE="$(mktemp)"
trap 'rm -f "$REQUEST_FILE" "$RESPONSE_FILE"' EXIT

python3 - "$PROVIDER" "$MODEL" "$SURVIVOR_NAME" "$SURVIVOR_GENDER" > "$REQUEST_FILE" <<'PY'
import json
import sys

provider, model, survivor_name, survivor_gender = sys.argv[1:5]

system_prompt = """คุณคือระบบสร้างเนื้อเรื่องของเกม The End of Mine เกมเอาตัวรอดหลังหายนะที่แสดงผลเป็นภาษาไทย

หน้าที่ของคุณคือสร้างหนึ่งรอบการเล่นใหม่ที่เล่นได้จริง ไม่ใช่เขียนเรื่องสั้นให้อ่านอย่างเดียว

กฎสำคัญ:
- ตอบกลับเป็น JSON object ที่ถูกต้องเพียงก้อนเดียวเท่านั้น
- ห้ามครอบ JSON ด้วย markdown
- ห้ามใส่คำอธิบาย คอมเมนต์ หรือ key นอก schema ที่กำหนด
- ข้อความทุกอย่างที่ผู้เล่นเห็นต้องเป็นภาษาไทยธรรมชาติ
- โทนเรื่องต้องกดดัน สมจริง เน้นการเอาตัวรอด และมีทางเลือกที่ลำบากทางศีลธรรม
- หลีกเลี่ยงแฟนตาซี เวทมนตร์ มุกตลก ซูเปอร์ฮีโร่ และฉากแอ็กชันทหารเกินจริง
- ทุก event ต้องเป็นสถานการณ์ที่ผู้เล่นตัดสินใจได้ทันทีในเกม
- ทุก choice ต้องมี tradeoff ที่มีความหมาย
- ผลกระทบต่อทรัพยากรต้องสมดุล ห้ามทำให้ตัวเลือกหนึ่งดีกว่าอีกตัวเลือกแบบชัดเจนเสมอ
- ไอเทมต้องเป็นของใช้เอาตัวรอดที่เข้ากับบริบทของเหตุการณ์
- id ของไอเทมที่สร้างต้องเป็น lowercase snake_case และขึ้นต้นด้วย gen_

game engine จะ deserialize คำตอบของคุณทันที ถ้า JSON ผิดหรือ field ไม่ครบ คำตอบจะถูกปฏิเสธ"""

user_prompt = f"""สร้างเนื้อเรื่องรอบใหม่สำหรับเกม The End of Mine
ชื่อตัวละคร: {survivor_name}
เพศตัวละคร: {survivor_gender}

ข้อกำหนด:
- ใช้ภาษาไทยสำหรับ storyTitle, title, description, choice text, resultText, ชื่อไอเทม และคำอธิบายไอเทม
- สร้าง event ให้ครบ 8 เหตุการณ์
- แต่ละ event ต้องมี choice 2 ตัวเลือกพอดี
- แต่ละ choice ต้องมี id, text, hpEffect, hungerEffect, thirstEffect, fatigueEffect, resultText
- ค่าผลกระทบตัวเลขต้องสมดุล และอยู่ระหว่าง -30 ถึง 30
- ใส่ itemReward ให้ choice จำนวน 4 จุดพอดี โดย itemReward ต้องเป็น item object ครบถ้วน
- choice อื่นที่ไม่ได้ให้ไอเทมห้ามใส่ key itemReward
- สร้าง startingItems 3 ชิ้น
- โทนต้องกดดัน เน้นการเอาตัวรอด และแต่ละเหตุการณ์ต้องไม่ซ้ำอารมณ์กัน

ตอบ JSON ตามรูปแบบนี้เท่านั้น:
{{
  "storyTitle": "string",
  "events": [
    {{
      "id": "evt_01",
      "title": "string",
      "description": "string",
      "choices": [
        {{
          "id": "c1",
          "text": "string",
          "hpEffect": 0,
          "hungerEffect": 0,
          "thirstEffect": 0,
          "fatigueEffect": 0,
          "resultText": "string",
          "itemReward": {{
            "id": "gen_item_id",
            "name_th": "string",
            "name_en": "string",
            "category": "Food|Water|Medicine|Weapon|Tool|Misc",
            "subcategory": "string",
            "rarity": "common|uncommon|rare",
            "weight_kg": 1,
            "trade_value": 1,
            "stackable": false,
            "max_stack": 1,
            "found_in": ["generated_story"],
            "durability_max": 1,
            "effects": {{
              "hp_restore": 0,
              "hunger_restore": 0,
              "thirst_restore": 0,
              "fatigue_restore": 0
            }},
            "description_th": "string",
            "story_alias": "gen_alias"
          }}
        }}
      ]
    }}
  ],
  "startingItems": []
}}"""

if provider == "openai":
    payload = {
        "model": model,
        "input": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt},
        ],
        "text": {"format": {"type": "json_object"}},
    }
else:
    payload = {
        "model": model,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt},
        ],
        "max_tokens": 8192,
        "temperature": 0.7,
        "top_p": 0.95,
        "repetition_penalty": 1.05,
        "stream": False,
    }

print(json.dumps(payload, ensure_ascii=False))
PY

echo "requesting $PROVIDER content..."
curl -sS "$ENDPOINT" \
  -H "Authorization: Bearer $API_KEY" \
  -H "Content-Type: application/json" \
  -d @"$REQUEST_FILE" \
  -o "$RESPONSE_FILE"

python3 - "$RESPONSE_FILE" "$OUT_FILE" <<'PY'
import json
import sys

response_path, out_path = sys.argv[1:3]

with open(response_path, "r", encoding="utf-8") as f:
    response = json.load(f)

if "error" in response:
    raise SystemExit(f"LLM error: {response['error']}")

content = response.get("output_text")
if not content:
    for item in response.get("output", []):
        for part in item.get("content", []):
            if "text" in part:
                content = part["text"]
                break
        if content:
            break

if not content:
    choices = response.get("choices", [])
    if choices:
        content = choices[0].get("message", {}).get("content")

if not content:
    raise SystemExit("No model content found in response")

data = json.loads(content)

events = data.get("events", [])
items = data.get("startingItems", [])
errors = []

if not data.get("storyTitle"):
    errors.append("missing storyTitle")
if len(events) != 8:
    errors.append(f"events must be 8, got {len(events)}")
if len(items) != 3:
    errors.append(f"startingItems must be 3, got {len(items)}")

reward_count = 0
for event_index, event in enumerate(events, start=1):
    choices = event.get("choices", [])
    if len(choices) != 2:
        errors.append(f"event {event_index} choices must be 2, got {len(choices)}")
    for choice_index, choice in enumerate(choices, start=1):
        for field in ("id", "text", "hpEffect", "hungerEffect", "thirstEffect", "fatigueEffect", "resultText"):
            if field not in choice:
                errors.append(f"event {event_index} choice {choice_index} missing {field}")
        for field in ("hpEffect", "hungerEffect", "thirstEffect", "fatigueEffect"):
            value = choice.get(field)
            if isinstance(value, (int, float)) and not -30 <= value <= 30:
                errors.append(f"event {event_index} choice {choice_index} {field} out of range: {value}")
        if choice.get("itemReward"):
            reward_count += 1
            if reward_count > 5:
                choice.pop("itemReward", None)

normalized_reward_count = sum(
    1
    for event in events
    for choice in event.get("choices", [])
    if choice.get("itemReward")
)

if not 3 <= normalized_reward_count <= 5:
    errors.append(f"itemReward count must be 3-5, got {normalized_reward_count}")

if errors:
    print("validation failed:")
    for error in errors:
        print(f"- {error}")
    raise SystemExit(1)

with open(out_path, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)
    f.write("\n")

print(f"storyTitle: {data['storyTitle']}")
print(f"events: {len(events)}")
print(f"startingItems: {len(items)}")
print(f"itemRewards: {normalized_reward_count}")
print(f"saved: {out_path}")
PY
