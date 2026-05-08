# samplemanager-NewPharma-dev

Development workspace for SampleManager LIMS 21.3 customizations against the
remote VGSM instance.

## Remote Instance

- Host alias: `conversion-project`
- Product: SampleManager LIMS 21.3
- Instance root: `C:\Thermo\SampleManager\Server\VGSM`
- Solution root: `C:\Thermo\SampleManager\Server\VGSM\Solution`
- Runtime binaries: `C:\Thermo\SampleManager\Server\VGSM\Exe`
- Logs: `C:\Thermo\SampleManager\Server\VGSM\Logfile`

## Workflow

1. Design and code locally.
2. Build and package local changes.
3. Deploy to the remote SampleManager instance over SSH.
4. Fetch remote logs for review.
5. Commit code, scripts, and design notes to GitHub.
