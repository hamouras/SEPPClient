# SEPPClient

OCR Setup
---------

If you want to use OCR functionality do the following:

1) Download and install Engu-cv 3.0.0. Currently available from here:

Sourceforge:
https://sourceforge.net/projects/emgucv/files/emgucv/3.0.0/

Sourceforge exe download:
https://sourceforge.net/projects/emgucv/files/emgucv/3.0.0/libemgucv-windows-universal-cuda-3.0.0.2158.exe/download

2) Build the project

3) Copy the bin/x64 (or bin/x86) dlls from emgu-cv to SEPPClient Debug/Release x64 (or x86) folder.

For example, copy this:

C:\Emgu\emgucv-windows-universal-cuda 3.0.0.2158\bin\x64\ to SEPPClient\bin\Debug\x64

4) Run the app. You should no longer get missing DLL errors when performing OCR.
