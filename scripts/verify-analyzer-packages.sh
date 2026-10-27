#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
packages_directory="$(cd "${1:?Pass the directory containing packed NuGet packages}" && pwd)"
version="${2:?Pass the package version}"
work_directory="$repository_root/artifacts/analyzer-package-check"
mkdir -p "$work_directory"

for package in ManagedCode.Communication ManagedCode.Communication.Extensions ManagedCode.Communication.AspNetCore ManagedCode.Communication.Orleans Combined; do
    consumer_directory="$work_directory/$package"
    mkdir -p "$consumer_directory"
    # Isolate the consumer from this repository's analyzer ProjectReferences and central packages.
    cat > "$consumer_directory/Directory.Build.props" <<'XML'
<Project />
XML
    cat > "$consumer_directory/Directory.Packages.props" <<'XML'
<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>
XML
    package_references="<PackageReference Include=\"$package\" Version=\"$version\" />"
    if [[ "$package" == Combined ]]; then
        package_references=""
        for combined_package in ManagedCode.Communication ManagedCode.Communication.Extensions ManagedCode.Communication.AspNetCore ManagedCode.Communication.Orleans; do
            package_references+="<PackageReference Include=\"$combined_package\" Version=\"$version\" />"
        done
    fi
    cat > "$consumer_directory/Consumer.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup>$package_references</ItemGroup>
</Project>
XML
    dotnet restore "$consumer_directory/Consumer.csproj" --source "$packages_directory" --source https://api.nuget.org/v3/index.json > "$consumer_directory/restore.log" 2>&1 || { tail -60 "$consumer_directory/restore.log"; exit 1; }

    dotnet msbuild "$consumer_directory/Consumer.csproj" -target:ResolveReferences -getItem:Analyzer -verbosity:quiet > "$consumer_directory/analyzer-items.json"
    analyzer_count="$(jq '[.Items.Analyzer[] | select(.Identity | endswith("/ManagedCode.Communication.Analyzers.dll"))] | length' "$consumer_directory/analyzer-items.json")"
    if [[ "$analyzer_count" != 1 ]]; then
        echo "$package must load exactly one Communication analyzer; found $analyzer_count" >&2
        exit 1
    fi

    codefix_count="$(jq '[.Items.Analyzer[] | select(.Identity | endswith("/ManagedCode.Communication.CodeFixes.dll"))] | length' "$consumer_directory/analyzer-items.json")"
    if [[ "$codefix_count" != 1 ]]; then
        echo "$package must load exactly one Communication IDE fix asset; found $codefix_count" >&2
        exit 1
    fi

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
var result = Result.Succeed();
return !result.IsSuccess ? 1 : 0;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0001.log" 2>&1; then
        echo "$package incorrectly accepted !IsSuccess" >&2
        exit 1
    fi
    grep -q 'error MCC0001' "$consumer_directory/MCC0001.log" || { tail -60 "$consumer_directory/MCC0001.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
return Read(Result.Fail()) is null ? 1 : 0;
static Problem? Read(Result result)
{
    if (result.IsFailed) return result.Problem!;
    return null;
}
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0002.log" 2>&1; then
        echo "$package incorrectly accepted redundant Problem!" >&2
        exit 1
    fi
    grep -q 'error MCC0002' "$consumer_directory/MCC0002.log" || { tail -60 "$consumer_directory/MCC0002.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
var result = Result.Fail();
return result.IsFailed && result.Problem is { } problem ? problem.StatusCode : 0;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0002-pattern.log" 2>&1; then
        echo "$package incorrectly accepted a redundant Problem capture" >&2
        exit 1
    fi
    grep -q 'error MCC0002' "$consumer_directory/MCC0002-pattern.log" || { tail -60 "$consumer_directory/MCC0002-pattern.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using System;
using ManagedCode.Communication;
var result = Result<string>.Succeed(string.Empty);
result.ThrowIfProblem();
ArgumentNullException.ThrowIfNull(result.Value);
return 0;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0002-postcondition.log" 2>&1; then
        echo "$package incorrectly accepted a null guard after ThrowIfProblem" >&2
        exit 1
    fi
    grep -q 'error MCC0002' "$consumer_directory/MCC0002-postcondition.log" || { tail -60 "$consumer_directory/MCC0002-postcondition.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
#pragma warning disable MCC0003
using ManagedCode.Communication;
using ManagedCode.Communication.Commands;
var command = Command.From(1);
var result = Result.Succeed(command);
return result.IsSuccess ? 0 : 1;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release -p:NoWarn=MCC0003 > "$consumer_directory/MCC0003.log" 2>&1; then
        echo "$package incorrectly accepted a command result payload or its suppression" >&2
        exit 1
    fi
    grep -q 'error MCC0003' "$consumer_directory/MCC0003.log" || { tail -60 "$consumer_directory/MCC0003.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using System.Collections.Generic;
using ManagedCode.Communication;
using ManagedCode.Communication.Commands;
var result = Result<List<Payload>>.Succeed([new Payload(Command.From(1))]);
return result.IsSuccess ? 0 : 1;
public sealed record Payload(ICommand Work);
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0003-nested.log" 2>&1; then
        echo "$package incorrectly accepted a command nested inside a result DTO collection" >&2
        exit 1
    fi
    grep -q 'error MCC0003' "$consumer_directory/MCC0003-nested.log" || { tail -60 "$consumer_directory/MCC0003-nested.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.Commands;
var command = Command.From(Result.Succeed(1));
return command.IsEmpty ? 1 : 0;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0003-command.log" 2>&1; then
        echo "$package incorrectly accepted a result inside a command payload" >&2
        exit 1
    fi
    grep -q 'error MCC0003' "$consumer_directory/MCC0003-command.log" || { tail -60 "$consumer_directory/MCC0003-command.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
var result = Result.Succeed(Result.Succeed(1));
return result.IsFailed ? 1 : 0;
CS
    dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release -p:TreatWarningsAsErrors=false > "$consumer_directory/MCC0004-nested.log" 2>&1 || { tail -60 "$consumer_directory/MCC0004-nested.log"; exit 1; }
    grep -q 'warning MCC0004' "$consumer_directory/MCC0004-nested.log" || { tail -60 "$consumer_directory/MCC0004-nested.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
var result = Preserve(Result.Succeed(1));
return result.IsFailed ? 1 : 0;
static Result<int> Preserve(Result<int> result)
{
    if (result.IsFailed) return result;
    return Result.Succeed(result.Value);
}
CS
    dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release -p:TreatWarningsAsErrors=false > "$consumer_directory/MCC0004-reuse.log" 2>&1 || { tail -60 "$consumer_directory/MCC0004-reuse.log"; exit 1; }
    grep -q 'warning MCC0004' "$consumer_directory/MCC0004-reuse.log" || { tail -60 "$consumer_directory/MCC0004-reuse.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
var result = Result<int>.Succeed(7);
CqrsStreamChunk<string, int> chunk = result.IsSuccess
    ? CqrsStreamChunk<string, int>.Completed(result)
    : CqrsStreamChunk<string, int>.Failed(result);
return chunk.IsCompleted ? 0 : 1;
CS
    dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release -p:TreatWarningsAsErrors=false > "$consumer_directory/MCC0005.log" 2>&1 || { tail -60 "$consumer_directory/MCC0005.log"; exit 1; }
    grep -q 'warning MCC0005' "$consumer_directory/MCC0005.log" || { tail -60 "$consumer_directory/MCC0005.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
var chunk = CqrsStreamChunk<int, int>.Progress(Result<int>.Succeed(7));
return chunk.IsProgress ? 0 : 1;
CS
    dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release -p:TreatWarningsAsErrors=false > "$consumer_directory/MCC0006.log" 2>&1 || { tail -60 "$consumer_directory/MCC0006.log"; exit 1; }
    grep -q 'warning MCC0006' "$consumer_directory/MCC0006.log" || { tail -60 "$consumer_directory/MCC0006.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
var chunk = CqrsStreamChunk<int, int>.Completed(Result<int>.Fail());
return chunk.IsFailed ? 0 : 1;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0007.log" 2>&1; then
        echo "$package incorrectly accepted a contradictory terminal result" >&2
        exit 1
    fi
    grep -q 'error MCC0007' "$consumer_directory/MCC0007.log" || { tail -60 "$consumer_directory/MCC0007.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.CQRS;
var chunk = CqrsStreamChunk<int, string>.Completed(Result.Succeed());
return chunk.IsCompleted ? 0 : 1;
CS
    if dotnet build "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/MCC0007-converted.log" 2>&1; then
        echo "$package incorrectly accepted a success converted into a failed typed terminal result" >&2
        exit 1
    fi
    grep -q 'error MCC0007' "$consumer_directory/MCC0007-converted.log" || { tail -60 "$consumer_directory/MCC0007-converted.log"; exit 1; }

    cat > "$consumer_directory/Program.cs" <<'CS'
using ManagedCode.Communication;
using ManagedCode.Communication.Commands;
using ManagedCode.Communication.CQRS;
var problem = Problem.GenericError();
var failed = Convert(Result<string>.Fail(problem));
if (failed.IsSuccess) return 1;
if (!object.ReferenceEquals(failed.Problem, problem)) return 2;
var success = Convert(Result<string>.Succeed(string.Empty));
if (success.IsFailed) return 3;
var command = Command.From(7);
var output = Handle(command);
if (output.IsFailed) return 4;
if (output.Value != 7) return 5;
var checkedPayload = Result<string>.Succeed(string.Empty);
checkedPayload.ThrowIfProblem();
if (checkedPayload.Value.Length != 0) return 6;
CqrsStreamChunk<int, string> completed = checkedPayload;
CqrsStreamChunk<int, string> failure = Result<string>.Fail(problem);
if (!completed.IsCompleted || !failure.IsFailed || !object.ReferenceEquals(failure.Problem, problem)) return 7;
var converted = CqrsStreamChunk<int, string>.Failed(Result.Succeed());
if (!converted.IsFailed) return 8;
return success.Value;
static Result<int> Handle(Command<int> command) => Result.Succeed(command.Value);
static Result<int> Convert(IResult<string> result)
{
    if (result.IsFailed) return Result<int>.Fail(result.Problem);
    return Result<int>.Succeed(result.Value.Length);
}
CS
    dotnet run --project "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/accepted.log" 2>&1 || { tail -60 "$consumer_directory/accepted.log"; exit 1; }
    echo "$package $version: all compiler errors and warnings enforced; separate command/result flow built and executed."
done
