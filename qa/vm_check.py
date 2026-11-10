import subprocess

def sh(cmd):
    r = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    return (r.stdout + r.stderr).strip() or "(vazio)"

for user, uid in (("detroytcorp", 1001), ("detro", 1002)):
    print(f"== timers de usuario: {user} ==")
    print(sh(f"sudo -u {user} env XDG_RUNTIME_DIR=/run/user/{uid} systemctl --user list-timers --all --no-pager 2>&1 | cut -c1-170"))
    print(f"-- units de usuario em /home/{user}/.config/systemd/user")
    print(sh(f"sudo ls -la /home/{user}/.config/systemd/user 2>&1"))
    print()

print("== arquivos/pastas com nome de agente, F1 ou formula (ate 4 niveis) ==")
print(sh("sudo find /home /opt /var/www /srv /usr/local -maxdepth 4 \\( -iname '*formula*' -o -iname '*agente*' -o -iname '*f1*' -o -iname '*paddock*' \\) -not -path '*/node_modules/*' -not -path '*/.npm/*' 2>/dev/null | head -40"))
