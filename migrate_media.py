import os
import zipfile
import requests
import sys

def zip_directory(folder_path, zip_path):
    print(f"Zipping {folder_path} into {zip_path}...")
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(folder_path):
            for file in files:
                file_path = os.path.join(root, file)
                # Keep 'uploads/...' as prefix in the ZIP
                arcname = os.path.relpath(file_path, start=os.path.dirname(folder_path))
                zipf.write(file_path, arcname)

def upload_zip(zip_path, url, secret):
    print(f"Uploading {zip_path} to {url}...\nThis might take a few minutes as it transfers your 116MB media! Please wait.")
    with open(zip_path, 'rb') as f:
        files = {'file': (os.path.basename(zip_path), f, 'application/zip')}
        headers = {'X-Migration-Secret': secret}
        try:
            response = requests.post(url, files=files, headers=headers)
            print(f"Server responded with status: {response.status_code}")
            print(response.text)
            if response.status_code == 200:
                print("====================================")
                print("SUCCESS! Media successfully migrated to production.")
                print("Please refresh the website to see your images!")
                print("====================================")
            else:
                print("Migration failed. Did you restart the Railway deployment first?")
        except Exception as e:
            print(f"Error during upload: {e}")

if __name__ == "__main__":
    folder_to_zip = "uploads"
    zip_filename = "uploads_migration.zip"
    target_url = "https://fatalecore-production.up.railway.app/api/migrate-media"
    secret = "fatale-rescue-2026"

    if not os.path.exists(folder_to_zip):
        print(f"Error: Directory '{folder_to_zip}' doesn't exist.")
        sys.exit(1)

    zip_directory(folder_to_zip, zip_filename)
    upload_zip(zip_filename, target_url, secret)
    
    print("\nCleaning up local zip file...")
    if os.path.exists(zip_filename):
        os.remove(zip_filename)
