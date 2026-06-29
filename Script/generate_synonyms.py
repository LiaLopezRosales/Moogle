#!/usr/bin/env python3
"""Generate Spanish synonym groups from OMW (Open Multilingual WordNet) for Moogle!"""

import os
import zipfile
from collections import defaultdict
from nltk import data

script_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.dirname(script_dir)
output_path = os.path.join(project_root, "Content", "synonyms.txt")

# Load OMW Spanish data
path = data.find("corpora/omw-1.4.zip")
with zipfile.ZipFile(str(path)) as z:
    content = z.read("omw-1.4/mcr/wn-data-spa.tab").decode("utf-8")

# Group lemmas by synset (offset + pos)
synsets = defaultdict(set)
for line in content.split("\n"):
    line = line.strip()
    if not line or line.startswith("#") or line.startswith("-"):
        continue
    # Format: offset-pos \t lemma \t word
    parts = line.split("\t")
    if len(parts) >= 3:
        synset_id = parts[0]  # e.g., "00001740-a"
        lemma = parts[2].strip().lower()
        # Skip multi-word lemmas (can't stem)
        if " " in lemma or "_" in lemma:
            continue
        synsets[synset_id].add(lemma)

# Filter to groups with 2+ lemmas
seen = set()
count = 0
with open(output_path, "w", encoding="utf-8") as f:
    f.write("# Spanish synonym groups generated from OMW (Open Multilingual WordNet)\n")
    f.write(f"# Source: omw-1.4/mcr/wn-data-spa.tab\n")
    f.write(f"# Total synsets: {len(synsets)}, single-word: {sum(1 for v in synsets.values() if len(v) >= 2)}\n\n")

    for synset_id, lemmas in sorted(synsets.items()):
        if len(lemmas) >= 2:
            sorted_lemmas = sorted(lemmas)
            key = ",".join(sorted_lemmas)
            if key not in seen:
                seen.add(key)
                f.write(",".join(sorted_lemmas) + "\n")
                count += 1

print(f"Generated {output_path}")
print(f"Total synonym groups: {count}")
print(f"Total unique words covered: {len(seen)}")
