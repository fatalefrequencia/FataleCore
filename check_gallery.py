import psycopg2
import os

db_url = os.environ.get("DATABASE_URL") or "postgresql://postgres:fSgJpZgYlXXFCHOhNusTngjPzGIfaKqL@junction.proxy.rlwy.net:47466/railway"

conn = psycopg2.connect(db_url)
cur = conn.cursor()

cur.execute("SELECT \"Id\", \"UserId\", \"Title\", \"Url\" FROM \"GalleryItems\" WHERE \"UserId\" IN (SELECT \"Id\" FROM \"Users\" WHERE \"Username\" LIKE '%mel00%');")
rows = cur.fetchall()
print("Gallery Items:")
for r in rows:
    print(r)

conn.close()
