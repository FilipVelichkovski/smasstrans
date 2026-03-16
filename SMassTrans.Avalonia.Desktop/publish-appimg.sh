#!/bin/bash
# Usage: ./publish-appimg.sh aarch64 (or x86_64)
TARGET_ARCH=${1:-x86_64}

if [ -d smasstrans ]; then
    rm -rf smasstrans
fi

mkdir -p smasstrans/usr/bin
cp -r publish/. smasstrans/usr/bin
mkdir -p smasstrans/usr/share/applications
cp smasstrans.desktop smasstrans/usr/share/applications
mkdir -p smasstrans/usr/share/icons/smasstrans/512x512/apps/
mkdir -p smasstrans/usr/share/metainfo/

cp ../../../../../Icon.png smasstrans/usr/share/icons/smasstrans/512x512/apps/SMassTrans.png
ln -s usr/share/applications/smasstrans.desktop smasstrans
cp smasstrans/usr/share/icons/smasstrans/512x512/apps/SMassTrans.png smasstrans/SMassTrans.png
ln -s usr/bin/SMassTrans.Avalonia.PC smasstrans/AppRun

# Download the tool matching the HOST architecture (usually x86_64 in CI)
# but we tell it to package for the TARGET_ARCH
HOST_ARCH=$(uname -m)
TOOL_NAME="appimagetool-$HOST_ARCH.AppImage"

if [ ! -f "$TOOL_NAME" ]; then
    wget "https://github.com/AppImage/appimagetool/releases/download/continuous/$TOOL_NAME"
    chmod +x "$TOOL_NAME"
fi

# Set the ARCH env variable so appimagetool knows what it's building
export ARCH=$TARGET_ARCH

# Use --appimage-extract-and-run because Docker usually lacks FUSE
./"$TOOL_NAME" smasstrans "SMassTrans-$TARGET_ARCH.AppImage"