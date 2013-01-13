%systemroot%\Microsoft.NET\Framework\V4.0.30319\msbuild /t:clean /v:n /p:Configuration=DEBUG src\Git.Unite.sln
%systemroot%\Microsoft.NET\Framework\V4.0.30319\msbuild /t:build /v:n /p:Configuration=DEBUG src\Git.Unite.sln
