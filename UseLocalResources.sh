CSPROJ="osu.Game/osu.Game.csproj"
SLN="osu.sln"

dotnet remove $CSPROJ package moorf.hotia.Game.Resources;
dotnet sln $SLN add ../osu-resources/osu.Game.Resources/osu.Game.Resources.csproj
dotnet add $CSPROJ reference ../osu-resources/osu.Game.Resources/osu.Game.Resources.csproj

SLNF="osu.Desktop.slnf"
TMP=$(mktemp)
jq '.solution.projects += ["../osu-resources/osu.Game.Resources/osu.Game.Resources.csproj"]' $SLNF > $TMP
mv -f $TMP $SLNF
