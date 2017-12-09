#/bin/sh

set -eur

echo "Building for release..."
msbuild TinCan/TinCan.csproj /p:Configuration=Release

echo "Building NuGet package..."
nuget pack TinCan/TinCan.csproj -IncludeReferencedProjects -Prop Configuration=Release
