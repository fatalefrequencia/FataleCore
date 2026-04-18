import sqlite3
import os

db_path = 'fatale_core.db'
if not os.path.exists(db_path):
    # Try alternate location if not in CWD
    db_path = 'fatale.db' 

print(f"Connecting to {db_path}...")
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

try:
    print("Adding StatusMessage column to Users table...")
    cursor.execute("ALTER TABLE Users ADD COLUMN StatusMessage TEXT")
    conn.commit()
    print("Column added successfully.")
except sqlite3.OperationalError as e:
    if "duplicate column name" in str(e).lower():
        print("Column already exists.")
    else:
        print(f"Error: {e}")
finally:
    conn.close()
