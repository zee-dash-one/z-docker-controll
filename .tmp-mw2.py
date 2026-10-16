
from pathlib import Path
t = Path("/tmp/9rnext/middleware.js").read_text(errors="ignore")
# extract aS,aO,aN,aL,aK,aH functions vicinity
for mark in ["function aN", "function aO", "async function aS", "async function aH", "async function aG", "let aL=", "let aK=", "aM=new Set"]:
    j = t.find(mark)
    print(f"\n######## {mark} @{j} ########")
    print(t[j:j+700] if j>=0 else "NOT FOUND")

# derive CLI token same way? show aC/aD/aG
for mark in ["async function aC", "function aD", "aD=", "9r-cli-auth"]:
    j = t.find(mark)
    print(f"\n######## {mark} @{j} ########")
    print(t[max(0,j-100):j+500] if j>=0 else "NOT FOUND")
