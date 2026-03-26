import requests

url = "https://fatalecore-production.up.railway.app/uploads/avatars/9b4dcbd3-7be6-4a10-9794-44e76539cb7b.PNG"
r = requests.get(url)
print(f"Status: {r.status_code}")
print(f"Headers: {r.headers}")
if r.status_code == 200:
    print(f"Content Length: {len(r.content)}")
else:
    print(f"Error: {r.text[:200]}")
