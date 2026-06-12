"""
Point Colab at the UniRig 3.11 venv and verify the key deps.

Upload this file to your Colab session (Files panel ▸ Upload), then in a cell run:

    %run /content/colab_setup_env.py

Using %run (not !python) matters: it executes inside the kernel, so the PATH
change persists for every later `!bash ...` cell — which is what makes the rig
scripts use the 3.11 venv's `python` instead of Colab's base 3.12.
"""
import os
import subprocess

VENV = "/content/uenv"
PY = f"{VENV}/bin/python"

# Put the venv first on PATH for all subsequent cells (process-global, persists via %run).
os.environ["PATH"] = f"{VENV}/bin:" + os.environ.get("PATH", "")
os.environ["VIRTUAL_ENV"] = VENV

print("python ->", PY)
subprocess.run([PY, "--version"])

check = (
    "import bpy, lightning, spconv, torch_scatter, transformers; "
    "print('all imports OK | bpy', bpy.app.version_string)"
)
result = subprocess.run([PY, "-c", check])

if result.returncode == 0:
    print("\nENV READY — now re-run the skeleton cell.")
else:
    print("\nAn import failed above. Send me the traceback for the package that broke.")
