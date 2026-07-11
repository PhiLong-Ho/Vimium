# Code Signing & Publishing — resolving the SmartScreen "unrecognized app" warning

When a user downloads `Vimium.exe` from a GitHub release and runs it, Microsoft
Defender SmartScreen shows:

> **Windows protected your PC** — Microsoft Defender SmartScreen prevented an
> unrecognized app from starting. Running this app might put your PC at risk.

This document explains **why** that happens and **every practical way to fix it**,
from free (internal-only) to the modern recommended path for public downloads.

> TL;DR — SmartScreen trusts apps by the **identity of the code-signing
> certificate** they are signed with (plus accumulated download reputation). Our
> releases are currently **unsigned**, so every new build starts from zero
> reputation and is flagged. Sign the exe with a validated certificate and the
> warning goes away (immediately with EV / Azure Trusted Signing, or as
> reputation accrues with a standard OV certificate).

---

## 1. Why Vimium is flagged

SmartScreen reputation is keyed on two things:

1. **The file hash** — an unsigned binary earns reputation only for that exact
   hash. Every rebuild changes the hash and resets reputation to zero. A
   fast-moving project therefore never accumulates trust.
2. **The signing certificate identity** — a *signed* binary inherits the
   reputation of its **publisher certificate**, which persists across builds.
   This is why signing is the real fix: reputation follows the publisher, not the
   file.

Because our published `Vimium.exe` has **no Authenticode signature**, SmartScreen
has no publisher identity to trust and falls back to per-hash reputation (≈ none
for a fresh release) → the "unrecognized app" prompt.

Two things also worth knowing:

- The exe is a **self-contained single-file** build (native WPF libs are bundled
  and self-extracted). You only need to sign the **one** `Vimium.exe` — there are
  no loose DLLs to sign.
- Making admin mode **opt-in** (v1.4.1, default `RunAsAdministrator = false`)
  already reduces friction: the app no longer requests elevation on launch, so an
  unsigned build no longer triggers *both* a SmartScreen prompt *and* a UAC
  elevation prompt on an unrecognized binary. Signing removes the remaining
  SmartScreen prompt.

---

## 2. Options at a glance

| Option | Cost | Hardware token? | Clears public SmartScreen? | Best for |
|--------|------|-----------------|----------------------------|----------|
| **A. Azure Trusted Signing** | ~US$10/month | No (cloud HSM) | **Yes** — fast reputation, chains to Microsoft-trusted roots | ✅ Recommended for public GitHub downloads |
| **B. EV code-signing cert** | ~US$250–650/yr | Yes (USB token / cloud HSM) | **Yes** — immediate reputation | Vendors who want instant trust and already have a token |
| **C. OV (standard) code-signing cert** | ~US$150–400/yr | Yes (since Jun 2023) | Eventually — reputation builds over downloads/time | Budget publishers who can wait out reputation |
| **D. Self-signed cert + GPO trust** | Free | No | **No** (public) / Yes (managed fleet) | ✅ Enterprise internal distribution only |
| **E. Do nothing** | Free | — | No | Users click *More info → Run anyway* |

Recommendation:

- **Distributing publicly on GitHub** → **A. Azure Trusted Signing**. It is the
  cheapest way to get real SmartScreen trust without buying a hardware token, and
  Microsoft operates the HSM for you.
- **Deploying only inside one or more enterprises you control** → **D.
  Self-signed + GPO**. Free, and those orgs can trust your publisher cert fleet-wide.
- **You already own an EV token** → **B**.

---

## 3. Path A — Azure Trusted Signing (recommended for public releases)

Azure Trusted Signing (formerly *Azure Code Signing*) is a Microsoft-managed
service: Microsoft holds the private key in its HSM, issues short-lived
certificates under a validated identity, and the signatures chain to roots that
Windows/SmartScreen already trust. No USB token, ~US$10/month (Basic SKU).

### 3.1 One-time setup

1. **Azure subscription** — create one at <https://portal.azure.com> if needed.
2. **Register the resource provider** `Microsoft.CodeSigning`.
3. **Create a Trusted Signing account** (region e.g. `EastUS`), Basic or Premium
   SKU.
4. **Create an Identity Validation** and complete verification:
   - *Public* type → validates your legal identity/organization (this identity is
     what appears as the publisher). Individuals are supported; validation can
     take a few business days.
5. **Create a Certificate Profile** (e.g. `Public Trust`) under the account. This
   is the profile name you sign with.
6. Grant your signing principal (a service principal / GitHub OIDC identity) the
   **Trusted Signing Certificate Profile Signer** role on the account.

### 3.2 Sign locally

Install the tooling and sign the published exe:

```powershell
# One-time: the Trusted Signing dlib + signtool (Windows SDK) must be available.
dotnet tool install --global sign            # cross-platform 'sign' CLI (optional)

# Publish the portable exe first (see §6), then sign it:
sign code trusted-signing `
  publish\win-x64\Vimium.exe `
  --trusted-signing-account   "<account-name>" `
  --trusted-signing-cert-profile "<profile-name>" `
  --trusted-signing-endpoint  "https://eus.codesigning.azure.net" `
  --description "Vimium" `
  --description-url "https://github.com/PhiLong-Ho/Vimium"
```

The `sign` tool timestamps automatically. Authentication uses the standard Azure
credential chain (`az login`, environment vars, or OIDC in CI).

> Alternative CLIs: the official `Invoke-TrustedSigning` PowerShell module, or
> `signtool.exe` with the Trusted Signing **dlib** (`/dlib` + `/dmdf` metadata).

### 3.3 In CI

The included workflow `.github/workflows/release.yml` is wired for this path via
GitHub OIDC (no long-lived secret) — see §7.

---

## 4. Path B / C — Traditional certificate + `signtool`

Buy a code-signing certificate from a CA (DigiCert, Sectigo, SSL.com, GlobalSign,
Certum — Certum offers budget individual OV certs). Since **June 2023** all newly
issued standard (OV) and EV code-signing keys must live on **FIPS 140-2 Level 2+
hardware** (a USB token or a cloud HSM the CA provisions) — you cannot get a plain
`.pfx` download for a fresh OV/EV cert anymore.

- **EV** → SmartScreen reputation is effectively immediate.
- **OV** → the warning persists until enough users download & run the signed
  build; reputation then transfers to all future builds signed with the same cert.

### 4.1 Sign with `signtool.exe`

`signtool` ships with the Windows SDK
(`C:\Program Files (x86)\Windows Kits\10\bin\<ver>\x64\signtool.exe`).

**From a hardware token / certificate store (by thumbprint):**

```powershell
signtool sign `
  /sha1 <CERT_THUMBPRINT> `
  /fd SHA256 `
  /tr http://timestamp.digicert.com `
  /td SHA256 `
  /d "Vimium" `
  publish\win-x64\Vimium.exe
```

**From a `.pfx` (only possible for legacy certs or test/self-signed):**

```powershell
signtool sign `
  /f Vimium-codesign.pfx `
  /p "$env:PFX_PASSWORD" `
  /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `
  /d "Vimium" `
  publish\win-x64\Vimium.exe
```

**From Azure Key Vault (cloud HSM cert):** use
[`AzureSignTool`](https://github.com/vcsjones/AzureSignTool):

```powershell
dotnet tool install --global AzureSignTool
AzureSignTool sign `
  --azure-key-vault-url "https://<vault>.vault.azure.net" `
  --azure-key-vault-certificate "<cert-name>" `
  --azure-key-vault-managed-identity `        # or --azure-key-vault-client-id/secret
  --file-digest sha256 `
  --timestamp-rfc3161 http://timestamp.digicert.com `
  --timestamp-digest sha256 `
  publish\win-x64\Vimium.exe
```

> **Always timestamp** (`/tr` + `/td`, or `--timestamp-rfc3161`). A timestamped
> signature stays valid after the certificate expires; an un-timestamped one
> becomes invalid the moment the cert lapses.

---

## 5. Path D — Self-signed certificate for internal enterprise deployment

Free, and appropriate when Vimium is deployed **only** to machines an organization
manages. It does **not** clear SmartScreen for the general public (their machines
don't trust your self-signed root), but a managed fleet can be told to trust it.

### 5.1 Create the certificate

```powershell
# Creates a code-signing cert in the current user's store.
$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject "CN=Vimium (Internal), O=<YourOrg>" `
  -KeyUsage DigitalSignature `
  -KeyAlgorithm RSA -KeyLength 3072 `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -NotAfter (Get-Date).AddYears(3)

# Export the PUBLIC cert (.cer) for fleet distribution — never export the key.
Export-Certificate -Cert $cert -FilePath .\Vimium-Internal.cer
```

### 5.2 Sign

```powershell
Set-AuthenticodeSignature `
  -FilePath publish\win-x64\Vimium.exe `
  -Certificate $cert `
  -TimestampServer http://timestamp.digicert.com
```

### 5.3 Trust it fleet-wide (domain admin)

Distribute `Vimium-Internal.cer` via **Group Policy** to managed machines:

- **Computer Configuration → Policies → Windows Settings → Security Settings →
  Public Key Policies**
  - Import to **Trusted Publishers** (so the signature is accepted), and
  - Import to **Trusted Root Certification Authorities** (so the chain validates).

Once deployed, the signed `Vimium.exe` is trusted on those machines and can also
be allow-listed in **AppLocker / Windows Defender Application Control (WDAC)** by
publisher — a common enterprise requirement.

---

## 6. Building the portable exe (what you sign)

Sign the **final published** single-file exe, not the `bin\` build output:

```powershell
dotnet publish src\Vimium\Vimium.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -o publish\win-x64
# → publish\win-x64\Vimium.exe   (sign THIS file)
```

`IncludeNativeLibrariesForSelfExtract=true` is already set in the csproj, so native
WPF libraries are bundled inside the exe. There is exactly one artifact to sign.

---

## 7. Verifying the signature

```powershell
# Quick check
Get-AuthenticodeSignature publish\win-x64\Vimium.exe | Format-List Status, SignerCertificate

# Thorough check (chain + policy), via signtool
signtool verify /pa /v publish\win-x64\Vimium.exe
```

`Status` should be `Valid` and the signer subject should match your identity. A
`NotSigned` / `UnknownError` means signing did not apply.

---

## 8. Publishing a signed release

Order matters — **build → sign → verify → upload**:

```powershell
# 1. Build
dotnet publish src\Vimium\Vimium.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64
# 2. Sign  (pick a path from §3–5)
# 3. Verify (§7)
# 4. Create the release with the SIGNED exe
gh release create v1.4.1 --repo PhiLong-Ho/Vimium `
  --title "Vimium v1.4.1" --notes-file notes.md `
  "publish\win-x64\Vimium.exe#Vimium.exe (portable, signed)"
```

CI equivalent: `.github/workflows/release.yml` runs this automatically on a
`v*` tag push. It is pre-wired for **Azure Trusted Signing via GitHub OIDC**;
fill in the repository variables/secrets it references (see comments in the
workflow) or comment that step out to publish unsigned.

---

## 9. SmartScreen reputation notes

- **EV / Azure Trusted Signing** → trust is effectively immediate because the
  identity is strongly validated and chains to roots Microsoft already trusts.
- **OV cert** → the warning may persist for the first days/weeks until downloads
  accumulate. Reputation then attaches to the **certificate**, so every later
  release signed with the same cert is trusted from its first download.
- **Keep the same certificate** across releases — switching certs restarts
  reputation.
- You can proactively submit the signed binary to Microsoft for analysis at
  <https://www.microsoft.com/wdsi/filesubmission> to speed things up.
- Signing does **not** bypass SmartScreen for genuinely low-reputation/malicious
  files; it establishes a persistent, accountable publisher identity.

---

## 10. FAQ

**Do I have to sign every DLL?**
No — the release is a single self-contained exe; sign `Vimium.exe` only.

**Can I sign on Linux CI?**
Azure Trusted Signing and `AzureSignTool` are cross-platform (.NET). Classic
`signtool.exe` needs Windows. The provided workflow runs on `windows-latest`.

**Is a `.pfx` enough for public trust?**
Only legacy/test certs come as `.pfx`. New OV/EV keys are hardware-bound; use the
token, Azure Key Vault, or Azure Trusted Signing instead.

**Cheapest path to clear the public warning?**
Azure Trusted Signing (~US$10/month), no hardware token.

**We only ship to our own company machines.**
Use a self-signed cert (§5) and trust it via GPO — free, and it also enables
AppLocker/WDAC publisher allow-listing.
