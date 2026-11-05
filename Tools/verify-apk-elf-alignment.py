"""Read-only ZIP/ELF inspection; does not establish device/store readiness."""
import argparse, hashlib, json, struct, zipfile
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument("apk", type=Path)
p.add_argument("--sha256", required=True)
p.add_argument("--output", type=Path, required=True)
a = p.parse_args()
before = hashlib.sha256(a.apk.read_bytes()).hexdigest()
if before != a.sha256.lower():
    raise SystemExit("APK hash mismatch")
libraries = []
with zipfile.ZipFile(a.apk) as z:
    for item in z.infolist():
        if not (item.filename.startswith("lib/") and item.filename.endswith(".so")):
            continue
        with z.open(item) as f:
            h = f.read(64)
            assert h[:6] == b"\x7fELF\x02\x01", item.filename
            phoff = struct.unpack_from("<Q", h, 32)[0]
            entsize, count = struct.unpack_from("<HH", h, 54)
            end = phoff + entsize * count
            assert 64 <= phoff <= end <= min(item.file_size, 1048576)
            assert entsize >= 56
            data = h + f.read(end - 64)
            assert len(data) == end
        loads = []
        for i in range(count):
            off = phoff + entsize * i
            if struct.unpack_from("<I", data, off)[0] != 1:
                continue
            fileoff, va = struct.unpack_from("<QQ", data, off + 8)
            align = struct.unpack_from("<Q", data, off + 48)[0]
            loads.append(dict(index=i, fileOffset=fileoff, virtualAddress=va,
                              alignment=align, passed=align >= 16384 and fileoff % 16384 == va % 16384))
        assert loads
        libraries.append(dict(entry=item.filename, bytes=item.file_size, loads=loads))
assert len(libraries) == 3 and all(x["entry"].startswith("lib/arm64-v8a/") for x in libraries)
assert before == hashlib.sha256(a.apk.read_bytes()).hexdigest()
passed = all(s["passed"] for lib in libraries for s in lib["loads"])
a.output.write_text(json.dumps(dict(apk=str(a.apk), sha256=before,
    nativeLibraries=libraries, elfLoad16KB=passed,
    zipAlignment="separate APK build check", physicalDeviceTested=False,
    storeReadinessClaim=False), indent=2), encoding="utf-8")
if not passed:
    raise SystemExit("ELF LOAD alignment failed")
print("PASS: three ARM64 ELF libraries, all PT_LOAD >=16KB and offset/address congruent")
