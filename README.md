# SGS-PE

Sistema de Gestão de Sustentabilidade para Pequenas Empresas: API (back-end).

Permite que pequenas empresas registrem consumo de energia, água, resíduos e matérias-primas, calculem a pegada de carbono, acompanhem metas ambientais, comparem-se com o setor e gerem relatórios (PDF/Excel), alinhados aos ODS da ONU.

## Tecnologias

- .NET 10 / C# (ASP.NET Core Web API, arquitetura em camadas: Controllers → Services → Data)
- Entity Framework Core, com SQLite em desenvolvimento e PostgreSQL em produção
- Autenticação JWT com três papéis: **Admin**, **Manager** (Gestor) e **Employee** (Colaborador)
- QuestPDF (PDF) e ClosedXML (Excel)
- xUnit para testes
- Front-end: Angular 20, em projeto separado (`sgspe-web`)

## Como executar (desenvolvimento)

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

1. Defina as credenciais do primeiro administrador. Ele é criado automaticamente na inicialização, se ainda não houver nenhum Admin. A senha precisa ter ao menos 8 caracteres.

   ```bash
   # Linux/macOS/Git Bash
   export Seed__AdminEmail=admin@exemplo.com
   export Seed__AdminPassword='troque-esta-senha'
   ```
   ```powershell
   # PowerShell
   $env:Seed__AdminEmail = "admin@exemplo.com"
   $env:Seed__AdminPassword = "troque-esta-senha"
   ```

2. Na pasta do projeto, rode a API:

   ```bash
   dotnet run --launch-profile http
   ```

   A API sobe em `http://localhost:5101` e o navegador abre o Swagger (`http://localhost:5101/swagger`). As migrations são aplicadas na inicialização e o banco `SGS-PE.db` é criado na pasta do projeto.

   O perfil `https` também existe (`https://localhost:7123`), mas nele as chamadas para a porta HTTP são redirecionadas para HTTPS, o que bloqueia o front-end configurado com `http://localhost:5101`. Use o perfil `http` junto com o front-end.

3. No Swagger, chame `POST /api/users/login`:

   ```json
   { "email": "admin@exemplo.com", "password": "troque-esta-senha" }
   ```

   Copie o `token` da resposta, clique em **Authorize** e cole só o token (o Swagger acrescenta o prefixo `Bearer`). Fora do Swagger, envie o cabeçalho `Authorization: Bearer <token>`. O token vale por tempo limitado; quando expirar, faça login de novo.

## Primeiros passos

O administrador inicial fica vinculado a uma empresa interna ("SGS-PE Administração"). Para começar a usar o sistema:

1. **Cadastrar a empresa** (Admin): `POST /api/companies`

   ```json
   { "legalName": "Padaria Exemplo Ltda", "tradingName": "Padaria Exemplo", "sector": "Industrias_de_transformacao", "size": "Micro" }
   ```

   `sector` aceita os valores do enum `Sector` (`Models/Enum/SectorEnum.cs`, seções da CNAE) e `size` aceita `Micro`, `Small` ou `Medium` (opcional). Guarde o `id` retornado.

2. **Cadastrar o gestor** (Admin): `POST /api/users`

   ```json
   { "name": "Maria Gestora", "email": "gestor@exemplo.com", "password": "senha123", "role": "Manager", "companyId": "<id da empresa>" }
   ```

   O gestor, por sua vez, cadastra os colaboradores da própria empresa (`"role": "Employee"`).

3. **Usar o sistema** com o login do gestor ou do colaborador: registrar consumo, cadastrar equipamentos e metas, consultar carbono, sugestões, benchmark e relatórios pelas rotas abaixo.

Os corpos de todas as requisições aparecem no Swagger. Os enums são enviados e recebidos como texto (ex.: `"Manager"`, `"Micro"`).

## Módulos

Todas as rotas abaixo começam com `/api/companies/{companyId}/`.

| Módulo | Rotas |
|---|---|
| Energia, água, resíduos, matérias-primas | `energy`, `water`, `waste`, `materials` |
| Equipamentos (estimativa de consumo) | `equipments` |
| Pegada de carbono | `carbon/{ano}/{mês}` |
| Sugestões e consumo acima do padrão (variação > 20% sobre o mês anterior) | `insights/{ano}/{mês}` |
| Série histórica dos gráficos, com os ODS (1 a 36 meses) | `insights/history?months=12` |
| Metas, progresso e alertas | `goals`, `goals/{id}/progress/{ano}/{mês}`, `goals/alerts/{ano}/{mês}` |
| Notificações de metas | `notifications`, `notifications/unread-count`, `notifications/read-all` |
| Benchmark setorial (por setor e porte; exige ao menos 3 outras empresas) | `benchmark/{ano}/{mês}` |
| Relatórios mensais | `reports/{ano}/{mês}/pdf` e `reports/{ano}/{mês}/xlsx` |
| Relatório anual de certificação | `reports/certification/{ano}/pdf` |

Usuários e empresas ficam em `/api/users` e `/api/companies`.

## Perfis de acesso

| Ação | Admin | Manager | Employee |
|---|:-:|:-:|:-:|
| Consultar e registrar consumo da própria empresa | ✓ | ✓ | ✓ |
| Excluir registros de consumo | ✓ | ✓ | |
| Gerenciar metas e equipamentos | ✓ | ✓ | |
| Benchmark e relatórios | ✓ | ✓ | |
| Cadastrar usuários | ✓ | só da própria empresa | |
| Editar dados da empresa | ✓ | ✓ (exceto setor e porte) | |
| Criar e excluir empresas, excluir usuários | ✓ | | |
| Acessar qualquer empresa | ✓ | | |

Gestores veem todas as notificações da empresa; colaboradores veem só as das metas pelas quais são responsáveis. Os dados são sempre isolados por empresa.

## Front-end

O Angular fica em outro projeto (`sgspe-web`), que precisa de Node.js 20.19 ou mais recente. Com a API rodando:

```bash
cd sgspe-web
npm install
npx ng serve          # http://localhost:4200
```

A URL da API fica em `src/environments/environment.ts` (padrão `http://localhost:5101/api`). O CORS de desenvolvimento libera `http://localhost:3000` e `http://localhost:4200`.

## Dados de demonstração

Os scripts em `scripts/` cadastram, pela própria API, o cenário usado no TCC: uma padaria fictícia (microempresa) com gestor, colaborador, equipamentos, metas e registros mensais, além de três padarias do mesmo setor e porte para o benchmark. Precisam apenas de Python 3, sem bibliotecas extras, e da API rodando:

```bash
# 12 meses (12/2025 a 11/2026)
python scripts/seed_demo.py --admin-email admin@exemplo.com --admin-password troque-esta-senha \
    --manager-password <senha do gestor> --employee-password <senha do colaborador>

# opcional: ano anterior (12/2024 a 11/2025), para a comparação anual do dashboard
python scripts/seed_prior_year.py --admin-email admin@exemplo.com --admin-password troque-esta-senha
```

O gestor criado é `gestor@sgspe.com.br` e o colaborador, `colaborador@sgspe.com.br`. Os scripts podem ser executados de novo sem duplicar dados. Para outra URL da API, use `--api`.

## Testes

```bash
dotnet test
```

São 39 testes (`tests/sgspe.Tests`) que usam SQLite em memória e exercitam os serviços reais: carbono, resíduos, sugestões e série histórica, metas, notificações, benchmark, relatório de certificação, modelo e segurança.

## Configuração

| Chave | Descrição |
|---|---|
| `ConnectionStrings:SqliteConnection` | Banco SQLite (desenvolvimento). |
| `Database:Provider` | `Postgres` para usar PostgreSQL; qualquer outro valor usa SQLite. |
| `ConnectionStrings:PostgresConnection` | Conexão PostgreSQL (produção). |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | Assinatura e validação do token (a chave deve ter 32+ caracteres). |
| `Cors:AllowedOrigins` | Origens permitidas do front-end. |
| `Seed:AdminEmail`, `Seed:AdminPassword` | Primeiro administrador. |
| `Seed:AdminName`, `Seed:AdminCompanyName` | Opcionais: nome do administrador e da empresa interna a que ele fica vinculado. |
| `Emissions:EnergyKgCo2PerKwh`, `WaterKgCo2PerM3`, `WasteLandfillKgCo2PerKg` | Fatores de emissão. |
| `Notifications:IntervalMinutes` | Intervalo da verificação automática de metas (padrão 360). |
| `Smtp:Host`, `Port`, `User`, `Password`, `From`, `EnableSsl` | E-mail opcional para as notificações; sem `Host`, elas ficam só no sistema. |

Em desenvolvimento a API lê `appsettings.Development.json`; em produção, `appsettings.Production.json`. Variáveis de ambiente têm prioridade e usam `__` como separador (ex.: `Jwt__Key`).

## Produção

O arquivo `appsettings.Production.json` é um modelo sem segredos. Forneça os valores sensíveis por variáveis de ambiente:

```bash
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=Postgres
ConnectionStrings__PostgresConnection="Host=...;Database=sgspe;Username=...;Password=..."
Jwt__Key="<chave aleatória com 32+ caracteres>"
Cors__AllowedOrigins__0="https://seu-front.exemplo.com"
Seed__AdminEmail=...
Seed__AdminPassword=...
```

O Swagger fica disponível só em desenvolvimento.

Observação: as migrations do projeto foram geradas para SQLite. No PostgreSQL o esquema é criado a partir do modelo (`EnsureCreated`) na primeira execução; alterações futuras de esquema em um banco PostgreSQL já existente precisam ser aplicadas manualmente ou com migrations próprias do PostgreSQL.

## Fatores de emissão

O fator padrão de energia (0,0461 kg CO₂/kWh) é o fator médio anual de 2025 do Sistema Interligado Nacional para inventários corporativos, publicado pelo MCTI (SIRENE). Os fatores de água (0,5 kg CO₂/m³) e de resíduos em aterro (0,5 kg CO₂/kg) são valores de referência sem fonte oficial única e devem ser ajustados em `Emissions` conforme a realidade local; o relatório de certificação também traz esse aviso.

## Licença

Veja o arquivo [LICENSE](LICENSE).
