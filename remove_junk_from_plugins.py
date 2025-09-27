#!/usr/bin/env python3
"""
Remove junk code from Plugins folder by restoring from git
"""

import os
import subprocess

def restore_plugins_folder():
    """Restore the Plugins folder from git"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    plugins_path = os.path.join(script_dir, "Plugins")

    # Use git to restore the Plugins folder
    try:
        # Change to the repository root
        os.chdir(script_dir)

        # Restore all files in Plugins folder
        result = subprocess.run(
            ["git", "checkout", "HEAD", "--", "Plugins/"],
            capture_output=True,
            text=True
        )

        if result.returncode == 0:
            print(f"[OK] Restored Plugins folder from git")
            return True
        else:
            print(f"[ERROR] Failed to restore: {result.stderr}")
            return False
    except Exception as e:
        print(f"[ERROR] {str(e)}")
        return False

def count_cs_files(directory):
    """Count CS files in directory"""
    count = 0
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                count += 1
    return count

def main():
    """Main function"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    plugins_path = os.path.join(script_dir, "Plugins")

    print("=" * 60)
    print("Removing junk code from Plugins folder")
    print("=" * 60)

    # Count files before
    file_count = count_cs_files(plugins_path)
    print(f"Found {file_count} C# files in Plugins folder")

    # Restore from git
    if restore_plugins_folder():
        print("=" * 60)
        print("[COMPLETE] Plugins folder restored to original state")
    else:
        print("=" * 60)
        print("[FAILED] Could not restore Plugins folder")

    print("=" * 60)

if __name__ == "__main__":
    main()