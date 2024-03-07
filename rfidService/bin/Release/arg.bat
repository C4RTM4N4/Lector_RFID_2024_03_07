@ECHO off
REM IF "%1" == "u" GOTO UN
REM SET /p open=Open Service Control Manager (Y/n):%=%
REM ECHO %open%
REM IF "%open%"== "" SET "open=y"
REM IF "%open%"== "y" services.msc
REM ECHO %open%
REM SETLOCAL DISABLEDELAYEDEXPANSION

REM FOR /F "tokens=1,2,3 delims= " %%A IN ('"SC qc "rfid Service" | FIND "BIN""') DO  SET DELPATH=%%~dpB
REM ECHO "%DELPATH%"
REM RMDIR /S "%DELPATH%"

REM DO IF "%%B"=="BINARY_PATH_NAME" SET PC=%%A
REM ECHO %PC%

REM SC query "rfid Service" | find "RUNNING" >nul 2>&1 && SET running=y 
REM ECHO %running%
REM IF "%running%"== "y"  SC stop "rfid Service" || goto :ERROR   
FOR /F "tokens=1,2,3 delims= " %%A IN ('"SC qc "rfid Service" | FIND "BIN""') DO  SET DELPATH=%%~dpB
SC query "rfid Service" | find "RUNNING" >nul 2>&1 && SET "running=y" 
ECHO "%running%" 
IF %running%== y  ECHO SC stop "rfid Service"
ECHO Unregistering the Service 
REM SC delete "rfid Service" 
ECHO Deleting Files from %DELPATH%
REM RMDIR /S "%DELPATH%"

GOTO END
:ERROR
ECHO "SC stop".

:END

