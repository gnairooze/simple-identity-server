import os
import sys

# Environment variables to set
env_vars = {
    "SIMPLE_IDENTITY_SERVER_DB_PASSWORD": "sample@Strong23Password",
    "SIMPLE_IDENTITY_SERVER_CERT_PASSWORD": "SharedCertPassword123!",
    "ASPNETCORE_ENVIRONMENT": "Production",
    "ASPNETCORE_URLS": "https://+:443",
    "Kestrel__Certificates__Default__Path": "c:\dev\ssl\identity-dev-test.crt",
    "Kestrel__Certificates__Default__KeyPath": "c:\dev\ssl\identity-dev-test.key",
    "SIMPLE_IDENTITY_SERVER_DEFAULT_CONNECTION_STRING": "Server=.,14333;Database=SimpleIdentityServer;MultipleActiveResultSets=true;uid=sa;pwd=sample@Strong23Password;TrustServerCertificate=true;Encrypt=true",
    "SIMPLE_IDENTITY_SERVER_SECURITY_LOGS_CONNECTION_STRING": "Server=.,14333;Database=SimpleIdentityServerSecurityLogs;MultipleActiveResultSets=true;uid=sa;pwd=sample@Strong23Password;TrustServerCertificate=true;Encrypt=true"
}

def set_env_vars():
    # Set for current process
    for key, value in env_vars.items():
        os.environ[key] = value
        print(f"Set {key} for current process.")

    # Set persistently, if desired
    if sys.platform == "win32":
        import subprocess
        for key, value in env_vars.items():
            subprocess.run(f'setx {key} "{value}" /M', shell=True)
            print(f"Set {key} system-wide (Windows).")
    elif sys.platform.startswith("linux"):
        with open("/etc/environment", "a") as env_file:
            for key, value in env_vars.items():
                env_file.write(f'{key}="{value}"\n')
                print(f"Appended {key} to /etc/environment (Linux).")

if __name__ == "__main__":
    set_env_vars()
