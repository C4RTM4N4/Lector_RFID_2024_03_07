@echo off
CLS 

:checkPrivileges 
NET FILE 1>NUL 2>NUL
if '%errorlevel%' == '0' ( goto gotPrivileges ) else ( goto getPrivileges ) 

:getPrivileges 
if '%1'=='ELEV' (shift & goto gotPrivileges)  
ECHO ********************************************************************************
ECHO *                                                                              *
ECHO *    NEM Solutions  2014                                                       *
ECHO *                                                                              *
ECHO *    rfid Service installation utility                                         *
ECHO *                                                                              *
ECHO *    Invoking UAC for Privilege Escalation                                     *
ECHO *                                                                              *
ECHO ********************************************************************************

SETLOCAL DisableDelayedExpansion
SET "batchPath=%~0"
SET "INSTPATH=%~dp0"
SET "param=%1"

SETLOCAL EnableDelayedExpansion
ECHO Set UAC = CreateObject^("Shell.Application"^) > "%temp%\OEgetPrivileges.vbs" 
ECHO UAC.ShellExecute "!batchPath!", "ELEV %param%", "", "runas", 1 >> "%temp%\OEgetPrivileges.vbs" 
"%temp%\OEgetPrivileges.vbs" 
EXIT /B 

:gotPrivileges 
::::::::::::::::::::::::::::
::START
::::::::::::::::::::::::::::
setlocal & pushd .

REM The following directory is for .NET 2.0
REM SET DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
REM SET PATH=%PATH%;%DOTNETFX2%

IF "%2" == "u" GOTO UNINSTALL
    
    ECHO.
    ECHO Set service installation path, the configuration file (rfidService_cfg.xml) 
    ECHO will be located in that folder.
    ECHO.
    SET /p pathName=Path where Install (c:\service):%=%
    IF "%pathName%"== "" SET "pathName=c:\service"
    SET "pathName="%pathName%\""
    ECHO.
    ECHO Copying Files....
    ECHO.
    IF NOT EXIST "%pathName%" MKDIR "%pathName%"    || goto :ERROR
    COPY "%~dp0\bin\rfidService.exe" "%pathName%"   || goto :ERROR
    COPY "%~dp0\bin\rfidService_cfg.xml" "%pathName%" || goto :ERROR
    COPY "%~dp0\bin\Intermec.DataCollection.RFID.BasicBRI.dll" "%pathName%" || goto :ERROR
    ECHO Installing service....
    "%~dp0\bin\InstallUtil.exe" "%pathName:~1,-1%rfidService.exe" || goto :ERROR
    ECHO.
    ECHO.
    ECHO  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    ECHO.
    ECHO  Remember to configure the service by editing the file rfidService_cfg.xml
    ECHO  at %pathName% and restart the service afterwards
    ECHO  The service will have "Local Service" privileges, configure the watch directory
    ECHO  security accordly. The service privileges can be changed in the 
    ECHO  Service Control Manager (services.msc)
    ECHO  Restart the machine to start the service or start it manually from the 
    ECHO  Service Control Manager (services.msc)
    ECHO.
    ECHO  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    ECHO.
    SET /p open=Open Service Control Manager (Y/n):%=%
    IF "%open%"== "" SET "open=y"
    IF "%open%"== "y"  services.msc

GOTO END
:ERROR
ECHO Failed with error #%errorlevel%.
PAUSE
EXIT /b %errorlevel%

:UNINSTALL
    
    FOR /F "tokens=1,2,3 delims= " %%A IN ('"SC qc "rfid Service" | FIND "BIN""') DO  SET DELPATH=%%~dpB
    SET "running=n"
    SC query "rfid Service" | find "RUNNING" >nul 2>&1 && SET "running=y" 
    IF %running%== y  SC stop "rfid Service"
    ECHO Unregistering the Service 
    SC delete "rfid Service" 
    ECHO Deleting Files from %DELPATH%
    RMDIR /S "%DELPATH%"
    
:END
PAUSE