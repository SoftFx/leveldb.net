# if you try to run this script and see the error:
# .\cross-compile-win.ps1 : File \cross-compile-win.ps1 cannot be loaded
# because running scripts is disabled on this system. For more information, see about_Execution_Policies at
# https:/go.microsoft.com/fwlink/?LinkID=135170.
# At line:1 char:1
# + .\cross-compile-win.ps1
# + ~~~~~~~~~~~~~~~~~~~~~~~
#     + CategoryInfo          : SecurityError: (:) [], PSSecurityException
#     + FullyQualifiedErrorId : UnauthorizedAccess
#
# run the following command (in powershell with Admin rights)
# set-executionpolicy remotesigned

# for cmake command download binary distribution https://cmake.org/download/  
# for devenv command set Environmental variables -> Path to your devenv.exe folder location. (Ex. "D:\Program Files\Microsoft Visual Studio Community 2017\Common7\IDE\")

mkdir -p ..\native  # create folder for .dll if it doesn't exist

# clone google/leveldb reporistory
rm -r -fo google_sources
mkdir google_sources
git clone https://github.com/google/leveldb.git google_sources
mkdir google_sources\build
cd google_sources\build

# if you need a specific version of leveldb you should download this version in current folder (ex. leveldb-1.22)
# after that you should uncomment the following 3 lines and comment cloning google/leveldb reporistory
# cd leveldb-1.22  # specific version leveldb folder
# mkdir -p build
# cd build

# Building win_x64
echo Building win_x64...
cmake -G "Visual Studio 15 Win64" .. -DBUILD_SHARED_LIBS=1
devenv /build Release .\leveldb.sln
cp .\Release\leveldb.dll ..\..\..\native\LevelDB64.dll
cd ..
rm -r -fo build

mkdir -p build
cd build

# Building win_x86
echo Building win_x86...
cmake -G "Visual Studio 15" .. -DBUILD_SHARED_LIBS=1
devenv /build Release .\leveldb.sln
cp .\Release\leveldb.dll ..\..\..\native\LevelDB32.dll
cd ..
rm -r -fo build

cd ..
rm -r -fo google_sources