import sqlite3
import os

db_path = 'fatale_core.db'
migrations_to_apply = [
    {
        'id': '20260418030000_AddMonitorStyling',
        'columns': [
            ("MonitorImageUrl", "TEXT"),
            ("MonitorBackgroundColor", "TEXT"),
            ("MonitorIsGlass", "INTEGER NOT NULL DEFAULT 0")
        ]
    },
    {
        'id': '20260418040000_AddStatusMessage',
        'columns': [
            ("StatusMessage", "TEXT")
        ]
    }
]
product_version = '10.0.6'

def apply_migration():
    if not os.path.exists(db_path):
        print(f"Error: {db_path} not found. Ensure you have run the project at least once.")
        return

    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()

        for migration in migrations_to_apply:
            m_id = migration['id']
            print(f"Checking migration {m_id}...")

            # Add columns
            for col_name, col_type in migration['columns']:
                try:
                    cursor.execute(f"ALTER TABLE Users ADD \"{col_name}\" {col_type};")
                    print(f"  Added column {col_name}")
                except sqlite3.OperationalError as e:
                    if "duplicate column name" in str(e).lower():
                        print(f"  Column {col_name} already exists.")
                    else:
                        raise e

            # Update migration history if not already present
            cursor.execute("SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = ?", (m_id,))
            if cursor.fetchone() is None:
                cursor.execute("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (?, ?);", 
                               (m_id, product_version))
                print(f"  Migration {m_id} recorded.")
            else:
                print(f"  Migration {m_id} already recorded.")
        
        conn.commit()
        print("\nAll migrations checked/applied successfully!")
        conn.close()
    except Exception as e:
        print(f"Failed to apply migrations: {e}")

if __name__ == "__main__":
    apply_migration()
