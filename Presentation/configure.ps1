# if (-not (Get-Module -ListAvailable -Name ActiveDirectory)) {
#     Write-Output "Installing Active Directory module..."
#     Install-Module -Name ActiveDirectory -Force -Scope CurrentUser
# }

if (-not (Get-Module -ListAvailable -Name SqlServer)) {
    Write-Output "Installing SQL Server module..."
    Install-Module -Name SqlServer -Force -Scope CurrentUser
}

Import-Module ActiveDirectory
Import-Module SqlServer

# Function to enable autologon on a remote machine
function Enable-AutoLogon {
    param (
        [string]$ComputerName,
        [string]$Username = "admin",
        [string]$Password = "admin",
        [string]$Domain = $env:COMPUTERNAME
    )

    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"
    $scriptBlock = {
        param ($Username, $Password, $Domain, $regPath)

        Set-ItemProperty -Path $regPath -Name "AutoAdminLogon" -Value "1" -Type String
        Set-ItemProperty -Path $regPath -Name "DefaultUserName" -Value $Username -Type String
        Set-ItemProperty -Path $regPath -Name "DefaultPassword" -Value $Password -Type String
        Set-ItemProperty -Path $regPath -Name "DefaultDomainName" -Value "$Domain" -Type String
    }

    Invoke-Command -ComputerName $ComputerName -ScriptBlock $scriptBlock -ArgumentList $Username, $Password, $Domain, $regPath
    Invoke-Command -ComputerName $ComputerName -ScriptBlock { Restart-Computer -Force }
}

# Allow remote connections to MSSQL
function Configure-MSSQL {
    param (
        [string]$SqlServer,
        [string]$InstanceName = "MSSQLSERVER"
    )

    $scriptBlock = {
        param ($InstanceName)

        Invoke-Sqlcmd -Query "EXEC sp_configure 'remote access', 1; RECONFIGURE;" -ServerInstance $InstanceName

        $service = Get-Service -Name "SQLBrowser" -ErrorAction SilentlyContinue
        if ($null -eq $service) {
            Write-Host "SQL Browser service not found. Please install SQL Server Browser."
        } else {
            try {
                Start-Service -Name "SQLBrowser"
            } catch{}
            Write-Host "SQL Browser service started"
        }

        $port = 1433
        New-NetFirewallRule -DisplayName "Allow SQL Server" -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow
        Write-Host "Firewall rule created to allow inbound traffic on port $port."

        Write-Host "Remote access to SQL Server has been configured."
    }

    Invoke-Command -ComputerName $SqlServer -ScriptBlock $scriptBlock
}

# Function to add a user to a SQL Admin group and configure permissions
function Add-UserToSQLAdminGroup {
    param (
        [string]$Username,
        [string]$SqlServer,
        [string]$DomainGroupName = "SQLAdmins",
        [string]$DomainPart = $env:COMPUTERNAME
    )

    if (-not (Get-ADGroup -Filter { Name -eq $DomainGroupName } -ErrorAction SilentlyContinue)) {
        Write-Output "Group '$DomainGroupName' does not exist. Creating it now..."
        New-ADGroup -Name $DomainGroupName -GroupScope Global -GroupCategory Security -Description "SQL Server Administrators"
    } else {
        Write-Output "Group '$DomainGroupName' already exists."
    }

    try {
        Add-ADGroupMember -Identity $DomainGroupName -Members $Username -ErrorAction Stop
        Write-Output "User '$Username' added to group '$DomainGroupName'."
    } catch {
        Write-Output "User '$Username' is already a member of group '$DomainGroupName'."
    }

    $connectionString = "Server=$SqlServer;Database=master;Integrated Security=True;TrustServerCertificate=True;User Id=sa"

    Invoke-Sqlcmd -ConnectionString $connectionString -Query "IF EXISTS (SELECT * FROM sys.server_principals WHERE name = '$DomainPart\$DomainGroupName') DROP LOGIN [$DomainPart\$DomainGroupName];"
    Invoke-Sqlcmd -ConnectionString $connectionString -Query "CREATE LOGIN [$DomainPart\$DomainGroupName] FROM WINDOWS; ALTER SERVER ROLE sysadmin ADD MEMBER [$DomainPart\$DomainGroupName]"
    Write-Output "Granted sysadmin role to group '$DomainGroupName' on SQL Server '$SqlServer'."
}

# Function to add an SPN to a service account and make it a local administrator on a specified machine
function Configure-SPNandAdminForKerberoasting {
    param (
        [string]$ServiceAccount = "web_svc",
        [string]$MachineName = "WEB01",
        [string]$SPNServiceType = "HTTP",
        [string]$Domain = $env:COMPUTERNAME
    )

    $SPN = "$SPNServiceType/$MachineName"
    try {
        Set-ADUser -Identity $ServiceAccount -Add @{ServicePrincipalName = $SPN}
        Write-Output "SPN '$SPN' added to service account '$ServiceAccount'."
    } catch {
        Write-Output "Error adding SPN: $_. The SPN may already exist or require additional permissions."
    }

    try {
        Invoke-Command -ScriptBlock {
            param ($ServiceAccountWithDomain)

            $group = [ADSI]"WinNT://./Administrators,group"
            $group.Add("WinNT://$ServiceAccountWithDomain")

            Write-Output "Service account '$ServiceAccountWithDomain' added to Administrators group on '$env:COMPUTERNAME'."
        } -ArgumentList "$Domain/$ServiceAccount"
    } catch {
        Write-Output "Possible error at '$MachineName': $_"
    }
}

Enable-AutoLogon -Username "michal.kuklovsky" -Password "1234" -ComputerName "WEB01" -Domain "BITDEMO.CORP"
Configure-SPNandAdminForKerberoasting -ServiceAccount "web_svc" -MachineName "WEB01" -Domain "BITDEMO.CORP"
Configure-MSSQL -SqlServer "SQL01"
Add-UserToSQLAdminGroup -Username "jan.skalny" -SqlServer "SQL01" -DomainPart "BITDEMO"