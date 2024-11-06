# `range.yaml`
Range config which can be used to spin up a testing environment with [Ludus](https://gitlab.com/badsectorlabs/ludus).

Note - if the `MSSQL` provisioning fails, re-run the role with:
```
ludus range deploy -t user-defined-roles --user <your_user> --limit localhost,SQL01 --only-roles ludus_mssql
```

# `configure.ps1`
Script which configures the vulnerabilities and misconfigurations shown in the presentation. Must be ran as a domain administrator on a domain controller after [Ludus](https://gitlab.com/badsectorlabs/ludus) provisions the environment.