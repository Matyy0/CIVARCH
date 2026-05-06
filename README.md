# CIVARCH

Interní webová aplikace pro evidenci a správu záznamů o civilní službě.

## K čemu aplikace slouží

- vyhledávání občanů a souvisejících řízení
- práce se záznamy (detail, editace, export)
- hromadné exporty dat
- přihlášení uživatelů přes LDAP

## Technologie

- **.NET 8**
- **ASP.NET Core Razor Pages**
- **C#**
- **SQL Server** (datová vrstva přes `DatabaseHandler.cs`)
- **LDAP / Active Directory** (autentizace)
- **Bootstrap + vlastní CSS** (`wwwroot/css/site.css`)

## Hlavní části projektu

- `Pages/` – UI stránky a jejich PageModel logika
- `DatabaseHandler.cs` – SQL dotazy a datový přístup
- `DataHandler.cs` – pomocné funkce (transformace, exporty)
- `AppConfig.cs` – konfigurační konstanty
- modely: `Obcan.cs`, `Rizeni.cs`, `Zaznam.cs`, `Podnik.cs`, `Organizace.cs`
- `wwwroot/` – statické soubory (CSS, obrázky, skripty)

## Spuštění ve VS Code

1. Otevři složku projektu `CIVARCH source/CIVARCH`.
2. V terminálu VS Code přejdi do aplikační složky:
   ```bash
   cd CIVARCH
   ```
3. Obnov balíčky a build:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Spusť aplikaci:
   ```bash
   dotnet run
   ```
5. Otevři URL vypsanou v terminálu (typicky `https://localhost:44312` podle `Properties/launchSettings.json`).

## Požadavky prostředí

- .NET 8 SDK
- dostupný SQL Server s databází CIVARCH
- dostupné LDAP/AD služby pro přihlášení
