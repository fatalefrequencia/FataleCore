import requests

url = "https://fatalecore-production.up.railway.app/api/ping"
r = requests.get(url)
print(f"Status: {r.status_code}")
print(f"Content: {r.text}")
print(f"Headers: {r.headers}")
