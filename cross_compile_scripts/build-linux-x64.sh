#!/bin/bash
# Builds native/LevelDBLinux.dll: LevelDB with Snappy linked in statically, for linux-x64.
#
# Run from anywhere; needs Docker:
#   cross_compile_scripts/build-linux-x64.sh
#
# Why it looks like this:
# - LevelDB's CMake enables Snappy only when it finds libsnappy on the build machine and says
#   nothing when it does not. The library shipped up to 1.2.1 was built that way and silently
#   ignored CompressionLevel.SnappyCompression. Here Snappy is built from source, linked
#   statically, HAVE_SNAPPY is forced on (the stock check cannot see a static libsnappy.a), and
#   the result is checked for Snappy.
# - Snappy's symbols are kept private to the library (--exclude-libs), so a process that has
#   another libsnappy loaded cannot bind LevelDB's calls to that other version.
# - Built in ubuntu:20.04 with its GCC 9, as the 1.2.1 library was, and checked against the
#   1.2.1 baseline: it must need nothing newer than GLIBC_2.14 and GLIBCXX_3.4.22.
# - Sources and the image are pinned, and the image is the amd64 one on any host.
set -euo pipefail

LEVELDB_TAG=1.23
LEVELDB_COMMIT=99b3c03b3284f5886f9ef9a4ef703d57373e61be
SNAPPY_TAG=1.1.10
SNAPPY_COMMIT=dc05e026488865bc69313a68bcc03ef2e4ea8e83
IMAGE=ubuntu:20.04@sha256:8feb4d8ca5354def3d8fce243717141ce31e2c428701f6682bd2fafe15388214

NATIVE_DIR="$(cd "$(dirname "$0")/../native" && pwd)"
# Git Bash on Windows: Docker wants a Windows path for the mount, and MSYS_NO_PATHCONV below
# stops Git Bash from rewriting /out into one.
if command -v cygpath >/dev/null; then NATIVE_DIR="$(cygpath -w "$NATIVE_DIR")"; fi

MSYS_NO_PATHCONV=1 docker run --rm --platform linux/amd64 \
  -e LEVELDB_TAG="$LEVELDB_TAG" -e LEVELDB_COMMIT="$LEVELDB_COMMIT" \
  -e SNAPPY_TAG="$SNAPPY_TAG" -e SNAPPY_COMMIT="$SNAPPY_COMMIT" \
  -v "$NATIVE_DIR:/out" \
  "$IMAGE" bash -euo pipefail -c '
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends ca-certificates git cmake make g++ binutils >/dev/null

clone() { # url tag commit dir
  git -c advice.detachedHead=false clone -q --depth 1 --branch "$2" "$1" "$4"
  test "$(git -C "$4" rev-parse HEAD)" = "$3" || { echo "$4: tag $2 is not $3"; exit 1; }
}
mkdir /build && cd /build
clone https://github.com/google/snappy.git  "$SNAPPY_TAG"  "$SNAPPY_COMMIT"  snappy
clone https://github.com/google/leveldb.git "$LEVELDB_TAG" "$LEVELDB_COMMIT" leveldb

cmake -S snappy -B snappy/build -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF -DCMAKE_POSITION_INDEPENDENT_CODE=ON \
  -DSNAPPY_BUILD_TESTS=OFF -DSNAPPY_BUILD_BENCHMARKS=OFF >/dev/null
cmake --build snappy/build -j "$(nproc)" >/dev/null 2>&1
cmake --install snappy/build >/dev/null   # into /usr/local, on the default search paths

cmake -S leveldb -B leveldb/build -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON \
  -DLEVELDB_BUILD_TESTS=OFF -DLEVELDB_BUILD_BENCHMARKS=OFF -DHAVE_SNAPPY=ON \
  -DCMAKE_SHARED_LINKER_FLAGS="-Wl,--exclude-libs,ALL" >/dev/null
cmake --build leveldb/build -j "$(nproc)" >/dev/null

lib=leveldb/build/libleveldb.so.$LEVELDB_TAG.0
fail() { echo "FAILED: $*"; exit 1; }
max_version() { objdump -T "$lib" | grep -o "$1_[0-9.]*" | sed "s/$1_//" | sort -u -V | tail -1; }
at_most() { test "$(printf "%s\n%s\n" "$1" "$2" | sort -V | tail -1)" = "$2"; }

readelf -h "$lib" | grep -q "Advanced Micro Devices X86-64" || fail "not an x86-64 library"
# The library is not stripped, so the static symbol table shows the Snappy code linked in.
snappy_symbols=$(nm --defined-only "$lib" | grep -c "snappy" || true)
test "$snappy_symbols" -gt 0 || fail "built without Snappy"
! readelf -d "$lib" | grep -q "NEEDED.*snappy" || fail "Snappy is linked dynamically"
! nm -D --defined-only "$lib" | grep -q "snappy" || fail "Snappy symbols are exported"
glibc=$(max_version GLIBC); glibcxx=$(max_version GLIBCXX)
at_most "$glibc" 2.14 || fail "needs GLIBC_$glibc, more than GLIBC_2.14"
at_most "$glibcxx" 3.4.22 || fail "needs GLIBCXX_$glibcxx, more than GLIBCXX_3.4.22"

cp "$lib" /out/LevelDBLinux.dll
echo "LevelDB $LEVELDB_TAG ($LEVELDB_COMMIT), Snappy $SNAPPY_TAG ($SNAPPY_COMMIT), static"
echo "compiler:    $(g++ --version | head -1)"
echo "snappy code: $snappy_symbols symbols, none exported"
echo "needs:       $(readelf -d "$lib" | sed -n "s/.*NEEDED.*\[\(.*\)\]/\1/p" | tr "\n" " ")"
echo "max GLIBC_$glibc, max GLIBCXX_$glibcxx"
sha256sum /out/LevelDBLinux.dll
'
