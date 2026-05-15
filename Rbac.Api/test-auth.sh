#!/usr/bin/env bash
set -e

BASE_URL="http://localhost:5083"
EMAIL="daniel@email.com"
PASSWORD="123456"

echo "1) Fazendo login..."
LOGIN_JSON=$(curl -sS -X POST "$BASE_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo "$LOGIN_JSON" | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')

if [ -z "$TOKEN" ]; then
  echo "Falhou: token vazio. Resposta:"
  echo "$LOGIN_JSON"
  exit 1
fi

echo "Token recebido (primeiros 30 chars): ${TOKEN:0:30}..."

echo "2) Chamando /users/me..."
curl -sS -X GET "$BASE_URL/users/me" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json"
echo
