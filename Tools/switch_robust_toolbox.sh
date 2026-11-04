#!/bin/sh
cd RobustToolbox
ENGINE_TAG=$(git describe --tags --abbrev=0)
cd ..
rm -rf RobustToolbox
git clone --recursive https://github.com/space-wizards/RobustToolbox -b $ENGINE_TAG
sed -i Content.*/*.csproj -e s/TraumaStation.ImageSharp/SixLabors.ImageSharp/g
