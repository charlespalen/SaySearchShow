
candle -ext WiXNetFxExtension SaySearchShowWixInstaller.wxs
light -ext WiXNetFxExtension -ext WixUIExtension -cultures:en-us SaySearchShowWixInstaller.wixobj -out ..\SaySearchShow.msi
PAUSE