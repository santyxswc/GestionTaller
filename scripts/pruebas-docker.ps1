# Corre la verificación de formato, la compilación y las pruebas dentro de un contenedor
# con el SDK de .NET 10, igual que el flujo de GitHub Actions, sin gastar minutos de Actions.
# Uso (desde la raíz del repositorio, con Docker Desktop abierto):
#   powershell -ExecutionPolicy Bypass -File scripts/pruebas-docker.ps1

$raiz = Split-Path -Parent $PSScriptRoot

# Se copia el código a /tmp dentro del contenedor para no mezclar bin/obj de Windows y Linux.
docker run --rm -v "${raiz}:/repo:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash -c @'
set -e
mkdir /src && cd /repo && tar --exclude='./**/bin' --exclude='./**/obj' --exclude='./.git' -cf - . | tar -xf - -C /src
cd /src
dotnet restore TallerMoreno.slnx
dotnet format TallerMoreno.slnx --verify-no-changes --no-restore
dotnet build TallerMoreno.slnx -c Release --no-restore
dotnet run --project tests/TallerMoreno.Tests -c Release --no-build
'@
exit $LASTEXITCODE
