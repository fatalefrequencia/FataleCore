import sqlite3
import os

db_path = 'c:/Users/USER/.gemini/antigravity/scratch/FataleCore/fatale_core.db'

def verify():
    if not os.path.exists(db_path):
        print(f"DB not found at {db_path}")
        return
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    try:
        cursor.execute("PRAGMA table_info(Users)")
        columns = [row[1] for row in cursor.fetchall()]
        print("Columns in Users table:")
        for col in columns:
            print(f" - {col}")
        
        expected = ["StatusMessage", "MonitorImageUrl", "MonitorBackgroundColor", "MonitorIsGlass"]
        missing = [c for c in expected if c not in columns]
        
        if not missing:
            print("\nSUCCESS: All expected columns exist!")
        else:
            print(f"\nMISSING: {missing}")
            
    except Exception as e:
        print(f"Error: {e}")
    finally:
        conn.close()

if __name__ == "__main__":
    verify()
