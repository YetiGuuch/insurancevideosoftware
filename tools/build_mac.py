"""Package the local review server as a double-clickable macOS application."""
import argparse
import plistlib
import stat
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
dest = parser.parse_args().output
prefix = 'Video Activity Review.app/Contents/'
manifest = {
    'CFBundleIdentifier': 'io.github.yetiguuch.videoactivityreview',
    'CFBundleName': 'Video Activity Review',
    'CFBundleExecutable': 'launch',
    'CFBundlePackageType': 'APPL',
    'CFBundleVersion': '1',
    'CFBundleShortVersionString': '1.0',
    'LSMinimumSystemVersion': '14.0',
}
files = {'Info.plist': plistlib.dumps(manifest), 'MacOS/launch': (root/'mac/launch').read_bytes()}
for name in ('Start Video Activity Review.command', 'server.mjs', 'engine.mjs', 'index.html'):
    files['Resources/' + name] = (root/name).read_bytes()
with zipfile.ZipFile(dest, 'w') as archive:
    for name, content in files.items():
        info = zipfile.ZipInfo(prefix + name)
        info.create_system = 3
        mode = 0o755 if name == 'MacOS/launch' else 0o644
        info.external_attr = (stat.S_IFREG | mode) << 16
        archive.writestr(info, content.replace(b'\r\n', b'\n'), zipfile.ZIP_DEFLATED)
with zipfile.ZipFile(dest) as archive:
    assert archive.testzip() is None
    exe = plistlib.loads(archive.read(prefix+'Info.plist'))['CFBundleExecutable']
    entry = archive.getinfo(prefix+'MacOS/'+exe)
    assert (entry.external_attr >> 16) & stat.S_IXUSR
    assert archive.read(entry).startswith(b'#!/bin/bash\n')
