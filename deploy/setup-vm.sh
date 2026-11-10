#!/usr/bin/env bash
set -e

sudo mkdir -p /var/www/paddockspace
sudo tar -xzf /tmp/paddockspace-release.tar.gz -C /var/www/paddockspace
sudo chmod +x /var/www/paddockspace/bff-web/PaddockSpace.Bff.Web
sudo chown -R detroytcorp:detroytcorp /var/www/paddockspace

cat << 'EOF' | sudo tee /etc/systemd/system/paddockspace-bff.service > /dev/null
[Unit]
Description=PaddockSpace Web BFF API (.NET 10 + SQLite)
After=network.target

[Service]
WorkingDirectory=/var/www/paddockspace/bff-web
ExecStart=/var/www/paddockspace/bff-web/PaddockSpace.Bff.Web
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=paddockspace-bff
User=detroytcorp
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5180
Environment="ConnectionStrings__PaddockSpace=Data Source=/var/www/paddockspace/bff-web/paddockspace.db"
Environment=DOTNET_GCHeapHardLimit=0x8000000

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable paddockspace-bff.service
sudo systemctl restart paddockspace-bff.service

cat << 'EOF' | sudo tee /etc/nginx/sites-available/paddockspace.com.br > /dev/null
server {
    listen 80;
    server_name paddockspace.com.br www.paddockspace.com.br;

    root /var/www/paddockspace/dist;
    index index.html;

    location /api/ {
        proxy_pass http://127.0.0.1:5180/api/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 60s;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
EOF

sudo ln -sf /etc/nginx/sites-available/paddockspace.com.br /etc/nginx/sites-enabled/paddockspace.com.br
sudo nginx -t
sudo systemctl reload nginx

sleep 3
systemctl status paddockspace-bff.service --no-pager | head -n 15
curl -sS http://127.0.0.1:5180/api/web/v1/context | head -c 300
echo ""
curl -sS "http://127.0.0.1:5180/api/web/v1/f1/standings/teams?season=all" | head -c 300
echo ""

sudo certbot --nginx -d paddockspace.com.br -d www.paddockspace.com.br --non-interactive --agree-tos --register-unsafely-without-email --redirect || echo "Certbot awaiting DNS propagation"

