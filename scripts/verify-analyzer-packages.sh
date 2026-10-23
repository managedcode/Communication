#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
packages_directory="$(cd "${1:?Pass the directory containing packed NuGet packages}" && pwd)"
version="${2:?Pass the package version}"
work_directory="$repository_root/artifacts/analyzer-package-check"
mkdir -p "$work_directory"

for package in ManagedCode.Communication ManagedCode.Communication.Extensions ManagedCode.Communication.AspNetCore ManagedCode.Communication.Orleans; do
    consumer_directory="$work_directory/$package"
    mkdir -p "$consumer_directory"
    # Isolate the consumer from this repository's analyzer ProjectReferences and central packages.
    cat > "$consumer_directory/Directory.Build.props" <<'XML'
<Project />
XML
    cat > "$consumer_directory/Directory.Packages.props" <<'XML'
<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>
XML
    cat > "$consumer_directory/Consumer.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><PackageReference Include="$package" Version="$version" /></ItemGroup>
</Project>
XML
    dotnet restore "$consumer_directory/Consumer.csproj" --source "$packages_directory" --source https://api.nuget.org/v3/index.json > "$consumer_directory/restore.log" 2>&1 || { tail -60 "$consumer_directory/restore.log"; exit 1; }

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
var problem = Problem.GenericError();
var failed = Convert(Result<string>.Fail(problem));
if (failed.IsSuccess) return 1;
if (!object.ReferenceEquals(failed.Problem, problem)) return 2;
var success = Convert(Result<string>.Succeed(string.Empty));
if (success.IsFailed) return 3;
return success.Value;
static Result<int> Convert(IResult<string> result)
{
    if (result.IsFailed) return Result<int>.Fail(result.Problem);
    return Result<int>.Succeed(result.Value.Length);
}
CS
    dotnet run --project "$consumer_directory/Consumer.csproj" --no-restore -c Release > "$consumer_directory/accepted.log" 2>&1 || { tail -60 "$consumer_directory/accepted.log"; exit 1; }
    echo "$package $version: both compiler errors enforced; correct flow built and executed."
done
