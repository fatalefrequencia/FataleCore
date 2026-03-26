import sqlite3
import os

db_path = 'fatale_core.db'
if not os.path.exists(db_path):
    print(f"Error: {db_path} not found")
    exit(1)

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("--- Users (first 5) ---")
cursor.execute("SELECT id, username, profilePictureUrl FROM Users LIMIT 5")
for row in cursor.fetchall():
    print(row)

print("\n--- Tracks (first 5) ---")
cursor.execute("SELECT id, title, coverImageUrl FROM Tracks LIMIT 5")
for row in cursor.fetchall():
    print(row)

conn.close()
