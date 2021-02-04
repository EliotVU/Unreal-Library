@echo off

set projn=TestUC2
set inin=Test

title %projn%
color 0F

echo.
echo Deleting compiled files %projn%
echo.
cd..
cd system
del %projn%.u
del %projn%.ucl
del %projn%.int

ucc.exe MakeCommandletUtils.EditPackagesCommandlet 1 %projn%
ucc.exe editor.MakeCommandlet -EXPORTCACHE -SHOWDEP
ucc.exe MakeCommandletUtils.EditPackagesCommandlet 0 %projn%
pause

echo.
echo Generate files?
echo.
pause

echo.
echo Generating cache files
echo.
ucc.exe dumpintCommandlet %projn%.u

echo.
echo StripSource?
echo.
pause

echo.
echo Stripping source!
echo.
ren %inin%.ini %inin%_bak.ini
ucc.exe editor.stripsourcecommandlet %projn%.u
ren %inin%_bak.ini %inin%.ini
echo.
echo Successfuly stripped the source code
echo.
pause