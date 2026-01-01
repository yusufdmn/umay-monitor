import os

# --- CONFIGURATION ---
# Make sure this matches the filename you saved the bash script as
TEMPLATE_FILE = "agent-installer.sh" 
OUTPUT_FILE = "install-test.sh"

# Mock Data (Simulating what the backend would provide)
TEST_VARIABLES = {
    "{{AGENT_ID}}": "Ankara-01",
    "{{TOKEN}}": "MY_SECURE_TOKEN",
    "{{DOMAIN}}": "localhost",  # Change this to your IP or Domain
}

def generate_script():
    if not os.path.exists(TEMPLATE_FILE):
        print(f"❌ Error: Could not find template file '{TEMPLATE_FILE}'")
        return

    # 1. Read the template
    with open(TEMPLATE_FILE, "r") as f:
        script_content = f.read()

    # 2. Inject Variables
    for placeholder, value in TEST_VARIABLES.items():
        script_content = script_content.replace(placeholder, value)

    # 3. Write the final installer
    with open(OUTPUT_FILE, "w") as f:
        f.write(script_content)

    print(f"✅ Generated '{OUTPUT_FILE}' with test variables.")
    print(f"👉 Run it locally: sudo bash {OUTPUT_FILE}")

if __name__ == "__main__":
    generate_script()