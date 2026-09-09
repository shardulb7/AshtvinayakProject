# 📦 Deployment Notes — Ashtavinayak Travel App

> Written on: 9 September 2026  
> Deployed by: Ganesh (ganeshnmankar28@gmail.com)  
> Live URL: https://ashtavinayak-api.azurewebsites.net

---

## 🧠 What Is This Project?

This project is a **backend-only ASP.NET Core 8 web application**. It has:

- A **REST API** — used by the frontend developer's mobile/web app
- An **Admin Panel** — MVC Razor views for managing tours, bookings, agents, etc.
- A **SQL Server database** — stores all data (users, bookings, packages, etc.)

There is **no separate frontend in this repository**. The frontend developer has his own project that calls our API.

---

## ☁️ Where Is It Hosted?

**Cloud Provider:** Microsoft Azure  
**Region:** Central India  
**Azure Account:** ganeshnmankar28@gmail.com

### Azure Resources Created

| Resource | Name | Type | Purpose |
|---|---|---|---|
| Resource Group | `rg-ashtavinayak` | Container | Groups all Azure resources together |
| App Service Plan | `asp-ashtavinayak` | B1 Linux | The server that runs our app (₹~1,400/month) |
| Web App | `ashtavinayak-api` | App Service | Runs our .NET 8 application |
| SQL Server | `sql-ashtavinayak` | Azure SQL Server | Database engine |
| SQL Database | `AshtavinayakDB` | Azure SQL DB Basic | Stores all app data (2GB, ₹~500/month) |

**Total estimated cost: ~₹1,900/month**  
*(You have ₹13,400 free Azure credit — app runs free for ~7 months)*

---

## 🔐 Important Credentials (Keep Secret!)

> ⚠️ Never commit these to GitHub. Store them safely.

| What | Value |
|---|---|
| Azure login | ganeshnmankar28@gmail.com |
| SQL Server admin user | `sqladmin` |
| SQL Server admin password | `Ashta@Vinayak2026!` |
| Admin panel username | `admin` |
| Admin panel password | The password that matches the BCrypt hash in `appsettings.Development.json` |
| JWT Secret | `Ashta$Vinayak@JWT#Secret2026!XyZ987PqR` |

All of these are stored as **App Service Environment Variables** in Azure Portal → App Service → Configuration → Application Settings.

---

## 📋 Step-by-Step: What We Did

### Step 1 — Created Azure Account
- Went to azure.microsoft.com/en-in/free
- Signed up with a Microsoft account
- Added credit card for verification (no charge — ₹13,400 free credit)

### Step 2 — Installed Azure CLI
- Installed via: `winget install --id Microsoft.AzureCLI`
- This lets us control Azure from the command line (PowerShell)

### Step 3 — Logged into Azure CLI
```powershell
az login
# A browser window opened → signed in with Azure account
```

### Step 4 — Registered Azure Resource Providers
New Azure accounts need to "activate" Microsoft's services before using them:
```powershell
az provider register --namespace Microsoft.Sql --wait
az provider register --namespace Microsoft.Web --wait
```

### Step 5 — Created All Azure Resources
```powershell
# Resource Group (a folder that holds everything)
az group create --name rg-ashtavinayak --location centralindia

# SQL Server (the database engine)
az sql server create --name sql-ashtavinayak --resource-group rg-ashtavinayak --location centralindia --admin-user sqladmin --admin-password "Ashta@Vinayak2026!"

# The actual database inside the server
az sql db create --name AshtavinayakDB --server sql-ashtavinayak --resource-group rg-ashtavinayak --service-objective Basic

# Allow Azure services to connect to the database
az sql server firewall-rule create --server sql-ashtavinayak --resource-group rg-ashtavinayak --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# App Service Plan (defines server size = B1)
az appservice plan create --name asp-ashtavinayak --resource-group rg-ashtavinayak --location centralindia --sku B1 --is-linux

# The Web App itself
az webapp create --name ashtavinayak-api --resource-group rg-ashtavinayak --plan asp-ashtavinayak --runtime "DOTNETCORE:8.0"
```

### Step 6 — Set Environment Variables (Secrets) on Azure
Instead of putting secrets in code, we set them as environment variables on the App Service:
```powershell
az webapp config appsettings set --name ashtavinayak-api --resource-group rg-ashtavinayak --settings \
  "ASPNETCORE_ENVIRONMENT=Production" \
  "ConnectionStrings__DefaultConnection=Server=sql-ashtavinayak.database.windows.net;..." \
  "JwtSettings__Issuer=AshtavinayakApp" \
  "JwtSettings__Audience=AshtavinayakApp-Users" \
  "JwtSettings__SecretKey=..." \
  "SmsGateway__User=iTasT" \
  "SmsGateway__Password=..." \
  "Admin__Username=admin" \
  "Admin__PasswordHash=..."
  # (and more)
```

> **Why double underscores `__`?**  
> ASP.NET Core maps `JwtSettings__SecretKey` → `JwtSettings:SecretKey` automatically.
> It's just how you write nested config keys as environment variables.

### Step 7 — Added Auto-Migration to the App
We added code to `Program.cs` so that every time the app starts, it automatically applies any new database migrations. This means:
- On first deploy: all tables were created automatically
- On future deploys: new tables/columns are added automatically
- No manual `dotnet ef database update` needed on the server

```csharp
// In Program.cs, after app is built:
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AshtvinayakTravelContext>();
await db.Database.MigrateAsync();
```

### Step 8 — Built the App
```powershell
dotnet publish AshtavinayakApp/AshtavinayakAPP.csproj -c Release -o ./publish
```
This compiles the app into a folder ready to run on Linux.

### Step 9 — Created a Linux-Compatible Zip
PowerShell's default zip uses Windows backslashes (`\`) in file paths, which Linux rejects.
We used .NET's ZipFile class to create a zip with forward slashes (`/`):
```powershell
Add-Type -Assembly System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open("app.zip", "Create")
Get-ChildItem ./publish -Recurse -File | ForEach-Object {
    $linuxPath = $_.FullName.Substring($sourceDir.Length + 1).Replace('\', '/')
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $linuxPath)
}
$zip.Dispose()
```

### Step 10 — Enabled Basic Auth on Azure SCM
New Azure accounts have this disabled by default. We enabled it so our deploy tool could authenticate:
```powershell
az resource update \
  --resource-group rg-ashtavinayak \
  --name "ashtavinayak-api/basicPublishingCredentialsPolicies/scm" \
  --resource-type "Microsoft.Web/sites" \
  --set properties.allow=true
```

### Step 11 — Deployed the Zip to Azure
```powershell
# Get deploy credentials
$creds = az webapp deployment list-publishing-credentials --name ashtavinayak-api ...

# Upload zip directly to Kudu (Azure's deployment engine) using curl
curl.exe -X POST \
  --user "$($creds.user):$($creds.pass)" \
  --data-binary "@app.zip" \
  --header "Content-Type: application/zip" \
  "https://ashtavinayak-api.scm.azurewebsites.net/api/zipdeploy"
# Result: HTTP 200 ✅
```

### Step 12 — Verified It Works
```
GET https://ashtavinayak-api.azurewebsites.net/health/ready
→ 200 Healthy ✅
```
The "ready" health check also verifies database connectivity — so this single check confirms everything is working.

### Step 13 — Created GitHub Actions CI/CD Workflow
Created `.github/workflows/azure-deploy.yml`:  
Every time you push code to `main` branch → GitHub automatically builds and deploys to Azure.
No manual deployment ever needed again (once GitHub secrets are configured).

---

## 🔄 How to Deploy in the Future

### Option A — Automatic (Recommended)
```
Just push your code to the main branch:

git add .
git commit -m "your message"
git push

→ GitHub Actions automatically builds & deploys to Azure
```
*Requires the 2 GitHub secrets to be set — see "What's Next" below.*

### Option B — Manual (if GitHub Actions isn't set up yet)
```powershell
# From the repo root in PowerShell:

# 1. Build
dotnet publish AshtavinayakApp/AshtavinayakAPP.csproj -c Release -o ./publish

# 2. Zip (Linux-compatible)
$sourceDir = (Resolve-Path ".\publish").Path
$zip = [System.IO.Compression.ZipFile]::Open("app.zip", "Create")
Get-ChildItem $sourceDir -Recurse -File | % {
    $p = $_.FullName.Substring($sourceDir.Length + 1).Replace('\','/')
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $p, "Fastest") | Out-Null
}
$zip.Dispose()

# 3. Get credentials and deploy
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$creds = az webapp deployment list-publishing-credentials --name ashtavinayak-api --resource-group rg-ashtavinayak --query "{user:publishingUserName, pass:publishingPassword}" --output json | ConvertFrom-Json
"machine ashtavinayak-api.scm.azurewebsites.net login $($creds.user) password $($creds.pass)" | Out-File deploy.netrc -Encoding ascii
curl.exe -X POST --netrc-file deploy.netrc --data-binary "@app.zip" --header "Content-Type: application/zip" "https://ashtavinayak-api.scm.azurewebsites.net/api/zipdeploy"
Remove-Item deploy.netrc
```

---

## 🗺️ What's Next?

### 🔴 Must Do (Before Frontend Can Fully Test)

- [ ] **Add GitHub Actions Secrets** — Go to:  
  https://github.com/shardulb7/AshtvinayakProject/settings/secrets/actions/new  
  Add secret: `AZURE_APP_NAME` = `ashtavinayak-api`  
  Add secret: `AZURE_WEBAPP_PUBLISH_PROFILE` = (download from Azure Portal → App Service → Get publish profile → paste the XML)  
  Once done, every push auto-deploys ✅

- [ ] **Share API URL with frontend developer:**  
  `https://ashtavinayak-api.azurewebsites.net`  
  He uses this as the base URL for all his API calls.

- [ ] **Test the Admin Panel:**  
  Open https://ashtavinayak-api.azurewebsites.net → log in as admin  
  Verify you can see packages, bookings, agents, etc.

### 🟡 Do Before Going Live with Real Users

- [ ] **Razorpay Live Keys** — Replace test keys with live keys in Azure Portal → App Service → Configuration:  
  `Razorpay__Key` = `rzp_live_XXXXXXXX`  
  `Razorpay__Secret` = your live secret

- [ ] **Set CORS to Frontend's URL** — Once the frontend is deployed, add its URL to App Service Configuration:  
  `Cors__AllowedOrigins__0` = `https://your-frontend-domain.com`  
  Right now CORS allows everything (fine for testing, not ideal for production).

- [ ] **Custom Domain** — Buy a domain (e.g. `ashtavinayak.in`) and map it to the App Service.  
  Azure Portal → App Service → Custom Domains → Add custom domain

- [ ] **HTTPS on Custom Domain** — Azure provides free SSL certificates for custom domains.  
  Azure Portal → App Service → TLS/SSL settings → Managed Certificate

- [ ] **Upgrade Plan if Needed** — B1 is fine for testing. Once you have real users:  
  - B2 (₹2,800/month) for moderate traffic  
  - S1 Standard (₹4,200/month) for auto-scaling support

### 🟢 Nice to Have

- [ ] **Set up Azure Monitor / Alerts** — Get email alerts if the app crashes or goes slow

- [ ] **Backups for SQL Database** — Azure SQL Basic tier has 7-day automatic backups already. Consider upgrading for longer retention.

- [ ] **Separate Staging Environment** — A second App Service slot for testing before pushing to production.

---

## 🚨 If Something Goes Wrong

### App is down / not responding
```powershell
# Check if app is running
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
az webapp show --name ashtavinayak-api --resource-group rg-ashtavinayak --query state --output tsv

# Restart the app
az webapp restart --name ashtavinayak-api --resource-group rg-ashtavinayak

# View live logs
az webapp log tail --name ashtavinayak-api --resource-group rg-ashtavinayak
```

### Database issues
```powershell
# Check health endpoint (includes DB check)
curl https://ashtavinayak-api.azurewebsites.net/health/ready
```

### View app logs in Azure Portal
Azure Portal → App Service (`ashtavinayak-api`) → Log stream (left sidebar)

---

## 📞 Key Links

| What | URL |
|---|---|
| Azure Portal | https://portal.azure.com |
| Your App Service | https://portal.azure.com → search "ashtavinayak-api" |
| Live App | https://ashtavinayak-api.azurewebsites.net |
| Admin Panel | https://ashtavinayak-api.azurewebsites.net/ |
| Health Check | https://ashtavinayak-api.azurewebsites.net/health/ready |
| Kudu (deploy logs) | https://ashtavinayak-api.scm.azurewebsites.net |
| GitHub Repo | https://github.com/shardulb7/AshtvinayakProject |
| GitHub Actions | https://github.com/shardulb7/AshtvinayakProject/actions |
| GitHub Secrets | https://github.com/shardulb7/AshtvinayakProject/settings/secrets/actions |
