# Workshop Identity Server 4

*Let op:* Dit is de uitwerking MET persistent storage en roles. De andere uitwerking staat op de master-branch.

## Inleiding

In deze workshop wordt met behulp van Identity Server 4 authenticatie toegepast binnen een simpele applicatie. Het
betreft het toevoegen van authenticatie bij een API, zodat alleen geauthoriseerden hier bij kunnen, en het toevoegen van
authenticatie bij een web client zodat een gebruiker kan in- en uitloggen.

*Let op:*

- Paden op andere besturingssystemen dan Windows zouden net anders getypt kunnen worden
- Start de applicaties niet met IIS maar met via de naam van de applicatie (of SelfHost)

### Identity Server 4 setup

We beginnen met het installeren van de Identity Server 4 templates, om dit te doen dient het volgende commando
uitgevoerd te worden:

```powershell
dotnet new -i IdentityServer4.Templates
```

Na het installeren van de IS4 templates kunnen we beginnen met het maken van het project.

Maak een nieuwe map aan, ```IS4Workshop```. Ga naar deze map en maak hierin een folder genaamd ```src```. Eventueel kun
je dit doen door de volgende commando's uit te voeren:

```powershell
md IS4Workshop
cd IS4Workshop

md src
cd src
```

Maak een nieuw Identity Server project aan met behulp van het volgende commando:

```powershell
dotnet new is4aspid -n IdentityServer
```

Het commando wordt uitgevoerd en zal je op het eind vragen of je de seeds wil draaien. Vul in je terminal 'N' (
hoofdlettergevoelig) in, omdat we zometeen een andere database zullen verbinden.

Als laatst moet de solution nog worden gemaakt en het Identity Server project hier aan worden toegevoegd, dit moet in de
root folder gebeuren (dus ```IS4Workshop```):

```powershell
dotnet new sln -n workshopIS4
dotnet sln add .\src\IdentityServer\IdentityServer.csproj
```

<br></br>
<b>Pas vervolgens in properties/launchSettings.json de applicationUrl aan
naar `"applicationUrl": "http://localhost:5001"`.</b>

<br>

## Het opzetten van Identity server

De API is het gene wat wij willen "beschermen" met behulp van Identity Server, hiervoor moet de API worden toegevoegd
aan de lijst van ApiScopes.

Ga naar Config.cs en pas de ApiScopes aan. In plaats van een array wordt er gebruik gemaakt van een lijst van ApiScopes.

```csharp
    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope("api1", "My API")
        };
```

<br>

Voeg in het bestand Startup.cs in de methode Configure helemaal bovenaan in de methode de volgende code toe (de methode
wordt geimporteerd uit `Microsoft.AspNetCore.Http.SameSiteMode`. Hier zal IntelliSense om vragen.):

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
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                .AddAspNetIdentity<ApplicationUser>();
```

Dit zorgt er voor dat de options bij AddIdentityServer weggeghaald worden, deze hebben wij niet nodig.

Om te controleren of de Identity Server correct is opgezet, kan de applicatie worden gestart. Navigeer naar <br>
```http://localhost:5001/.well-known/openid-configuration```<br>
Als alles goed is verlopen zul je nu
het [Discovery Document](https://docs.identityserver.io/en/latest/endpoints/discovery.html) zien, in het Discovery
Document staat metadata over Identity Server, zoals de beschikbare scopes.

 <br>

## Inrichten van de database

IdentityServer maakt in dit voorbeeld gebruik van een MSSQL database, om zo veel mogelijk op de casus toe te spitsen.
Standaard zal IdentityServer gebruik maken van SQLLite. Om ondersteuning te bieden voor MSSQL moeten we een paar
packages installeren. Installeer de volgende NuGet Packages:

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore

Houd er wel rekening mee, dat de IdentityServer in dotnet 3 is gemaakt voor longterm support. Kies daarom de juiste
versies van deze packages.

Nu is het tijd om de connectie in te regelen. Open appsettings.json en vervang de connectionstring door de volgende
string: `"Server=(localdb)\\mssqllocaldb;Database=Injection;Trusted_Connection=True;MultipleActiveResultSets=true"`. Dit
voorbeeld maakt gebruik van de localDB waar Windows-gebruikers standaard gebruik van kunnen maken. Dit voorbeeld werkt
ook prima met Docker. Om meer te lezen over Docker, lees
je [het volgende artikel](https://docs.docker.com/samples/aspnet-mssql-compose/).

Navigeer naar je Startup.cs en vervang `options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));`
met `options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));`.

We zijn nu klaar om de database op te zetten. Execute het volgende commando in de map waar ook je appsettings.cs staat:

`dotnet ef database update`

<br>

## Het opzetten van de API

We hebben nu een ApiScope gedefinieerd, echter "wijst" dit nog nergens naar toe, er bestaat immers nog geen api die wij
kunnen aanroepen.

Voer in de ```src``` folder het volgende commando uit om een nieuwe WebApi aan te maken en deze meteen aan de solution
toe te voegen:

```powershell
dotnet new webapi -n Api
cd ..
dotnet sln add .\src\Api\Api.csproj
```

Hierna moet de JwtBearer Nuget package worden toegevoegd aan de api, je kunt het volgende commando gebruiken, dit moet
wel in de api folder.

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

<br></br>
<b>Pas vervolgens in pproperties/launchSettings.json de aplicationUrl op line 23 aan
naar ```"applicationUrl": "http://localhost:6001",```.</b>

Er is al een controller voor ons gemaakt, de WeatherForecastController. Ga naar het bestand WeatherForecastController.cs
en pas ```[Route("[controller]")]``` aan naar ```[Route("weatherforecast")]```.<br>

Voeg dan bovenaan het bestand ```using Microsoft.AspNetCore.Mvc;``` toe.<br>
Hierna moet onder ```[Route("weatherforecast")]``` ```[Authorize]``` toe worden gevoegd.

We zullen deze controller voor een groot deel overnemen in onze 'eigen' controller die we willen alleen toegankelijk
willen hebben voor administrators. Maak een nieuwe controller aan in de Controllers map, genaamd CarController. Zorg dat
de controller er als volgt uitziet:

```csharp

    [ApiController]
    [Route("cars")]
    [Authorize(Roles = "Admin")]
    public class CarController : Controller
    {
        private static readonly string[] Cars =
        {
            "Polestar 2", "VW ID4", "Audi A6 E-tron", "Tesla Model S", "Skoda Enyaq"
        };

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return Cars.ToArray();
        }
    }

```

Belangrijk hierbij is `[Authorize(Roles = "Admin")]`. Hierbij geven we aan dat deze controller alleen benaderd mag
worden door gebruikers met de rol 'Admin'.

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

Haal vervolgens in ```Configure()``` het ```if statement``` weg, deze wordt niet gebruikt. Ook de
regel `app.UseHttpsRedirection()` moet weggehaald worden. Als laatst moet onder ```app.UseRouting();```

```csharp
app.UseAuthentication();
```

worden toegevoegd.

Door dit toe te voegen is de Authenticatie middelware toegevoegd aan de pipeline. Hierdoor wordt de inkomende token
gevalideerd en tevens gecontroleerd of de token gebruikt mag worden met deze API.

Start de applicatie en navigeer naar ```http://localhost:6001/weatherforecast```. Als alles goed is gegaan zul je
een ```401: unauthorized``` statuscode krijgen, de api is nu beschermd.

## Authorisatie toevoegen aan de API

Het volgende wat moet gebeuren is zorgen dat er toegang kan worden verschaft aan de API met behulp van Identity Server,
terwijl er ook gecontroleerd wordt of het request de resource mag bezoeken. Hiervoor wordt ASP.NET Core Authorization
Policy gebruikt.

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

## Registratie toevoegen aan Identity Server

Het gebruikte template van Identity Server biedt reeds een UI om in te loggen en uit te loggen. Wat we hier echter aan
toe willen voegen, is een registratie-systeem.

Maak allereerst een nieuw model aan in het IdentityServer project genaamd "Registration". Voeg de volgende velden en
annotaties toe:

```csharp
    public class Registration
    {
        public string Username { get; set; }
        
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        
        [DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
```

Dit is een heel gewoon POCO (Plain old C# Object). De annotaties zijn misschien nieuw.

Required spreekt voor zich. Verder representeert `DataType(DataType.Password)` een wachtwoord die niet in de UI getoond
wordt. `Compare(nameof(Password))`
vergelijkt 'ConfirmPassword' met 'Password'. Als deze niet overeenkomen, faalt de registratie.

Maak vervolgens een nieuwe map "Controller" en een bestand genaamd "UserController.cs" aan. Laat de klasse overerven
van 'Controller'.

Voeg als eerste de get toe voor Register. Dit is de simpelste methode van de controller, die alleen een view
retourneert:

```csharp
[HttpGet]
        public ViewResult Register()
        {
            return View("register");
        }
```

Om ons voor te bereiden op de volgende stap, moeten we wat extra logica toevoegen aan de controller. We zullen
gebruikmaken van de UserManager van IdentityServer. Om deze in onze controller te gebruiken, maken we gebruik van
Dependency Injection. Voeg boven je Get-functie de volgende code toe:

```csharp
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
```

Ook willen we vast de logica uitprogrammeren omtrent hte afhandelen van het registratieformulier. Hiervoor maken we de
volgende functie:

```csharp

[HttpPost]
        public async Task<IActionResult> Register([FromForm] Registration registration)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = registration.Username
                };

                var createResult = await _userManager.CreateAsync(user, registration.Password);

                if (createResult.Succeeded)
                    return RedirectToAction("Index", "Home");

                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View("register");
        }

```

Dit lijkt veel, maar het valt wel mee. Deze post-route heeft ons nieuwe Registration-model uit de formdata als
parameter. We checken of het meegegeven model correct is volgens onze gestelde eisen. Als dat zo is, wordt er een nieuwe
instantie van IdentityServer's ApplicationUser aangemaakt, met als username de username die de gebruiker heeft ingevuld
in het registratie-formulier. We doen een poging om dit op te slaan in de database door gebruik te maken van de
geinjecteerde userManager. Als hier het "succeeded"-resultaat uitkomt, is alles goed gegaan en sturen we de gebruiker
door naar de Home-controller.

Is er iets misgegaan? Dan sturen we de gebruiker terug naar de pagina met de eventuele foutmeldingen die opgetreden
zijn.

Nu de front-end. Maak een map "User" aan onder het mapje "Views" en maak daarin een nieuw bestand genaamd "
Register.cshtml". Voeg daaraan de volgende code toe:

```html

@model IdentityServer.Models.Registration
@{
ViewBag.Title = "Register";
}

<div>
    <div class="page-header">
        <h1>Registration</h1>
    </div>

    <div class="col-sm-6">
        <form method="post" asp-controller="User" asp-action="Register">
            <div asp-validation-summary="ModelOnly"></div>

            <div class="form-floating mb-3">
                <label asp-for="Username"></label>
                <input asp-for="Username" type="text" placeholder="Username" class="form-control">
                <span asp-validation-for="Username" class="invalid-feedback"></span>
            </div>
            <div class="form-floating mb-3">
                <label asp-for="Password"></label>
                <input asp-for="Password" type="password" placeholder="Password" class="form-control">
                <span asp-validation-for="Password" class="invalid-feedback"></span>
            </div>
            <div class="form-floating mb-3">
                <label asp-for="ConfirmPassword"></label>
                <input asp-for="ConfirmPassword" type="password" placeholder="Confirm Password" class="form-control">
                <span asp-validation-for="ConfirmPassword" class="invalid-feedback"></span>
            </div>

            <button type="submit" class="btn btn-primary">Register</button>
        </form>
    </div>
</div>

```

Dit is vrij eenvoudige HTML, daarom zal hier verder niet op ingegaan worden.

Ga tot slot naar het bestand "Views/Account/Login.cshtml". Vervang daar

```html
<p>The default users are alice/bob, password: Pass123$</p>
```

door

```html
<a href="/User/Register">Register</a>
```

<br>

## MVC Client toevoegen

Het laatste wat toegevoegd moet worden voordat wij een applicatie hebben die gebruik maakt van Identity Server om een
api te bezoeken is een MVC client.

Dit kunnen wij doen door in de ```src``` folder de volgende commando's uit te voeren:

```powershell
dotnet new mvc -n MvcClient
cd ..
dotnet sln add .\src\MvcClient\MvcClient.csproj
```

Hierna moet nog de OpenIdConnect Nuget package worden toegevoegd aan het MvcClient project, dit kan met het vollgende
commando:

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

<br>
<b>Verander vervolgens de applicationUrl onder het kopje 'MvcClient' op line 23 in properties/launchsettings.json naar 

```json
"applicationUrl": "http://localhost:5002",
```

</b>

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

Wat deze code doet is het gebruik maken van een cookie om de gebruiker in te loggen, daarnaast wordt nu het OpenID
Connect protocol gebruikt. De ```options.Authority``` wijst naar de Identity Server.

Ga nu naar de methode ```Configure()``` en voeg onder ```app.UseRouting()```

```
app.UseAuthentication();
```

toe.

<b>Verwijder daarnaast app.UseHttpsRedirection()</b>

Als laatst moet de ```app.UseEndpoints``` aanroep worden aangepast zodat er gebruik wordt gemaakt van authorisatie,
vervang de ```app.UseEndpoints``` met:

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

Dit zorgt er voor dat de lokale cookie wordt verwijderd, tevens wordt er een bericht gestuurd naar Identity Server dat
de gebruiker is uitgelogd en zal deze ook de cookie verwijderen, de gebruiker is nu officieel uitgelogt.

Om gemakkelijk uit te loggen kun je in ```_Layout.cshtml``` in de Navbar (onder het list item van privacy) het volgende
toevoegen:

```html

<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Logout">Logout</a>
</li>
```

<br>

## Config.cs van Identity Server aanpassen

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

Nu is de MvcClient toegevoegd aan de lijst van Clients voor de Identity Server. Daarnaast zal de MvcClient nu
doorgestuurd worden naar de inlog (of uitlog) pagina als deze wel of niet ingelogd is. De MvcClient kan nu enkel bij de
scopes die geleverd zijn door OpenId Connect, dat zijn dus de OpenId scope en de Profile Scope. Een andere scope die de
MvcClient moet kunnen bezoeken is de ```api1``` scope. Voeg deze toe aan de lijst van AllowedScopes.

## De MvcClient de API laten aanroepen

We zijn er bijna, het laatste wat er moet gebeuren is het mogelijk maken voor de MvcClient om de API aan te roepen.

Ga naar ```Startup.cs``` en voeg in de methode ```ConfigureServices()``` <b>onderaan</b> in de aanroep
van ```services.AddAuthentication()```:

```csharp
        options.Scope.Add("api1");
```

toe. De MvcClient weet nu dat deze de scope met naam "api1" mag bezoeken.

Echter is er nog geen manier om een request te versturen naar de API. Ga naar ```Controllers/HomeController.cs``` en
voeg het volgende toe:

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

Ook voor de administrator-gegevens (auto's) maken we een soortgelijke methode aan:

```csharp
        public async Task<IActionResult> Cars()
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = await client.GetStringAsync("http://localhost:6001/cars");

            ViewBag.Json = JArray.Parse(content).ToString();

            return View("json");
        }
```

Om dalijk gemakkelijk deze methode aan te roepen zullen we aan de NavBar een element toevoegen die hier naar wijst. Voeg
aan ```Views/Shared._Layout.cshtml``` onder het list item van Privacy het volgende toe:

```html

<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Weather">Weather</a>
</li>
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Cars">Cars</a>
</li>
```

Het allerlaatste dat er moet gebeuren is het maken van een view om de data die we krijgen van de API te tonen. Maak een
nieuw bestand in de map ```Views/Home``` genaamd ```json.cshtml```. We willen enkel de gekregen data tonen, verwijder
alles uit het bestand en voeg de volgende regel toe:

```html

<pre>@ViewBag.Json</pre>
```

Start alle applicaties en navigeer naar ```http://localhost:5002/home/weather```, je zult moeten inloggen. Maak een
nieuw account aan en log je hier vervolgens mee in.

Je zult zien dat je toegang krijgt tot de applicatie, maar dat "Cars" nog een foutmelding geeft. Hier gaan we nu aan
werken.

## Rol en administrator maken

Om de rol van Admin en een administrator-gebruiker te maken, gaan we een deel aan de Startup.cs van IdentityServer toevoegen. Open het bestand en voeg
de volgende methode onder "Configure" toe:

```csharp

private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var doesAdminRoleExist = await roleManager.RoleExistsAsync("Admin");

            if (!doesAdminRoleExist)
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var administrator = await userManager.FindByNameAsync("admin");
            if (administrator == null)
            {
                await userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin"
                }, "AdminPassword123!");

            }
            
            var storedAdministrator = await userManager.FindByNameAsync("admin");


            await userManager.AddToRoleAsync(storedAdministrator, "Admin");
        }

```

Deze code checkt of de administrator en de administrator rol bestaan. Als die niet bestaan, worden deze hierbij aangemaakt. Daarna wordt (wanneer dat nog niet zo is) de rol toegewezen aan de administrator.

Scroll naar boven tot je bij het begin van de Configure-methode bent. Zorg dat hier de IServiceProvider wordt geinjecteerd en dat onze nieuwe methode wordt aangeroepen. Het begin van Configure ziet er nu zo uit:

```csharp
        public void Configure(IApplicationBuilder app, IServiceProvider provider)
        {
            CreateRoles(provider).Wait();
```
## Toevoegen van de Rol-claim

We moeten de rol nu toevoegen aan de claims die in het token opgeslagen zijn. Om dit te doen, gaan we een interface van IdentityServer implementeren. Maak een nieuwe map genaamd "Services" aan in de hoofdmap van IdentityService. Maak hierin een bestand aan genaamd "ProfileService.cs". Voeg hier de volgende code aan toe:

```cshtml
 class ProfileService : IProfileService
    {
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var roleClaims = context.Subject.FindAll(JwtClaimTypes.Role);
            context.IssuedClaims.AddRange(roleClaims);
            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            return Task.CompletedTask;
        }
    }
```

Dit bestand moet vervolgens in de configuratie van IdentityService worden geladen. Ga naar het Startup.cs bestand van IdentityService en voeg de volgende code toe, helemaal onder aan de ConfigureServices methode:
`services.AddTransient<IProfileService, ProfileService>();`

## API om laten gaan met Claims

Om de API ook van de claims die we net toegevoegd hebben af te laten weten, moeten we hier ook een klein stukje configuratie in toevoegen. Ga naar de Config.cs van IdentityService. 

Voeg aan de lijst met IdentityResources de volgende code toe:

`new IdentityResource("roles", new[] { "role" })`. Voeg daarna aan de allowedScopes van de client in de lijst met clients de volgende scope toe: `"roles"`. 

We hebben nu de laatste IS4 wijzigingen gemaakt. Tijd voor de laatste stap.

## Toepassen van roles in de MVCClient

In deze laatste stap voegen we de nieuwe claims toe aan de MVC Client. Open de startup.cs van dit bestand en voeg onder `options.Scope.Add("api1");` in ConfigureServices het volgende toe:

```csharp

options.Scope.Add("roles");
options.ClaimActions.MapJsonKey("role", "role", "role");
options.TokenValidationParameters.RoleClaimType = "role";

```

Start alle projecten op nieuw op, log je uit in de MVC client en log je in met het Admin account (zie inloggegevens in de Startup.cs van IdentityServer). Klink nu op de "Cars" resource. Je zult nu resultaat zien.
