# build-service-classfilter.ps1
#
# Generates a reportgenerator "classfilters" argument value that includes ONLY
# the concrete service classes inside ECS.Application/Services/. Excludes
# interfaces (I*.cs), request/response/validator DTOs, and helper classes.
#
# Then runs `reportgenerator` to produce a service-only HTML coverage report.
#
# Usage (called from run-tests.bat):
#   powershell -NoProfile -ExecutionPolicy Bypass -File build-service-classfilter.ps1 ^
#       -CoverageFile "<absolute path to coverage.cobertura.xml>" ^
#       -TargetDir "<absolute path to output directory>" ^
#       -ServicesRoot "<absolute path to ECS.Application\Services>" ^
#       -ReportGeneratorPath "<absolute path to reportgenerator.exe>"
#
# Returns:
#   exit code 0 on success, 1 on failure.

param (
    [Parameter(Mandatory = $true)]
    [string]$CoverageFile,

    [Parameter(Mandatory = $true)]
    [string]$TargetDir,

    [Parameter(Mandatory = $true)]
    [string]$ServicesRoot,

    [Parameter(Mandatory = $false)]
    [string]$ReportGeneratorPath = "reportgenerator"
)

# ---------------------------------------------------------------------------
# 0. Resolve all paths to absolute form.
#    reportgenerator compares class names against the cobertura <class> entries,
#    which are stored as fully-qualified minus the assembly name, e.g.
#    "ECS.Application.Services.AuthServices.LoginServices.LoginService".
#    If $ServicesRoot is passed as a relative path (e.g. "..\ECS.Application\Services"),
#    `$file.FullName` is absolute and ($file.FullName).Substring($ServicesRoot.Length)
#    chops the absolute path and produces malformed names like
#    "ECS.Application.Services.ct.BE.api.ECSBackendAPI.ECSBackendAPI.ECS.Application.Services.X.Y".
#    Resolve to absolute upfront so Substring() works on the same prefix.
# ---------------------------------------------------------------------------
try {
    $ServicesRootAbs = (Resolve-Path -LiteralPath $ServicesRoot -ErrorAction Stop).ProviderPath
} catch {
    Write-Host "[service-classfilter] FATAL: ServicesRoot '$ServicesRoot' does not exist." -ForegroundColor Red
    exit 1
}
$resolvedCoverage = Resolve-Path -LiteralPath $CoverageFile -ErrorAction SilentlyContinue
if ($resolvedCoverage) {
    $CoverageFile = $resolvedCoverage.ProviderPath
} else {
    Write-Host "[service-classfilter] FATAL: CoverageFile '$CoverageFile' not found." -ForegroundColor Red
    exit 1
}
$TargetDirAbs = [System.IO.Path]::GetFullPath($TargetDir)

# ---------------------------------------------------------------------------
# 1. Enumerate .cs files directly under the Services root (recursive).
# ---------------------------------------------------------------------------
$allCs = Get-ChildItem -Path $ServicesRootAbs -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue

# ---------------------------------------------------------------------------
# 2. Apply inclusion rules to keep ONLY concrete service classes:
#    - Drop SmtpEmailService.cs (CLI helper, not a domain service).
#    - Drop interfaces (I*Service.cs).
#    - Keep only concrete classes ending with "Service.cs".
# ---------------------------------------------------------------------------
$serviceFiles = $allCs | Where-Object {
    $name = $_.Name
    if ($name -eq "SmtpEmailService.cs") { return $false }
    if ($name -like "I*Service.cs") { return $false }
    if ($name -like "*Service.cs") { return $true }
    return $false
}

# ---------------------------------------------------------------------------
# 3. Convert each file path to its full namespace + class name.
#    reportgenerator compares class names against the <class> entries in the
#    cobertura XML, which carry the assembly's runtime namespace
#    (e.g. "ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices.CreateStaffService").
#
#    The folder structure does NOT always mirror the namespace. In this
#    project several folders carry a "Clinic"/"Batch" prefix that the actual
#    namespace does not have, e.g.:
#      Folder:    ClinicAdminManagementServices\ClinicCreateStaffAccountServices\
#      Namespace: ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
#
#    Deriving the filter from the folder path produces names that don't exist
#    in the cobertura XML, and reportgenerator silently drops those classes
#    from the HTML report. To prevent that, read the `namespace` declaration
#    out of each .cs file and use it as the source of truth.
# ---------------------------------------------------------------------------
$filters = foreach ($file in $serviceFiles) {
    $namespace = $null
    $className = $null
    $lines = Get-Content -LiteralPath $file.FullName -TotalCount 200 -ErrorAction SilentlyContinue

    # Collect the namespace declaration across multiple lines. C# allows
    # multi-line declarations like:
    #     namespace ECS.Application.Services
    #         .ClinicDoctorDiscoveryService
    #         .ViewClinicProfileServices
    # and C# 10+ file-scoped namespaces like `namespace ECS.Application.X;`.
    # We stitch the lines together before matching so both forms work.
    $collected = ''
    foreach ($line in $lines) {
        $trimmed = $line.TrimEnd()
        if ($collected -eq '') {
            if ($trimmed -match '^\s*namespace\s+([\w\.]+)') {
                $collected = $Matches[1]
                if ($trimmed -match '[\{;]') { break }
            }
        } else {
            if ($trimmed -match '^\s*\.([\w\.]+)') {
                $collected = $collected + '.' + $Matches[1]
                if ($trimmed -match '[\{;]') { break }
            } elseif ($trimmed -match '[\{;]') {
                break
            } else {
                break
            }
        }
    }
    $namespace = $collected

    # Extract the actual concrete class name declared in the file. We look for
    # `public class <Name>` (with optional generic / inheritance suffixes
    # already covered by `:`) and capture the first match. This handles the
    # case where the file name does not match the class name (e.g.
    # CreateClinicService.cs declares `public class CreateService`).
    foreach ($line in $lines) {
        if ($line -match '^\s*public\s+(?:sealed\s+|partial\s+|abstract\s+)*class\s+([A-Za-z_]\w*)') {
            $className = $Matches[1]
            break
        }
    }
    if (-not $className) {
        Write-Host "[service-classfilter] WARN: no public class found in $($file.FullName) - skipping." -ForegroundColor Yellow
        continue
    }

    # Strip the ECS.Application.Services. prefix so we can rebuild the
    # reportgenerator class name from the *runtime* namespace, not the folder.
    $nsRelative = $namespace -replace '^ECS\.Application\.Services\.', ''
    $fullName = "ECS.Application.Services.$nsRelative.$className"
    "+{0}" -f $fullName
}

$joined = ($filters | Sort-Object) -join ";"
Write-Host "[service-classfilter] Mapped $($serviceFiles.Count) concrete service classes." -ForegroundColor DarkCyan
Write-Host "[service-classfilter] ServicesRoot   : $ServicesRootAbs" -ForegroundColor DarkCyan
Write-Host "[service-classfilter] Coverage file  : $CoverageFile" -ForegroundColor DarkCyan
Write-Host "[service-classfilter] Target dir     : $TargetDirAbs" -ForegroundColor DarkCyan
Write-Host "[service-classfilter] Filter length  : $($joined.Length) chars" -ForegroundColor DarkCyan

# Persist the filter to a file so the operator can inspect it if the report
# comes back empty. Also useful for debugging future regressions. We do this
# BEFORE the sanity check so the operator can always see the actual filter.
$debugDir = Join-Path $TargetDirAbs "_debug"
$null = New-Item -ItemType Directory -Path $debugDir -Force
$filterFile = Join-Path $debugDir "classfilter.txt"
Set-Content -LiteralPath $filterFile -Value $joined -Encoding UTF8

# Guard: a healthy filter for ~110 services is roughly 12 KB. If it explodes
# past ~30 KB, path resolution regressed (e.g. ServicesRoot was passed relative
# and not normalized) and we'd produce a 0-coverage report. Fail fast instead of
# silently shipping a broken report.
if ($joined.Length -gt 30000) {
    Write-Host "[service-classfilter] FATAL: filter string is unreasonably long ($($joined.Length) chars). Stopping to avoid producing a 0-coverage report." -ForegroundColor Red
    Write-Host "[service-classfilter] First 400 chars: $($joined.Substring(0, [Math]::Min(400, $joined.Length)))" -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# 4. Run reportgenerator with the dynamic class filter.
#    Uses array form of $args so the long filter string is passed as a single
#    argument without any cmd-line buffer truncation.
# ---------------------------------------------------------------------------
$reportTypes = "Html_Dark;Badges"
$title = "ECS Backend Service Coverage"
$assemblyFilter = "+ECS.*"
$classFilter = $joined

Write-Host "[service-classfilter] Running reportgenerator..." -ForegroundColor DarkCyan

& $ReportGeneratorPath `
    "-reports:$CoverageFile" `
    "-targetdir:$TargetDirAbs" `
    "-reporttypes:$reportTypes" `
    "-title:$title" `
    "-verbosity:Info" `
    "-assemblyfilters:$assemblyFilter" `
    "-classfilters:$classFilter"

if ($LASTEXITCODE -ne 0) {
    Write-Host "[service-classfilter] reportgenerator failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit 1
}

Write-Host "[service-classfilter] Service coverage report generated at: $TargetDirAbs" -ForegroundColor DarkCyan
exit 0
