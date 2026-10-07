# Crawler politicos pentru CIMEC ETNOMON (CC BY-SA 4.0): lista -> fise -> JSON.
import re, html, json, time, urllib.request, os, sys
BASE = "https://monumente-etnografice.cimec.ro"
OUT = os.path.dirname(os.path.abspath(__file__))
UA = {"User-Agent": "Mozilla/5.0 (research; knowledge-base extraction; contact via user)"}

def get(url, tries=3):
    for i in range(tries):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=30) as r:
                return r.read().decode("utf-8", errors="replace")
        except Exception as e:
            time.sleep(3 * (i + 1))
    return ""

def text(s):
    t = re.sub(r"<script.*?</script>|<style.*?</style>|<!--.*?-->", "", s, flags=re.S)
    t = re.sub(r"<(br|/tr|/p|/div|/h\d|/li|/td)[^>]*>", "\n", t, flags=re.I)
    t = re.sub(r"<[^>]+>", " ", t); t = html.unescape(t)
    return [re.sub(r"\s+", " ", l).strip() for l in t.splitlines() if l.strip()]

FIELDS = ["Muzeu:", "Categoria:", "Denumirea în muzeu:", "Denumirea locală:", "Descriere", "Zona etnografică:", "Provenienţa:",
          "Etnia:", "Datare:", "Tipul construcţiei:", "Categoria construcţiei:", "Fundaţia construcţiei:", "Temelia:",
          "Pereţii construcţiei:", "Şarpanta:", "Învelitoare:", "Stare de conservare:", "Autorul fişei:", "Data fișei:"]

def parse(lines):
    joined = "\n".join(lines)
    # unifica etichetele sparte pe doua linii ("Denumirea în\nmuzeu:")
    joined = joined.replace("Denumirea în\nmuzeu:", "Denumirea în muzeu:").replace("Denumirea\nlocală:", "Denumirea locală:")
    joined = joined.replace("Zona\netnografică:", "Zona etnografică:")
    L = joined.split("\n")
    rec, cur = {}, None
    for l in L:
        hit = next((f for f in FIELDS if l.startswith(f)), None)
        if hit:
            cur = hit.rstrip(":"); rest = l[len(hit):].strip()
            rec[cur] = rest
        elif cur and l not in ("Scroll",) and not l.startswith("Bază de date") and not l.startswith("Sector muzeal") \
                and not l.endswith(":"):
            if cur in rec and len(rec[cur]) < 1500:
                rec[cur] = (rec[cur] + " " + l).strip()
        elif l.endswith(":"):
            cur = None
    return rec

ids_file = os.path.join(OUT, "ids.json")
if os.path.exists(ids_file):
    ids = json.load(open(ids_file))
else:
    ids = []
    for p in range(1, 60):
        s = get(f"{BASE}/muzee-in-aer-liber.asp?page={p}")
        new = re.findall(r'muzee-in-aer-liber\.asp\?ID=([0-9A-F]{32})', s)
        new = [x for x in dict.fromkeys(new) if x not in ids]
        if not new and p > 56: break
        ids += new
        time.sleep(0.6)
    json.dump(ids, open(ids_file, "w"))
print("ids", len(ids), flush=True)

out_file = os.path.join(OUT, "records.jsonl")
done = set()
if os.path.exists(out_file):
    for l in open(out_file, encoding="utf-8"):
        try: done.add(json.loads(l)["ID"])
        except: pass
with open(out_file, "a", encoding="utf-8") as fo:
    for i, ID in enumerate(ids):
        if ID in done: continue
        s = get(f"{BASE}/muzee-in-aer-liber.asp?ID={ID}")
        if not s: continue
        r = parse(text(s)); r["ID"] = ID
        fo.write(json.dumps(r, ensure_ascii=False) + "\n"); fo.flush()
        if i % 100 == 0: print(i, flush=True)
        time.sleep(0.6)
print("done", flush=True)
