@rem Copyright 2016 gRPC authors.
@rem
@rem Licensed under the Apache License, Version 2.0 (the "License");
@rem you may not use this file except in compliance with the License.
@rem You may obtain a copy of the License at
@rem
@rem     http://www.apache.org/licenses/LICENSE-2.0
@rem
@rem Unless required by applicable law or agreed to in writing, software
@rem distributed under the License is distributed on an "AS IS" BASIS,
@rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
@rem See the License for the specific language governing permissions and
@rem limitations under the License.

@rem Current package versions
set VERSION=1.16.0-dev

@rem Adjust the location of nuget.exe
set NUGET=C:\nuget\nuget.exe
set DOTNET=dotnet

mkdir ..\..\artifacts

%NUGET% update -self

%NUGET%

%NUGET% pack Grpc.nuspec -Version %VERSION% -OutputDirectory ..\..\artifacts || goto :error

%NUGET% sign ..\..\artifacts\Grpc.%VERSION%.nupkg -CertificateSubjectName "Google Inc" -Timestamper http://timestamp.comodoca.com/authenticode

@rem copy resulting nuget packages to artifacts directory
xcopy /Y /I *.nupkg ..\..\artifacts\ || goto :error

@rem create a zipfile with the artifacts as well
powershell -Command "Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('..\..\artifacts', 'csharp_nugets_windows_dotnetcli.zip');"
xcopy /Y /I csharp_nugets_windows_dotnetcli.zip ..\..\artifacts\ || goto :error

goto :error

goto :EOF

:error
echo Failed!
exit /b %errorlevel%
