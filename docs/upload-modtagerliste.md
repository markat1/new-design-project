# Upload af modtagerlisten

En udsendelse starter med en fil: en Excel- eller PDF-fil med én person pr. række. Hver række bliver til en modtager. En marketingliste kan komme med bagefter, men den er det ekstra, ikke udgangspunktet.

Prototypen ligger i `SendChecklist/` (Blazor WASM, port 5179). Den har fire sider: Trinliste på `/`, Tjekliste på `/tjekliste`, Upload på `/upload` og Lister på `/lister` (alle i `Pages/`). Der er ingen vælger; siderne nås via deres adresse og linkene mellem dem. Trinenes indhold står i `Components/StepBody.razor`, og modellen ligger i `Model/Checklist.cs`. Udseendet styres af `.v2`-blokken (Trinliste) og `.v3`-blokken (Tjekliste) i `wwwroot/css/app.css`.

---

## Beslutningen: Trinliste i fuld side

Afgjort 11. november 2026 ud fra fem retninger.

> **Valget:** variant 2, **Trinliste**, lagt ud over hele siden og med Upload som første trin.
>
> **Status:** besluttet, men ikke bygget ind i `OnboardingChecklist` endnu. Prototypen står stadig. Trinliste er siden på `/`, og Tjekliste ligger som en ekstra side på `/tjekliste`. Se [Parkeret](#parkeret).

**Grunden:** Når filen er uploadet, skal tabellen med rækkerne kunne ses. Trinliste giver tabellen hele bredden, mens trinene hele tiden kan ses i kolonnen til venstre.

```
┌────────────────┬──────────────────────────────────────────────────────┐
│ Ny udsendelse  │ TRIN 1 AF 4                                          │
│                │ Upload modtagerlisten                                │
│ (1) Upload     │ En Excel- eller PDF-fil med én person pr. række. …   │
│     6 fra mes… │ ┌──────────────────────────────────────────────────┐ │
│ (2) Marketing… │ │ XLSX messe-oktober-2026.xlsx    [Udskift]  [✕]   │ │
│     Valgfrit   │ └──────────────────────────────────────────────────┘ │
│ (3) Besked     │ ☑ # Navn      Mail        Firma      Land  Status    │
│ (4) Klar       │ ☑ 2 Anna …    anna@…      Vela …     DK    Klar      │
│                │ ☐ 6 Marie …   tom         Vela …     DK    ⚠ Mangler │
│                │   … (scroller for sig selv)                          │
│ 6              ├──────────────────────────────────────────────────────┤
│ modtagere ·    │ [Tilbage]                               [Fortsæt]    │
│ 4 prislister   │                                                      │
└────────────────┴──────────────────────────────────────────────────────┘
```

## Flowet

| # | Trin | Spørgsmål | Kan man gå videre uden? |
| --- | --- | --- | --- |
| 1 | **Upload** | Upload modtagerlisten | Ja, og knappen hedder så "Fortsæt uden fil" |
| 2 | **Marketingliste** | Skal en marketingliste med? Standardvalget er "Ingen liste, kun filen" | Nej, hvis hverken fil eller liste giver modtagere |
| 3 | **Besked** | Emne og tekst. `{navn}` og `{firma}` udfyldes pr. modtager | Nej, uden emnelinje |
| 4 | **Klar** | Se det hele igennem og send. Knappen hedder "Send N prislister" | – |

- Man kan kun klikke på trin, man allerede har nået. Hvert trin viser sit svar under navnet ("6 fra messe-oktober-2026.xlsx", "Norden", emnet).
- Står en person både i filen og på listen, får de én mail. Trin 2 siger det højt: "1 fra filen står allerede på Norden og får kun én mail."
- Fejl står under trinnets indhold med ikon og tekst, og de forsvinder, så snart de er rettet.

## Tabellen efter upload

Kolonnerne er: afkrydsning, # (rækkens nummer i regnearket, så den kan findes i Excel), Navn, Mail, Firma, Land og Status. Tabellen har `table-layout: fixed`. Lange navne og mails afkortes med `…` og står fuldt ud i `title`.

| Status | Tone | Kan fravælges |
| --- | --- | --- |
| Klar | grøn | ja |
| Klar · ingen prisliste | grøn | ja |
| Klar · engelsk (intet land) | grøn | ja |
| Står på listen | grå | nej, får mail via listen |
| Fravalgt | grå | ja, kan vælges til igen |
| Mangler mail / Mailen er ikke hel | rød + ⚠ | nej, rettes i filen |

Under tabellen står "N rækker har ingen brugbar mail og kommer ikke med. Ret dem i filen, og upload den igen." Er filen en PDF, står der også, at kolonnerne skal tjekkes.

## Layoutet

| Del | Værdi |
| --- | --- |
| Sidens højde | `100dvh`; siden selv scroller aldrig på desktop |
| Trinkolonne | 272px, fast, `--raised` baggrund, kant til højre. Tælleren (modtagere · prislister) står nederst, `aria-live="polite"` |
| Indhold | scroller for sig selv, padding `40px 48px`, max-bredde 1120px. **Trin 1 undtaget**, se næste række |
| Trin 1 | fylder hele fladen uden max-bredde. Før upload står overskriften over drop-zonen. **Efter upload skjules overskriften** (den bliver i DOM'en for skærmlæsere og fokus), og trinnet går helt ud til kanterne uden padding og uden runde hjørner: filbjælken er en værktøjslinje med streg under, tabellen har ingen ramme, og første og sidste kolonne har 24px luft ind til kanten. Noten om dårlige rækker ligger som en linje med streg over. I tabellen hvor kun rækkerne scroller og kolonneoverskrifterne står fast (`position: sticky`). Celler 44px høje, `padding-inline: 14px`; kolonner: afkrydsning 48px, # 52px, Land 72px, Status 196px |
| Knapbjælke | står altid i bunden, kant øverst, padding `14px 48px`. Tilbage er ghost til venstre, Fortsæt er primær til højre |
| Overskrift | 24/32, 600; trin-etiket 12px versaler, `letter-spacing: 0.04em`, accentfarve |
| Valgmuligheder / felter | max 560px / 720px brede, så de ikke strækker sig over hele siden |
| Under 760px | kolonnen lægger sig øverst, siden scroller, knapbjælken er `sticky` i bunden |

---

## Upload uden trin (`/upload`)

Til det tilfælde, hvor en liste kun skal frigives og ikke blive til en modtagerliste.

- Øverst: en knap **← Lister** (samme stil som knapperne i bunden) og titlen "Upload en liste".
- Før upload fylder drop-zonen siden. Efter upload går filbjælken og tabellen fra kant til kant, som i Trinliste.
- I bunden: "N rækker klar · M springes over" og **to knapper**. **Frigiv N til lagersystemet** er den primære; **Lav modtagerliste** er den sekundære og åbner en kladde i Trinliste på trin 2 med filen indlæst. Før upload er begge slået fra.
- Efter frigivelse: "Frigivet: N rækker … ligger nu i lagersystemet" med **Se i Lister** og **Upload en ny fil**.
- Frigivelsen er simuleret i prototypen (`Checklist.Store()`); det rigtige lagersystem er ikke bestemt.
- **Forkastet: faner** ("Kun frigiv" / "Lav modtagerliste") over tabellen. Begge faner viste den samme tabel; forskellen var kun, hvad knappen gjorde, så det var et valg af handling og ikke af visning, og det kostede et ekstra klik.

## Oversigt (`/lister`)

Alt, der er frigivet eller lavet til modtagerlister, nyeste øverst.

- Øverst: fanerne **Alle · Frigivet · Modtagerlister** med antal, og **Upload en liste** til højre. Ingen titellinje.
- Tabel fra kant til kant: Fil, Hvad (pill: *Frigivet til lager* grå, *Kladde* blå, *Sendt* grøn), Rækker (+ sprunget over), Marketingliste, Hvornår ("I dag 14:32", "4. nov. 11:20").
- Pr. linje: en kladde har **Åbn kladde**, en frigivet fil har **Lav modtagerliste**. Sendte linjer har ingen knap.
- Historikken ligger kun i hukommelsen i prototypen (`Model/History.cs`), med seks eksempellinjer.
- Kendt fejl: "sprunget over" tæller også personer, der allerede står på marketinglisten.

## Tabelstørrelse

Tabellen er gjort større: 15px celler, 12px overskrifter, 48px rækker. Alle mål afledes af `--tbl-fs`, så pills og kolonner vokser med. Tjekliste har et prototype-panel øverst til højre (`wwwroot/js/dial.js`) med skydere for bredde, luft, tabeltekst og højde; det skal fjernes, når tallene er valgt.

## Forkastet

De tre forkastede varianter er slettet fra prototypen.


- **1 · Kort**: mørkt, centreret kort som på billederne i `prototypes/`. Man ser kun ét trin ad gangen og kan ikke se de tidligere svar, og kortet er for smalt til tabellen.
- **4 · Med forhåndsvisning**: formular til venstre og en live modtagerliste med mailen til højre. Den kræver en bred skærm, og tabellen bliver klemt, så Firma afkortes.
- **5 · Fokus**: ét spørgsmål pr. skærm i stor tekst, Enter for videre. Den er for langsom til noget, der bruges jævnligt, og tabellen passer ikke til formatet.
- **Upload som trin 2 efter Modtagere**: den første udgave. Den blev vendt om, fordi filen er det første, man har i hånden. Marketinglisten er det valgfrie ekstra.
- **Trinliste som kort midt på siden** (1040px): tabellen fik for lidt plads, og knapperne flyttede sig med indholdets højde.

## Parkeret

- **Tjekliste**: alle trin på én side, og klarede trin foldes sammen med "Ret". Den har sin egen side på `/tjekliste`, så den kan vælges i stedet for Trinliste. Ulempen er, at siden bliver lang, når upload-tabellen er åben.
