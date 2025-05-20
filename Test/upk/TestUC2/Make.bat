@echo off

set projn=__TestUC2__Temp
set workDir=%cd%
set RootDir="C:\UT2004"
set UCCDir="%RootDir%\System"
set ScriptDir="%RootDir%\"
set DestDir="%RootDir%\System"

title %projn%
color 0F

echo %cd%
cd /d C:
cd "%UCCDir%"

xcopy "%workDir%\Classes\" "%ScriptDir%\%projn%\Classes\" /f /r /y /s /i

del "%DestDir%\%projn%.u"

cd "%UCCDir%"
ucc.exe editor.MakeCommandlet -EXPORTCACHE -SHOWDEP -ini="%workDir%\make.ini"
xcopy "%DestDir%\%projn%.u" "%workDir%\TestUC2.u" /f /y /r