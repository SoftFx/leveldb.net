mkdir -p ../native  # create folder for .dll if it doesn't exist

# clone google/leveldb reporistory
rm -f -r google_sources
mkdir google_sources
git clone https://github.com/google/leveldb.git google_sources
mkdir google_sources/build
cd google_sources/build

# if you need a specific version of leveldb you should download this version in current folder (ex. leveldb-1.22)
# then you should uncomment the following 2 lines and comment cloning google/leveldb reporistory
# cd leveldb-1.22  # specific version leveldb folder
# mkdir -p build && cd build

# Building x86_64
echo Building x86_64...
cmake -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=true .. && cmake --build .
if [ $? -ne 0 ]; then
	echo
    echo Fail to build x86_64 library
	exit 1
fi
cp libleveldb.so ../../../native/LevelDBLinux.dll
rm -rf *

cd ../..
rm -f -r google_sources

# you should use the folowing 2 lines instead of previous 2 lines if you've downloaded a specific version of leveldb
# cd ..
# rm -f -r build