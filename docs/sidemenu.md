# Sidemenuen og kladden

Afgjort ud fra seks runder prototyper (13. september 2026), alle bygget på et aftryk af appens egen ramme.

Filerne: `Shell.razor` (menuen), `Components/StepRail.razor` (trinnene), `Components/StartOverDialog.razor` (spørgsmålet), `Model/MailDraft.cs` (`Started`, `Untouched`, `StartOver`) og `wwwroot/js/app.js` (`app.dialog`).

```
┌──────────────────────────┐
│ [+ Ny udsendelse       ] │  ← knappen, øverst
│                          │
│ ✉ Udsendelser         4  │  ← listen
│░✎ Jeres priser for 2027 ░│  ← kladden …
│░  Kladde · 8 mails · 7 kunder
│░● Modtagere             ░│  ← … og dens trin på én flade
│░● Skabelon              ░│
│░● Afsendelse            ░│
└──────────────────────────┘
```

---

## Navnet: udsendelse, ikke mail

Menuen sagde *Mails* og *Ny mail*, mens resten af appen allerede sagde *"4 udsendelser"* og *"Vælg en udsendelse"*. En udsendelse er én ting, du laver, og den bliver til mange mails.

**"Nye mails" blev fravalgt.** I Outlook betyder "nye mails" ulæste mails, og under "Mails 4" læses det som fire nye i indbakken. At der sendes mange, siges med tallet på kladden ("8 mails · 7 kunder"), ikke med flertal i navnet.

## Kladden hører til listen

- **Kladden står lige under Udsendelser**, med emne og størrelse: *"Jeres priser for 2027 · Kladde · 8 mails · 7 kunder"*. Et tomt emne står som *"(intet emne endnu)"* i den dæmpede farve.
- **Kladden og trinnene deler én flade** (`--canvas` med en hårstreg over og under), når kladden er åben. Uden fladen stod kladden og de tre trin med samme venstrekant og samme afstand, og øjet læste fire ligeværdige ting.
- **Kun trinnet er blåt.** Kladden er, hvor du er, ikke hvad der er valgt, så den får mørk tekst og ingen farve. To blå rækker betød to markeringer.
- **Kladden står der kun, når du selv har startet den.** Appen har altid et udkast i hukommelsen, og viste menuen det, stod der en kladde, ingen havde lavet.

## Knappen står øverst

Den blå "Ny udsendelse" står øverst i menuen, med Udsendelser mellem sig og kladden.

Knappen stod først *mellem* listen og kladden, lige over det den laver. Det så rigtigt ud, men kladden og trinnene er rækker, man klikker på hele tiden, og en tung knap lige over dem er et nemt mål for et forkert klik. Øverst er der en række imellem, og det er samme plads som Outlooks "Ny mail".

## Der er kun én kladde

En ny udsendelse erstatter kladden. Flere kladder på én gang kan komme senere, men er ikke udgangspunktet: et klik for meget ville lave kladder, ingen kan finde rundt i.

**Der spørges kun, når noget går tabt.** En kladde, der ikke er ændret siden den blev startet (`MailDraft.Untouched`), bliver bare startet forfra. En ændret kladde giver en dialog:

```
Start en ny udsendelse?

Der er kun plads til én kladde ad gangen. Starter du en ny,
bliver »Jeres priser for 2027« til 8 mails slettet.

                [🗑 Slet kladden og start ny]  [Fortsæt kladden]
```

- **Spørgsmålet er det, knappen bad om**, og brødteksten siger, hvad det koster. Første udgave svarede med en oplysning (*"Du har en kladde i gang"*), og så besvarede knapperne et spørgsmål, ingen havde stillet.
- **Knapperne hedder det, de gør**, ikke "Ja" og "Nej". Den destruktive er rød.
- **Fokus starter på "Fortsæt kladden".** Enter i blinde sletter ikke noget.
- **Escape og klik uden for dialogen fortsætter.** Det er native `<dialog>` med `showModal()`: fokusfælden, Escape og det øverste lag følger med.

Ændret betyder: en anden liste end den første, en person taget af, en tekst der ikke er skabelonens, eller en afsendelse der ikke er standard.

## Forkastet

| Retning | Hvorfor ikke |
| --- | --- |
| **Opgave** ("Send prislister" med nummererede trin) | Et verbum i menuen kan læses som en knap, der sender med det samme. |
| **"I gang"** som eget afsnit | Kladden stod adskilt fra listen, og dens trin lignede søskende til den. |
| **Overskrift** (kladdens navn som lille overskrift) | Roligst, men på listen, hvor trinnene er skjult, lignede den en etiket, ikke noget man klikker på. |
| **Under listen** (indrykket i tre niveauer) | Menuen er 248px, så "CRM · Gemt lokalt · Kopi til mig" blev skåret af. |
| **I listen** (kladden som række i tabellen) | Menuen viste ikke kladden, mens man stod på listen. |
| **Knappen i bunden** | Ca. 580px fra kladden, og den hørte ikke sammen med noget. |
| **Skjult** (ingen knap, mens der er en kladde) | Man kan ikke starte forfra uden først at sende, og en knap, der forsvinder, undrer. |
| **Flere kladder** | Et uheldigt klik laver en kladde mere, og menuen fyldes med "(intet emne endnu)". |
| **Spørg** (menu under knappen) | Lå hen over Udsendelser og kladden. Dialogen kan ikke overses. |
| **Fortryd** (slet med det samme, "Fortryd" bagefter) | Man opdager først bagefter, at kladden er væk. |

## Faldgruber

**Sticky sidemenu og bagtæppe.** `.side` er `position: sticky` og laver sin egen stablingskontekst. En menu inde i den ligger *under* et bagtæppe uden for den, uanset z-index, og kan ikke klikkes. Native `<dialog>` lægger sig i browserens øverste lag og har ikke problemet.

**Et klik på dialogens egen kant er et klik på `<dialog>`.** Bagtæppet og dialogens padding er det samme mål. Luk kun, når klikket ligger uden for dialogens boks.
