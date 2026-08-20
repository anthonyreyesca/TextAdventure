# Secure Text Adventure

Een C# text-adventure-:

- **TextAdventure** — de console-game (client)
- **TextAdventureAPI** — beveiligde REST-API voor login, registratie en AES-keyshares voor versleutelde kamers
- **TextAdventureTests** — testproject met MSTest + Moq

---

## Inhoud

1. [Opstarten](#opstarten)
2. [API endpoints](#api-endpoints)
3. [Rollen](#rollen)
4. [Plattegrond](#plattegrond)
5. [Speel-scenario's](#speel-scenarios)
6. [Beveiliging](#beveiliging)

---

## Opstarten

1. Open `TextAdventure.sln` in Visual Studio.
2. **Rechtsklik op de Solution** → **Configure Startup Projects** → **Multiple startup projects**.
3. Zet zowel `TextAdventureAPI` als `TextAdventure` op **Start** (API bovenaan).
4. Start op
5. Registreer een nieuw account bij elke opstart — de API houdt accounts in-memory bij en die verdwijnen bij afsluiten.

---

## API endpoints

| Endpoint | Methode | Beschrijving |

| `/api/auth/register` | POST | Registreer een nieuwe gebruiker |
| `/api/auth/login` | POST | Login en ontvang een JWT-token |
| `/api/auth/me` | GET | Bekijk je eigen gebruikersinfo *(JWT vereist)* |
| `/api/keys/keyshare/{roomId}` | GET | Haal een keyshare op *(JWT vereist)* |

---

## Rollen

| Rol | Wat kan deze rol? |

| **Player** | Versleutelde kamers ontgrendelen via `unlock` |
| **Admin** | `noclip` gebruiken (door deuren lopen zonder sleutel) |

> De rollen zijn **complementair, niet cumulatief**. Wil je alles uitproberen? Maak twee accounts aan.

---

## Plattegrond

```
                         ┌──────────────┐
                         │  De Uitgang  │  (WIN — vereist Sleutel)
                         └──────n───────┘
                                │
   ┌──────────────┐       ┌─────┴────┐       ┌─────────────┐       ┌────────────────────┐
   │  Dodelijke   │◄──w───│   Start  │───e──►│  Schatkamer │───n──►│ Geheime Schatkamer │
   │ Gang (DOOD)  │       │          │       │  (Sleutel)  │       │   (versleuteld)    │
   └──────────────┘       └─────s────┘       └─────────────┘       └────────────────────┘
                                │
                          ┌─────┴────┐──e──►┌──────────────────┐
                          │  Kelder  │      │  Geheime Kelder  │
                          │ (Zwaard) │      │   (versleuteld)  │
                          └─────s────┘      └──────────────────┘
                                │
                          ┌─────┴────────┐
                          │ Monsterkamer │  (monster — vereist Zwaard)
                          └──────────────┘
```

---

## Speel-scenario's

### Snelste manier om te WINNEN

```
go e            → Schatkamer
take sleutel    → Sleutel opgepakt
go w            → terug naar Start
go n            → De Uitgang → GEWONNEN
```

### Snelste manier om te VERLIEZEN

```
go w            → Dodelijke Gang → GAME OVER
```

### Alternatieve manieren om te verliezen

| Scenario | Commando's | Wat gebeurt er |
|---|---|---|
| Monster zonder zwaard | `go s` → `go s` → `fight` | Geen wapen, je sterft |
| Wegrennen voor monster | `go s` → `go s` → `go n` | Het monster grijpt je |

### Volledig "alles meepakken"-traject *(Player)*

Ontgrendelt **beide** geheime kamers, verslaat het monster én wint via de uitgang.

**Wachtwoorden:**
- Geheime Kelder → `kerkergeheim`
- Geheime Schatkamer → `schatkamercode`

**Fase 1 — Pak het zwaard en versla het monster**
```
go s            → Kelder
take zwaard     → Zwaard in inventory
go s            → Monsterkamer
fight           → Monster verslagen
```

**Fase 2 — Ontgrendel de Geheime Kelder**
```
go n            → terug naar Kelder
go e            → Geheime Kelder [VERGRENDELD]
unlock          → typ wachtwoord: kerkergeheim
                  → toont kaart naar de eindbaas
go w            → terug naar Kelder
```

**Fase 3 — Pak de sleutel en ontgrendel de Geheime Schatkamer**
```
go n            → Start
go e            → Schatkamer
take sleutel    → Sleutel in inventory
go n            → Geheime Schatkamer [VERGRENDELD]
unlock          → typ wachtwoord: schatkamercode
                  → toont "De geheime schatkamer bevat een diamanten zwaard. Je wint!"
go s            → terug naar Schatkamer
```

**Fase 4 — Eindwin via de uitgang**
```
go w            → Start
go n            → De Uitgang → GEWONNEN
```

> Alleen **Player**-accounts kunnen `unlock` gebruiken. Een Admin krijgt: *"Geen toegang: controleer of je ingelogd bent met de juiste rol."*

### Admin-traject *(noclip)*

```
noclip          → "Noclip ingeschakeld."
go n            → De Uitgang → GEWONNEN (zonder sleutel!)
```

---

## Beveiliging

- Wachtwoorden gehasht met **SHA-256** *(zonder salt — schoolproject; in productie zou je bcrypt/argon2 willen)*.
- **JWT-tokens** zijn 2 uur geldig (zie `GenerateJwtToken` in `TextAdventureAPI/Program.cs`).
- Account **blokkeert na 3 foute pogingen** op de server (blijft geblokkeerd tot API-restart).
- Geheime kamers gebruiken **AES-256-CBC** met sleutel = `SHA256(keyshare + ":" + passphrase)`.
- Keyshares zitten hardcoded in `TextAdventureAPI/Program.cs` en `EncryptRooms.cs` — voor productie zouden die naar een secrets vault moeten.