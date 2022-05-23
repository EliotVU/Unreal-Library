@echo off

set projn=MyMod
set workDir=%cd%
set RootDir="D:\Games\UDK\Custom"
set UCCDir="%RootDir%\Binaries\Win64"
set ScriptDir="%RootDir%\Development\Src"
set DestDir="%RootDir%\UDKGame\Script"

title %projn%
color 0F

echo %cd%
cd /d D:
cd "%UCCDir%"

xcopy "%workDir%\Classes\" "%ScriptDir%\%projn%\Classes\" /f /r /y /s /i

cd "%UCCDir%"
udk.exe make -EXPORTCACHE -SHOWDEP

xcopy "%DestDir%\%projn%.u" "%workDir%\TestUC3.u" /f /y /r