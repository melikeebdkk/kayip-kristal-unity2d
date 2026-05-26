# Kayip Kristal Demo

Bu Unity projesi, MIT lisansli `practical-works/unity2d-prototype` temelinden baslatildi ve ders demosu icin `Kayip Kristal` konseptine uyarlandi.

## Demo Mekanikleri

- 2D platformer hareketi
- Kristal toplama ve puan
- HUD uzerinde hedef/sayac gosterimi
- Checkpoint
- Tuzak sonrasi checkpoint'e donme
- Tum kristaller toplaninca aktif olan cikis bolgesi

## Unity'de Hazirlama

Unity'de projeyi actiktan sonra menuden:

`Kayip Kristal > Prepare Demo Scene`

komutunu calistirin. Bu komut `SampleLevel` sahnesini demo icin hazirlar.

## Build Alma

Editor batch komutu:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\Melike\Documents\New project 2\KayipKristal_Unity2D" -executeMethod KayipKristalSetup.BuildWindows -logFile "C:\Users\Melike\Documents\New project 2\KayipKristal_Unity2D\build.log"
```

Build cikisi:

`Builds/KayipKristalDemo/KayipKristal.exe`
