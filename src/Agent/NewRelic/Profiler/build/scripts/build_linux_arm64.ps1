############################################################
# Copyright 2020 New Relic Corporation. All rights reserved.
# SPDX-License-Identifier: Apache-2.0
############################################################

Write-Host ""
Write-Host "********"
Write-Host "Build Linux Arm64 profiler shared object (.so)"
Write-Host "********"

$baseProfilerPath = (Get-Item (Split-Path $script:MyInvocation.MyCommand.Path)).parent.parent.FullName
Push-Location "$baseProfilerPath"

Write-Host "docker-compose build build_arm64"
docker-compose build build_arm64 #--no-cache

Write-Host "docker-compose run build_arm64"
docker-compose run build_arm64

if ($LastExitCode -ne 0) {
    exit $LastExitCode
}

Write-Host ""
Write-Host "********"
Write-Host "Clean up old containers"
Write-Host "********"

Write-Host "Cleaning up old containers"
Write-Host 'Running command: docker container prune --force --filter "until=60m"'
docker container prune --force --filter "until=60m"

Pop-Location

exit $LastExitCode