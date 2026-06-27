pushd "%~dp0"
cd ./Server
REM Run in a visible console (no --service / no hidden background host).
start "DDON Server" cmd /k "Arrowgene.Ddon.Cli.exe server start"