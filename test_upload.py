import subprocess
import os

print("Creating test archive...")
subprocess.run(["tar", "-czf", "test.tar.gz", ".gitignore"])

print("Testing Railway push...")
with open("test.tar.gz", "rb") as f:
    proc = subprocess.Popen(["npx.cmd", "@railway/cli", "run", "tar", "-xzf", "-", "-C", "/data"], stdin=f)
    proc.wait()

print(f"Exit code: {proc.returncode}")
if proc.returncode == 0:
    print("Success!")
