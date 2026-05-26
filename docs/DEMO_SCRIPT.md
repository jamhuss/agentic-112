# Demo-manus: AI som Backend-komponent i .NET

**Målgrupp:** .NET-utvecklare  
**Längd:** ~35-40 min  
**Förutsättning:** Server + client igång, testscenarier redo

---

## DEL 1 – Intro & Problemställning (5 min)

### Prata:

> "Idag ska jag visa hur man kan använda AI som en komponent i backend – inte som en chatbot, inte som en fristående tjänst, utan som en injicerbar service i ert DI-container precis som vilken repository eller gateway som helst."

> "Kontexten är ett incident-hanteringssystem – tänk SOS Alarm, 112. En operatör tar emot ett larm och behöver snabbt avgöra: vilka tjänster ska skickas? Hur allvarligt är det? Är det ens på riktigt?"

> "Traditionellt ser det ut så här: operatören fyller i ett formulär – väljer tjänster från en dropdown, sätter prioritet, klickar spara. Systemet lagrar exakt vad operatören skrev. Ingen validering av rimlighet, ingen second opinion. Om operatören missar att en brand kräver räddningstjänst – ja, då skickas ingen räddningstjänst."

> "Det jag vill visa idag är tre nivåer av AI-ansvar i samma system:"
> 1. **Partiell** – operatören bestämmer, AI kvalitetssäkrar
> 2. **Fullständig** – AI tar hela ansvaret från fritext
> 3. **Omvärdering** – AI körs om vid förändring

---

## DEL 2 – Arkitekturöversikt (3 min)

### Visa: `Program.cs`

> "Innan vi kör live vill jag visa hur AI:n registreras. Det här är hela setupen."

**Peka på:**

```csharp
var azureClient = new AzureOpenAIClient(new Uri(aiOptions.Endpoint), credential);

builder.Services.AddSingleton<IChatClient>(
    azureClient.GetChatClient(aiOptions.Model).AsIChatClient());

builder.Services.AddSingleton<IAiGateway, AiGateway>();
builder.Services.AddSingleton<ICredibilityGateway, CredibilityGateway>();
```

> "Vi använder `Microsoft.Extensions.AI` – ett nytt abstraktionslager från Microsoft. `IChatClient` är vendor-agnostisk. Idag kör vi Azure OpenAI GPT-4o, men vi kan byta till Ollama, Anthropic, vad som helst – utan att röra en enda rad applikationskod."

> "`IAiGateway` och `ICredibilityGateway` är våra egna interfaces. AI:n sitter bakom samma mönster som `IEmailService` eller `IPaymentGateway`. Den kan mockas, testas, bytas ut."

---

## DEL 3 – Live: Partiell AI (10 min)

### Scenario: Brand – operatören väljer FEL tjänster

> "Nu kör vi live. Jag är operatör och får in ett samtal om en brand."

**Gör:**
1. Klicka **+ Nytt ärende**
2. Klistra in: *"Det brinner kraftigt i en lägenhet på Drottninggatan 14, tredje våningen. Jag ser lågor och svart rök ur fönstren. Flera personer skriker från balkongen."*
3. Välj tjänster: **bara Polis + Ambulans** (medvetet fel)
4. Prioritet: **Hög** (inte kritisk – också lite fel)
5. Klicka Spara → visa "Analyserar..."

> "Nu händer det saker i backend. Mina val skickas till AI:n som kontext – inte som sanning. AI:n klassificerar själv OCH granskar mina val."

**När resultatet kommer, peka på kortet:**

> "Titta här – AI:n har lagt till `fire_department` som jag missade. Den har höjt prioriteten till `critical`. Och i reasoning står det: 'Brand med personer i fara kräver räddningstjänst. Operatören valde enbart polis och ambulans, men fire_department är kritiskt i detta scenario.'"

> "Det här är partiell AI. Operatören gör sin bedömning – AI:n fungerar som en second opinion som fångar upp misstag."

### Visa kod: `IncidentService.CreateManualAsync`

```csharp
var analysis = await _ai.AnalyzeAsync(description, services); // ← user services som kontext

incident.Services = analysis.Services;   // AI:ns klassificering vinner
incident.Priority = analysis.Priority;
incident.Confidence = analysis.Confidence;
```

> "Operatörens valda tjänster skickas med till prompten, men AI:ns klassificering avgör slutresultatet. Det här är ett designval – man kan lika gärna göra tvärtom och bara visa en varning."

### Visa kod: `ClassificationPrompt.cs`

> "Så här ser prompten ut – system prompt med tydliga regler och en JSON Schema."

**Peka på prompten:**
> "Notera sista regeln: 'Om operatörens valda tjänster anges: granska om de är korrekta och tillräckliga. Påpeka i reasoning om kritiska tjänster saknas.' Det här ger oss audit trail – vi vet varför AI:n ändrade."

### Visa kod: Structured Outputs

```csharp
ResponseFormat = ChatResponseFormat.ForJsonSchema(SchemaElement, "classification")
```

> "Det här är nyckeln – Structured Outputs. Vi skickar ett JSON Schema till GPT-4o och modellen MÅSTE svara i det formatet. Ingen regex-parsing, ingen 'hoppas att JSON:en stämmer'. Om schemat säger att `priority` ska vara en enum med fyra värden – ja, då får vi ett av de fyra värdena."

### Visa kod: `AiResponseValidator.ValidateClassification`

> "Men vi litar inte blint. Vi validerar ändå – för defense in depth. Finns services i vår tillåtna lista? Är priority en giltig sträng? Confidence mellan 0 och 1? Om valideringen misslyckas → retry."

---

## DEL 4 – Live: Fullständig AI (8 min)

> "Nu tar vi nästa steg – AI tar hela ansvaret. Operatören skriver bara fritext."

### Scenario: Trafikolycka

**Gör:**
1. Klicka **🤖 Nytt AI-ärende**
2. Klistra in: *"Två bilar har frontalkrockat på E4 strax söder om avfart Kungens kurva. En person verkar sitta fastklämd. Det luktar bensin."*
3. Klicka Skapa → visa "Analyserar..."

> "Här finns inget formulär för tjänster eller prioritet – AI:n extraherar allt från fritexten."

**När resultatet kommer:**

> "AI:n har valt ambulans, polis, räddningstjänst. Prioritet critical. Confidence 0.95. Reasoning: 'Frontalkrock med fastklämd person och bensinlukt indikerar omedelbar livsfara.' Och trovärdigheten: high – detaljerad beskrivning med specifik plats."

### Scenario: Falskt larm – draken

**Gör:**
1. Nytt AI-ärende: *"En drake flyger över Sergels torg och spottar eld på turisterna"*

> "Nu kör vi ett falsklarm – medvetet absurt."

**När resultatet kommer:**

> "Titta – status 'flagged', trovärdighet 'low'. Reasoning: 'Drakar existerar inte. Beskrivningen är fysiskt omöjlig.' Och `needsHumanReview: true`."

### Visa kod: Tvåstegs-pipeline

> "Det som hände bakom kulisserna var två separata AI-anrop."

**Visa `IncidentService.CreateFromAiAsync`:**

> "Steg 1: Klassificering – 'vad händer och vad behövs?' Steg 2: Trovärdighetskontroll – 'är det här på riktigt?' Två olika prompts, två olika scheman, två olika validerare. De kan ha olika temperaturer, olika modeller om man vill."

### Visa kod: `CredibilityPrompt.cs`

> "Trovärdighets-prompten får med sig hela kontexten: beskrivningen, valda tjänster, prioritet, och vem som skapade ärendet. Den har egna regler – 'Var skeptisk mot extremt korta beskrivningar eller uppenbara testmeddelanden.'"

### Scenario: Subtilt falsklarm

**Gör:**
1. Nytt AI-ärende: *"Jag vill bara kolla om det här systemet fungerar, kan ni skicka en ambulans till Vasagatan 1?"*

> "Det här är intressantare – inget orealistiskt, men avsikten är uppenbart ett test."

**Visa resultatet:**

> "AI:n fångade det. Trovärdighet 'low', reasoning: 'Avsändaren erkänner explicit att det är ett test.' Den här typen av bedömning hade krävt manuell granskning i ett traditionellt system."

---

## DEL 5 – Live: Omvärdering vid redigering (5 min)

> "Sista nivån – vad händer när information uppdateras?"

**Gör:**
1. Klicka Redigera på ett befintligt ärende
2. Ändra beskrivningen till något annat
3. Klicka Spara → visa "Analyserar..."

> "Nu körs hela pipelinen om – klassificering OCH trovärdighet. Titta: `createdBy` har flippat till 'User'. Gamla pipeline-steg är borta, nya har genererats."

### Visa kod: Controller

```csharp
// Content edit → reclassify via AI pipeline
if (request.Description is not null)
{
    var result = await _service.ReclassifyAsync(incident, request.Description, request.Services);
    return Ok(result);
}

// Status-only update (approve/reject)
if (request.Status is not null) incident.Status = request.Status;
```

> "Det är samma PATCH-endpoint men med en branch: om description ändras → kör pipeline. Om bara status ändras (godkänn/avvisa) → enkel uppdatering. AI:n sitter inte i vägen för enkla operationer."

---

## DEL 6 – Felhantering & Robusthet (4 min)

### Visa kod: Retry-logik i `AiGateway`

```csharp
for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
{
    var response = await _chat.GetResponseAsync(messages, chatOptions);
    var result = AiResponseValidator.ValidateClassification(response.Text ?? "");
    if (result is not null) return ...;
}
throw new InvalidOperationException("AI classification failed after N attempts");
```

> "AI är opålitlig by design. Modeller kan hallucinera, ge ogiltig JSON trots Structured Outputs, eller timea ut. Varje svar valideras – och om det inte passerar, provar vi igen."

### Visa kod: Fallback i `RunCredibilityCheck`

```csharp
catch (Exception ex)
{
    incident.Credibility = null;
    incident.NeedsHumanReview = true;
    incident.Status = "flagged";
}
```

> "Om trovärdighetskontrollen kraschar helt – systemet stannar inte. Ärendet flaggas för manuell granskning. Operatören får fortfarande jobba."

> "Det här är viktigt: AI-komponenten får aldrig vara en single point of failure. Om AI:n är nere ska systemet degradera gracefully, inte krascha."

---

## DEL 7 – Sammanfattning & Mönster (3 min)

> "Så, vad tar vi med oss?"

> "Mönstret för att använda AI som backend-komponent i .NET:"

1. **Interface-first** – `IAiGateway`, inte `OpenAiService`. Vendor-agnostiskt.
2. **Structured Outputs** – tvinga modellen att svara i ert domänformat. JSON Schema → determinism.
3. **Validera alltid** – AI:ns svar passerar samma validering som användarinput.
4. **Retry + Fallback** – AI misslyckas. Planera för det.
5. **Pipeline, inte monolith** – separata steg med eget ansvar. Lättare att debugga, testa, skala.
6. **Audit trail** – `reasoning`-fältet ger transparens. Ni vet varför AI:n tog beslutet.

> "AI:n är inte magi – det är en komponent. Behandla den som en opålitlig extern tjänst med bra dagar och dåliga dagar. Wrappa den i interfaces, validera output, ha fallbacks. Då får ni kraften utan kaoset."

> "Frågor?"

---

## Backup-scenarion (om tid finns / frågor leder hit)

| Scenario | Beskrivning | Poäng |
|----------|-------------|-------|
| 3 – Svårtolkad | "Min son ringde hysterisk..." | AI resonerar om implicit fara |
| 7 – Gravid på E4 | Motorfel + höggravid | Visar multi-service klassificering |
| Redigera med ny info | Ändra brand → "Det var bara en grillkväll" | Visa omklassificering till lägre prioritet |

---

## Checklista innan demo

- [ ] Server körs (`dotnet run`)
- [ ] Client körs (`npm run dev`)
- [ ] Testscenarier öppna i en flik
- [ ] DevTools Network-tab öppen (visa latens)
- [ ] Inga gamla ärenden i listan (starta om server för clean state)
- [ ] Swagger öppen som backup (`/swagger`)
