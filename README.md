# CIVARCH

Webová aplikace pro správu záznamů o výkonu civilní služby (ASP.NET Core 8 · Razor Pages · SQL Server).

## Funkce

- Přihlášení přes LDAP (Active Directory MPSV)
- Vyhledávání občanů podle jména, příjmení, rodného čísla nebo obce **včetně vyhledávání bez diakritiky**
- Stránkování, řazení a export do CSV
- Modální okna pro detail záznamu a přidání nového záznamu
- Hromadný export

## Požadavky

- .NET 8 SDK
- SQL Server (přistup k databázi CIVARCH)
- Přístup do LDAP / Active Directory MPSV

## Konfigurace

Aplikace čte připojení k databázi a LDAP nastavení z `appsettings.json`.  
**Hesla a citlivé údaje NIKDY nedávejte přímo do `appsettings.json`** – použijte jeden z těchto způsobů:

### 1. appsettings.Local.json (doporučeno pro lokální vývoj)

Vytvořte soubor `CIVARCH/appsettings.Local.json` (je v `.gitignore`, takže se nedostane do gitu):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=<SERVER>;Initial Catalog=CIVARCH;User ID=<USER>;Password=<HESLO>;TrustServerCertificate=True"
  }
}
```

### 2. .NET User Secrets

```bash
cd CIVARCH
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=...;Password=..."
```

### 3. Proměnné prostředí (produkce)

```
ConnectionStrings__DefaultConnection=Data Source=...;Password=...
```

## Spuštění

```bash
cd CIVARCH
dotnet run
```

Aplikace se spustí na `https://localhost:44312` (nebo portu dle `launchSettings.json`).

## Struktura projektu

```
CIVARCH/
├── Pages/                  # Razor Pages (UI)
│   ├── Index.cshtml(.cs)   # Hlavní stránka – seznam, vyhledávání, modály
│   ├── Login.cshtml(.cs)   # LDAP přihlášení
│   ├── Zaznam.cshtml(.cs)  # Detail + export záznamu
│   ├── NovyZaznam.cshtml   # Formulář nového záznamu (záloha)
│   └── Shared/             # Layout, _LayoutSingleZaznam
├── DatabaseHandler.cs      # Datová vrstva – SQL Server dotazy
├── DataHandler.cs          # Pomocné metody (RC → datum, export CSV)
├── AppConfig.cs            # Konfigurační konstanty aplikace
├── Obcan.cs                # Model občana
├── Rizeni.cs               # Model řízení
├── Zaznam.cs               # Wrapper (Obcan + Rizeni + metadata)
├── wwwroot/css/site.css    # Vlastní CSS
└── appsettings.json        # Konfigurace (bez hesel!)
```

## Bezpečnostní poznámky

- Hesla do DB a LDAP **nesmí** být commitována do repozitáře
- Connection string je načítán z `appsettings.Local.json` nebo User Secrets
- Všechny SQL dotazy používají parametrizované příkazy (ochrana proti SQL injection)
- Autentizace přes cookie s `HttpOnly` a `Secure` atributy
- Antiforgery token je vyžadován na všech POST formulářích

## Licence

Interní software MPSV – není určen pro veřejné šíření.
