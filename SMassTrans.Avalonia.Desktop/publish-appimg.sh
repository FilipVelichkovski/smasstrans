if [ -d smasstrans ]; then
    rm -rf smasstrans
fi
mkdir -p smasstrans/usr/bin
cp -r publish/. smasstrans/usr/bin
mkdir -p smasstrans/usr/share/applications
cp smasstrans.desktop smasstrans/usr/share/applications
mkdir -p smasstrans/usr/share/icons/smasstrans/scalable/apps/
mkdir -p smasstrans/usr/share/icons/smasstrans/512x512/apps/
mkdir -p smasstrans/usr/share/metainfo/
#cp smasstrans.appdata.xml smasstrans/usr/share/metainfo

cp ../../../../../icon.svg smasstrans/usr/share/icons/smasstrans/scalable/apps/SMassTrans.svg
cp ../../../../../Icon.png smasstrans/usr/share/icons/smasstrans/512x512/apps/SMassTrans.png
#ln -s usr/share/icons/smasstrans/scalable/apps/SMassTrans.svg smasstrans/SMassTrans.svg

ln -s usr/share/applications/smasstrans.desktop smasstrans
#cp smasstrans.desktop smasstrans/smasstrans.desktop


#ln -s usr/share/icons/smasstrans/512x512/apps/SMassTrans.png smasstrans/.DirIcon
cp smasstrans/usr/share/icons/smasstrans/512x512/apps/SMassTrans.png smasstrans/SMassTrans.png

ln -s usr/bin/SMassTrans.Avalonia.PC smasstrans/AppRun

if [ -f smasstrans.AppImage ]; then
    rm smasstrans.AppImage
fi


if [ ! -f appimagetool-x86_64.AppImage ]; then
    wget https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
fi
chmod +x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage smasstrans