"""
Ano anterior do cenário de demonstração (12/2024 a 11/2025) para as quatro padarias do
seed_demo.py, para a comparação anual (mesmo mês do ano anterior) no Dashboard.

A Padaria Pão Dourado consome mais no ano anterior (sem painéis solares, iluminação antiga)
e recicla menos (antes da compostagem), de modo que a comparação anual mostre a evolução.
Nenhuma variação mês a mês passa de 20%, para não gerar alertas novos, inclusive na
virada 11/2025 -> 12/2025, que agora passa a ter mês anterior.

Uso (com a API rodando e o seed_demo.py já executado):
    python scripts/seed_prior_year.py --admin-email admin@... --admin-password ...

Pode ser executado de novo: meses que já têm registro de energia são ignorados.
"""
import argparse
import sys

from seed_demo import COMPANY, MATERIALS, PEERS, Api, waste_entries

MONTHS = [(12, 2024)] + [(m, 2025) for m in range(1, 12)]

# Padaria Pão Dourado, 12/2024 a 11/2025 (só rede, sem solar).
ENERGY = [2690, 2710, 2650, 2520, 2350, 2190, 2120, 2080, 2110, 2170, 2280, 2470]
WATER = [42.8, 49.0, 43.5, 41.9, 40.2, 38.4, 37.0, 36.5, 36.8, 37.2, 37.9, 39.0]
WASTE_TOTAL = [360, 372, 350, 345, 338, 340, 330, 326, 332, 335, 340, 342]
WASTE_RATE = [0.25, 0.26, 0.28, 0.30, 0.30, 0.32, 0.33, 0.35, 0.36, 0.37, 0.38, 0.39]
MATERIAL_FACTOR = [1.09, 1.11, 1.04, 1.02, 0.99, 0.99, 0.97, 0.97, 0.98, 1.00, 1.01, 1.05]

# Padarias do benchmark: mesma sazonalidade do seed_demo.py, 3% acima no ano anterior.
PEER_ENERGY_FACTOR = [1.10, 1.11, 1.08, 1.04, 1.00, 0.96, 0.93, 0.92, 0.93, 0.95, 0.97, 1.00]
PEER_WATER_FACTOR = [1.08, 1.15, 1.10, 1.05, 1.00, 0.95, 0.93, 0.92, 0.93, 0.95, 0.97, 1.00]
PEER_WASTE_FACTOR = [1.06, 1.08, 1.04, 1.02, 1.00, 0.98, 0.95, 0.94, 0.96, 0.97, 0.98, 1.00]
PEER_PRIOR_YEAR = 1.03


def company_id(api, legal):
    _, companies = api.call("GET", "/companies")
    found = next((c for c in companies if c["legalName"] == legal), None)
    if not found:
        raise SystemExit(f"Empresa '{legal}' não encontrada; rode o seed_demo.py antes.")
    return found["id"]


def done_months(api, base):
    _, rows = api.call("GET", f"{base}/energy")
    return {(r["month"], r["year"]) for r in rows}


def post_month(api, base, m, y, grid, water, waste, materials_factor):
    api.call("POST", f"{base}/energy", {"month": m, "year": y, "gridConsumptionKwh": grid, "solarGenerationKwh": 0})
    api.call("POST", f"{base}/water", {"month": m, "year": y, "consumptionM3": water, "rainwaterHarvestedM3": 0})
    for t, q, rec in waste:
        api.call("POST", f"{base}/waste", {"month": m, "year": y, "type": t, "quantityKg": q, "recycled": rec})
    for name, unit, qty, ren in MATERIALS:
        api.call("POST", f"{base}/materials", {"month": m, "year": y, "name": name, "unit": unit,
                                               "quantity": round(qty * materials_factor, 1), "recycledOrRenewable": ren})


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--api", default="http://localhost:5101/api")
    p.add_argument("--admin-email", required=True)
    p.add_argument("--admin-password", required=True)
    a = p.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    api = Api(a.api)
    api.login(a.admin_email, a.admin_password)

    base = f"/companies/{company_id(api, COMPANY[0])}"
    done, n = done_months(api, base), 0
    for i, (m, y) in enumerate(MONTHS):
        if (m, y) in done:
            continue
        total = WASTE_TOTAL[i]
        post_month(api, base, m, y, ENERGY[i], WATER[i],
                   waste_entries(total, round(total * WASTE_RATE[i], 1)), MATERIAL_FACTOR[i])
        n += 1
    print(f"{COMPANY[1]}: {n} meses inseridos")

    for legal, trading, energy, water, waste, recycled in PEERS:
        base = f"/companies/{company_id(api, legal)}"
        done, n, rate = done_months(api, base), 0, recycled / waste
        for i, (m, y) in enumerate(MONTHS):
            if (m, y) in done:
                continue
            k = PEER_PRIOR_YEAR
            total = round(waste * PEER_WASTE_FACTOR[i] * k, 1)
            rec = round(total * rate, 1)
            post_month(api, base, m, y, round(energy * PEER_ENERGY_FACTOR[i] * k),
                       round(water * PEER_WATER_FACTOR[i] * k, 1),
                       [("Recyclable", rec, True), ("Organic", round(total - rec, 1), False)],
                       energy / 2328 * PEER_ENERGY_FACTOR[i] * k)
            n += 1
        print(f"{trading}: {n} meses inseridos")


if __name__ == "__main__":
    main()
