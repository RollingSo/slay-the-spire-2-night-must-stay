from pathlib import Path
from PIL import Image, ImageOps, ImageEnhance, ImageFilter, ImageChops

ROOT = Path(r"D:\sts-2-mod")
GEN = Path(r"C:\Users\wenti\.codex\generated_images\019f6b05-d0b0-7af1-a624-760fba7be29d")

S = {
    "select_bg": "exec-b9e1d6bb-1c3a-4e14-9295-c50ba7317cd4.png",
    "portrait": "exec-d3f5a5b7-4558-44a6-87ba-bec1ffb52597.png",
    "idle": "exec-c6f49b1a-1d0c-44dc-bcd1-c041f7832c30.png",
    "helen": "exec-09a3f41d-9db7-4fd4-8d2a-b9c64983086c.png",
    "frederick": "exec-de107473-63b6-47b6-acc7-5de1288d5fbd.png",
    "sebastian": "exec-8c810f7b-5d74-4e0c-92a6-ba6f5201de4e.png",
    "strike": "exec-40b34fb1-e97c-4eae-8976-faac81071a19.png",
    "defend": "exec-1e0ea722-6e05-48c9-b6e8-6b2271f544fc.png",
    "call": "exec-0ab24d12-11d7-4c8d-a002-6df5343eb167.png",
    "resonance": "exec-c6978cd6-dd1c-42c1-9ccc-73416735f073.png",
    "helen_card": "exec-6fadbf32-4d08-47be-8b88-53a834f0b9ec.png",
    "frederick_card": "exec-6ced7cee-8bd8-42bd-b6b3-975ad3f90e74.png",
    "sebastian_card": "exec-a1ce80f6-2d36-423b-9311-0209f66ab057.png",
    "relic": "exec-87b111a6-3ee1-42fb-9197-264550930938.png",
    "rest": "exec-e66f3cbb-3bca-4a41-982d-6d46ada7a7d4.png",
    "merchant": "exec-f51802d0-725e-4244-8440-86dd1dbed9f4.png",
    "power": "exec-763fbf21-4bd4-4fc0-bc26-f8d40fa6f583.png",
    "energy": "exec-6c2e670a-3ee1-41af-993f-5fa339d14cf6.png",
    "attack": "exec-c00ca746-6299-4fe5-a9f2-374cce867fc3.png",
    "hit": "exec-3c298008-c636-492b-b044-5d407403edf0.png",
    "necro": "exec-1c6b107c-2929-439f-b588-ce157c2c6ea7.png",
    "hands": "exec-2c11cb82-eb01-4858-8f5e-3866021eab0c.png",
}

def load(key):
    im = Image.open(GEN / S[key]).convert("RGBA")
    if key == "portrait":
        px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = px[x, y]
                # Remove the flat magenta chroma key while retaining antialiased edges.
                d = min(abs(r - 255) + abs(g - 0) + abs(b - 255), abs(r - 255) + abs(g - 0) + abs(b - 240))
                if d < 72:
                    px[x, y] = (r, g, b, 0)
                elif d < 150:
                    px[x, y] = (r, g, b, int(a * (d - 72) / 78))
    return im
def save(im, rel):
    p = ROOT / rel; p.parent.mkdir(parents=True, exist_ok=True); im.save(p)
def fit(key, size, rel): save(ImageOps.fit(load(key), size, Image.Resampling.LANCZOS), rel)
def contain(key, size, rel, scale=.94):
    src=load(key); box=(int(size[0]*scale),int(size[1]*scale)); src.thumbnail(box,Image.Resampling.LANCZOS)
    out=Image.new("RGBA",size); out.alpha_composite(src,((size[0]-src.width)//2,(size[1]-src.height)//2)); save(out,rel)

fit("select_bg", (2560,1200), "revenant_assets/character_select_revenant_bg.png")
contain("idle", (1156,1360), "revenant_assets/combat/revenant_idle.png")
contain("attack", (1156,1360), "revenant_assets/combat/revenant_attack.png")
contain("hit", (1156,1360), "revenant_assets/combat/revenant_hit.png")
contain("rest", (1158,1358), "revenant_assets/rest_site/revenant_rest_site.png")
contain("merchant", (1156,1360), "revenant_assets/merchant/revenant_merchant.png")

portrait=load("portrait")
save(ImageOps.fit(portrait,(132,195),Image.Resampling.LANCZOS),"revenant_assets/char_select_revenant.png")
locked=ImageOps.grayscale(ImageOps.fit(portrait,(132,195),Image.Resampling.LANCZOS)).convert("RGBA")
locked=ImageEnhance.Brightness(locked).enhance(.55); save(locked,"revenant_assets/char_select_revenant_locked.png")
idle=load("idle")
head=idle.crop((int(idle.width*.30),0,int(idle.width*.76),int(idle.height*.45)))
icon=ImageOps.fit(head,(64,64),Image.Resampling.LANCZOS); save(icon,"revenant_assets/character_icon_revenant.png")
outline=icon.filter(ImageFilter.EDGE_ENHANCE_MORE); save(outline,"revenant_assets/character_icon_revenant_outline.png")
save(ImageOps.fit(head,(49,64),Image.Resampling.LANCZOS),"revenant_assets/map_marker_revenant.png")

for key,name in [("helen","helen"),("frederick","frederick"),("sebastian","sebastian"),("necro","necro")]: contain(key,(512,512),f"revenant_assets/families/{name}.png")
for key,name in [("strike","strike_revenant"),("defend","defend_revenant"),("call","call"),("resonance","resonance"),("helen_card","helen_family"),("frederick_card","pumpkin_head_family"),("sebastian_card","skeleton_family")]: fit(key,(1000,760),f"revenant_assets/cards/{name}.png")
contain("relic",(256,256),"revenant_assets/relics/revenant_starter_relic.png")
contain("power",(256,256),"revenant_assets/powers/revenant_summon_controller_power.png")

energy=load("energy")
for i in range(1,6):
    layer=ImageOps.fit(energy.rotate((i-3)*7,Image.Resampling.BICUBIC,expand=False),(256,256),Image.Resampling.LANCZOS)
    if i<5: layer.putalpha(layer.getchannel("A").point(lambda a:int(a*(.28+i*.11))))
    save(layer,f"revenant_assets/energy/revenant_orb_layer_{i}.png")
save(ImageOps.fit(energy,(74,74),Image.Resampling.LANCZOS),"revenant_assets/energy/revenant_energy.png")
save(ImageOps.fit(energy,(24,24),Image.Resampling.LANCZOS),"revenant_assets/energy/revenant_energy_font_icon.png")

hands=load("hands"); w,h=hands.size
for name,box in {"point":(0,0,w//2,h//2),"rock":(w//2,0,w,h//2),"paper":(0,h//2,w//2,h),"scissors":(w//2,h//2,w,h)}.items():
    save(ImageOps.fit(hands.crop(box),(627,627),Image.Resampling.LANCZOS),f"revenant_assets/multiplayer_hands/revenant_{name}.png")

# A grayscale threshold mask: lyre-eye core expands into three spirit rings.
mask=Image.new("L",(2560,1200),0)
em=ImageOps.fit(energy,(900,900),Image.Resampling.LANCZOS).convert("L")
mask.paste(em,(830,150),em)
for radius,value in [(540,80),(760,140),(980,200),(1250,245)]:
    ring=Image.new("L",mask.size,0)
    from PIL import ImageDraw
    d=ImageDraw.Draw(ring); d.ellipse((1280-radius,600-radius,1280+radius,600+radius),outline=value,width=100)
    mask=ImageChops.lighter(mask,ring)
mask_rgba=Image.new("RGBA",mask.size,(255,255,255,0)); mask_rgba.putalpha(mask)
save(mask_rgba,"revenant_assets/revenant_transition_mask.png")
print("installed formal Revenant artwork")
