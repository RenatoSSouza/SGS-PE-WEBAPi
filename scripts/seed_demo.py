"""
Cenário de demonstração do SGS-PE (seção 4 do TCC): padaria fictícia, microempresa do
setor de panificação (CNAE seção C, Indústrias de transformação), com 12 meses de
registros (12/2025 a 11/2026) e três padarias fictícias do mesmo setor e porte para o
benchmark. Tudo é cadastrado pela API, com as mesmas validações do sistema.

Uso (com a API rodando):
    python scripts/seed_demo.py --admin-email admin@... --admin-password ... \
        --manager-password ... --employee-password ...

Pode ser executado de novo: empresas e usuários existentes são reaproveitados e uma
empresa que já tem registros de energia não recebe dados duplicados.
"""
import argparse
import json
import sys
import urllib.error
import urllib.request

COMPANY = ("Padaria Pão Dourado Ltda", "Padaria Pão Dourado")
SECTOR, SIZE = "Industrias_de_transformacao", "Micro"
MANAGER = ("Marina Duarte", "gestor@sgspe.com.br")
EMPLOYEE = ("Caio Ferraz", "colaborador@sgspe.com.br")

# 12 meses: (mês, ano)
MONTHS = [(12, 2025)] + [(m, 2026) for m in range(1, 12)]

# Energia (rede kWh, solar kWh). Painéis solares instalados em 06/2026.
# 11/2026: líquido 2.328 kWh, +32,4% sobre 10/2026 (consumo acima do padrão).
ENERGY = [(2575, 0), (2580, 0), (2530, 0), (2401, 0), (2237, 0), (2076, 0),
          (1954, 310), (1894, 310), (1904, 310), (1970, 310), (2068, 310), (2638, 310)]

# Água (m³). 01/2026: +25,9% (verão). 11/2026: 36,3 m³, +5,2%.
WATER = [40.2, 50.6, 41.0, 39.5, 37.8, 35.9, 34.2, 33.6, 33.9, 34.1, 34.5, 36.3]

# Resíduos: total kg e taxa de reciclagem (orgânico vai para compostagem).
# 11/2026: 150 kg orgânico + 101 kg recicláveis reciclados, 61 kg de rejeito (80,4%).
WASTE_TOTAL = [345, 352, 330, 326, 318, 322, 305, 300, 308, 310, 298, None]
WASTE_RATE = [0.40, 0.42, 0.45, 0.50, 0.55, 0.60, 0.65, 0.70, 0.74, 0.77, 0.79, None]

# Matérias-primas: nome, unidade, quantidade base, renovável/reciclada
MATERIALS = [("Farinha de trigo", "kg", 1500, False), ("Açúcar", "kg", 300, False),
             ("Óleo vegetal", "L", 60, False), ("Embalagens de papel kraft", "kg", 40, True)]
MATERIAL_FACTOR = [1.08, 1.10, 1.02, 1.00, 0.97, 0.98, 0.95, 0.96, 0.97, 0.99, 1.00, 1.04]

EQUIPMENTS = [  # nome, categoria, potência W, quantidade, horas/dia (refrigeração: ciclo do compressor)
    # Estimativa total ≈ 1.990 kWh/mês; a iluminação fluorescente passa de 15% e gera a sugestão de LED.
    ("Forno elétrico turbo", "Other", 9000, 1, 3),
    ("Freezer horizontal", "Refrigeration", 400, 2, 12),
    ("Geladeira expositora", "Refrigeration", 350, 1, 12),
    ("Amassadeira", "Other", 1500, 1, 2),
    ("Batedeira planetária", "Other", 750, 1, 1.5),
    ("Lâmpada fluorescente tubular", "Lighting", 40, 20, 14),
    ("Ar-condicionado split", "AirConditioning", 1400, 1, 6),
    ("Computador do caixa (PDV)", "Computer", 150, 1, 12),
]

# Metas: título, indicador, alvo, prazo, responsável (True = Caio Ferraz)
# Situação em 11/2026: energia e carbono fora da meta, resíduos em atenção,
# água e reciclagem no caminho.
GOALS = [
    ("Reduzir o consumo de energia para 2.000 kWh/mês", "EnergyKwh", 2000, "2026-12-31", True),
    ("Manter o consumo de água abaixo de 45 m³", "WaterM3", 45, "2026-12-31", True),
    ("Emitir no máximo 150 kg CO₂e por mês", "CarbonKgCo2e", 150, "2027-03-31", False),
    ("Reciclar 75% dos resíduos", "RecyclingRatePercent", 75, "2027-06-30", False),
    ("Gerar no máximo 330 kg de resíduos por mês", "WasteKg", 330, "2026-12-31", True),
]

# Padarias para o benchmark (11/2026). Médias resultantes: energia 2.342,3 kWh,
# água 45,9 m³, resíduos 338,7 kg, reciclagem 30,5%, carbono 248,6 kg CO₂e.
PEERS = [  # razão social, fantasia, energia kWh, água m³, resíduos kg, reciclados kg
    ("Panificadora Trigo Bom Ltda", "Trigo Bom", 2210, 41.2, 320, 91.2),
    ("Padaria Sabor do Forno Ltda", "Sabor do Forno", 2405, 47.9, 345, 103.5),
    ("Panificadora Bela Vista Ltda", "Bela Vista", 2412, 48.6, 351, 115.5),
]


class Api:
    def __init__(self, base):
        self.base, self.token = base.rstrip("/"), None

    def call(self, method, path, body=None, ok=(200, 201, 204)):
        req = urllib.request.Request(
            self.base + path, method=method,
            data=json.dumps(body).encode() if body is not None else None,
            headers={"Content-Type": "application/json",
                     **({"Authorization": "Bearer " + self.token} if self.token else {})})
        try:
            with urllib.request.urlopen(req) as r:
                raw = r.read()
                return r.status, json.loads(raw) if raw else None
        except urllib.error.HTTPError as e:
            status, text = e.code, e.read().decode("utf-8", "replace")
            if status in ok:
                return status, None
            raise SystemExit(f"{method} {path} -> {status}: {text}")

    def login(self, email, password):
        _, r = self.call("POST", "/users/login", {"email": email, "password": password})
        self.token = r["token"]
        return r["user"]


def ensure_company(api, legal, trading):
    _, companies = api.call("GET", "/companies")
    found = next((c for c in companies if c["legalName"] == legal), None)
    body = {"legalName": legal, "tradingName": trading, "sector": SECTOR, "size": SIZE}
    if found:
        api.call("PUT", f"/companies/{found['id']}", body)
        return found["id"], False
    _, c = api.call("POST", "/companies", body)
    return c["id"], True


def ensure_user(api, name, email, password, role, company_id):
    status, u = api.call("POST", "/users", {"name": name, "email": email, "password": password,
                                            "role": role, "companyId": company_id}, ok=(201, 409))
    if status == 409:
        _, users = api.call("GET", "/users")
        u = next(x for x in users if x["email"] == email)
        api.call("PUT", f"/users/{email}", {"name": name})
        print(f"  usuário {email} já existia (senha mantida)")
    return u["id"]


def has_data(api, cid):
    _, rows = api.call("GET", f"/companies/{cid}/energy")
    return bool(rows)


def waste_entries(total, recycled):
    """Divide os resíduos recicláveis entre orgânico (compostagem) e recicláveis; o resto é rejeito."""
    organic = round(recycled * 0.6, 1)
    return [("Organic", organic, True), ("Recyclable", round(recycled - organic, 1), True),
            ("Reject", round(total - recycled, 1), False)]


def seed_main(api, cid, caio_id):
    base = f"/companies/{cid}"
    # Equipamentos primeiro: cada registro de energia guarda a estimativa no momento do lançamento.
    for name, cat, w, q, h in EQUIPMENTS:
        api.call("POST", f"{base}/equipments", {"name": name, "category": cat, "powerWatts": w, "quantity": q, "averageDailyHours": h})
    for i, (m, y) in enumerate(MONTHS):
        grid, solar = ENERGY[i]
        api.call("POST", f"{base}/energy", {"month": m, "year": y, "gridConsumptionKwh": grid, "solarGenerationKwh": solar})
        api.call("POST", f"{base}/water", {"month": m, "year": y, "consumptionM3": WATER[i], "rainwaterHarvestedM3": 0})
        if WASTE_TOTAL[i] is None:
            entries = [("Organic", 150, True), ("Recyclable", 101, True), ("Reject", 61, False)]
        else:
            entries = waste_entries(WASTE_TOTAL[i], round(WASTE_TOTAL[i] * WASTE_RATE[i], 1))
        for t, q, rec in entries:
            api.call("POST", f"{base}/waste", {"month": m, "year": y, "type": t, "quantityKg": q, "recycled": rec})
        for name, unit, qty, ren in MATERIALS:
            api.call("POST", f"{base}/materials", {"month": m, "year": y, "name": name, "unit": unit,
                                                   "quantity": round(qty * MATERIAL_FACTOR[i], 1), "recycledOrRenewable": ren})
    for title, metric, target, deadline, caio in GOALS:
        api.call("POST", f"{base}/goals", {"title": title, "metric": metric, "targetValue": target,
                                           "deadline": deadline, "responsibleUserId": caio_id if caio else None})


# Sazonalidade das padarias do benchmark em relação a 11/2026 (mais consumo no verão,
# sem saltos acima de 20%, para não gerar alertas nelas). 11/2026 = 1,00: valores de PEERS.
PEER_ENERGY_FACTOR = [1.10, 1.11, 1.08, 1.04, 1.00, 0.96, 0.93, 0.92, 0.93, 0.95, 0.97, 1.00]
PEER_WATER_FACTOR = [1.08, 1.15, 1.10, 1.05, 1.00, 0.95, 0.93, 0.92, 0.93, 0.95, 0.97, 1.00]
PEER_WASTE_FACTOR = [1.06, 1.08, 1.04, 1.02, 1.00, 0.98, 0.95, 0.94, 0.96, 0.97, 0.98, 1.00]
PEER_EQUIPMENT_SCALE = [0.95, 1.05, 1.05]  # porte parecido com a padaria principal


def seed_peer(api, idx, cid, energy, water, waste, recycled):
    """Completa os 12 meses de uma padaria do benchmark; meses já lançados não são repetidos."""
    base = f"/companies/{cid}"
    _, equipments = api.call("GET", f"{base}/equipments")
    if not equipments:
        scale = PEER_EQUIPMENT_SCALE[idx]
        for name, cat, w, q, h in EQUIPMENTS:
            api.call("POST", f"{base}/equipments", {"name": name, "category": cat, "powerWatts": w,
                                                    "quantity": q, "averageDailyHours": round(h * scale, 1)})
    _, rows = api.call("GET", f"{base}/energy")
    done = {(r["month"], r["year"]) for r in rows}
    _, mats = api.call("GET", f"{base}/materials")
    mats_done = {(r["month"], r["year"]) for r in mats}
    rate = recycled / waste
    for i, (m, y) in enumerate(MONTHS):
        if (m, y) not in done:
            api.call("POST", f"{base}/energy", {"month": m, "year": y, "solarGenerationKwh": 0,
                                                "gridConsumptionKwh": round(energy * PEER_ENERGY_FACTOR[i])})
            api.call("POST", f"{base}/water", {"month": m, "year": y, "rainwaterHarvestedM3": 0,
                                               "consumptionM3": round(water * PEER_WATER_FACTOR[i], 1)})
            total = round(waste * PEER_WASTE_FACTOR[i], 1)
            rec = recycled if (m, y) == (11, 2026) else round(total * rate, 1)
            for t, q, r in [("Recyclable", rec, True), ("Organic", round(total - rec, 1), False)]:
                api.call("POST", f"{base}/waste", {"month": m, "year": y, "type": t, "quantityKg": q, "recycled": r})
        if (m, y) not in mats_done:
            for name, unit, qty, ren in MATERIALS:
                api.call("POST", f"{base}/materials", {"month": m, "year": y, "name": name, "unit": unit,
                                                       "quantity": round(qty * energy / 2328 * PEER_ENERGY_FACTOR[i], 1),
                                                       "recycledOrRenewable": ren})


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--api", default="http://localhost:5101/api")
    p.add_argument("--admin-email", required=True)
    p.add_argument("--admin-password", required=True)
    p.add_argument("--manager-password", required=True)
    p.add_argument("--employee-password", required=True)
    a = p.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    api = Api(a.api)
    api.login(a.admin_email, a.admin_password)

    cid, _ = ensure_company(api, *COMPANY)
    print(f"Empresa: {COMPANY[1]} ({cid})")
    ensure_user(api, *MANAGER, a.manager_password, "Manager", cid)
    caio_id = ensure_user(api, *EMPLOYEE, a.employee_password, "Employee", cid)
    if has_data(api, cid):
        print("  já tem registros; dados não foram inseridos de novo")
    else:
        seed_main(api, cid, caio_id)
        print("  12 meses de registros, equipamentos e metas cadastrados")

    for idx, (legal, trading, *values) in enumerate(PEERS):
        pid, _ = ensure_company(api, legal, trading)
        seed_peer(api, idx, pid, *values)
        print(f"Empresa do benchmark: {trading} (12 meses, equipamentos e matérias-primas)")

    api.login(MANAGER[1], a.manager_password)
    _, n = api.call("POST", f"/companies/{cid}/notifications/refresh")
    print(f"Notificações geradas: {n}")


if __name__ == "__main__":
    main()
