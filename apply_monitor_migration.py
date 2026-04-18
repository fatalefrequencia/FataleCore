import sqlite3
import os

db_path = 'fatale_core.db'
migration_id = '20260418030000_AddMonitorStyling'
product_version = '10.0.6'

def apply_migration():
    if not os.path.exists(db_path):
        print(f"Error: {db_path} not found. Searching for database...")
        # Try to find it in common spots
        return

    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()

        print(f"Applying migration {migration_id} to {db_path}...")

        # Add columns to Users table
        columns_to_add = [
            ("MonitorImageUrl", "TEXT"),
            ("MonitorBackgroundColor", "TEXT"),
            ("MonitorIsGlass", "INTEGER NOT NULL DEFAULT 0")
        ]

        for col_name, col_type in columns_to_add:
            try:
                cursor.execute(f"ALTER TABLE Users ADD \"{col_name}\" {col_type};")
                print(f"  Added column {col_name}")
            except sqlite3.OperationalError as e:
                if "duplicate column name" in str(e).lower():
                    print(f"  Column {col_name} already exists.")
                else:
                    raise e

        # Update migration history
        cursor.execute("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (?, ?);", 
                       (migration_id, product_version))
        
        conn.commit()
        print("Migration applied successfully!")
        conn.close()
    except Exception as e:
        print(f"Failed to apply migration: {e}")

if __name__ == "__main__":
    apply_migration()
