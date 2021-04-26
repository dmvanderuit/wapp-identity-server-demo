# Workshop Identity Server 4

## Inleiding
In deze workshop wordt met behulp van Identity Server 4 authenticatie toegepast binnen een simpele applicatie. Het betreft het toevoegen van authenticatie bij een API, zodat alleen geauthoriseerden hier bij kunnen, en het toevoegen van authenticatie bij een web client zodat een gebruiker kan in- en uitloggen.

*Let op:*
- Paden op andere besturingssystemen dan Windows zouden net anders getypt kunnen worden
- Start de applicaties niet met IIS maar met via de naam van de applicatie (of SelfHost)

### Identity Server 4 setup

We beginnen met het installeren van de Identity Server 4 templates, om dit te doen dient het volgende commando uitgevoerd te worden:

```powershell
dotnet new -i IdentityServer4.Templates
```

Na het installeren van de IS4 templates kunnen we beginnen met het maken van het project.

Maak een nieuwe map aan, ```IS4Workshop```. Ga naar deze map en maak hierin een folder genaamd ```src```.
Eventueel kun je dit doen door de volgende commando's uit te voeren:
```powershell
md IS4Workshop
cd IS4Workshop

md src
cd src
```

Maak een nieuw Identity Server project aan met behulp van het volgende commando:

```powershell
dotnet new is4empty -n IdentityServer
```

Als laatst moet de solution nog worden gemaakt en het Identity Server project hier aan worden toegevoegd, dit moet in de root folder gebeuren (dus ```IS4Workshop```):

```powershell
dotnet new sln -n workshopIS4
dotnet sln add .\src\IdentityServer\IdentityServer.csproj
```

<br></br>
<b>Pas vervolgens in properties/launchSettings.json de applicationUrl aan naar `"applicationUrl": "http://localhost:5001"`.</b>

<br>

## Het opzetten van Identity server

De API is het gene wat wij willen "beschermen" met behulp van Identity Server, hiervoor moet de API worden toegevoegd aan de lijst van ApiScopes.

Ga naar Config.cs en pas de ApiScopes aan. In plaats van een array wordt er gebruik gemaakt van een lijst van ApiScopes.

```csharp
    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope("api1", "My API")
        };
```

<br>

Voeg in het bestand Startup.cs in de methode Configure helemaal bovenaan in de methode de volgende code toe (de methode wordt geimporteerd uit `Microsoft.AspNetCore.Http.SameSiteMode`. Hier zal IntelliSense om vragen.):

```csharp
app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.Lax
    });
```

<br>
Als laatst moet Startup.cs aangepast worden. Vervang in ConfigureServices() de builder met het volgende stuk code:

```csharp
    var builder = services.AddIdentityServer()
        .AddDeveloperSigningCredential()
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryClients(Config.Clients);
```

Dit zorgt er voor dat de options bij AddIdentityServer weggeghaald worden, deze hebben wij niet nodig.

Als laatst kunnen alle dingen waarboven staat <br>
```//uncomment if you want to add MVC```<br>
geuncomment worden, later in de workshop gaan wij gebruik maken van de Identity Server MVC.

 Om te controleren of de Identity Server correct is opgezet, kan de applicatie worden gestart.
 Navigeer naar <br>
 ```http://localhost:5001/.well-known/openid-configuration```<br>
 Als alles goed is verlopen zul je nu het [Discovery Document](https://docs.identityserver.io/en/latest/endpoints/discovery.html) zien, in het Discovery Document staat metadata over Identity Server, zoals de beschikbare scopes.

<br>

## Het opzetten van de API

We hebben nu een ApiScope gedefinieerd, echter "wijst" dit nog nergens naar toe, er bestaat immers nog geen api die wij kunnen aanroepen.

Voer in de ```src``` folder het volgende commando uit om een nieuwe WebApi aan te maken en deze meteen aan de solution toe te voegen:

```powershell
dotnet new webapi -n Api
cd ..
dotnet sln add .\src\Api\Api.csproj
```

Hierna moet de JwtBearer Nuget package worden toegevoegd aan de api, je kunt het volgende commando gebruiken, dit moet wel in de api folder.

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```


<br></br>
<b>Pas vervolgens in properties/launchSettings.json de applicationUrl op line 23 aan naar ```"applicationUrl": "http://localhost:6001",```.</b>

Er is al een controller voor ons gemaakt, de WeatherForecastController. Ga naar het bestand WeatherForecastController.cs en pas ```[Route("[controller]")]``` aan naar ```[Route("weatherforecast")]```.<br>

Voeg dan bovenaan het bestand ```using Microsoft.AspNetCore.Mvc;``` toe.<br>
Hierna moet onder ```[Route("weatherforecast")]``` ```[Authorize]``` toe worden gevoegd.

Hierna moet de Startup.cs <b>van de api</b> worden aangepast.

In ```ConfigureServices()``` kun je de 
```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
});
```

verwijderen, deze wordt niet gebruikt.

Plaats vervolgens onder ```services.AddControllers();``` het volgende stuk code:

```csharp
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "http://localhost:5001";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };

                    options.RequireHttpsMetadata = false;
                });
```

Haal vervolgens in ```Configure()``` het ```if statement``` weg, deze wordt niet gebruikt. Ook de regel `app.UseHttpsRedirection()` moet weggehaald worden. Als laatst moet onder ```app.UseRouting();``` 

```csharp
app.UseAuthentication();
```
 worden toegevoegd.


Door dit toe te voegen is de Authenticatie middelware toegevoegd aan de pipeline. Hierdoor wordt de inkomende token gevalideerd en tevens gecontroleerd of de token gebruikt mag worden met deze API.

Start de applicatie en navigeer naar ```http://localhost:6001/weatherforecast```. Als alles goed is gegaan zul je een ```401: unauthorized``` statuscode krijgen, de api is nu beschermd.


## Authorisatie toevoegen aan de API

Het volgende wat moet gebeuren is zorgen dat er toegang kan worden verschaft aan de API met behulp van Identity Server, terwijl er ook gecontroleerd wordt of het request de resource mag bezoeken. Hiervoor wordt ASP.NET Core Authorization Policy gebruikt.

Voeg in Startup.cs (van de api..) <b>boven</b> ```services.AddAuthentication()``` het volgende toe:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api1");
    });
});
```

Als laatst moet in de ```Configure``` methode de ```app.UseEndpoints``` aangepast worden, voeg aan de
```endpoints.MapControllers();``` de volgende methode toe:
```csharp
        .RequireAuthorization("ApiScope");
```

De aanroep van ```app.UseEndpoints``` zal er nu zo uit moeten zien:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers()
        .RequireAuthorization("ApiScope");
});
```
<br>

## UI toevoegen aan Identity Server

Identity Server biedt een simpel UI aan wat gebruikt kan worden om bijvoorbeeld in te loggen. Deze UI zal getoond worden als de gebruiker moet inloggen of als deze uitgelogt is.

De UI van Identity Server toevoegen is erg simpel, voer in de Identity Server map het volgende commando uit:

```powershell
dotnet new is4ui
```

Er zijn nu een aantal views en controllers aangemaakt.

<br>

## MVC Client toevoegen

Het laatste wat toegevoegd moet worden voordat wij een applicatie hebben die gebruik maakt van Identity Server om een api te bezoeken is een MVC client. 

Dit kunnen wij doen door in de ```src``` folder de volgende commando's uit te voeren:

```powershell
dotnet new mvc -n MvcClient
cd ..
dotnet sln add .\src\MvcClient\MvcClient.csproj
```

Hierna moet nog de OpenIdConnect Nuget package worden toegevoegd aan het MvcClient project, dit kan met het vollgende commando:

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```
<br>
<b>Verander vervolgens de applicationUrl onder het kopje `MvcClient` op line 23 in properties/launchsettings.json naar ```"applicationUrl": "http://localhost:5002",```</b>

<br><br>

Ga naar ```Startup.cs``` <b>van de MvcClient</b>, voeg in de methode ConfigureServices het volgende toe:

```csharp
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.RequireHttpsMetadata = false;
        options.Authority = "http://localhost:5001";

        options.ClientId = "mvc";
        options.ClientSecret = "secret";
        options.ResponseType = "code";

        options.SaveTokens = true;
        
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    });
```

Om dit te laten werken moet daarnaast ```using System.IdentityModel.Tokens.Jwt;``` bovenaan het bestand worden gezet.

Wat deze code doet is het gebruik maken van een cookie om de gebruiker in te loggen, daarnaast wordt nu het OpenID Connect protocol gebruikt. De ```options.Authority``` wijst naar de Identity Server.

Ga nu naar de methode ```Configure()``` en voeg onder ```app.UseRouting()```
```
app.UseAuthentication();
```

toe.

Als laatst moet de ```app.UseEndpoints``` aanroep worden aangepast zodat er gebruik wordt gemaakt van authorisatie, vervang de ```app.UseEndpoints``` met:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute()
                .RequireAuthorization();
});
```


In de applicatie moet ook uitgelogd kunnen worden, dit toevoegen is erg simpel.

Ga naar ```Controllers/HomeController.cs``` en voeg daar het volgende stukje code aan toe:

```csharp
public IActionResult Logout()
{
    return SignOut("Cookies", "oidc");
}
```

Dit zorgt er voor dat de lokale cookie wordt verwijderd, tevens wordt er een bericht gestuurd naar Identity Server dat de gebruiker is uitgelogd en zal deze ook de cookie verwijderen, de gebruiker is nu officieel uitgelogt.

Om gemakkelijk uit te loggen kun je in ```_Layout.cshtml``` in de Navbar (onder het list item van privacy) het volgende toevoegen:

```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Logout">Logout</a>
</li>
```
<br>

## Config.cs van Identity Server aanpassen

Om ook Identity Server gebruik te laten maken van OpenID Connect moeten de scopes van OpenID Connect worden toegevoegd.

Pas de IdentityResources aan naar het volgende:

```csharp
public static IEnumerable<IdentityResource> IdentityResources =>
    new List<IdentityResource>
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    };
```

Aan de lijst van Clients moet een nieuwe Client worden toegevoegd, die onze MvcClient representeert:


```csharp
public static IEnumerable<Client> Clients =>
    new List<Client>
    {
        new Client
        {
            ClientId = "mvc",
            ClientSecrets = { new Secret("secret".Sha256()) },

            AllowedGrantTypes = GrantTypes.Code,

            RedirectUris = { "http://localhost:5002/signin-oidc" },

            PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },

            AllowedScopes = new List<string>
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "api1"
            }
        }
    };
```

Nu is de MvcClient toegevoegd aan de lijst van Clients voor de Identity Server. Daarnaast zal de MvcClient nu doorgestuurd worden naar de inlog (of uitlog) pagina als deze wel of niet ingelogd is. De MvcClient kan nu enkel bij de scopes die geleverd zijn door OpenId Connect, dat zijn dus de OpenId scope en de Profile Scope. Een andere scope die de MvcClient moet kunnen bezoeken is de ```ap1``` scope. Voeg deze toe aan de lijst van AllowedScopes.


## De MvcClient de API laten aanroepen

We zijn er bijna, het laatste wat er moet gebeuren is het mogelijk maken voor de MvcClient om de API aan te roepen.

Ga naar ```Startup.cs``` en voeg in de methode ```ConfigureServices()``` <b>onderaan</b> in de aanroep van ```services.AddAuthentication()```:
```csharp
        options.Scope.Add("api1");
```
toe. De MvcClient weet nu dat deze de scope met naam "api1" mag bezoeken.

Echter is er nog geen manier om een request te versturen naar de API. Ga naar ```Controllers/HomeController.cs``` en voeg het volgende toe:


```csharp
public async Task<IActionResult> Weather()
{
    var accessToken = await HttpContext.GetTokenAsync("access_token");

    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var content = await client.GetStringAsync("http://localhost:6001/weatherforecast");

    ViewBag.Json = JArray.Parse(content).ToString();
    return View("json");
}
```

Het allerlaatste dat er moet gebeuren is het maken van een view om de data die we krijgen van de API te tonen. Maak een nieuw bestand in de map ```Views/Home``` genaamd ```json.cshtml```. We willen enkel de gekregen data tonen, verwijder alles uit het bestand en voeg de volgende regel toe:

```html
<pre>@ViewBag.Json</pre>
```

Start alle applicaties en navigeer naar ```http://localhost:5002/home/weather```, als alles goed is gegaan, en je bent ingelogd, zul je nu de data van de api zien (het weerbericht).

