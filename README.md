# new-design-project

Et værktøj til at sende prismails. Én udsendelse går til en hel marketinggruppe —
mennesker fra forskellige firmaer — og **hver kunde får sin egen prisliste**
vedhæftet, hentet fra regnskabssystemet.

Bygget i Blazor WebAssembly (.NET 8).

## Kør appen

```
cd OnboardingChecklist
dotnet run
```

Mappenavnet er en rest fra det appen voksede ud af; læs ikke noget i det.

Stop serveren på porten, ikke med `pkill -f "blazor-devserver"` — det mønster
matcher sin egen kommandolinje og dræber processen med kode 144:

```
lsof -ti:5210 -sTCP:LISTEN | xargs -r kill
```

## Dokumentation

`docs/` indeholder guiden i tre former. **HTML'en er kilden** — den bærer
print-stylingen PDF'en skal bruge, og Markdown'en genereres fra den:

```
python3 docs/guide-to-markdown.py
```

| Fil | Til hvad |
| --- | --- |
| [`docs/blazor-checklist-guide.md`](docs/blazor-checklist-guide.md) | Læsning på GitHub, eller til at give en AI |
| `docs/blazor-checklist-guide.pdf` | 33 sider, til print |
| `docs/blazor-checklist-guide.html` | Kilden — ret her, ikke i de to andre |
| [`docs/kolonne-dropdown.md`](docs/kolonne-dropdown.md) | Listens kolonnemenu: sortering og filter i ét panel |
| [`docs/excel-arket.md`](docs/excel-arket.md) | Prislisterne: rigtige .xlsx, læst og skrevet med Open XML SDK |
| [`docs/liste-vaelger.md`](docs/liste-vaelger.md) | Trin 1: tre kort og et søgefelt til de øvrige hundrede |
| [`docs/skabelonen.md`](docs/skabelonen.md) | Trin 2 og 3: ét brev pr. sprog, og hvor udsendelsen ender |

Guiden er en beslutningslog: den fortæller hvorfor hver ting ser ud som den gør,
og hvilke fælder der ligger undervejs. Den gengiver **ikke** hele kildekoden —
461 linjer i guiden mod 2741 i projektet, og næsten intet af de 1240 linjer CSS.
Den kan bygge appen igen *med repoet ved hånden*, ikke fra PDF'en alene.
**Appendiks B** viser modellen som den ser ud nu, hvis du bare vil have
sluttilstanden uden at læse historien.

Skal en AI læse guiden, så giv den rå-adressen frem for GitHub-siden — sidstnævnte
er 669 KB HTML med filen begravet indeni:

```
https://raw.githubusercontent.com/markat1/new-design-project/main/docs/blazor-checklist-guide.md
```

## Hvad ligger hvor

| Mappe | Indhold |
| --- | --- |
| `OnboardingChecklist/` | Appen |
| `docs/` | Guiden og dens generator |
| `proto/` | Den oprindelige prototype — tre onboarding-varianter bag en vælger, plus HTML-udforskninger af tabel-layouts. Appen voksede ud af `Checklist`-varianten. |
