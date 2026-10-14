# Regenerates FluentSystemIcons-Resizable.ttf from the pinned upstream commit.
# Requires: pip install fonttools
$ErrorActionPreference = 'Stop'
$sha = 'b02c6d621e02c05b890d9f03f3e98f888d39a80e'
$url = "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/$sha/fonts/FluentSystemIcons-Resizable.ttf"
$full = Join-Path $env:TEMP 'FluentSystemIcons-Resizable-full.ttf'
Invoke-WebRequest -Uri $url -OutFile $full

# Regular + Filled pairs for the concepts listed in icons.txt, plus error_circle,
# image and sign_out (needed by later redesign tasks but not yet given a MzGlyph* key).
$unicodes = 'E00C,E00D,E0BE,E0BF,E114,E115,E14E,E14F,E1A6,E1A7,E1B0,E1B1,E35D,E35E,E45F,E460,' +
	'E487,E488,E48D,E48E,E499,E49A,E507,E508,E5D6,E5D7,E66C,E66D,E6D2,E6D3,E782,E783,' +
	'E836,E837,E876,E877,E8A6,E8A7,E8EA,E8EB,EA31,EA32,EA51,EA52,EA69,EA6A,EA6D,EA6E,' +
	'EA87,EA88,EB67,EB68,EBBB,EBBC,EC71,EC72,ECA9,ECAA,ECF3,ECF4,EDB2,EDB3,EE8E,EE8F,' +
	'EF90,EF91,EF98,EF99,EFD6,EFD7,F00E,F00F,F04A,F04B,F08C,F08D,F282,F283,F317,F318,' +
	'F50A,F50B,F560,F561,F57C,F57D,F596,F597,' +
	'E138,E139,E580,E581,E888,E889,E8B6,E8B7,EE00,EE01'

python -m fontTools.subset $full --unicodes="$unicodes" `
	--output-file="$PSScriptRoot/FluentSystemIcons-Resizable.ttf" `
	--no-hinting --desubroutinize --name-IDs='*' --name-legacy --name-languages='*'

Write-Host "Subset written: $((Get-Item "$PSScriptRoot/FluentSystemIcons-Resizable.ttf").Length) bytes"
