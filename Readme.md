# ForceWT
This application will open the WindowsTerminal on every attempt of opening `cmd.exe`or `powershell.exe`.   
The commandline will be preserved, so you can execute `*.bat` files like normal but in WT!   
## Setup
It works using `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`:   

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\cmd.exe]
"Debugger"="\"<Path>\\ForceWT.exe\""

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\powershell.exe]
"Debugger"="\"<Path>\\ForceWT.exe\""
```
## ToDo
 - [ ] Testing
 - [ ] Prevent console flickering
 - [ ] Installer
 - [ ] Console handler (e.g. `Ctrl + C`)
 - [ ] ...
