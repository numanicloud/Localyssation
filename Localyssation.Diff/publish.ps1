dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=false
Copy-Item .\bin\Release\net10.0\win-x64\publish\* .\dist\Localyssation.Diff\
Copy-Item .\README.md .\dist\Localyssation.Diff\README.md
Copy-Item .\assets .\dist\Localyssation.Diff\assets -Recurse