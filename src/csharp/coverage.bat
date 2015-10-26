@rem Generates C# coverage report

setlocal

@rem enter this directory
cd /d %~dp0

packages\JetBrains.dotCover.CommandLineTools.3.2.20150819.165728\tools\dotCover.exe analyse coverage.xml

endlocal

goto :EOF

:error
echo Failed!
exit /b %errorlevel%
