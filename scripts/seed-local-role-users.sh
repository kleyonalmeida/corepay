#!/usr/bin/env bash
# Cria usuários locais por papel (Admin, Director, Financial, Manager, User) via API.
# Não faz parte do startup da aplicação — execute manualmente após a API estar no ar.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ -f .env ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

API_URL="${API_URL:-${API_BASE_URL:-http://localhost:5000}}"
API_URL="${API_URL%/}"

SUPERADMIN_EMAIL="${Seed__SuperAdmin__Email:-}"
SUPERADMIN_PASSWORD="${Seed__SuperAdmin__Password:-}"
ROLE_USERS_PASSWORD="${DEV_ROLE_USERS_PASSWORD:-}"

if [[ -z "$SUPERADMIN_EMAIL" || -z "$SUPERADMIN_PASSWORD" ]]; then
  echo "Erro: defina Seed__SuperAdmin__Email e Seed__SuperAdmin__Password no .env (ou exporte no shell)." >&2
  exit 1
fi

if [[ -z "$ROLE_USERS_PASSWORD" ]]; then
  echo "Erro: defina DEV_ROLE_USERS_PASSWORD no .env (senha local compartilhada dos usuários de teste por papel)." >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "Erro: python3 é necessário." >&2
  exit 1
fi

if ! curl -sf "${API_URL}/api/v1/health" >/dev/null; then
  echo "Erro: API indisponível em ${API_URL}. Suba a API antes de executar este script." >&2
  exit 1
fi

json_get() {
  local json="$1"
  local key="$2"
  JSON_INPUT="$json" JSON_KEY="$key" python3 -c 'import json, os; print(json.loads(os.environ["JSON_INPUT"]).get(os.environ["JSON_KEY"], "") or "")'
}

json_array_id_by_name() {
  local json="$1"
  local name="$2"
  JSON_INPUT="$json" JSON_NAME="$name" python3 -c '
import json, os
items = json.loads(os.environ["JSON_INPUT"])
for item in items:
    if item.get("name") == os.environ["JSON_NAME"]:
        print(item.get("id", ""))
        break
'
}

login_payload="$(SUPERADMIN_EMAIL="$SUPERADMIN_EMAIL" SUPERADMIN_PASSWORD="$SUPERADMIN_PASSWORD" python3 -c '
import json, os
print(json.dumps({"email": os.environ["SUPERADMIN_EMAIL"], "password": os.environ["SUPERADMIN_PASSWORD"]}))
')"

login_response="$(curl -sf -X POST "${API_URL}/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "$login_payload")"

token="$(json_get "$login_response" "accessToken")"
if [[ -z "$token" ]]; then
  echo "Erro: login SuperAdmin falhou. Verifique credenciais no .env e se a API foi iniciada com as mesmas variáveis." >&2
  exit 1
fi

auth_header="Authorization: Bearer ${token}"

ensure_department_for_manager() {
  local departments
  departments="$(curl -sf "${API_URL}/api/v1/departments" -H "$auth_header")"
  local existing_id
  existing_id="$(json_array_id_by_name "$departments" "Analistas Comerciais Demo")"
  if [[ -n "$existing_id" ]]; then
    echo "$existing_id"
    return
  fi

  local create_payload='{
    "name": "Analistas Comerciais Demo",
    "calculationType": "commercialAnalyst",
    "goalBonusPercentage": 0,
    "lowRevenueThreshold": 200000,
    "lowRevenueBonusPct": 0.4,
    "description": "Setor fictício para testes locais de Manager",
    "isActive": true,
    "isAllocatedFixed": false,
    "routesFixedToLimaKarttos": false
  }'

  local create_response http_code body
  create_response="$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/api/v1/departments" \
    -H "$auth_header" \
    -H "Content-Type: application/json" \
    -d "$create_payload")"
  http_code="$(echo "$create_response" | tail -n1)"
  body="$(echo "$create_response" | sed '$d')"

  if [[ "$http_code" == "201" ]]; then
    json_get "$body" "id"
    return
  fi

  if [[ "$http_code" == "409" ]]; then
    departments="$(curl -sf "${API_URL}/api/v1/departments" -H "$auth_header")"
    json_array_id_by_name "$departments" "Analistas Comerciais Demo"
    return
  fi

  echo "Erro: não foi possível criar setor demo (HTTP ${http_code})." >&2
  echo "$body" >&2
  exit 1
}

create_role_user() {
  local email="$1"
  local display_name="$2"
  local role="$3"
  local department_ids_json="$4"

  local payload
  payload="$(EMAIL="$email" PASSWORD="$ROLE_USERS_PASSWORD" DISPLAY_NAME="$display_name" ROLE="$role" DEPS="$department_ids_json" python3 -c '
import json, os
print(json.dumps({
    "email": os.environ["EMAIL"],
    "password": os.environ["PASSWORD"],
    "displayName": os.environ["DISPLAY_NAME"],
    "roleNames": [os.environ["ROLE"]],
    "departmentIds": json.loads(os.environ["DEPS"]),
}))
')"

  local response http_code body
  response="$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/api/v1/users" \
    -H "$auth_header" \
    -H "Content-Type: application/json" \
    -d "$payload")"
  http_code="$(echo "$response" | tail -n1)"
  body="$(echo "$response" | sed '$d')"

  case "$http_code" in
    201)
      echo "  + criado: ${email} (${role})"
      ;;
    409)
      echo "  = já existe: ${email} (${role})"
      ;;
    *)
      echo "  ! falhou: ${email} (${role}) — HTTP ${http_code}" >&2
      echo "$body" >&2
      exit 1
      ;;
  esac
}

manager_department_id="$(ensure_department_for_manager)"

echo "Criando usuários locais por papel (domínio .test, senha = DEV_ROLE_USERS_PASSWORD)..."
create_role_user "admin@corepay.test" "Admin Demo" "Admin" "[]"
create_role_user "director@corepay.test" "Director Demo" "Director" "[]"
create_role_user "financial@corepay.test" "Financial Demo" "Financial" "[]"
create_role_user "manager@corepay.test" "Manager Demo" "Manager" "[\"${manager_department_id}\"]"
create_role_user "user@corepay.test" "User Demo" "User" "[]"

cat <<EOF

Pronto. Use no login da UI (senha = valor de DEV_ROLE_USERS_PASSWORD no .env):

| Papel     | E-mail                  |
|-----------|-------------------------|
| Admin     | admin@corepay.test      |
| Director  | director@corepay.test   |
| Financial | financial@corepay.test  |
| Manager   | manager@corepay.test    |  (escopo: Analistas Comerciais Demo)
| User      | user@corepay.test       |

SuperAdmin continua sendo: ${SUPERADMIN_EMAIL}
EOF
