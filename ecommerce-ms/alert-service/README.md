# 🚨 Alert Service — ECommerce Microservices

Serviço .NET 9 que recebe webhooks do Grafana Alerting e dispara notificações simultâneas via **WhatsApp**, **Telegram** e **Email**.

## Fluxo

```
Prometheus (coleta métricas a cada 10s)
    └─► Grafana Alerting (avalia regras a cada 30s)
            └─► Webhook → alert-service:8080/api/alerts/webhook
                    ├─► Telegram Bot
                    ├─► WhatsApp (Evolution API ou Z-API)
                    └─► Email (SMTP/Gmail)
```

## Setup em 5 passos

### 1. Configurar o Telegram Bot

1. Fale com [@BotFather](https://t.me/BotFather) → `/newbot` → copie o token
2. Adicione o bot ao grupo ou chat desejado
3. Mande qualquer mensagem pro bot
4. Acesse `https://api.telegram.org/bot<TOKEN>/getUpdates`
5. Copie o `id` dentro de `"chat"` — esse é o `ChatId`

### 2. Configurar o WhatsApp (Evolution API)

```bash
# Após subir o docker-compose, acesse:
http://localhost:8081

# 1. Vá em "Instances" → "Create Instance" → nome: cardipio
# 2. Clique em "QR Code" e escaneie com o WhatsApp
# 3. Pronto — a instância fica conectada
```

> **Z-API**: se preferir Z-API (cloud, sem container), troque `Provider: evolution` por `zapi` no `.env` e preencha `ZApiInstanceId`, `ZApiToken` e `ZApiClientToken`.

### 3. Configurar o Email (Gmail)

1. Acesse sua conta Google → **Segurança** → **Verificação em 2 etapas** (ative)
2. Ainda em Segurança → **Senhas de app** → crie uma senha para "Mail"
3. Use essa senha de 16 caracteres em `EMAIL_PASSWORD` (não a senha da conta)

### 4. Subir os serviços

```bash
# Copie o .env.example
cp .env.example .env

# Edite com os valores reais
nano .env

# Adicione o snippet ao docker-compose.yml principal
# (veja docker-compose.snippet.yml)

# Monte o arquivo de alertas no grafana (no docker-compose.yml):
# volumes:
#   - "./alert-service/grafana-alerts.yaml:/etc/grafana/provisioning/alerting/alerts.yaml"

# Suba tudo
docker compose up -d
```

### 5. Testar os canais

```bash
# Teste rápido — dispara alerta fake para todos os canais
curl -X POST http://localhost:5005/api/alerts/test?severity=critical

# Health check
curl http://localhost:5005/api/alerts/health
```

## Alertas configurados

| Alerta | Condição | Severidade | For |
|--------|----------|------------|-----|
| Serviço DOWN | `up == 0` | 🔴 Critical | 30s |
| Memória alta | Memória > 80% | 🟡 Warning | 2m |
| Error rate alto | Taxa 4xx/5xx > 5% | 🟡 Warning | 1m |
| Fila acumulando | RabbitMQ ready > 500 | 🟡 Warning | 2m |
| Outbox com falha | `outbox_failed > 0` | 🔴 Critical | 1m |

## Estrutura do projeto

```
alert-service/
├── src/
│   ├── AlertService.API/          # Controllers, Program.cs, appsettings.json
│   ├── AlertService.Core/         # AlertDispatcher, modelos, interfaces
│   └── AlertService.Infrastructure/  # TelegramChannel, WhatsAppChannel, EmailChannel
├── Dockerfile
├── AlertService.sln
├── grafana-alerts.yaml            # Provisioning automático dos alertas no Grafana
├── docker-compose.snippet.yml     # Adicione ao docker-compose principal
└── .env.example                   # Copie para .env e preencha
```

## Variáveis de ambiente

| Variável | Descrição |
|----------|-----------|
| `TELEGRAM_BOT_TOKEN` | Token do bot obtido via @BotFather |
| `TELEGRAM_CHAT_ID` | ID do chat/grupo (pode ser negativo para grupos) |
| `EVOLUTION_API_KEY` | API Key da Evolution API |
| `WHATSAPP_NUMBERS` | Números separados por vírgula (DDI+DDD+Número) |
| `EMAIL_USERNAME` | Email do remetente |
| `EMAIL_PASSWORD` | App Password do Gmail |
| `EMAIL_TO_ADDRESSES` | Destinatários separados por vírgula |
| `WEBHOOK_SECRET` | Token de segurança do webhook (mesmo no grafana-alerts.yaml) |
