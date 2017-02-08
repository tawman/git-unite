# Setup MsBuild with C# 6 support
Framework "4.6"

$sln = "src\Git.Unite.sln"
$config = "DEBUG"

task default -depends Test

task Test -depends Compile {

}

task Compile -depends Clean {
  Exec { msbuild /t:build /v:n /p:Configuration=$config $sln }
}

task Clean {
  Exec { msbuild /t:clean /v:n /p:Configuration=$config $sln }
}

task Restore {
  Exec { Tools\Nuget.exe restore -source "https://www.nuget.org/api/v2" $sln }
}
