@echo off

set projn=TestUC2
set workDir=%cd%
set UCCDir="C:\UT2004\System"

title %projn%
color 0F

echo %cd%
cd /d C:
cd %UCCDir%

cd ..
xcopy %workDir% %projn% /y /s /i
cd %UCCDir%

del %projn%.u
ucc.exe MakeCommandletUtils.EditPackagesCommandlet 1 %projn%
ucc.exe editor.MakeCommandlet -EXPORTCACHE -SHOWDEP
ucc.exe MakeCommandletUtils.EditPackagesCommandlet 0 %projn%
xcopy %projn%.u %workDir% /y
del %projn%.u

cd ..
del %projn%