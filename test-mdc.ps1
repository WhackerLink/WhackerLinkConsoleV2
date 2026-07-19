$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("wl-mdc-" + [System.Guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $work | Out-Null

try {
    $lib = Join-Path $root "WhackerLinkLib\WhackerLinkLib.csproj"
    $proj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$lib" />
  </ItemGroup>
</Project>
"@

    $program = @"
using WhackerLinkLib.Utils;

Mdc1200.Encoder encoder = new Mdc1200.Encoder(8000);
encoder.SetPacket(0x01, 0x00, 12345);

byte[] pcm = encoder.GetAllSamples();

if (pcm.Length == 0)
    throw new Exception("encoder returned no pcm");

Mdc1200.Decoder decoder = new Mdc1200.Decoder(8000);
Mdc1200.Packet packet = null;

for (int i = 0; i < pcm.Length; i += 320)
{
    int count = Math.Min(320, pcm.Length - i);
    byte[] block = new byte[count];
    Buffer.BlockCopy(pcm, i, block, 0, count);

    Mdc1200.Packet decoded = decoder.ProcessSamples(block);
    if (decoded != null)
        packet = decoded;
}

if (packet == null)
    throw new Exception("decoder returned no packet");

if (packet.Op != 0x01 || packet.Arg != 0x00 || packet.UnitId != 12345)
    throw new Exception($"decoded {packet.Op:X2}/{packet.Arg:X2}/{packet.UnitId}, expected 01/00/12345");

Console.WriteLine($"MDC PCM 8000 encode/decode ok: {packet.Op:X2}/{packet.Arg:X2}/{packet.UnitId}, {pcm.Length} bytes");
"@

    Set-Content -Path (Join-Path $work "MdcRoundTrip.csproj") -Value $proj
    Set-Content -Path (Join-Path $work "Program.cs") -Value $program
    dotnet run --project (Join-Path $work "MdcRoundTrip.csproj")
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
