"""
Make UniRig run on any GPU (incl. Colab T4) by disabling FlashAttention everywhere.

FlashAttention-2 requires Ampere+ and errors on Turing. UniRig requests flash in THREE
places, so we disable all of them:
  1. LLM (skeleton):            _attn_implementation: flash_attention_2  -> sdpa
  2. michelangelo encoder:      flash: True                              -> flash: False
  3. PTv3 point-cloud encoder:  enable_flash defaults True in code; the skin config's
                                ptv3obj block has no flag, so we INJECT enable_flash: False
                                (it has a built-in non-flash fallback).

Upload to Colab, then run in a cell:

    %run /content/colab_disable_flash.py

Then re-run the skin cell (skeleton already succeeded). Idempotent — safe to re-run.
"""
import glob
import os
import re


def patch_text(txt: str) -> str:
    new = txt.replace("flash_attention_2", "sdpa")
    new = re.sub(r"flash:\s*True", "flash: False", new)
    # Inject enable_flash: False as a sibling of any `__target__: ptv3obj` (PTv3 encoder).
    if "enable_flash" not in new:
        new = re.sub(
            r"(?m)^(\s*)__target__:\s*ptv3obj\s*$",
            lambda m: f"{m.group(0)}\n{m.group(1)}enable_flash: False",
            new,
        )
    return new


changed = []
for path in glob.glob("/content/UniRig/configs/**/*.yaml", recursive=True):
    with open(path) as f:
        txt = f.read()
    new = patch_text(txt)
    if new != txt:
        with open(path, "w") as f:
            f.write(new)
        changed.append(path)

if changed:
    print(f"Patched {len(changed)} config file(s) to disable FlashAttention:")
    for c in changed:
        print("  -", c)
    print("\nNow re-run the skin cell.")
else:
    print("No changes made — configs may already be patched (idempotent).")
