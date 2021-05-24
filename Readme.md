# ForceWT
This application will open the WindowsTerminal on every attempt of opening `cmd.exe`or `powershell.exe`!   
It works using `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`:   

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\cmd.exe]
"Debugger"="\"<Path>\\ForceWT.exe\""

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\powershell.exe]
"Debugger"="\"<Path>\\ForceWT.exe\""
```
