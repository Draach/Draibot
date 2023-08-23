## WIP

# Development

1. Duplicate `launchSettings.example.json` and rename it to `launchSettings.json`
2. In the new file (`launchSettings.json`), fill in the `commandLineArgs` field with the respective discord's bot token. It should look something like this:
```
{
  "profiles": {
    "Draibot": {
      "commandName": "Project",
      "commandLineArgs": "REPLACETHISWITHYOURACTUALDISCORDTOKEN"
    }
  }
}
```
3. Build and exec the application.